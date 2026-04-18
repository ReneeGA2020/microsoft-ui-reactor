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
    private readonly McpTransport _transport;
    private StdioMcpLoop? _stdioLoop;
    private bool _disposed;

    public int Port { get; }
    public McpToolRegistry Tools => _tools;
    public string BuildTag => _buildTag;
    public DispatcherQueue DispatcherQueue => _dispatcherQueue;
    public Window Window => _window;
    public McpTransport Transport => _transport;
    internal DevtoolsLogger? Logger => _logger;

    /// <summary>
    /// Routes banner/announcement lines. When stdio is the active MCP
    /// transport, stdout is reserved for JSON-RPC framing so everything else
    /// has to go to stderr — otherwise we'd corrupt the agent's message stream.
    /// </summary>
    private TextWriter BannerWriter =>
        _transport == McpTransport.Stdio ? Console.Error : Console.Out;

    public DevtoolsMcpServer(
        DispatcherQueue dispatcherQueue,
        Window window,
        int? preferredPort = null,
        DevtoolsLogger? logger = null,
        McpTransport transport = McpTransport.Http)
    {
        _dispatcherQueue = dispatcherQueue;
        _window = window;
        _tools = new McpToolRegistry();
        _buildTag = ResolveBuildTag();
        _logger = logger;
        _transport = transport;

        Port = preferredPort ?? FindFreePort();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
    }

    public void Start()
    {
        if (_transport == McpTransport.Http)
        {
            _listener.Start();
            _ = ListenAsync().ContinueWith(
                t => Console.Error.WriteLine($"[devtools:mcp] Listener loop failed: {t.Exception!.GetBaseException()}"),
                TaskContinuationOptions.OnlyOnFaulted);

            BannerWriter.WriteLine($"[devtools] MCP serving on http://127.0.0.1:{Port}/mcp");
            BannerWriter.WriteLine($"MCP_TRANSPORT=http");
            BannerWriter.WriteLine($"MCP_ENDPOINT=http://127.0.0.1:{Port}/mcp");
            BannerWriter.WriteLine($"MCP_PORT={Port}");
        }
        else // Stdio
        {
            _stdioLoop = new StdioMcpLoop(
                new McpDispatcher(_tools, _logger),
                Console.In,
                Console.Out);
            _stdioLoop.Start();

            // Stdio banner goes to stderr so stdout stays clean for framing.
            BannerWriter.WriteLine($"[devtools] MCP serving over stdio");
            BannerWriter.WriteLine($"MCP_TRANSPORT=stdio");
        }
        BannerWriter.Flush();
    }

    /// <summary>
    /// Emits the one-time <c>[devtools] ready</c> line after the first render
    /// completes. Callers invoke this from the reconciler's first-commit hook.
    /// </summary>
    public void AnnounceReady()
    {
        BannerWriter.WriteLine($"[devtools] ready (build {_buildTag})");
        BannerWriter.Flush();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _shutdownCts.Cancel();
        try { _listener.Stop(); } catch { }
        try { _listener.Close(); } catch { }
        try { _stdioLoop?.Dispose(); } catch { }
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
    /// Exceptions raised on the dispatcher surface with their original type
    /// (in particular <see cref="McpToolException"/>) — not wrapped in
    /// <see cref="AggregateException"/> — so structured errors round-trip.
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

        // Avoid Task.Wait — it re-wraps faults in AggregateException, hiding the
        // structured McpToolException payload. Poll IsCompleted with a timeout,
        // then unwrap manually so the original exception type propagates.
        using var completed = new ManualResetEventSlim(false);
        tcs.Task.ContinueWith(_ => completed.Set(), TaskContinuationOptions.ExecuteSynchronously);
        if (!completed.Wait(timeoutMs))
            throw new McpToolException("Dispatcher call timed out.");

        if (tcs.Task.IsFaulted)
        {
            var inner = tcs.Task.Exception!.InnerException ?? tcs.Task.Exception;
            global::System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(inner).Throw();
        }
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
