using System.Diagnostics;
using System.Text.Json;

namespace Microsoft.UI.Reactor.Hosting.Devtools;

/// <summary>
/// Registers the Phase 2 MCP tools on a <see cref="DevtoolsMcpServer"/>.
/// Each tool's handler is written to be side-effect-free on the HTTP thread
/// and marshal UI work through <see cref="DevtoolsMcpServer.OnDispatcher{T}"/>
/// where required. The <c>reactor.*</c> names in spec prose are unprefixed on
/// the wire — agents see the bare names.
/// </summary>
internal static class DevtoolsTools
{
    /// <summary>Supplies data the tools need from the host: components, switch callback, reload request.</summary>
    internal sealed class ToolHostContext
    {
        public required Func<IReadOnlyList<string>> GetComponents { get; init; }
        public required Func<string?> GetCurrentComponent { get; init; }
        public required Func<string, bool> SwitchComponent { get; init; }
        public required Action RequestReload { get; init; }
        public required WindowRegistry Windows { get; init; }

        /// <summary>
        /// Optional node registry — set by the host bring-up so
        /// <c>switchComponent</c> can invalidate stale ids for the active window
        /// after a successful swap. Absent in minimal selftest harnesses that
        /// don't rely on post-switch tree walks.
        /// </summary>
        public NodeRegistry? Nodes { get; init; }
    }

    public static void RegisterCore(DevtoolsMcpServer server, ToolHostContext ctx)
    {
        Register_Version(server);
        Register_Components(server, ctx);
        Register_SwitchComponent(server, ctx);
        Register_Reload(server, ctx);
        Register_Windows(server, ctx);
    }

    // -- version -----------------------------------------------------------------

    private static void Register_Version(DevtoolsMcpServer server)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "version",
                Description: "Returns the running app's build tag, pid, and MCP port. Zero side effects.",
                InputSchema: new { type = "object", properties = new { }, additionalProperties = false }),
            _ => new
            {
                build = server.BuildTag,
                pid = Process.GetCurrentProcess().Id,
                mcpPort = server.Port,
            });
    }

    // -- components --------------------------------------------------------------

    private static void Register_Components(DevtoolsMcpServer server, ToolHostContext ctx)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "components",
                Description: "Lists the Component class names in the loaded assembly.",
                InputSchema: new { type = "object", properties = new { }, additionalProperties = false }),
            _ => new
            {
                components = ctx.GetComponents().ToArray(),
                current = ctx.GetCurrentComponent(),
            });
    }

    // -- switchComponent ---------------------------------------------------------

    private static void Register_SwitchComponent(DevtoolsMcpServer server, ToolHostContext ctx)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "switchComponent",
                Description: "Switches the hosted root component. Invalidates every tree id in the target window.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string", description = "Component class name" },
                    },
                    required = new[] { "name" },
                    additionalProperties = false,
                }),
            @params =>
            {
                var name = ReadString(@params, "name")
                    ?? throw new McpToolException("switchComponent requires a 'name' argument.",
                        JsonRpcErrorCodes.InvalidParams);

                var ok = ctx.SwitchComponent(name);
                if (!ok)
                    throw new McpToolException($"Component '{name}' not found.",
                        JsonRpcErrorCodes.ToolExecution,
                        new { code = "unknown-component", available = ctx.GetComponents().ToArray() });

                // The old tree is gone; invalidate its ids so a subsequent
                // selector resolution against an old id returns `"gone"`
                // rather than silently reaching a stale element. Scoped to
                // every known active window — the swap replaces all roots.
                if (ctx.Nodes is { } nodes)
                {
                    foreach (var snap in ctx.Windows.Snapshot())
                        nodes.InvalidateWindow(snap.Id);
                }

                return new { ok = true, current = name };
            });
    }

    // -- reload ------------------------------------------------------------------

    private static void Register_Reload(DevtoolsMcpServer server, ToolHostContext ctx)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "reload",
                Description:
                    "Flushes the response, closes listeners, and exits with sentinel code 42 so the " +
                    "`mur devtools` supervisor rebuilds and relaunches. Old node ids do not carry over.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        component = new { type = "string", description = "Optional component to focus after restart" },
                    },
                    additionalProperties = false,
                }),
            @params =>
            {
                // Return the response immediately; the reload fires after the HTTP write flushes.
                var exitingBuild = server.BuildTag;
                ctx.RequestReload();
                return new { ok = true, exitingBuild };
            });
    }

    // -- windows -----------------------------------------------------------------

    private static void Register_Windows(DevtoolsMcpServer server, ToolHostContext ctx)
    {
        server.Tools.Register(
            new McpToolDescriptor(
                Name: "windows",
                Description: "Lists active windows with their ids, titles, bounds, and build tag.",
                InputSchema: new { type = "object", properties = new { }, additionalProperties = false }),
            _ => server.OnDispatcher(() => new
            {
                windows = ctx.Windows.Snapshot().Select(w => new
                {
                    id = w.Id,
                    title = w.Title,
                    hwnd = w.Hwnd,
                    bounds = new
                    {
                        x = w.Bounds.X,
                        y = w.Bounds.Y,
                        width = w.Bounds.Width,
                        height = w.Bounds.Height,
                    },
                    isMain = w.IsMain,
                    buildTag = w.BuildTag,
                }).ToArray(),
            }));
    }

    // -- helpers -----------------------------------------------------------------

    internal static string? ReadString(JsonElement? args, string name)
    {
        if (args is not { } a || a.ValueKind != JsonValueKind.Object) return null;
        if (!a.TryGetProperty(name, out var el)) return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString() : null;
    }

    internal static int? ReadInt(JsonElement? args, string name)
    {
        if (args is not { } a || a.ValueKind != JsonValueKind.Object) return null;
        if (!a.TryGetProperty(name, out var el)) return null;
        return el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var v) ? v : null;
    }

    internal static bool? ReadBool(JsonElement? args, string name)
    {
        if (args is not { } a || a.ValueKind != JsonValueKind.Object) return null;
        if (!a.TryGetProperty(name, out var el)) return null;
        return el.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }
}
