using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Reactor.Core;

namespace Microsoft.UI.Reactor.Hosting.Devtools;

/// <summary>
/// <c>reactor.fire</c> — escape hatch for cases where no UIA pattern reaches the
/// behavior (drag sequences, custom gestures, awaited async tests). Resolves a
/// live <see cref="Component"/> by class name, finds a method matching the event
/// name (case-insensitive), and invokes it on the UI dispatcher.
///
/// Scope: v1 walks the root component only. A future pass (§3.8 full wiring)
/// can expand to child components once the reconciler exposes a component
/// registry. Unknown component or unknown event produces a structured error;
/// invoked handlers log <c>via: "reactor-event-injection"</c> so the shortcut
/// is visible in traces.
/// </summary>
internal static class DevtoolsFireTool
{
    public static void Register(DevtoolsMcpServer server, Func<Component?> rootComponent)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "fire",
                Description: "Invokes a handler on a live component by name. Escape hatch — prefer UIA patterns first.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        component = new { type = "string" },
                        @event = new { type = "string" },
                        args = new { type = "array", description = "Optional positional args for the handler." },
                    },
                    required = new[] { "component", "event" },
                    additionalProperties = false,
                }),
            @params => server.OnDispatcher(() =>
            {
                var componentName = DevtoolsTools.ReadString(@params, "component")
                    ?? throw new McpToolException("Missing 'component'.", JsonRpcErrorCodes.InvalidParams);
                var eventName = DevtoolsTools.ReadString(@params, "event")
                    ?? throw new McpToolException("Missing 'event'.", JsonRpcErrorCodes.InvalidParams);

                var root = rootComponent();
                if (root is null)
                    throw new McpToolException(
                        "No root component is mounted.",
                        JsonRpcErrorCodes.ToolExecution,
                        new { code = "not-ready" });

                var instance = FindComponent(root, componentName)
                    ?? throw new McpToolException(
                        $"Component '{componentName}' is not the root (child-component search not implemented in v1).",
                        JsonRpcErrorCodes.ToolExecution,
                        new { code = "unknown-component", available = new[] { root.GetType().Name } });

                var handler = FindHandler(instance, eventName)
                    ?? throw new McpToolException(
                        $"Component '{componentName}' has no handler named '{eventName}'.",
                        JsonRpcErrorCodes.ToolExecution,
                        new { code = "unknown-event", component = componentName, @event = eventName });

                var argsArray = ExtractArgs(@params);
                try
                {
                    handler.Invoke(instance, argsArray);
                }
                catch (TargetInvocationException ex)
                {
                    throw new McpToolException(
                        $"Handler '{eventName}' threw: {ex.InnerException?.Message ?? ex.Message}",
                        JsonRpcErrorCodes.ToolExecution,
                        new { code = "handler-threw" });
                }

                return new { ok = true, via = "reactor-event-injection" };
            }));
    }

    internal static Component? FindComponent(Component root, string name)
    {
        // v1 only resolves the root. Keeps the tool well-defined without
        // requiring reconciler-level component registration.
        return string.Equals(root.GetType().Name, name, StringComparison.OrdinalIgnoreCase)
            ? root
            : null;
    }

    internal static MethodInfo? FindHandler(Component instance, string eventName)
    {
        var type = instance.GetType();
        // Prefer an exact-cased public or internal instance method; fall back to
        // case-insensitive. Only parameter shapes compatible with our passed args
        // will succeed at invoke time — we don't filter by signature here.
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        return type.GetMethods(flags)
            .FirstOrDefault(m => string.Equals(m.Name, eventName, StringComparison.OrdinalIgnoreCase));
    }

    internal static object?[] ExtractArgs(JsonElement? @params)
    {
        if (@params is not { } p || p.ValueKind != JsonValueKind.Object) return Array.Empty<object?>();
        if (!p.TryGetProperty("args", out var argsEl) || argsEl.ValueKind != JsonValueKind.Array) return Array.Empty<object?>();

        var list = new List<object?>(argsEl.GetArrayLength());
        foreach (var item in argsEl.EnumerateArray())
        {
            list.Add(JsonElementToClr(item));
        }
        return list.ToArray();
    }

    private static object? JsonElementToClr(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => el.GetRawText(),
    };
}
