using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for BrushHelper color parsing.
/// Uses the internal ParseHex method directly to avoid creating SolidColorBrush
/// (which requires a XAML Application context / UI thread).
/// </summary>
public class BrushHelperTests
{
    // ── Hex #RRGGBB ──────────────────────────────────────────────

    [Fact]
    public void ParseHex_RRGGBB()
    {
        var color = BrushHelper.ParseHex("#FF8800");
        Assert.Equal(255, color.A);
        Assert.Equal(255, color.R);
        Assert.Equal(0x88, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ParseHex_RRGGBB_Lowercase()
    {
        var color = BrushHelper.ParseHex("#ff0000");
        Assert.Equal(255, color.A);
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ParseHex_RRGGBB_AllZeros()
    {
        var color = BrushHelper.ParseHex("#000000");
        Assert.Equal(255, color.A);
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ParseHex_RRGGBB_AllMax()
    {
        var color = BrushHelper.ParseHex("#FFFFFF");
        Assert.Equal(255, color.A);
        Assert.Equal(255, color.R);
        Assert.Equal(255, color.G);
        Assert.Equal(255, color.B);
    }

    // ── Hex #AARRGGBB ────────────────────────────────────────────

    [Fact]
    public void ParseHex_AARRGGBB()
    {
        var color = BrushHelper.ParseHex("#80FF0000");
        Assert.Equal(0x80, color.A);
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    [Fact]
    public void ParseHex_AARRGGBB_Transparent()
    {
        var color = BrushHelper.ParseHex("#00000000");
        Assert.Equal(0, color.A);
        Assert.Equal(0, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    // ── Invalid / edge cases → gray fallback ─────────────────────

    [Fact]
    public void ParseHex_Invalid_Characters_Returns_Gray()
    {
        var color = BrushHelper.ParseHex("#GGHHII");
        Assert.Equal(128, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(128, color.B);
    }

    [Fact]
    public void ParseHex_Empty_After_Hash_Returns_Gray()
    {
        var color = BrushHelper.ParseHex("#");
        Assert.Equal(128, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(128, color.B);
    }

    [Fact]
    public void ParseHex_Short_Hex_3_Chars_Returns_Gray()
    {
        var color = BrushHelper.ParseHex("#FFF");
        Assert.Equal(128, color.R);
    }

    [Fact]
    public void ParseHex_Too_Long_Returns_Gray()
    {
        var color = BrushHelper.ParseHex("#AABBCCDDEE");
        Assert.Equal(128, color.R);
    }

    [Fact]
    public void ParseHex_5_Chars_Returns_Gray()
    {
        var color = BrushHelper.ParseHex("#AABBC");
        Assert.Equal(128, color.R);
    }

    [Fact]
    public void ParseHex_No_Hash_6_Chars()
    {
        // ParseHex trims # so passing without # should still work
        var color = BrushHelper.ParseHex("FF0000");
        Assert.Equal(255, color.A);
        Assert.Equal(255, color.R);
        Assert.Equal(0, color.G);
        Assert.Equal(0, color.B);
    }

    // ── Mixed case ───────────────────────────────────────────────

    [Fact]
    public void ParseHex_MixedCase()
    {
        var color = BrushHelper.ParseHex("#aAbBcC");
        Assert.Equal(0xAA, color.R);
        Assert.Equal(0xBB, color.G);
        Assert.Equal(0xCC, color.B);
    }
}
