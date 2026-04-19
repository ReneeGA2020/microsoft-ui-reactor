// Tests for d3-scale types: Ordinal, Band, Point, Log, Pow, Quantize, Quantile, Threshold

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class OrdinalScaleTests
{
    [Fact]
    public void Map_ReturnsRangeValues()
    {
        var s = new OrdinalScale<string>(["a", "b", "c"], [10, 20, 30]);
        Assert.Equal(10, s.Map("a"));
        Assert.Equal(20, s.Map("b"));
        Assert.Equal(30, s.Map("c"));
    }

    [Fact]
    public void Map_ImplicitDomain()
    {
        var s = new OrdinalScale<string>().SetRange(10, 20, 30);
        Assert.Equal(10, s.Map("a"));
        Assert.Equal(20, s.Map("b"));
        Assert.Equal(10, s.Map("a"));
    }

    [Fact]
    public void Map_WrapsRange()
    {
        var s = new OrdinalScale<string>(["a", "b", "c", "d"], [10, 20]);
        Assert.Equal(10, s.Map("c"));
        Assert.Equal(20, s.Map("d"));
    }

    [Fact]
    public void Unknown_ReturnedForUnknownDomain()
    {
        var s = new OrdinalScale<string>(["a", "b"], [10, 20]).SetUnknown(-1);
        Assert.Equal(-1, s.Map("c"));
    }
}

public class BandScaleTests
{
    [Fact]
    public void Map_ReturnsBandStart()
    {
        var s = new BandScale<string>().SetDomain("a", "b", "c").SetRange(0, 120);
        Assert.Equal(0, s.Map("a"));
        Assert.Equal(40, s.Map("b"));
        Assert.Equal(80, s.Map("c"));
    }

    [Fact]
    public void Bandwidth_Correct()
    {
        var s = new BandScale<string>().SetDomain("a", "b", "c").SetRange(0, 120);
        Assert.Equal(40, s.Bandwidth);
    }

    [Fact]
    public void PaddingInner_ShrinksBands()
    {
        var s = new BandScale<string>().SetDomain("a", "b").SetRange(0, 100).SetPaddingInner(0.5);
        Assert.True(s.Bandwidth < 50);
    }

    [Fact]
    public void UnknownDomain_ReturnsNaN()
    {
        var s = new BandScale<string>().SetDomain("a", "b").SetRange(0, 100);
        Assert.True(double.IsNaN(s.Map("z")));
    }
}

public class PointScaleTests
{
    [Fact]
    public void Map_ReturnsPoints()
    {
        var s = new PointScale<string>().SetDomain("a", "b", "c").SetRange(0, 120);
        Assert.Equal(0, s.Map("a"));
        Assert.Equal(60, s.Map("b"));
        Assert.Equal(120, s.Map("c"));
    }
}

public class LogScaleTests
{
    [Fact]
    public void Map_LogTransform()
    {
        var s = new LogScale([1, 100], [0, 1]);
        Assert.Equal(0.5, s.Map(10), 5);
    }

    [Fact]
    public void Map_Endpoints()
    {
        var s = new LogScale([1, 100], [0, 1]);
        Assert.Equal(0, s.Map(1), 10);
        Assert.Equal(1, s.Map(100), 10);
    }

    [Fact]
    public void Nice_ExtendsToLogBoundaries()
    {
        var s = new LogScale([3, 80], [0, 1]).Nice();
        Assert.Equal(1.0, s.Domain[0], 5);
        Assert.Equal(100.0, s.Domain[1], 5);
    }

    [Fact]
    public void Ticks_ReturnsLogSpacedValues()
    {
        var s = new LogScale([1, 1000], [0, 1]);
        var ticks = s.Ticks();
        Assert.Contains(1.0, ticks);
        Assert.Contains(10.0, ticks);
        Assert.Contains(100.0, ticks);
        Assert.Contains(1000.0, ticks);
    }
}

public class PowScaleTests
{
    [Fact]
    public void Map_SquaredTransform()
    {
        var s = new PowScale().SetExponent(2).SetDomain(0, 10).SetRange(0, 100);
        Assert.Equal(25, s.Map(5), 5);
    }

    [Fact]
    public void Sqrt_HalfExponent()
    {
        var s = PowScale.Sqrt().SetDomain(0, 100).SetRange(0, 10);
        Assert.Equal(5, s.Map(25), 5);
    }

    [Fact]
    public void Map_Linear_WhenExponentOne()
    {
        var s = new PowScale().SetExponent(1).SetDomain(0, 100).SetRange(0, 100);
        Assert.Equal(50, s.Map(50), 10);
    }
}

public class QuantizeScaleTests
{
    [Fact]
    public void Map_DividesIntoSegments()
    {
        var s = new QuantizeScale().SetDomain(0, 1).SetRange(0, 1);
        Assert.Equal(0, s.Map(0.25));
        Assert.Equal(1, s.Map(0.75));
    }

    [Fact]
    public void Map_ThreeSegments()
    {
        var s = new QuantizeScale().SetDomain(0, 1).SetRange(0, 1, 2);
        Assert.Equal(0, s.Map(0.1));
        Assert.Equal(1, s.Map(0.4));
        Assert.Equal(2, s.Map(0.8));
    }

    [Fact]
    public void InvertExtent_ReturnsInterval()
    {
        var s = new QuantizeScale().SetDomain(0, 1).SetRange(0, 1);
        var (x0, x1) = s.InvertExtent(0);
        Assert.Equal(0, x0);
        Assert.Equal(0.5, x1);
    }

    [Fact]
    public void Thresholds_Correct()
    {
        var s = new QuantizeScale().SetDomain(0, 1).SetRange(0, 1, 2);
        var t = s.Thresholds();
        Assert.Equal(2, t.Length);
        Assert.Equal(1.0 / 3, t[0], 10);
        Assert.Equal(2.0 / 3, t[1], 10);
    }
}

public class QuantileScaleTests
{
    [Fact]
    public void Map_SortsAndPartitions()
    {
        var s = new QuantileScale().SetDomain(3, 6, 7, 8, 8, 10, 13, 15, 16, 20).SetRange(0, 1, 2, 3);
        Assert.Equal(0, s.Map(3));
        Assert.Equal(3, s.Map(20));
    }

    [Fact]
    public void Quantiles_Correct()
    {
        var s = new QuantileScale().SetDomain(3, 6, 7, 8, 8, 10, 13, 15, 16, 20).SetRange(0, 1, 2, 3);
        var q = s.Quantiles();
        Assert.Equal(3, q.Length);
    }
}

public class ThresholdScaleTests
{
    [Fact]
    public void Map_DefaultThreshold()
    {
        var s = new ThresholdScale();
        Assert.Equal(0, s.Map(0));
        Assert.Equal(1, s.Map(0.5));
        Assert.Equal(1, s.Map(1));
    }

    [Fact]
    public void Map_CustomThresholds()
    {
        var s = new ThresholdScale().SetDomain(10, 20, 30).SetRange(0, 1, 2, 3);
        Assert.Equal(0, s.Map(5));
        Assert.Equal(1, s.Map(15));
        Assert.Equal(2, s.Map(25));
        Assert.Equal(3, s.Map(35));
    }
}
