using Duct.Core;
using Duct.Elements;
using Microsoft.UI.Xaml;
using static Duct.UI;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for lightweight styling (ResourceBuilder, ResourceOverrides, and the
/// Resources() fluent extension method). Pure C# record tests — no WinUI activation
/// needed for the builder/record tests.
/// </summary>
public class LightweightStylingTests
{
    // ════════════════════════════════════════════════════════════════
    //  ResourceBuilder
    // ════════════════════════════════════════════════════════════════

    [Fact(Skip = "Requires WinUI activation for SolidColorBrush — covered by UI tests")]
    public void ResourceBuilder_Set_String_Creates_Literal_Brush()
    {
        var builder = new ResourceBuilder();
        builder.Set("ButtonBackground", "#FF0000");
        var overrides = builder.Build();

        Assert.Single(overrides.Literals);
        Assert.True(overrides.Literals.ContainsKey("ButtonBackground"));
        Assert.Empty(overrides.ThemeRefs);
    }

    [Fact]
    public void ResourceBuilder_Set_ThemeRef_Creates_ThemeRef_Entry()
    {
        var builder = new ResourceBuilder();
        builder.Set("ButtonBackground", Theme.Accent);
        var overrides = builder.Build();

        Assert.Empty(overrides.Literals);
        Assert.Single(overrides.ThemeRefs);
        Assert.Equal("AccentFillColorDefaultBrush", overrides.ThemeRefs["ButtonBackground"].ResourceKey);
    }

    [Fact]
    public void ResourceBuilder_Set_Double_Creates_Literal()
    {
        var builder = new ResourceBuilder();
        builder.Set("ButtonBorderThemeThickness", 2.0);
        var overrides = builder.Build();

        Assert.Single(overrides.Literals);
        Assert.Equal(2.0, overrides.Literals["ButtonBorderThemeThickness"]);
    }

    [Fact]
    public void ResourceBuilder_Set_CornerRadius_Creates_Literal()
    {
        var builder = new ResourceBuilder();
        builder.Set("ControlCornerRadius", new CornerRadius(8));
        var overrides = builder.Build();

        Assert.Single(overrides.Literals);
        Assert.Equal(new CornerRadius(8), overrides.Literals["ControlCornerRadius"]);
    }

    [Fact(Skip = "Requires WinUI activation for SolidColorBrush — covered by UI tests")]
    public void ResourceBuilder_Fluent_Chaining_Works()
    {
        var builder = new ResourceBuilder();
        var result = builder
            .Set("ButtonBackground", "#0078D4")
            .Set("ButtonBackgroundPointerOver", "#106EBE")
            .Set("ButtonForeground", Theme.PrimaryText);

        Assert.Same(builder, result);
        var overrides = builder.Build();
        Assert.Equal(2, overrides.Literals.Count);
        Assert.Single(overrides.ThemeRefs);
    }

    [Fact]
    public void ResourceBuilder_Build_Returns_Immutable_Snapshot()
    {
        var builder = new ResourceBuilder();
        builder.Set("A", 1.0);
        var snapshot1 = builder.Build();

        builder.Set("B", 2.0);
        var snapshot2 = builder.Build();

        // First snapshot should not be affected by subsequent additions
        Assert.Single(snapshot1.Literals);
        Assert.Equal(2, snapshot2.Literals.Count);
    }

    // ════════════════════════════════════════════════════════════════
    //  ResourceOverrides record
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ResourceOverrides_AllKeys_Returns_Union()
    {
        var overrides = new ResourceOverrides(
            new Dictionary<string, object> { ["A"] = 1.0, ["B"] = 2.0 },
            new Dictionary<string, ThemeRef> { ["B"] = Theme.Accent, ["C"] = Theme.PrimaryText });

        var allKeys = overrides.AllKeys.ToHashSet();
        Assert.Equal(3, allKeys.Count);
        Assert.Contains("A", allKeys);
        Assert.Contains("B", allKeys);
        Assert.Contains("C", allKeys);
    }

    // ════════════════════════════════════════════════════════════════
    //  Element.Resources() extension method
    // ════════════════════════════════════════════════════════════════

    [Fact(Skip = "Requires WinUI activation for SolidColorBrush — covered by UI tests")]
    public void Resources_Extension_Sets_ResourceOverrides_On_Element()
    {
        var el = Button("Go").Resources(r => r
            .Set("ButtonBackground", "#0078D4")
            .Set("ButtonBackgroundPointerOver", "#106EBE"));

        Assert.NotNull(el.ResourceOverrides);
        Assert.Equal(2, el.ResourceOverrides!.Literals.Count);
    }

    [Fact(Skip = "Requires WinUI activation for SolidColorBrush — covered by UI tests")]
    public void Resources_Extension_Preserves_Other_Element_Properties_Brush()
    {
        var el = Button("Go", () => { })
            .Margin(10)
            .Resources(r => r.Set("ButtonBackground", "#FF0000"));

        Assert.Equal("Go", el.Label);
        Assert.NotNull(el.OnClick);
        Assert.Equal(new Thickness(10), el.Modifiers!.Margin);
        Assert.NotNull(el.ResourceOverrides);
    }

    [Fact]
    public void Resources_Extension_Preserves_Other_Element_Properties()
    {
        var el = Button("Go", () => { })
            .Margin(10)
            .Resources(r => r.Set("ButtonForeground", Theme.PrimaryText));

        Assert.Equal("Go", el.Label);
        Assert.NotNull(el.OnClick);
        Assert.Equal(new Thickness(10), el.Modifiers!.Margin);
        Assert.NotNull(el.ResourceOverrides);
    }

    [Fact]
    public void Resources_Extension_Works_With_ThemeRef_Overrides()
    {
        var el = Button("Go").Resources(r => r
            .Set("ButtonBackground", Theme.Accent)
            .Set("ButtonForeground", Theme.PrimaryText));

        Assert.Equal(2, el.ResourceOverrides!.ThemeRefs.Count);
        Assert.Empty(el.ResourceOverrides.Literals);
    }

    [Fact(Skip = "Requires WinUI activation for SolidColorBrush — covered by UI tests")]
    public void Resources_Extension_Works_With_Mixed_Literal_And_ThemeRef()
    {
        var el = Button("Go").Resources(r => r
            .Set("ButtonBackground", "#0078D4")
            .Set("ButtonForeground", Theme.PrimaryText)
            .Set("ControlCornerRadius", new CornerRadius(4)));

        Assert.Equal(2, el.ResourceOverrides!.Literals.Count);
        Assert.Single(el.ResourceOverrides.ThemeRefs);
    }

    [Fact]
    public void Resources_Extension_Works_With_Double_And_CornerRadius()
    {
        var el = Button("Go").Resources(r => r
            .Set("ButtonBorderThemeThickness", 2.0)
            .Set("ControlCornerRadius", new CornerRadius(4)));

        Assert.Equal(2, el.ResourceOverrides!.Literals.Count);
        Assert.Empty(el.ResourceOverrides.ThemeRefs);
    }

    [Fact]
    public void Removing_Resources_Sets_Null()
    {
        var el1 = Button("Go").Resources(r => r.Set("ButtonForeground", Theme.PrimaryText));
        Assert.NotNull(el1.ResourceOverrides);

        // Without Resources(), ResourceOverrides should be null (default)
        var el2 = Button("Go");
        Assert.Null(el2.ResourceOverrides);
    }
}
