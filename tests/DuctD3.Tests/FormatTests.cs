// Port of d3-format tests

using Xunit;

namespace Duct.D3.Tests;

public class FormatTests
{
    [Fact]
    public void Fixed_TwoDecimals()
    {
        var f = D3Format.Format(".2f");
        Assert.Equal("3.14", f(3.14159));
    }

    [Fact]
    public void Fixed_ZeroDecimals()
    {
        var f = D3Format.Format(".0f");
        Assert.Equal("3", f(3.14));
    }

    [Fact]
    public void Fixed_SixDecimalsDefault()
    {
        var f = D3Format.Format("f");
        Assert.Equal("3.141590", f(3.14159));
    }

    [Fact]
    public void Percent()
    {
        var f = D3Format.Format(".0%");
        Assert.Equal("75%", f(0.75));
    }

    [Fact]
    public void Percent_OneDecimal()
    {
        var f = D3Format.Format(".1%");
        Assert.Equal("75.0%", f(0.75));
    }

    [Fact]
    public void Integer()
    {
        var f = D3Format.Format("d");
        Assert.Equal("42", f(42));
    }

    [Fact]
    public void Comma_GroupsThousands()
    {
        var f = D3Format.Format(",.0f");
        Assert.Equal("1,234,567", f(1234567));
    }

    [Fact]
    public void General_ThreeDigits()
    {
        var f = D3Format.Format(".3g");
        Assert.Equal("3.14", f(3.14159));
    }

    [Fact]
    public void PrecisionFixed_Step()
    {
        Assert.Equal(2, D3Format.PrecisionFixed(0.01));
        Assert.Equal(0, D3Format.PrecisionFixed(1));
        Assert.Equal(1, D3Format.PrecisionFixed(0.1));
    }

    [Fact]
    public void Negative_HasSign()
    {
        var f = D3Format.Format(".2f");
        Assert.Equal("-3.14", f(-3.14));
    }

    [Fact]
    public void Plus_ShowsPositiveSign()
    {
        var f = D3Format.Format("+.2f");
        Assert.Equal("+3.14", f(3.14));
        Assert.Equal("-3.14", f(-3.14));
    }
}
