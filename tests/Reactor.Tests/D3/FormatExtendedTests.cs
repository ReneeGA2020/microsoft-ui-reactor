// Extended tests for D3Format — covers types, alignment, signs, precision helpers, and flags
// not fully exercised in FormatTests.cs

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class FormatExtendedTests
{
    // --- Binary / Octal / Hex ---

    [Fact]
    public void Binary_FormatsInteger()
    {
        var f = D3Format.Format("b");
        Assert.Equal("1010", f(10));
        Assert.Equal("11111111", f(255));
    }

    [Fact]
    public void Octal_FormatsInteger()
    {
        var f = D3Format.Format("o");
        Assert.Equal("12", f(10));
        Assert.Equal("377", f(255));
    }

    [Fact]
    public void Hex_LowerCase()
    {
        var f = D3Format.Format("x");
        Assert.Equal("ff", f(255));
        Assert.Equal("2a", f(42));
    }

    [Fact]
    public void Hex_UpperCase()
    {
        var f = D3Format.Format("X");
        Assert.Equal("FF", f(255));
        Assert.Equal("2A", f(42));
    }

    // --- Exponent notation ---

    [Fact]
    public void Exponent_DefaultPrecision()
    {
        var f = D3Format.Format("e");
        // Default precision is 6
        string result = f(3.14159);
        Assert.Contains("e", result);
        Assert.StartsWith("3.141590e", result);
    }

    [Fact]
    public void Exponent_TwoDecimals()
    {
        var f = D3Format.Format(".2e");
        string result = f(42000);
        Assert.StartsWith("4.20e", result);
    }

    // --- General notation ---

    [Fact]
    public void General_SmallNumber()
    {
        var f = D3Format.Format(".2g");
        Assert.Equal("3.1", f(3.14159));
    }

    [Fact]
    public void General_LargeNumber()
    {
        var f = D3Format.Format(".4g");
        string result = f(123456);
        // g-format switches to exponent for large values at low precision
        Assert.Equal("1.235e+05", result);
    }

    // --- SI-prefix with FormatPrefix ---

    [Fact]
    public void FormatPrefix_Kilo()
    {
        var f = D3Format.FormatPrefix(".1", 1e3);
        // reference=1000 → kilo prefix
        Assert.Equal("1.2k", f(1234));
    }

    [Fact]
    public void FormatPrefix_Mega()
    {
        var f = D3Format.FormatPrefix(".2", 1e6);
        // reference=1e6 → Mega prefix, value scaled by 1e-6
        string result = f(1234567);
        Assert.Equal("1.23M", result);
    }

    // --- SI type "s" ---

    [Fact]
    public void SI_FormatsWithPrefix()
    {
        var f = D3Format.Format(".3s");
        Assert.Contains("k", f(1500));
    }

    // --- Percent ---

    [Fact]
    public void Percent_TwoDecimals()
    {
        var f = D3Format.Format(".2%");
        Assert.Equal("75.00%", f(0.75));
        Assert.Equal("100.00%", f(1.0));
    }

    // --- Alignment ---

    [Fact]
    public void AlignRight_Padded()
    {
        var f = D3Format.Format(">10.2f");
        string result = f(3.14);
        Assert.Equal(10, result.Length);
        Assert.Equal("      3.14", result);
    }

    [Fact]
    public void AlignLeft_Padded()
    {
        var f = D3Format.Format("<10.2f");
        string result = f(3.14);
        Assert.Equal(10, result.Length);
        Assert.Equal("3.14      ", result);
    }

    [Fact]
    public void AlignCenter_Padded()
    {
        var f = D3Format.Format("^10.2f");
        string result = f(3.14);
        Assert.Equal(10, result.Length);
        // centered: 3 spaces + "3.14" + 3 spaces
        Assert.Equal("   3.14   ", result);
    }

    // --- Zero-padding ---

    [Fact]
    public void ZeroPad_Fixed()
    {
        var f = D3Format.Format("010.2f");
        Assert.Equal("0000003.14", f(3.14));
    }

    [Fact]
    public void ZeroPad_NegativeNumber()
    {
        var f = D3Format.Format("010.2f");
        // Sign should precede the zero fill: "-000003.14"
        Assert.Equal("-000003.14", f(-3.14));
    }

    // --- Sign options ---

    [Fact]
    public void Sign_Space_PositiveGetsSpace()
    {
        var f = D3Format.Format(" .2f");
        Assert.Equal(" 3.14", f(3.14));
        Assert.Equal("-3.14", f(-3.14));
    }

    [Fact]
    public void Sign_Parentheses_NegativeWrapped()
    {
        var f = D3Format.Format("(.2f");
        string result = f(-3.14);
        Assert.Equal("(3.14)", result);
    }

    [Fact]
    public void Sign_Parentheses_PositiveNoWrap()
    {
        var f = D3Format.Format("(.2f");
        Assert.Equal("3.14", f(3.14));
    }

    // --- Grouped notation (n type and comma flag) ---

    [Fact]
    public void Grouped_N_Type()
    {
        var f = D3Format.Format(".0n");
        Assert.Equal("1,234,567", f(1234567));
    }

    [Fact]
    public void Comma_WithDecimals()
    {
        var f = D3Format.Format(",.2f");
        Assert.Equal("1,234,567.89", f(1234567.89));
    }

    // --- PrecisionFixed / PrecisionRound / PrecisionPrefix ---

    [Fact]
    public void PrecisionFixed_FractionalSteps()
    {
        Assert.Equal(3, D3Format.PrecisionFixed(0.001));
        Assert.Equal(4, D3Format.PrecisionFixed(0.0001));
    }

    [Fact]
    public void PrecisionRound_StepMax()
    {
        int p = D3Format.PrecisionRound(0.01, 1.01);
        Assert.True(p >= 0);
    }

    [Fact]
    public void PrecisionPrefix_StepValue()
    {
        int p = D3Format.PrecisionPrefix(1e5, 1.3e6);
        Assert.True(p >= 0);
    }

    // --- Character type ---

    [Fact]
    public void Character_Type()
    {
        var f = D3Format.Format("c");
        Assert.Equal("A", f(65));
        Assert.Equal("!", f(33));
    }

    // --- FormatValue (string specifier overload) ---

    [Fact]
    public void FormatValue_StringOverload()
    {
        string result = D3Format.FormatValue(3.14159, ".2f");
        Assert.Equal("3.14", result);
    }
}
