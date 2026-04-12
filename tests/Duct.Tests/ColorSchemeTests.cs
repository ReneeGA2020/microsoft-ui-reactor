using Duct.Core;
using Microsoft.UI.Xaml;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for the ColorScheme enum and ColorSchemeContext mapping logic.
/// Pure C# tests — no WinUI activation needed.
/// </summary>
public class ColorSchemeTests
{
    [Fact]
    public void ColorScheme_Has_Three_Values()
    {
        var values = Enum.GetValues<ColorScheme>();
        Assert.Equal(3, values.Length);
        Assert.Contains(ColorScheme.Light, values);
        Assert.Contains(ColorScheme.Dark, values);
        Assert.Contains(ColorScheme.HighContrast, values);
    }

    [Fact]
    public void ColorSchemeContext_Update_Dark()
    {
        var ctx = new ColorSchemeContext();
        ctx.Update(ElementTheme.Dark);
        Assert.Equal(ColorScheme.Dark, ctx.CurrentScheme);
    }

    [Fact]
    public void ColorSchemeContext_Update_Light()
    {
        var ctx = new ColorSchemeContext();
        ctx.Update(ElementTheme.Light);
        Assert.Equal(ColorScheme.Light, ctx.CurrentScheme);
    }

    [Fact]
    public void ColorSchemeContext_Default_Is_Light()
    {
        var ctx = new ColorSchemeContext();
        Assert.Equal(ColorScheme.Light, ctx.CurrentScheme);
    }

    [Fact]
    public void ColorSchemeContext_FromActualTheme_Dark()
    {
        Assert.Equal(ColorScheme.Dark, ColorSchemeContext.FromActualTheme(ElementTheme.Dark));
    }

    [Fact]
    public void ColorSchemeContext_FromActualTheme_Light()
    {
        Assert.Equal(ColorScheme.Light, ColorSchemeContext.FromActualTheme(ElementTheme.Light));
    }

    [Fact]
    public void ColorSchemeContext_Update_Switches_Between_Themes()
    {
        var ctx = new ColorSchemeContext();
        ctx.Update(ElementTheme.Dark);
        Assert.Equal(ColorScheme.Dark, ctx.CurrentScheme);

        ctx.Update(ElementTheme.Light);
        Assert.Equal(ColorScheme.Light, ctx.CurrentScheme);

        ctx.Update(ElementTheme.Dark);
        Assert.Equal(ColorScheme.Dark, ctx.CurrentScheme);
    }
}
