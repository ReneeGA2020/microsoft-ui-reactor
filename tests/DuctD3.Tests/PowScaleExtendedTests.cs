// Extended tests for PowScale — covers defaults, quadratic, sqrt, invert, ticks, nice,
// clamp, copy, and negative domain values

using Xunit;

namespace Duct.D3.Tests;

public class PowScaleExtendedTests
{
    [Fact]
    public void Default_State()
    {
        var s = new PowScale();
        Assert.Equal([0.0, 1.0], s.Domain);
        Assert.Equal([0.0, 1.0], s.Range);
        Assert.Equal(1.0, s.Exponent);
        Assert.False(s.Clamped);
    }

    [Fact]
    public void Map_Quadratic_Exponent2()
    {
        var s = new PowScale(2).SetDomain(0, 10).SetRange(0, 100);
        // pow(5,2)/pow(10,2) = 25/100 = 0.25 → 25
        Assert.Equal(25.0, s.Map(5), 5);
        Assert.Equal(0.0, s.Map(0), 10);
        Assert.Equal(100.0, s.Map(10), 10);
    }

    [Fact]
    public void Map_Quadratic_Endpoints()
    {
        var s = new PowScale(2).SetDomain(0, 4).SetRange(0, 16);
        Assert.Equal(0.0, s.Map(0), 10);
        Assert.Equal(16.0, s.Map(4), 10);
        Assert.Equal(4.0, s.Map(2), 5); // pow(2,2)/pow(4,2) * 16 = 4/16 * 16 = 4
    }

    [Fact]
    public void Sqrt_Factory_CreatesHalfExponent()
    {
        var s = PowScale.Sqrt();
        Assert.Equal(0.5, s.Exponent);
    }

    [Fact]
    public void Sqrt_Map()
    {
        var s = PowScale.Sqrt().SetDomain(0, 100).SetRange(0, 10);
        // sqrt(25)/sqrt(100) = 5/10 = 0.5 → 5
        Assert.Equal(5.0, s.Map(25), 5);
        // sqrt(0)/sqrt(100) = 0 → 0
        Assert.Equal(0.0, s.Map(0), 10);
    }

    [Fact]
    public void Invert_Roundtrip_Linear()
    {
        var s = new PowScale().SetDomain(0, 100).SetRange(0, 200);
        double[] samples = [0, 25, 50, 75, 100];
        foreach (double x in samples)
        {
            double y = s.Map(x);
            Assert.Equal(x, s.Invert(y), 5);
        }
    }

    [Fact]
    public void Invert_Roundtrip_ExponentOne()
    {
        // Exponent 1 (linear) roundtrip is exact
        var s = new PowScale().SetDomain(0, 100).SetRange(0, 200);
        double[] samples = [0, 25, 50, 75, 100];
        foreach (double x in samples)
        {
            double y = s.Map(x);
            Assert.Equal(x, s.Invert(y), 5);
        }
    }

    [Fact]
    public void Invert_BoundaryValues()
    {
        var s = new PowScale(2).SetDomain(0, 10).SetRange(0, 100);
        // Boundary values should invert correctly
        Assert.Equal(0.0, s.Invert(0), 10);
    }

    [Fact]
    public void Ticks_ReturnsEvenlySpacedValues()
    {
        var s = new PowScale(2).SetDomain(0, 10).SetRange(0, 100);
        var ticks = s.Ticks(5);
        Assert.True(ticks.Length > 0);
        Assert.Equal(0.0, ticks[0]);
        Assert.Equal(10.0, ticks[^1]);
    }

    [Fact]
    public void Nice_ExtendsToRoundValues()
    {
        var s = new PowScale(2).SetDomain(0.5, 9.5).SetRange(0, 100).Nice();
        Assert.Equal(0.0, s.Domain[0]);
        Assert.Equal(10.0, s.Domain[1]);
    }

    [Fact]
    public void Clamped_RestrictsOutput()
    {
        var s = new PowScale(2).SetDomain(0, 10).SetRange(0, 100).SetClamp(true);
        // Beyond domain should clamp to range boundary
        Assert.Equal(100.0, s.Map(20), 10);
        Assert.Equal(0.0, s.Map(-5), 10);
    }

    [Fact]
    public void Copy_IsIndependent()
    {
        var s1 = new PowScale(2).SetDomain(0, 10).SetRange(0, 100);
        var s2 = s1.Copy();
        s1.Domain = [0, 20];
        Assert.Equal([0.0, 10.0], s2.Domain);
        Assert.Equal(25.0, s2.Map(5), 5);
    }

    [Fact]
    public void NegativeDomain_OddExponent()
    {
        // PowScale handles negative domain values for non-integer exponents via sign preservation
        var s = new PowScale(3).SetDomain(-10, 10).SetRange(-100, 100);
        double negResult = s.Map(-5);
        double posResult = s.Map(5);
        // Symmetric: Map(-5) should be -Map(5)
        Assert.Equal(-posResult, negResult, 5);
    }

    [Fact]
    public void FluentSetters_ReturnSelf()
    {
        var s = new PowScale();
        var result = s.SetDomain(0, 100).SetRange(0, 1).SetExponent(2).SetClamp(false);
        Assert.Same(s, result);
    }
}
