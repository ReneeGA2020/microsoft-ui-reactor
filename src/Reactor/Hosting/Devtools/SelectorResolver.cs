using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Reactor.Hosting.Devtools;

/// <summary>
/// Resolves a parsed <see cref="SelectorIr"/> to a single live <see cref="UIElement"/>
/// inside a window. Must be called on the UI dispatcher.
///
/// Ambiguity: the first matching type path wins; for AutomationName and
/// AutomationId matches, more than one candidate is an <c>ambiguous-selector</c>
/// error carrying the full candidate id list.
/// </summary>
internal sealed class SelectorResolver
{
    private readonly NodeRegistry _nodeRegistry;
    private readonly WindowRegistry _windowRegistry;

    public SelectorResolver(NodeRegistry nodeRegistry, WindowRegistry windowRegistry)
    {
        _nodeRegistry = nodeRegistry;
        _windowRegistry = windowRegistry;
    }

    /// <summary>
    /// Resolves a selector in the context of a given window (or the default one
    /// if only a single window is active). Throws <see cref="McpToolException"/>
    /// with a structured error when resolution fails.
    /// </summary>
    public UIElement Resolve(string rawSelector, string? explicitWindowId = null)
    {
        var ir = SelectorParser.Parse(rawSelector);

        if (ir.Kind == SelectorKind.NodeId)
        {
            // Cross-window check: if the caller pinned `window` explicitly, a
            // node id from a different window is a mismatch error, not a
            // silent resolve. Failing loudly forces the agent to fix the call.
            if (!string.IsNullOrEmpty(explicitWindowId))
            {
                var idWindow = ExtractWindowFromNodeId(ir.NodeId!);
                if (idWindow is not null &&
                    !string.Equals(idWindow, explicitWindowId, StringComparison.Ordinal))
                {
                    throw new McpToolException(
                        $"Node id '{ir.NodeId}' belongs to window '{idWindow}', but 'window' was set to '{explicitWindowId}'.",
                        JsonRpcErrorCodes.InvalidParams,
                        new { code = "window-mismatch", idWindow, requested = explicitWindowId });
                }
            }

            var lookup = _nodeRegistry.Resolve(ir.NodeId!);
            return lookup.Status switch
            {
                NodeLookupStatus.Found => lookup.Element!,
                NodeLookupStatus.Gone => throw new McpToolException(
                    $"Node '{ir.NodeId}' is gone.", JsonRpcErrorCodes.ToolExecution,
                    new { code = "gone", id = ir.NodeId }),
                _ => throw new McpToolException(
                    $"Node '{ir.NodeId}' is unknown.", JsonRpcErrorCodes.ToolExecution,
                    new { code = "unknown-selector", id = ir.NodeId }),
            };
        }

        var window = ResolveWindow(explicitWindowId);
        var root = window.Content ?? throw new McpToolException(
            "Window has no content yet.", JsonRpcErrorCodes.ToolExecution,
            new { code = "not-ready" });

        return ir.Kind switch
        {
            SelectorKind.AutomationId => ResolveByPredicate(root, el =>
                string.Equals(AutomationProperties.GetAutomationId(el), ir.AutomationId, StringComparison.Ordinal)),
            SelectorKind.AutomationName => ResolveByPredicate(root, el =>
                string.Equals(AutomationProperties.GetName(el), ir.AutomationName, StringComparison.Ordinal)),
            SelectorKind.TypePath => ResolveTypePath(root, ir.TypePath!),
            SelectorKind.ReactorSource => throw new McpToolException(
                "Reactor-source selectors require the source map (Phase 3).",
                JsonRpcErrorCodes.ToolExecution,
                new { code = "not-implemented" }),
            _ => throw new McpToolException("Unsupported selector.", JsonRpcErrorCodes.InvalidParams),
        };
    }

