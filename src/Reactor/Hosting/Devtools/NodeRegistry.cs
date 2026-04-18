using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Reactor.Hosting.Devtools;

/// <summary>
/// Resolution outcome from <see cref="NodeRegistry.Resolve"/>.
/// </summary>
internal enum NodeLookupStatus { Found, Gone, Unknown }

internal readonly record struct NodeLookup(NodeLookupStatus Status, UIElement? Element);

/// <summary>
/// Maps live <see cref="UIElement"/> instances to stable <c>r:&lt;window&gt;/&lt;local&gt;</c>
/// ids for the MCP tool surface. Spec §13.
///
/// <para>
/// Thread model: reads are safe from any thread (internal state is behind a lock);
/// writes happen on the UI dispatcher during tree walks. Id construction is delegated
/// to <see cref="NodeIdBuilder"/>, which is pure. Entries hold the element via a
/// <see cref="WeakReference{T}"/>, so a GC-collected element returns
/// <see cref="NodeLookupStatus.Gone"/> — ids are never reused.
/// </para>
/// </summary>
internal sealed class NodeRegistry
{
    private readonly object _lock = new();
    // Forward map (id -> weak target) plus a reverse map (element instance -> id)
    // so re-walking the same live element returns the same id. The weak ref is
    // typed as object so test code can inject a sentinel; the public API still
    // only hands back UIElement instances.
    private readonly Dictionary<string, WeakReference<object>> _byId = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<UIElement, string> _byElement = new();
    private readonly HashSet<string> _tombstones = new(StringComparer.Ordinal);
    private readonly List<object> _testSentinels = new();

    /// <summary>Returns the stable id for the given descriptor + element, allocating on first sight.</summary>
    public string GetOrCreate(NodeDescriptor descriptor, UIElement element)
    {
        lock (_lock)
        {
            if (_byElement.TryGetValue(element, out var existing))
                return existing;

            var id = NodeIdBuilder.Build(descriptor);
            _byElement.Add(element, id);
            _byId[id] = new WeakReference<object>(element);
            return id;
        }
    }

    /// <summary>Resolves an id to a live element. Returns Gone if the target was collected.</summary>
    public NodeLookup Resolve(string id)
    {
        lock (_lock)
        {
            if (_tombstones.Contains(id))
                return new NodeLookup(NodeLookupStatus.Gone, null);

            if (!_byId.TryGetValue(id, out var weak))
                return new NodeLookup(NodeLookupStatus.Unknown, null);

            if (weak.TryGetTarget(out var target))
                return new NodeLookup(NodeLookupStatus.Found, target as UIElement);

            _byId.Remove(id);
            _tombstones.Add(id);
            return new NodeLookup(NodeLookupStatus.Gone, null);
        }
    }

    /// <summary>
    /// Invalidates every id tied to a given window scope. Called when a window closes
    /// or when the hosted component is switched — the old tree is gone, its ids with it.
    /// </summary>
    public void InvalidateWindow(string windowId)
    {
        var prefix = $"r:{windowId}/";
        lock (_lock)
        {
            foreach (var key in _byId.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                _byId.Remove(key);
                _tombstones.Add(key);
            }
        }
    }

    internal int CountForTests()
    {
        lock (_lock) return _byId.Count;
    }

    internal int TombstoneCountForTests()
    {
        lock (_lock) return _tombstones.Count;
    }

    // Test-only hook: lets unit tests populate the registry without needing a live
    // WinUI UIElement. Only exercises the bookkeeping paths (id construction,
    // window invalidation, tombstones, unknown ids). The live-element path goes
    // through GetOrCreate in production.
    internal string InjectForTests(NodeDescriptor descriptor)
    {
        lock (_lock)
        {
            var id = NodeIdBuilder.Build(descriptor);
            // Keep a strong reference to a sentinel so the weak target survives GC
            // until the test disposes the registry.
            var sentinel = new object();
            _testSentinels.Add(sentinel);
            _byId[id] = new WeakReference<object>(sentinel);
            return id;
        }
    }
}
