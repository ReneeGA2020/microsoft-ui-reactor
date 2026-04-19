// Port of d3-interpolate/test/number-test.js and round-test.js

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class InterpolateNumberTests
{
    [Fact]
    public void Number_Endpoints()
    {
        var i = D3Interpolate.Number(10, 42);
        Assert.Equal(10, i(0));
        Assert.Equal(42, i(1));
    }

    [Fact]
    public void Number_Midpoint()
    {
        var i = D3Interpolate.Number(0, 100);
        Assert.Equal(50, i(0.5));
    }

    [Fact]
    public void Number_Quarter_And_Three_Quarter()
    {
        var i = D3Interpolate.Number(0, 100);
        Assert.Equal(25, i(0.25));
        Assert.Equal(75, i(0.75));
    }

    [Fact]
    public void Number_Reverse_Direction()
    {
        var i = D3Interpolate.Number(100, 0);
        Assert.Equal(50, i(0.5));
    }

    [Fact]
    public void Number_Negative_Range()
    {
        var i = D3Interpolate.Number(-50, 50);
        Assert.Equal(0, i(0.5));
    }

    [Fact]
    public void Number_Out_Of_Range_Extrapolates()
    {
        var i = D3Interpolate.Number(0, 10);
        Assert.Equal(-10, i(-1));
        Assert.Equal(20, i(2));
    }

    [Fact]
    public void Number_Same_Endpoints_Returns_Same_Value()
    {
        var i = D3Interpolate.Number(7, 7);
        Assert.Equal(7, i(0));
        Assert.Equal(7, i(0.5));
        Assert.Equal(7, i(1));
    }
}

public class InterpolateRoundTests
{
    [Fact]
    public void Round_Snaps_To_Nearest_Integer()
    {
        var i = D3Interpolate.Round(0, 10);
        Assert.Equal(3, i(0.33));
        Assert.Equal(7, i(0.66));
    }

    [Fact]
    public void Round_Endpoints_Are_Exact()
    {
        var i = D3Interpolate.Round(10, 42);
        Assert.Equal(10, i(0));
        Assert.Equal(42, i(1));
    }

    [Fact]
    public void Round_Midpoint_Rounds_To_Nearest()
    {
        // (0 + 3)/2 = 1.5 → rounds to 2 (banker's rounding in .NET: 2).
        var i = D3Interpolate.Round(0, 3);
        Assert.Equal(2, i(0.5));
    }
}
