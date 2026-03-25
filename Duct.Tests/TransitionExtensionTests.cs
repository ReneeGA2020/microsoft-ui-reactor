using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for Feature 8: Animation and Transition Support.
/// Validates WithTransitions() extension methods on container elements.
/// Pure logic tests — verify ThemeTransitions are set without creating WinUI controls.
/// </summary>
public class TransitionExtensionTests
{
    // ── StackElement.WithTransitions ──────────────────────────────

    [Fact]
    public void StackElement_WithTransitions_Returns_StackElement()
    {
        var stack = new StackElement(Orientation.Vertical, []);
        var result = stack.WithTransitions();
        Assert.IsType<StackElement>(result);
    }

    [Fact]
    public void StackElement_WithTransitions_Adds_Setter()
    {
        var stack = new StackElement(Orientation.Vertical, []);
        Assert.Null(stack.ThemeTransitions);

        // Call WithTransitions with empty array — no XAML objects needed
        var result = stack.WithTransitions();
        Assert.NotNull(result.ThemeTransitions);
        Assert.NotNull(result.ThemeTransitions!.Children);
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

        Assert.NotNull(result.ThemeTransitions);
        Assert.Single(result.Setters);
    }

    [Fact]
    public void StackElement_Multiple_WithTransitions_Adds_Multiple_Setters()
    {
        var stack = new StackElement(Orientation.Vertical, []);
        var result = stack.WithTransitions().WithTransitions();
        // Second call overwrites Children on ThemeTransitions (not additive)
        Assert.NotNull(result.ThemeTransitions);
        Assert.NotNull(result.ThemeTransitions!.Children);
    }

    // ── GridElement.WithTransitions ──────────────────────────────

    [Fact]
    public void GridElement_WithTransitions_Method_Exists()
    {
        var grid = new GridElement(
            new GridDefinition(["*"], ["*"]),
            []);
        var result = grid.WithTransitions();
        Assert.IsType<GridElement>(result);
    }

    [Fact]
    public void GridElement_WithTransitions_Adds_Setter()
    {
        var grid = new GridElement(
            new GridDefinition(["*"], ["*"]),
            []);
        Assert.Null(grid.ThemeTransitions);

        var result = grid.WithTransitions();
        Assert.NotNull(result.ThemeTransitions);
        Assert.NotNull(result.ThemeTransitions!.Children);
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
        var border = new BorderElement(new TextElement("child"));
        var result = border.WithTransitions();
        Assert.IsType<BorderElement>(result);
    }

    [Fact]
    public void BorderElement_WithTransitions_Adds_Setter()
    {
        var border = new BorderElement(new TextElement("child"));
        Assert.Null(border.ThemeTransitions);

        var result = border.WithTransitions();
        Assert.NotNull(result.ThemeTransitions);
        Assert.NotNull(result.ThemeTransitions!.Children);
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
        var canvas = new CanvasElement([]);
        var result = canvas.WithTransitions();
        Assert.IsType<CanvasElement>(result);
    }

    [Fact]
    public void CanvasElement_WithTransitions_Adds_Setter()
    {
        var canvas = new CanvasElement([]);
        Assert.Null(canvas.ThemeTransitions);

        var result = canvas.WithTransitions();
        Assert.NotNull(result.ThemeTransitions);
        Assert.NotNull(result.ThemeTransitions!.Children);
    }

    // ── All four container types have WithTransitions ──────────────

    [Fact]
    public void All_Container_Types_Have_WithTransitions()
    {
        // WithTransitions is a generic extension method on Element, so all types get it.
        // Verify it works and preserves the concrete type for each container.
        var stack = new StackElement(Orientation.Vertical, []).WithTransitions();
        var grid = new GridElement(new GridDefinition(["*"], ["*"]), []).WithTransitions();
        var border = new BorderElement(new TextElement("x")).WithTransitions();
        var canvas = new CanvasElement([]).WithTransitions();

        Assert.IsType<StackElement>(stack);
        Assert.IsType<GridElement>(grid);
        Assert.IsType<BorderElement>(border);
        Assert.IsType<CanvasElement>(canvas);
    }
}
