using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for Feature 2 (DuctHostControl) and Feature 3 (DuctPageHelper).
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
    public void DuctHostControl_Has_ComponentFactory_Property()
    {
        var prop = typeof(DuctHostControl).GetProperty("ComponentFactory");
        Assert.NotNull(prop);
        Assert.Equal(typeof(Func<Component>), prop!.PropertyType);
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

    // ── DuctPageHelper API surface ───────────────────────────────

    [Fact]
    public void DuctPageHelper_Has_Mount_Method()
    {
        var methods = typeof(DuctPageHelper).GetMethods()
            .Where(m => m.Name == "Mount")
            .ToArray();
        Assert.Equal(2, methods.Length); // Mount<T> and Mount<T, TProps>
    }

    [Fact]
    public void DuctPageHelper_Has_Unmount_Method()
    {
        var method = typeof(DuctPageHelper).GetMethod("Unmount");
        Assert.NotNull(method);
    }

    [Fact]
    public void DuctPageHelper_Mount_Returns_DuctHostControl()
    {
        var method = typeof(DuctPageHelper).GetMethods()
            .First(m => m.Name == "Mount" && m.GetGenericArguments().Length == 1);
        Assert.Equal(typeof(DuctHostControl), method.ReturnType);
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
