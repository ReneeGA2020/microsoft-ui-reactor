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

    private static NodeDescriptor Desc(string window, string automationId) =>
        new(window, "C", automationId, null, "Button", 0, null);
}
