using Microsoft.UI.Reactor.Hosting.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

// The pure bookkeeping paths on NodeRegistry — id allocation, window-scope
// invalidation, tombstoning — are exercised here without a live WinUI window.
// The live `GetOrCreate(element)` / WeakReference integration is covered by
// the self-host MCP fixture tests in Phase 2.17.
public class NodeRegistryTests
{
    [Fact]
    public void UnknownId_ResolvesAsUnknown()
    {
        var reg = new NodeRegistry();
        var r = reg.Resolve("r:main/does-not-exist");
        Assert.Equal(NodeLookupStatus.Unknown, r.Status);
        Assert.Null(r.Element);
    }

    [Fact]
    public void InvalidateWindow_TombstonesEveryIdInScope()
    {
        var reg = new NodeRegistry();
        var a = reg.InjectForTests(Desc("main", "btn-a"));
        var b = reg.InjectForTests(Desc("main", "btn-b"));
        var c = reg.InjectForTests(Desc("aux", "btn-c"));

        Assert.Equal(3, reg.CountForTests());

        reg.InvalidateWindow("main");

        // Both main-scoped ids now return Gone; the aux-scoped id is untouched.
        Assert.Equal(NodeLookupStatus.Gone, reg.Resolve(a).Status);
        Assert.Equal(NodeLookupStatus.Gone, reg.Resolve(b).Status);
        Assert.NotEqual(NodeLookupStatus.Gone, reg.Resolve(c).Status);
    }

    [Fact]
    public void InvalidateWindow_IdsAreNeverReusedAfterTombstone()
    {
        var reg = new NodeRegistry();
        var id = reg.InjectForTests(Desc("main", "btn"));

        reg.InvalidateWindow("main");

        Assert.Equal(NodeLookupStatus.Gone, reg.Resolve(id).Status);

        // Even if a new descriptor with the same identity is injected, the
        // prior tombstone should keep the id marked Gone rather than silently
        // reviving it — an agent holding the old id must learn it's stale.
        reg.InjectForTests(Desc("main", "btn"));
        Assert.Equal(NodeLookupStatus.Gone, reg.Resolve(id).Status);
    }

    [Fact]
    public void IdConstruction_RoundTripsViaBuilder()
    {
        var reg = new NodeRegistry();
        var id = reg.InjectForTests(new NodeDescriptor(
            WindowId: "main",
            ComponentName: "CounterDemo",
            AutomationId: "btn-inc",
            ReactorSource: null,
            TypeName: "Button",
            SiblingIndex: 0,
            StableAncestor: null));

        Assert.Equal("r:main/CounterDemo.btn-inc", id);
    }

    // §2.16 open item: two walks of the same live tree return the same ids for
    // the same elements. Prod call path is `GetOrCreate(NodeDescriptor, UIElement)`;
    // the reverse-map ConditionalWeakTable keyed on the element instance gives
    // this for free. We exercise the same internal path with a sentinel to
    // avoid spinning up a WinUI window here.
    [Fact]
    public void GetOrCreate_SameTarget_ReturnsSameId()
    {
        var reg = new NodeRegistry();
        var target = new object();

        var first = reg.GetOrCreateForTests(Desc("main", "btn-inc"), target);
        var second = reg.GetOrCreateForTests(Desc("main", "btn-inc"), target);

        Assert.Equal(first, second);
        // Only one forward-map entry even across two walks.
        Assert.Equal(1, reg.CountForTests());
    }

    [Fact]
    public void GetOrCreate_DifferentDescriptors_GetDistinctIds()
    {
        var reg = new NodeRegistry();
        var a = new object();
        var b = new object();

        // Distinct elements with distinct descriptors get distinct ids.
        // (Duplicate AutomationId within a window is an authoring error —
        // `NodeIdBuilder` is deterministic on the descriptor, so the
        // registry intentionally does not disambiguate that case.)
        var idA = reg.GetOrCreateForTests(Desc("main", "btn-inc"), a);
        var idB = reg.GetOrCreateForTests(Desc("main", "btn-dec"), b);

        Assert.NotEqual(idA, idB);
        Assert.Equal(2, reg.CountForTests());
    }

    [Fact]
    public void GetOrCreate_ResolvesBackToSameTarget()
    {
        var reg = new NodeRegistry();
        var target = new object();
        var id = reg.GetOrCreateForTests(Desc("main", "btn"), target);

        // Can't round-trip through Resolve() here since it narrows to UIElement,
        // but we can confirm the second GetOrCreate call hits the reverse map
        // rather than allocating a new id — that's the property we care about.
        var again = reg.GetOrCreateForTests(Desc("main", "btn"), target);
        Assert.Equal(id, again);
    }

    private static NodeDescriptor Desc(string window, string automationId) =>
        new(window, "C", automationId, null, "Button", 0, null);
}
