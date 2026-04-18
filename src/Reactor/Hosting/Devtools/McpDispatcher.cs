using System.Text.Json;

namespace Microsoft.UI.Reactor.Hosting.Devtools;

/// <summary>
/// Pure JSON-RPC dispatch for the MCP tool registry. Takes a request envelope,
/// returns a response envelope. No transport, no HTTP, no dispatcher hops.
/// <see cref="DevtoolsMcpServer"/> delegates to this on every request; tests
/// construct it directly with a registry of test-registered tools.
/// </summary>
internal sealed class McpDispatcher
{
    private readonly McpToolRegistry _tools;
    private readonly DevtoolsLogger? _logger;

    public McpDispatcher(McpToolRegistry tools) : this(tools, null) { }

    public McpDispatcher(McpToolRegistry tools, DevtoolsLogger? logger)
    {
        _tools = tools;
        _logger = logger;
    }

    public JsonRpcResponse Dispatch(string body)
    {
        JsonRpcRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<JsonRpcRequest>(body, DevtoolsMcpServer.JsonOpts);
        }
        catch (JsonException ex)
        {
            return new JsonRpcResponse
            {
                Error = new JsonRpcError { Code = JsonRpcErrorCodes.ParseError, Message = $"Parse error: {ex.Message}" },
            };
        }

        if (request is null || string.IsNullOrEmpty(request.Method) || request.JsonRpc != "2.0")
        {
            return new JsonRpcResponse
            {
                Id = request?.Id,
                Error = new JsonRpcError { Code = JsonRpcErrorCodes.InvalidRequest, Message = "Invalid JSON-RPC request." },
            };
        }

        try
        {
            object? result = request.Method switch
            {
                "tools/list" => new
                {
                    tools = _tools.List().Select(t => new
                    {
                        name = t.Name,
                        description = t.Description,
                        inputSchema = t.InputSchema,
                    }).ToArray(),
                },
                "tools/call" => HandleCall(request.Params),
                _ => HandleDirect(request.Method, request.Params),
            };
            return new JsonRpcResponse { Id = request.Id, Result = result };
        }
        catch (McpToolException ex)
        {
            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = ex.Code, Message = ex.Message, Data = ex.Payload },
            };
        }
        catch (Exception ex)
        {
            return new JsonRpcResponse
            {
                Id = request.Id,
                Error = new JsonRpcError { Code = JsonRpcErrorCodes.InternalError, Message = ex.Message },
            };
        }
    }

    private object? HandleCall(JsonElement? @params)
    {
        if (@params is not { } p || p.ValueKind != JsonValueKind.Object)
            throw new McpToolException("tools/call params must be an object with { name, arguments? }.",
                JsonRpcErrorCodes.InvalidParams);
        if (!p.TryGetProperty("name", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
            throw new McpToolException("tools/call requires a string 'name' field.", JsonRpcErrorCodes.InvalidParams);

        var name = nameEl.GetString()!;
        JsonElement? args = p.TryGetProperty("arguments", out var argsEl) ? argsEl : null;
        return Invoke(name, args);
    }

    private object? HandleDirect(string method, JsonElement? @params)
    {
        if (_tools.TryGet(method, out _)) return Invoke(method, @params);
        throw new McpToolException($"Method not found: '{method}'", JsonRpcErrorCodes.MethodNotFound);
    }

    private object? Invoke(string name, JsonElement? @params)
    {
        if (!_tools.TryGet(name, out var handler))
            throw new McpToolException($"Tool not found: '{name}'", JsonRpcErrorCodes.MethodNotFound);

        if (_logger is null) return handler(@params);

        var selector = TryReadSelector(@params);
        var sw = global::System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = handler(@params);
            sw.Stop();
            _logger.LogCall(name, selector, sw.ElapsedMilliseconds, success: true, resultCode: 0);
            return result;
        }
        catch (McpToolException mte)
        {
            sw.Stop();
            _logger.LogCall(name, selector, sw.ElapsedMilliseconds, success: false, resultCode: mte.Code);
            throw;
        }
        catch (Exception)
        {
            sw.Stop();
            _logger.LogCall(name, selector, sw.ElapsedMilliseconds,
                success: false, resultCode: JsonRpcErrorCodes.InternalError);
            throw;
        }
    }

    private static string? TryReadSelector(JsonElement? @params)
    {
        if (@params is not { } p || p.ValueKind != JsonValueKind.Object) return null;
        if (p.TryGetProperty("selector", out var sel) && sel.ValueKind == JsonValueKind.String)
            return sel.GetString();
        return null;
    }
}
