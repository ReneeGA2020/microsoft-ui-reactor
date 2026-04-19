// Port of d3-scale/test/linear-test.js

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class LinearScaleTests
{
    private static double RoundEpsilon(double x) => Math.Round(x * 1e12) / 1e12;

    [Fact]
    public void Defaults()
    {
        var s = new LinearScale();
        Assert.Equal([0.0, 1.0], s.Domain);
        Assert.Equal([0.0, 1.0], s.Range);
        Assert.False(s.Clamped);
    }

    [Fact]
    public void Map_DomainToRange()
    {
        var s = new LinearScale { Range = [1, 2] };
        Assert.Equal(1.5, s.Map(0.5));
    }

    [Fact]
    public void Constructor_DomainRange()
    {
        var s = new LinearScale([1, 2], [3, 4]);
        Assert.Equal([1.0, 2.0], s.Domain);
        Assert.Equal([3.0, 4.0], s.Range);
        Assert.Equal(3.5, s.Map(1.5));
    }

    [Fact]
    public void Map_EmptyDomain_MiddleOfRange()
    {
        Assert.Equal(1.5, new LinearScale { Domain = [0, 0], Range = [1, 2] }.Map(0));
        Assert.Equal(1.5, new LinearScale { Domain = [0, 0], Range = [2, 1] }.Map(1));
    }

    [Fact]
    public void Map_Bilinear()
    {
        var s = new LinearScale { Domain = [1, 2] };
        Assert.Equal(-0.5, s.Map(0.5));
        Assert.Equal(0.0, s.Map(1.0));
        Assert.Equal(0.5, s.Map(1.5));
        Assert.Equal(1.0, s.Map(2.0));
        Assert.Equal(1.5, s.Map(2.5));
    }

    [Fact]
    public void Invert_RangeToDomain()
    {
        var s = new LinearScale { Range = [1, 2] };
        Assert.Equal(0.5, s.Invert(1.5));
    }

    [Fact]
    public void Invert_Bilinear()
    {
        var s = new LinearScale { Domain = [1, 2] };
        Assert.Equal(0.5, s.Invert(-0.5));
        Assert.Equal(1.0, s.Invert(0.0));
        Assert.Equal(1.5, s.Invert(0.5));
        Assert.Equal(2.0, s.Invert(1.0));
        Assert.Equal(2.5, s.Invert(1.5));
    }

    [Fact]
    public void Invert_EmptyRange_MiddleOfDomain()
    {
        Assert.Equal(1.5, new LinearScale { Domain = [1, 2], Range = [0, 0] }.Invert(0));
        Assert.Equal(1.5, new LinearScale { Domain = [2, 1], Range = [0, 0] }.Invert(1));
    }

    [Fact]
    public void Clamp_DefaultFalse()
    {
        Assert.False(new LinearScale().Clamped);
        Assert.Equal(30.0, new LinearScale { Range = [10, 20] }.Map(2));
        Assert.Equal(0.0, new LinearScale { Range = [10, 20] }.Map(-1));
    }

    [Fact]
    public void Clamp_True_RestrictsOutput()
    {
        var s = new LinearScale { Range = [10, 20], Clamped = true };
        Assert.Equal(20.0, s.Map(2));
        Assert.Equal(10.0, s.Map(-1));
    }

    [Fact]
    public void Clamp_True_RestrictsInvert()
    {
        var s = new LinearScale { Range = [10, 20], Clamped = true };
        Assert.Equal(1.0, s.Invert(30));
        Assert.Equal(0.0, s.Invert(0));
    }

    [Fact]
    public void Nice_DefaultIs10()
    {
        Assert.Equal([0.0, 1.0], new LinearScale { Domain = [0, 0.96] }.Nice().Domain);
        Assert.Equal([0.0, 100.0], new LinearScale { Domain = [0, 96] }.Nice().Domain);
    }

    [Fact]
    public void Nice_ExtendsToRoundNumbers()
    {
        Assert.Equal([0.0, 1.0], new LinearScale { Domain = [0, 0.96] }.Nice(10).Domain);
        Assert.Equal([0.0, 100.0], new LinearScale { Domain = [0, 96] }.Nice(10).Domain);
        Assert.Equal([1.0, 0.0], new LinearScale { Domain = [0.96, 0] }.Nice(10).Domain);
        Assert.Equal([100.0, 0.0], new LinearScale { Domain = [96, 0] }.Nice(10).Domain);
        Assert.Equal([0.0, -1.0], new LinearScale { Domain = [0, -0.96] }.Nice(10).Domain);
        Assert.Equal([0.0, -100.0], new LinearScale { Domain = [0, -96] }.Nice(10).Domain);
        Assert.Equal([-1.0, 0.0], new LinearScale { Domain = [-0.96, 0] }.Nice(10).Domain);
        Assert.Equal([-100.0, 0.0], new LinearScale { Domain = [-96, 0] }.Nice(10).Domain);
    }

    [Fact]
    public void Nice_Nices_ExtendingToRoundNumbers()
    {
        Assert.Equal([1.0, 11.0], new LinearScale { Domain = [1.1, 10.9] }.Nice(10).Domain);
        Assert.Equal([11.0, 1.0], new LinearScale { Domain = [10.9, 1.1] }.Nice(10).Domain);
        Assert.Equal([0.0, 12.0], new LinearScale { Domain = [0.7, 11.001] }.Nice(10).Domain);
        Assert.Equal([130.0, 0.0], new LinearScale { Domain = [123.1, 6.7] }.Nice(10).Domain);
        Assert.Equal([0.0, 0.5], new LinearScale { Domain = [0, 0.49] }.Nice(10).Domain);
        Assert.Equal([0.0, 20.0], new LinearScale { Domain = [0, 14.1] }.Nice(5).Domain);
        Assert.Equal([0.0, 20.0], new LinearScale { Domain = [0, 15] }.Nice(5).Domain);
    }

    [Fact]
    public void Nice_NoEffectOnDegenerate()
    {
        Assert.Equal([0.0, 0.0], new LinearScale { Domain = [0, 0] }.Nice(10).Domain);
        Assert.Equal([0.5, 0.5], new LinearScale { Domain = [0.5, 0.5] }.Nice(10).Domain);
    }

    [Fact]
    public void Ticks_Ascending()
    {
        var s = new LinearScale();
        Assert.Equal(
            new[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 },
            s.Ticks(10).Select(RoundEpsilon).ToArray());
        Assert.Equal(
            new[] { 0.0, 0.2, 0.4, 0.6, 0.8, 1.0 },
            s.Ticks(7).Select(RoundEpsilon).ToArray());
        Assert.Equal(
            new[] { 0.0, 0.5, 1.0 },
            s.Ticks(3).Select(RoundEpsilon).ToArray());
        Assert.Equal(
            new[] { 0.0, 1.0 },
            s.Ticks(1).Select(RoundEpsilon).ToArray());

        s.Domain = [-100, 100];
        Assert.Equal(
            new double[] { -100, -80, -60, -40, -20, 0, 20, 40, 60, 80, 100 },
            s.Ticks(10));
        Assert.Equal(
            new double[] { -100, -50, 0, 50, 100 },
            s.Ticks(6));
        Assert.Equal(
            new double[] { -100, 0, 100 },
            s.Ticks(2));
    }

    [Fact]
    public void Ticks_Descending()
    {
        var s = new LinearScale { Domain = [1, 0] };
        var expected = new[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 }.Reverse().ToArray();
        Assert.Equal(expected, s.Ticks(10).Select(RoundEpsilon).ToArray());
    }

    [Fact]
    public void Polylinear_MoreThanTwoValues()
    {
        var s = new LinearScale { Domain = [4, 2, 1], Range = [1, 2, 4] };
        Assert.Equal(3.0, s.Map(1.5));
        Assert.Equal(1.5, s.Map(3));
        Assert.Equal(3.0, s.Invert(1.5));
        Assert.Equal(1.5, s.Invert(3));
    }

    [Fact]
    public void Nice_WithTickCount()
    {
        Assert.Equal([0.0, 100.0], new LinearScale { Domain = [12, 87] }.Nice(5).Domain);
        Assert.Equal([10.0, 90.0], new LinearScale { Domain = [12, 87] }.Nice(10).Domain);
        Assert.Equal([12.0, 87.0], new LinearScale { Domain = [12, 87] }.Nice(100).Domain);
    }

    [Fact]
    public void Copy_IsIndependent()
    {
        var s1 = new LinearScale { Domain = [1, 2], Range = [3, 4] };
        var s2 = s1.Copy();
        s1.Domain = [0, 10];
        Assert.Equal([1.0, 2.0], s2.Domain);
        Assert.Equal(3.5, s2.Map(1.5));
    }

    [Fact]
    public void Unknown_ReturnsForNaN()
    {
        var s = new LinearScale { Unknown = -1 };
        Assert.Equal(-1, s.Map(double.NaN));
        Assert.Equal(0.4, s.Map(0.4));
    }
}
