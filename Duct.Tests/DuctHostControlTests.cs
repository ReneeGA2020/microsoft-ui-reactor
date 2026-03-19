using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for Feature 2 (DuctHostControl) and Feature 3 (DuctPage adapter).
/// Type-level and API surface tests. Runtime lifecycle tests require a UI thread
/// and are covered by the DuctTestApp integration test.
/// </summary>
public class DuctHostControlTests
{
    // ── DuctHostControl API surface ────────────────────────────────

    [Fact]
    public void DuctHostControl_Is_ContentControl()
    {
        Assert.True(typeof(ContentControl).IsAssignableFrom(typeof(DuctHostControl)));
    }

    [Fact]
    public void DuctHostControl_Implements_IDisposable()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(DuctHostControl)));
    }

    [Fact]
    public void DuctHostControl_Has_ComponentType_Property()
    {
        var prop = typeof(DuctHostControl).GetProperty("ComponentType");
        Assert.NotNull(prop);
        Assert.Equal(typeof(Type), prop!.PropertyType);
    }

    [Fact]
    public void DuctHostControl_Has_Props_Property()
    {
        var prop = typeof(DuctHostControl).GetProperty("Props");
        Assert.NotNull(prop);
        Assert.Equal(typeof(object), prop!.PropertyType);
    }

    [Fact]
    public void DuctHostControl_Has_Reconciler_Property()
    {
        var prop = typeof(DuctHostControl).GetProperty("Reconciler");
        Assert.NotNull(prop);
        Assert.Equal(typeof(Reconciler), prop!.PropertyType);
    }

    [Fact]
    public void DuctHostControl_Has_Mount_Component_Method()
    {
        var method = typeof(DuctHostControl).GetMethod("Mount", [typeof(Component)]);
        Assert.NotNull(method);
    }

    [Fact]
    public void DuctHostControl_Has_Mount_Func_Method()
    {
        var method = typeof(DuctHostControl).GetMethod("Mount", [typeof(Func<RenderContext, Element>)]);
        Assert.NotNull(method);
    }

    // ── DuctPage API surface ───────────────────────────────────────

    [Fact]
    public void DuctPage_Is_Page()
    {
        Assert.True(typeof(Page).IsAssignableFrom(typeof(DuctPage<TestComponent>)));
    }

    [Fact]
    public void DuctPage_With_Props_Is_Page()
    {
        Assert.True(typeof(Page).IsAssignableFrom(typeof(DuctPage<PropsComponent, string>)));
    }

    [Fact]
    public void DuctPage_Generic_Constraint_Requires_Component()
    {
        // DuctPage<T> requires T : Component, new()
        // This compiles — proving the constraint is met
        var pageType = typeof(DuctPage<TestComponent>);
        Assert.NotNull(pageType);
    }

    [Fact]
    public void DuctPage_Props_Generic_Constraint_Requires_Component_TProps()
    {
        // DuctPage<T, TProps> requires T : Component<TProps>, new()
        var pageType = typeof(DuctPage<PropsComponent, string>);
        Assert.NotNull(pageType);
    }

    // ── Test stubs ─────────────────────────────────────────────────

    private class TestComponent : Component
    {
        public override Element Render() => new TextElement("test");
    }

    private class PropsComponent : Component<string>
    {
        public override Element Render() => new TextElement($"Prop: {Props}");
    }
}
