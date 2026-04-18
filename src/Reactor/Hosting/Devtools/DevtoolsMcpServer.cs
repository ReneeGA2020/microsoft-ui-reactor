using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Reactor.Hosting.Devtools;

/// <summary>
/// MCP server exposed on a loopback HTTP endpoint. Accepts JSON-RPC 2.0 POSTs at
/// <c>/mcp</c>. One method per MCP tool; <c>tools/list</c> returns the inventory
/// and <c>tools/call</c> dispatches by name. Spec §6, §17 Phase 2.
/// </summary>
internal sealed class DevtoolsMcpServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Window _window;
    private readonly McpToolRegistry _tools;
    private readonly string _buildTag;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly DevtoolsLogger? _logger;
    private bool _disposed;

    public int Port { get; }
    public McpToolRegistry Tools => _tools;
    public string BuildTag => _buildTag;
    public DispatcherQueue DispatcherQueue => _dispatcherQueue;
    public Window Window => _window;
    internal DevtoolsLogger? Logger => _logger;

    public DevtoolsMcpServer(
        DispatcherQueue dispatcherQueue,
        Window window,
        int? preferredPort = null,
        DevtoolsLogger? logger = null)
    {
        _dispatcherQueue = dispatcherQueue;
        _window = window;
        _tools = new McpToolRegistry();
        _buildTag = ResolveBuildTag();
        _logger = logger;

        Port = preferredPort ?? FindFreePort();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
    }

    public void Start()
    {
        _listener.Start();
        _ = ListenAsync().ContinueWith(
            t => Console.Error.WriteLine($"[devtools:mcp] Listener loop failed: {t.Exception!.GetBaseException()}"),
            TaskContinuationOptions.OnlyOnFaulted);

        Console.WriteLine($"[devtools] MCP serving on http://127.0.0.1:{Port}/mcp");
        Console.WriteLine($"MCP_ENDPOINT=http://127.0.0.1:{Port}/mcp");
        Console.WriteLine($"MCP_PORT={Port}");
        Console.Out.Flush();
    }

    /// <summary>
    /// Emits the one-time <c>[devtools] ready</c> line after the first render
    /// completes. Callers invoke this from the reconciler's first-commit hook.
    /// </summary>
    public void AnnounceReady()
    {
        Console.WriteLine($"[devtools] ready (build {_buildTag})");
        Console.Out.Flush();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shutdownCts.Cancel();
        try { _listener.Stop(); } catch { }
        try { _listener.Close(); } catch { }
        try { _logger?.Dispose(); } catch { }
    }

    // -- HTTP Loop ---------------------------------------------------------------

    private async Task ListenAsync()
    {
        while (!_disposed && _listener.IsListening)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync();
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) { break; }

            _ = Task.Run(() => HandleRequest(ctx));
        }
    }

    private void HandleRequest(HttpListenerContext ctx)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";
        var response = ctx.Response;

        if (ctx.Request.HttpMethod == "OPTIONS")
        {
            response.Headers.Add("Access-Control-Allow-Origin", "http://127.0.0.1");
            response.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            response.StatusCode = 204;
            response.Close();
            return;
        }

        if (!string.Equals(path, "/mcp", StringComparison.Ordinal))
        {
            response.StatusCode = 404;
            response.Close();
            return;
        }

        if (ctx.Request.HttpMethod != "POST")
        {
            response.StatusCode = 405;
            response.Close();
            return;
        }

        string body;
        using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
            body = reader.ReadToEnd();

        var responsePayload = DispatchRpc(body);
        var json = JsonSerializer.Serialize(responsePayload, JsonOpts);
        var bytes = Encoding.UTF8.GetBytes(json);

        response.ContentType = "application/json";
        response.ContentLength64 = bytes.Length;
        response.StatusCode = 200;
        try
        {
            response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        catch { }
        finally
        {
            try { response.Close(); } catch { }
        }
    }

    // -- Dispatch ----------------------------------------------------------------

    internal JsonRpcResponse DispatchRpc(string body) => new McpDispatcher(_tools, _logger).Dispatch(body);

    // -- Dispatcher marshalling --------------------------------------------------

    /// <summary>
    /// Runs <paramref name="action"/> on the UI dispatcher and blocks the caller
    /// until it completes. Tool handlers use this to touch WinUI state safely.
    /// Timeout defaults to 5s so a stuck UI thread doesn't hang the HTTP worker.
    /// </summary>
    public T OnDispatcher<T>(Func<T> action, int timeoutMs = 5000)
    {
        if (_dispatcherQueue.HasThreadAccess)
            return action();

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            try { tcs.TrySetResult(action()); }
            catch (Exception ex) { tcs.TrySetException(ex); }
        }))
        {
            throw new McpToolException("Could not enqueue work onto the UI dispatcher.");
        }

        if (!tcs.Task.Wait(timeoutMs))
            throw new McpToolException("Dispatcher call timed out.");
        return tcs.Task.Result;
    }

    public void OnDispatcher(Action action, int timeoutMs = 5000) =>
        OnDispatcher<object?>(() => { action(); return null; }, timeoutMs);

    // -- Helpers -----------------------------------------------------------------

    internal static JsonSerializerOptions JsonOpts { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = global::System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>
    /// Derives a stable build tag from the entry assembly's compile timestamp (or
    /// informational version, if richer). Agents use this to confirm a reload took.
    /// </summary>
    private static string ResolveBuildTag()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(info)) return info!;

        try
        {
            var path = asm.Location;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                return File.GetLastWriteTimeUtc(path).ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
        catch { }

        return "unknown";
    }
}
