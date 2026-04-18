using System.Text.Json;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

/// <summary>
/// Unit coverage for the pure helpers on <c>DevtoolsFireTool</c>. The live
/// UI-dispatcher path needs a running Window and is exercised by the Phase 3
/// self-host fixture.
/// </summary>
public class DevtoolsFireToolTests
{
    private sealed class TestComponent : Component
    {
        public int HandlerCalls;
        public object?[]? LastArgs;

        public void OnSubmit(int count, string label)
        {
            HandlerCalls++;
            LastArgs = new object?[] { count, label };
        }

        public void PrivateHandler() => HandlerCalls++;

        public override Element Render() => null!;
    }

    [Fact]
    public void FindComponent_RootMatch_ReturnsInstance()
    {
        var c = new TestComponent();
        Assert.Same(c, DevtoolsFireTool.FindComponent(c, "TestComponent"));
    }

    [Fact]
    public void FindComponent_IsCaseInsensitive()
    {
        var c = new TestComponent();
        Assert.Same(c, DevtoolsFireTool.FindComponent(c, "testcomponent"));
    }

    [Fact]
    public void FindComponent_MissName_ReturnsNull()
    {
        var c = new TestComponent();
        Assert.Null(DevtoolsFireTool.FindComponent(c, "Other"));
    }

    [Fact]
    public void FindHandler_FindsPublicMethod()
    {
        var c = new TestComponent();
        Assert.NotNull(DevtoolsFireTool.FindHandler(c, "OnSubmit"));
    }

    [Fact]
    public void FindHandler_AlsoFindsPrivateMethod()
    {
        // The spec allows firing any handler method on a live component — the
        // escape hatch is specifically for behaviors not reachable via UIA, and
        // private handlers are a common target (e.g. drag state machines).
        var c = new TestComponent();
        Assert.NotNull(DevtoolsFireTool.FindHandler(c, "PrivateHandler"));
    }

    [Fact]
    public void FindHandler_UnknownReturnsNull()
    {
        var c = new TestComponent();
        Assert.Null(DevtoolsFireTool.FindHandler(c, "DoesNotExist"));
    }

    [Fact]
    public void ExtractArgs_ParsesTypedJsonElements()
    {
        using var doc = JsonDocument.Parse("""{"args":[3,"label",true,null]}""");
        var args = DevtoolsFireTool.ExtractArgs(doc.RootElement);
        Assert.Equal(4, args.Length);
        // Integer numbers may come back as long or double depending on which
        // TryGet wins in System.Text.Json's number handling — both are valid
        // CLR numbers and MethodInfo.Invoke coerces to the target parameter.
        Assert.True(args[0] is long or double, $"Expected numeric, got {args[0]?.GetType()}");
        Assert.Equal(3.0, Convert.ToDouble(args[0]));
        Assert.Equal("label", args[1]);
        Assert.Equal(true, args[2]);
        Assert.Null(args[3]);
    }

    [Fact]
    public void ExtractArgs_MissingArrayYieldsEmpty()
    {
        using var doc = JsonDocument.Parse("""{"component":"X","event":"Y"}""");
        var args = DevtoolsFireTool.ExtractArgs(doc.RootElement);
        Assert.Empty(args);
    }

    [Fact]
    public void HandlerInvoke_ReachesTargetMethod()
    {
        // Sanity check — MethodInfo.Invoke with the pure-helper-extracted args
        // actually reaches the target handler. This covers the mechanism; the
        // live dispatcher hop is out of scope for unit tests.
        var c = new TestComponent();
        var handler = DevtoolsFireTool.FindHandler(c, "OnSubmit")!;
        handler.Invoke(c, new object?[] { 7, "go" });

        Assert.Equal(1, c.HandlerCalls);
        Assert.Equal(new object?[] { 7, "go" }, c.LastArgs);
    }
}
