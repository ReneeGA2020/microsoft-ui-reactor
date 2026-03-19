using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for Feature 8: Animation and Transition Support.
/// Validates WithTransitions() extension methods on container elements.
/// Pure logic tests — verify the setter is added without creating WinUI controls.
/// </summary>
public class TransitionExtensionTests
{
    // ── StackElement.WithTransitions ──────────────────────────────

    [Fact]
    public void StackElement_WithTransitions_Returns_StackElement()
    {
        var stack = new StackElement(Orientation.Vertical, []);
        // Use a dummy transition-like approach — WithTransitions takes params Transition[]
        // Since Transition requires XAML thread, test that the method exists and returns StackElement
        var method = typeof(ElementExtensions).GetMethod(
            "WithTransitions",
            [typeof(StackElement), typeof(Microsoft.UI.Xaml.Media.Animation.Transition[])]);
        Assert.NotNull(method);
        Assert.Equal(typeof(StackElement), method!.ReturnType);
    }

    [Fact]
    public void StackElement_WithTransitions_Adds_Setter()
    {
        var stack = new StackElement(Orientation.Vertical, []);
        Assert.Empty(stack.Setters);

        // Call WithTransitions with empty array — no XAML objects needed
        var result = stack.WithTransitions();
        Assert.Single(result.Setters);
    }

    [Fact]
    public void StackElement_WithTransitions_Preserves_Children()
    {
        var child = new TextElement("hello");
        var stack = new StackElement(Orientation.Vertical, [child]);
        var result = stack.WithTransitions();
        Assert.Single(result.Children);
        Assert.Equal(child, result.Children[0]);
    }

    [Fact]
    public void StackElement_WithTransitions_Preserves_Spacing()
    {
        var stack = new StackElement(Orientation.Vertical, []) { Spacing = 16 };
        var result = stack.WithTransitions();
        Assert.Equal(16, result.Spacing);
    }

    [Fact]
    public void StackElement_WithTransitions_Preserves_Orientation()
    {
        var stack = new StackElement(Orientation.Horizontal, []);
        var result = stack.WithTransitions();
        Assert.Equal(Orientation.Horizontal, result.Orientation);
    }

    [Fact]
    public void StackElement_WithTransitions_Chains_With_Set()
    {
        var stack = new StackElement(Orientation.Vertical, []);
        var result = stack
            .WithTransitions()
            .Set(sp => sp.Padding = new Thickness(10));

        Assert.Equal(2, result.Setters.Length);
    }

    [Fact]
    public void StackElement_Multiple_WithTransitions_Adds_Multiple_Setters()
    {
        var stack = new StackElement(Orientation.Vertical, []);
        var result = stack.WithTransitions().WithTransitions();
        Assert.Equal(2, result.Setters.Length);
    }

    // ── GridElement.WithTransitions ──────────────────────────────

    [Fact]
    public void GridElement_WithTransitions_Method_Exists()
    {
        var method = typeof(ElementExtensions).GetMethod(
            "WithTransitions",
            [typeof(GridElement), typeof(Microsoft.UI.Xaml.Media.Animation.Transition[])]);
        Assert.NotNull(method);
        Assert.Equal(typeof(GridElement), method!.ReturnType);
    }

    [Fact]
    public void GridElement_WithTransitions_Adds_Setter()
    {
        var grid = new GridElement(
            new GridDefinition(["*"], ["*"]),
            []);
        Assert.Empty(grid.Setters);

        var result = grid.WithTransitions();
        Assert.Single(result.Setters);
    }

    [Fact]
    public void GridElement_WithTransitions_Preserves_Definition()
    {
        var def = new GridDefinition(["*", "2*"], ["Auto", "*"]);
        var grid = new GridElement(def, []);
        var result = grid.WithTransitions();
        Assert.Equal(def, result.Definition);
    }

    // ── BorderElement.WithTransitions ─────────────────────────────

    [Fact]
    public void BorderElement_WithTransitions_Method_Exists()
    {
        var method = typeof(ElementExtensions).GetMethod(
            "WithTransitions",
            [typeof(BorderElement), typeof(Microsoft.UI.Xaml.Media.Animation.Transition[])]);
        Assert.NotNull(method);
        Assert.Equal(typeof(BorderElement), method!.ReturnType);
    }

    [Fact]
    public void BorderElement_WithTransitions_Adds_Setter()
    {
        var border = new BorderElement(new TextElement("child"));
        Assert.Empty(border.Setters);

        var result = border.WithTransitions();
        Assert.Single(result.Setters);
    }

    [Fact]
    public void BorderElement_WithTransitions_Preserves_Child()
    {
        var child = new TextElement("hello");
        var border = new BorderElement(child);
        var result = border.WithTransitions();
        Assert.Equal(child, result.Child);
    }

    // ── CanvasElement.WithTransitions ─────────────────────────────

    [Fact]
    public void CanvasElement_WithTransitions_Method_Exists()
    {
        var method = typeof(ElementExtensions).GetMethod(
            "WithTransitions",
            [typeof(CanvasElement), typeof(Microsoft.UI.Xaml.Media.Animation.Transition[])]);
        Assert.NotNull(method);
        Assert.Equal(typeof(CanvasElement), method!.ReturnType);
    }

    [Fact]
    public void CanvasElement_WithTransitions_Adds_Setter()
    {
        var canvas = new CanvasElement([]);
        Assert.Empty(canvas.Setters);

        var result = canvas.WithTransitions();
        Assert.Single(result.Setters);
    }

    // ── All four container types have WithTransitions ──────────────

    [Fact]
    public void All_Container_Types_Have_WithTransitions()
    {
        var transitionType = typeof(Microsoft.UI.Xaml.Media.Animation.Transition[]);
        var ext = typeof(ElementExtensions);

        Assert.NotNull(ext.GetMethod("WithTransitions", [typeof(StackElement), transitionType]));
        Assert.NotNull(ext.GetMethod("WithTransitions", [typeof(GridElement), transitionType]));
        Assert.NotNull(ext.GetMethod("WithTransitions", [typeof(BorderElement), transitionType]));
        Assert.NotNull(ext.GetMethod("WithTransitions", [typeof(CanvasElement), transitionType]));
    }
}