    private Window ResolveWindow(string? explicitWindowId)
    {
        if (!string.IsNullOrEmpty(explicitWindowId))
        {
            var w = _windowRegistry.Resolve(explicitWindowId!);
            return w ?? throw new McpToolException(
                $"Window '{explicitWindowId}' not found.",
                JsonRpcErrorCodes.ToolExecution,
                new { code = "unknown-window" });
        }

        var @default = _windowRegistry.TryDefault(out var activeIds);
        if (@default is not null) return @default;

        throw new McpToolException(
            "Multiple windows are active — pass 'window'.",
            JsonRpcErrorCodes.InvalidParams,
            new { code = "window-required", activeIds });
    }

    private UIElement ResolveByPredicate(UIElement root, Func<UIElement, bool> predicate)
    {
        var matches = new List<UIElement>();
        Collect(root, predicate, matches);

        if (matches.Count == 0)
            throw new McpToolException(
                "Selector matched no elements.", JsonRpcErrorCodes.ToolExecution,
                new { code = "unknown-selector" });

        if (matches.Count > 1)
        {
            var candidateIds = matches.Take(10).Select(el => DescribeCandidate(el)).ToArray();
            throw new McpToolException(
                "Selector matched multiple elements.",
                JsonRpcErrorCodes.ToolExecution,
                new { code = "ambiguous-selector", candidates = candidateIds });
        }

        return matches[0];
    }

    private UIElement ResolveTypePath(UIElement root, IReadOnlyList<TypeStep> steps)
    {
        // `A > B[2]` — for each step, restrict candidates to those matching the
        // previous result's descendants. Index disambiguates when > 1 match.
        IReadOnlyList<UIElement> frontier = new[] { root };
        for (int s = 0; s < steps.Count; s++)
        {
            var step = steps[s];
            var next = new List<UIElement>();
            foreach (var parent in frontier)
            {
                Collect(parent, el => el.GetType().Name == step.TypeName, next, skipRoot: s == 0);
            }
            if (next.Count == 0)
                throw new McpToolException(
                    $"Type '{step.TypeName}' matched no elements.",
                    JsonRpcErrorCodes.ToolExecution,
                    new { code = "unknown-selector" });

            if (step.Index is int idx)
            {
                if (idx < 0 || idx >= next.Count)
                    throw new McpToolException(
                        $"Type '{step.TypeName}' has {next.Count} matches; index {idx} out of range.",
                        JsonRpcErrorCodes.ToolExecution,
                        new { code = "index-out-of-range", count = next.Count });

                frontier = new[] { next[idx] };
            }
            else
            {
                frontier = next;
            }
        }

        if (frontier.Count == 1) return frontier[0];

        // Last step yielded multiple candidates without an index — ambiguous.
        var candidateIds = frontier.Take(10).Select(DescribeCandidate).ToArray();
        throw new McpToolException(
            "Selector matched multiple elements.",
            JsonRpcErrorCodes.ToolExecution,
            new { code = "ambiguous-selector", candidates = candidateIds });
    }

    private static void Collect(
        UIElement element,
        Func<UIElement, bool> predicate,
        List<UIElement> sink,
        bool skipRoot = false)
    {
        if (!skipRoot && predicate(element)) sink.Add(element);
        int childCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childCount; i++)
        {
            if (VisualTreeHelper.GetChild(element, i) is UIElement child)
                Collect(child, predicate, sink, skipRoot: false);
        }
    }

    /// <summary>
    /// Pulls the window id out of an <c>r:&lt;window&gt;/&lt;local&gt;</c> id.
    /// Returns null when the id is malformed — callers treat that as "no
    /// window claim" and fall through to the registry lookup.
    /// </summary>
    internal static string? ExtractWindowFromNodeId(string nodeId)
    {
        if (!nodeId.StartsWith("r:", StringComparison.Ordinal)) return null;
        int slash = nodeId.IndexOf('/', 2);
        if (slash < 0) return null;
        var window = nodeId.Substring(2, slash - 2);
        return string.IsNullOrEmpty(window) ? null : window;
    }

    private static string DescribeCandidate(UIElement el)
    {
        var aid = AutomationProperties.GetAutomationId(el);
        if (!string.IsNullOrEmpty(aid)) return $"#{aid}";
        var name = AutomationProperties.GetName(el);
        if (!string.IsNullOrEmpty(name)) return $"[name='{name}']";
        return el.GetType().Name;
    }
}
