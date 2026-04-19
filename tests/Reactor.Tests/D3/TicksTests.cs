// Port of d3-array/ticks tests

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class TicksTests
{
    [Fact]
    public void Ticks_ZeroCount_ReturnsEmpty()
    {
        Assert.Empty(D3Ticks.Ticks(0, 1, 0));
        Assert.Empty(D3Ticks.Ticks(0, 1, -1));
    }

    [Fact]
    public void Ticks_EqualStartStop_ReturnsSingleValue()
    {
        Assert.Equal([5.0], D3Ticks.Ticks(5, 5, 10));
    }

    [Fact]
    public void Ticks_0_1_10()
    {
        var ticks = D3Ticks.Ticks(0, 1, 10);
        Assert.Equal(11, ticks.Length);
        Assert.Equal(0.0, ticks[0]);
        Assert.Equal(1.0, ticks[^1]);
    }

    [Fact]
    public void Ticks_0_1_5()
    {
        var ticks = D3Ticks.Ticks(0, 1, 5);
        Assert.Equal(new[] { 0.0, 0.2, 0.4, 0.6, 0.8, 1.0 }, ticks);
    }

    [Fact]
    public void Ticks_Neg100_100_10()
    {
        var ticks = D3Ticks.Ticks(-100, 100, 10);
        Assert.Equal(new double[] { -100, -80, -60, -40, -20, 0, 20, 40, 60, 80, 100 }, ticks);
    }

    [Fact]
    public void Ticks_Neg100_100_2()
    {
        Assert.Equal(new double[] { -100, 0, 100 }, D3Ticks.Ticks(-100, 100, 2));
    }

    [Fact]
    public void Ticks_Reverse()
    {
        var ticks = D3Ticks.Ticks(1, 0, 10);
        Assert.Equal(0.0, ticks[^1]);
        Assert.Equal(1.0, ticks[0]);
    }

    [Fact]
    public void TickStep_Positive()
    {
        double step = D3Ticks.TickStep(0, 1, 10);
        Assert.Equal(0.1, step);
    }

    [Fact]
    public void TickStep_Large()
    {
        double step = D3Ticks.TickStep(0, 100, 5);
        Assert.Equal(20.0, step, 5);
    }
}
