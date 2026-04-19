using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Tests for Feature 2 (ReactorHostControl) and Feature 3 (PageHelper).
/// Type-level and API surface tests. Runtime lifecycle tests require a UI thread
/// and are covered by the Reactor.TestApp integration test.
/// </summary>
public class HostControlTests
{
    // ── ReactorHostControl API surface ────────────────────────────────

    [Fact]
    public void HostControl_Is_ContentControl()
    {
        Assert.True(typeof(ContentControl).IsAssignableFrom(typeof(ReactorHostControl)));
    }

    [Fact]
    public void HostControl_Implements_IDisposable()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(ReactorHostControl)));
    }

    [Fact]
    public void HostControl_Has_ComponentFactory_Property()
    {
        var prop = typeof(ReactorHostControl).GetProperty("ComponentFactory");
        Assert.NotNull(prop);
        Assert.Equal(typeof(Func<Component>), prop!.PropertyType);
    }

    [Fact]
    public void HostControl_Has_Props_Property()
    {
        var prop = typeof(ReactorHostControl).GetProperty("Props");
        Assert.NotNull(prop);
        Assert.Equal(typeof(object), prop!.PropertyType);
    }

    [Fact]
    public void HostControl_Has_Reconciler_Property()
    {
        var prop = typeof(ReactorHostControl).GetProperty("Reconciler");
        Assert.NotNull(prop);
        Assert.Equal(typeof(Reconciler), prop!.PropertyType);
    }

    [Fact]
    public void HostControl_Has_Mount_Component_Method()
    {
        var method = typeof(ReactorHostControl).GetMethod("Mount", [typeof(Component)]);
        Assert.NotNull(method);
    }

    [Fact]
    public void HostControl_Has_Mount_Func_Method()
    {
        var method = typeof(ReactorHostControl).GetMethod("Mount", [typeof(Func<RenderContext, Element>)]);
        Assert.NotNull(method);
    }

    // ── PageHelper API surface ───────────────────────────────

    [Fact]
    public void PageHelper_Has_Mount_Method()
    {
        var methods = typeof(PageHelper).GetMethods()
            .Where(m => m.Name == "Mount")
            .ToArray();
        Assert.Equal(2, methods.Length); // Mount<T> and Mount<T, TProps>
    }

    [Fact]
    public void PageHelper_Has_Unmount_Method()
    {
        var method = typeof(PageHelper).GetMethod("Unmount");
        Assert.NotNull(method);
    }

    [Fact]
    public void PageHelper_Mount_Returns_ReactorHostControl()
    {
        var method = typeof(PageHelper).GetMethods()
            .First(m => m.Name == "Mount" && m.GetGenericArguments().Length == 1);
        Assert.Equal(typeof(ReactorHostControl), method.ReturnType);
    }

    // ── Test stubs ─────────────────────────────────────────────────

    private class TestComponent : Component
    {
        public override Element Render() => new TextBlockElement("test");
    }

    private class PropsComponent : Component<string>
    {
        public override Element Render() => new TextBlockElement($"Prop: {Props}");
    }
}
