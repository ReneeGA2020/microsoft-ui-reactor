using Xunit;

namespace Duct.D3.Tests;

public class ColorTests
{
    // ── Constructor ────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsRgbAndOpacity()
    {
        var c = new D3Color(10, 20, 30, 0.5);
        Assert.Equal(10, c.R);
        Assert.Equal(20, c.G);
        Assert.Equal(30, c.B);
        Assert.Equal(0.5, c.Opacity);
    }

    [Fact]
    public void Constructor_DefaultOpacity_IsOne()
    {
        var c = new D3Color(100, 150, 200);
        Assert.Equal(1.0, c.Opacity);
    }

    // ── Parse: hex 3-char ──────────────────────────────────────────────

    [Fact]
    public void Parse_Hex3_ReturnsRgb()
    {
        var c = D3Color.Parse("#f00");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Parse_Hex3_Abc()
    {
        var c = D3Color.Parse("#abc");
        Assert.Equal(170, c.R); // 0xA * 17
        Assert.Equal(187, c.G); // 0xB * 17
        Assert.Equal(204, c.B); // 0xC * 17
    }

    // ── Parse: hex 6-char ──────────────────────────────────────────────

    [Fact]
    public void Parse_Hex6_ReturnsRgb()
    {
        var c = D3Color.Parse("#1f77b4");
        Assert.Equal(31, c.R);
        Assert.Equal(119, c.G);
        Assert.Equal(180, c.B);
    }

    [Fact]
    public void Parse_Hex6_White()
    {
        var c = D3Color.Parse("#ffffff");
        Assert.Equal(255, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(255, c.B);
    }

    // ── Parse: rgb() ───────────────────────────────────────────────────

    [Fact]
    public void Parse_Rgb_ReturnsColor()
    {
        var c = D3Color.Parse("rgb(100, 200, 50)");
        Assert.Equal(100, c.R);
        Assert.Equal(200, c.G);
        Assert.Equal(50, c.B);
        Assert.Equal(1.0, c.Opacity);
    }

    // ── Parse: rgba() ──────────────────────────────────────────────────

    [Fact]
    public void Parse_Rgba_ReturnsColorWithOpacity()
    {
        var c = D3Color.Parse("rgba(10, 20, 30, 0.5)");
        Assert.Equal(10, c.R);
        Assert.Equal(20, c.G);
        Assert.Equal(30, c.B);
        Assert.Equal(0.5, c.Opacity);
    }

    // ── Parse: hsl() ───────────────────────────────────────────────────

    [Fact]
    public void Parse_Hsl_Red()
    {
        var c = D3Color.Parse("hsl(0, 100%, 50%)");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Parse_Hsl_Green()
    {
        var c = D3Color.Parse("hsl(120, 100%, 50%)");
        Assert.Equal(0, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Parse_Hsl_Blue()
    {
        var c = D3Color.Parse("hsl(240, 100%, 50%)");
        Assert.Equal(0, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(255, c.B);
    }

    [Fact]
    public void Parse_Hsl_Yellow()
    {
        var c = D3Color.Parse("hsl(60, 100%, 50%)");
        Assert.Equal(255, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Parse_Hsl_Cyan()
    {
        var c = D3Color.Parse("hsl(180, 100%, 50%)");
        Assert.Equal(0, c.R);
        Assert.Equal(255, c.G);
        Assert.Equal(255, c.B);
    }

    [Fact]
    public void Parse_Hsl_Magenta()
    {
        var c = D3Color.Parse("hsl(300, 100%, 50%)");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(255, c.B);
    }

    // ── Parse: hsla() ──────────────────────────────────────────────────

    [Fact]
    public void Parse_Hsla_ReturnsColorWithOpacity()
    {
        var c = D3Color.Parse("hsla(0, 100%, 50%, 0.75)");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
        Assert.Equal(0.75, c.Opacity);
    }

    // ── Parse: named colors ────────────────────────────────────────────

    [Fact]
    public void Parse_NamedColor_Red()
    {
        var c = D3Color.Parse("red");
        Assert.Equal(255, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Parse_NamedColor_SteelBlue()
    {
        var c = D3Color.Parse("steelblue");
        Assert.Equal(70, c.R);
        Assert.Equal(130, c.G);
        Assert.Equal(180, c.B);
    }

    [Fact]
    public void Parse_NamedColor_Tomato()
    {
        var c = D3Color.Parse("tomato");
        Assert.Equal(255, c.R);
        Assert.Equal(99, c.G);
        Assert.Equal(71, c.B);
    }

    [Fact]
    public void Parse_NamedColor_Teal()
    {
        var c = D3Color.Parse("teal");
        Assert.Equal(0, c.R);
        Assert.Equal(128, c.G);
        Assert.Equal(128, c.B);
    }

    [Fact]
    public void Parse_NamedColor_Transparent()
    {
        var c = D3Color.Parse("transparent");
        Assert.Equal(0, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
        Assert.Equal(0.0, c.Opacity);
    }

    // ── Parse: invalid string ──────────────────────────────────────────

    [Fact]
    public void Parse_InvalidString_ReturnsBlack()
    {
        var c = D3Color.Parse("notacolor");
        Assert.Equal(0, c.R);
        Assert.Equal(0, c.G);
        Assert.Equal(0, c.B);
    }

    // ── Brighter / Darker ──────────────────────────────────────────────

    [Fact]
    public void Brighter_IncreasesRgb()
    {
        var c = new D3Color(70, 130, 180);
        var b = c.Brighter();
        Assert.True(b.R > c.R);
        Assert.True(b.G > c.G);
        Assert.True(b.B > c.B);
    }

    [Fact]
    public void Brighter_ClampsTo255()
    {
        var c = new D3Color(200, 200, 200);
        var b = c.Brighter(3);
        Assert.True(b.R <= 255);
        Assert.True(b.G <= 255);
        Assert.True(b.B <= 255);
    }

    [Fact]
    public void Darker_DecreasesRgb()
    {
        var c = new D3Color(70, 130, 180);
        var d = c.Darker();
        Assert.True(d.R < c.R);
        Assert.True(d.G < c.G);
        Assert.True(d.B < c.B);
    }

    [Fact]
    public void Darker_PreservesOpacity()
    {
        var c = new D3Color(100, 100, 100, 0.5);
        var d = c.Darker();
        Assert.Equal(0.5, d.Opacity);
    }

    // ── ToHex ──────────────────────────────────────────────────────────

    [Fact]
    public void ToHex_ReturnsUppercaseHex()
    {
        var c = new D3Color(31, 119, 180);
        Assert.Equal("#1F77B4", c.ToHex());
    }

    // ── ToRgb ──────────────────────────────────────────────────────────

    [Fact]
    public void ToRgb_FullOpacity_ReturnsRgbString()
    {
        var c = new D3Color(10, 20, 30);
        Assert.Equal("rgb(10, 20, 30)", c.ToRgb());
    }

    [Fact]
    public void ToRgb_PartialOpacity_ReturnsRgbaString()
    {
        var c = new D3Color(10, 20, 30, 0.5);
        Assert.Equal("rgba(10, 20, 30, 0.5)", c.ToRgb());
    }

    // ── ToString ───────────────────────────────────────────────────────

    [Fact]
    public void ToString_CallsToRgb()
    {
        var c = new D3Color(10, 20, 30);
        Assert.Equal(c.ToRgb(), c.ToString());
    }

    // ── Category10 / Tableau10 ─────────────────────────────────────────

    [Fact]
    public void Category10_HasTenColors()
    {
        Assert.Equal(10, D3Color.Category10.Count);
    }

    [Fact]
    public void Category10_FirstElement_IsCorrect()
    {
        var first = D3Color.Category10[0];
        Assert.Equal(31, first.R);
        Assert.Equal(119, first.G);
        Assert.Equal(180, first.B);
    }

    [Fact]
    public void Tableau10_HasTenColors()
    {
        Assert.Equal(10, D3Color.Tableau10.Count);
    }

    [Fact]
    public void Tableau10_FirstElement_IsCorrect()
    {
        var first = D3Color.Tableau10[0];
        Assert.Equal(78, first.R);  // 0x4e
        Assert.Equal(121, first.G); // 0x79
        Assert.Equal(167, first.B); // 0xa7
    }
}
