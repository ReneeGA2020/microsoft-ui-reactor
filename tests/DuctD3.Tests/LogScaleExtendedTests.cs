// Extended tests for LogScale — covers defaults, boundaries, NaN, invert, ticks, nice, clamp,
// custom base, and copy independence not fully exercised in ScaleTests.cs

using Xunit;

namespace Duct.D3.Tests;

public class LogScaleExtendedTests
{
    [Fact]
    public void Default_DomainAndRange()
    {
        var s = new LogScale();
        Assert.Equal([1.0, 10.0], s.Domain);
        Assert.Equal([0.0, 1.0], s.Range);
        Assert.Equal(10.0, s.Base);
        Assert.False(s.Clamped);
    }

    [Fact]
    public void Map_DomainBoundaries()
    {
        var s = new LogScale();
        Assert.Equal(0.0, s.Map(1), 10);
        Assert.Equal(1.0, s.Map(10), 10);
    }

    [Fact]
    public void Map_Midpoint_SqrtTen()
    {
        var s = new LogScale();
        // log10(sqrt(10)) = 0.5
        Assert.Equal(0.5, s.Map(Math.Sqrt(10)), 5);
    }

    [Fact]
    public void Map_ZeroReturnsNaN()
    {
        var s = new LogScale();
        Assert.True(double.IsNaN(s.Map(0)));
    }

    [Fact]
    public void Map_NegativeReturnsNaN()
    {
        var s = new LogScale();
        Assert.True(double.IsNaN(s.Map(-5)));
    }

    [Fact]
    public void Map_NaNInput_ReturnsNaN()
    {
        var s = new LogScale();
        Assert.True(double.IsNaN(s.Map(double.NaN)));
    }

    [Fact]
    public void Invert_Roundtrip()
    {
        var s = new LogScale([1, 1000], [0, 1]);
        double[] samples = [1, 10, 100, 1000, 31.623];
        foreach (double x in samples)
        {
            double y = s.Map(x);
            Assert.Equal(x, s.Invert(y), 3);
        }
    }

    [Fact]
    public void Invert_BoundaryValues()
    {
        var s = new LogScale([1, 100], [0, 1]);
        Assert.Equal(1.0, s.Invert(0), 10);
        Assert.Equal(100.0, s.Invert(1), 10);
    }

    [Fact]
    public void Ticks_OneToHundred_ContainsIntermediateValues()
    {
        var s = new LogScale([1, 100], [0, 1]);
        var ticks = s.Ticks();
        Assert.Contains(1.0, ticks);
        Assert.Contains(2.0, ticks);
        Assert.Contains(5.0, ticks);
        Assert.Contains(10.0, ticks);
        Assert.Contains(20.0, ticks);
        Assert.Contains(50.0, ticks);
        Assert.Contains(100.0, ticks);
    }

    [Fact]
    public void Ticks_AreNonDecreasing()
    {
        var s = new LogScale([1, 10000], [0, 1]);
        var ticks = s.Ticks();
        Assert.True(ticks.Length > 0);
        // Ticks should be non-decreasing
        for (int i = 1; i < ticks.Length; i++)
        {
            Assert.True(ticks[i] >= ticks[i - 1],
                $"Tick {i} ({ticks[i]}) should be >= tick {i - 1} ({ticks[i - 1]})");
        }
    }

    [Fact]
    public void Nice_RoundsDomainToPowers()
    {
        var s = new LogScale([2, 70], [0, 1]).Nice();
        Assert.Equal(1.0, s.Domain[0], 5);
        Assert.Equal(100.0, s.Domain[1], 5);
    }

    [Fact]
    public void Nice_AlreadyNice_NoChange()
    {
        var s = new LogScale([1, 1000], [0, 1]).Nice();
        Assert.Equal(1.0, s.Domain[0], 10);
        Assert.Equal(1000.0, s.Domain[1], 10);
    }

    [Fact]
    public void Clamped_RestrictsOutput()
    {
        var s = new LogScale([1, 10], [0, 1]).SetClamp(true);
        // Value beyond domain should be clamped to range boundary
        Assert.Equal(1.0, s.Map(100), 10);
        Assert.Equal(0.0, s.Map(0.1), 10);
    }

    [Fact]
    public void CustomBase_Two()
    {
        var s = new LogScale([1, 8], [0, 1]).SetBase(2);
        // log2(1)=0, log2(8)=3 → Map(2) = log2(2)/log2(8) = 1/3
        Assert.Equal(1.0 / 3, s.Map(2), 5);
        Assert.Equal(2.0 / 3, s.Map(4), 5);
    }

    [Fact]
    public void Copy_IsIndependent()
    {
        var s1 = new LogScale([1, 100], [0, 10]);
        var s2 = s1.Copy();
        s1.Domain = [1, 1000];
        Assert.Equal([1.0, 100.0], s2.Domain);
        Assert.Equal(5.0, s2.Map(10), 5);
    }

    [Fact]
    public void FluentSetters_ReturnSelf()
    {
        var s = new LogScale();
        var result = s.SetDomain(1, 1000).SetRange(0, 100).SetBase(10).SetClamp(false);
        Assert.Same(s, result);
    }
}
