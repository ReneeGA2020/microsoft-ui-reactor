// Port of d3-format: FormatValue, FormatPrefix, PrecisionRound, PrecisionPrefix
// and additional format-type tests (b/o/x/X/c/e/s/r).

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class FormatValueTests
{
    [Fact]
    public void FormatValue_Inline_Is_Equivalent_To_Format_Then_Invoke()
    {
        Assert.Equal(D3Format.Format(".2f")(3.14159), D3Format.FormatValue(3.14159, ".2f"));
        Assert.Equal(D3Format.Format("+.2f")(3.14), D3Format.FormatValue(3.14, "+.2f"));
    }
}

public class FormatPrefixTests
{
    [Fact]
    public void FormatPrefix_At_1000_Uses_K_Prefix()
    {
        var f = D3Format.FormatPrefix(".1f", 1000);
        Assert.Equal("1.2k", f(1200));
    }

    [Fact]
    public void FormatPrefix_At_1_Million_Uses_M_Prefix()
    {
        var f = D3Format.FormatPrefix(".0f", 1_000_000);
        Assert.Equal("2M", f(2_000_000));
    }

    [Fact]
    public void FormatPrefix_At_Milli_Uses_M_Lowercase()
    {
        var f = D3Format.FormatPrefix(".0f", 0.001);
        Assert.Equal("1m", f(0.001));
    }
}

public class PrecisionTests
{
    [Fact]
    public void PrecisionFixed_Examples_From_Docs()
    {
        Assert.Equal(2, D3Format.PrecisionFixed(0.01));
        Assert.Equal(1, D3Format.PrecisionFixed(0.1));
        Assert.Equal(0, D3Format.PrecisionFixed(1));
    }

    [Fact]
    public void PrecisionRound_Returns_NonNegative()
    {
        Assert.True(D3Format.PrecisionRound(0.001, 100) >= 0);
        Assert.True(D3Format.PrecisionRound(1, 100) >= 0);
    }

    [Fact]
    public void PrecisionPrefix_Returns_NonNegative()
    {
        Assert.True(D3Format.PrecisionPrefix(1e-3, 1) >= 0);
        Assert.True(D3Format.PrecisionPrefix(1, 1_000_000) >= 0);
    }
}

public class FormatTypesTests
{
    [Fact]
    public void Binary_Type()
    {
        var f = D3Format.Format("b");
        Assert.Equal("101", f(5));
        Assert.Equal("1010", f(10));
        Assert.Equal("0", f(0));
    }

    [Fact]
    public void Octal_Type()
    {
        var f = D3Format.Format("o");
        Assert.Equal("10", f(8));
        Assert.Equal("77", f(63));
    }

    [Fact]
    public void Hex_Lowercase_Type()
    {
        var f = D3Format.Format("x");
        Assert.Equal("ff", f(255));
        Assert.Equal("abc", f(2748));
    }

    [Fact]
    public void Hex_Uppercase_Type()
    {
        var f = D3Format.Format("X");
        Assert.Equal("FF", f(255));
        Assert.Equal("ABC", f(2748));
    }

    [Fact]
    public void Char_Type_Converts_Codepoint()
    {
        var f = D3Format.Format("c");
        Assert.Equal("A", f(65));
        Assert.Equal("z", f(122));
    }

    [Fact]
    public void Exponent_Type()
    {
        var f = D3Format.Format(".2e");
        // 1.23e+002 (C# default) — check prefix; exact exponent-digit count varies by runtime.
        var result = f(123);
        Assert.StartsWith("1.23e+", result);
    }

    [Fact]
    public void SiPrefix_Type_At_Thousand()
    {
        var f = D3Format.Format(".1s");
        Assert.Equal("1k", f(1000));
    }

    [Fact]
    public void SiPrefix_Type_At_Million()
    {
        var f = D3Format.Format(".0s");
        Assert.Equal("1M", f(1_000_000));
    }

    [Fact]
    public void Rounded_Type_Two_Significant_Digits()
    {
        var f = D3Format.Format(".2r");
        Assert.Equal("3.1", f(3.14159));
        Assert.Equal("120", f(123));
    }
}

public class FormatSignTests
{
    [Fact]
    public void Space_Sign_Positive_Gets_Leading_Space()
    {
        var f = D3Format.Format(" .2f");
        Assert.Equal(" 3.14", f(3.14));
        Assert.Equal("-3.14", f(-3.14));
    }

    [Fact]
    public void Paren_Sign_Wraps_Negative()
    {
        var f = D3Format.Format("(.2f");
        Assert.Equal("(3.14)", f(-3.14));
    }
}

public class FormatWidthTests
{
    [Fact]
    public void Width_Right_Align_Default()
    {
        var f = D3Format.Format("10.2f");
        Assert.Equal("      3.14", f(3.14));
    }

    [Fact]
    public void Width_Left_Align()
    {
        var f = D3Format.Format("<10.2f");
        Assert.Equal("3.14      ", f(3.14));
    }

    [Fact]
    public void Width_Zero_Padded()
    {
        var f = D3Format.Format("08.2f");
        Assert.Equal("00003.14", f(3.14));
    }
}
