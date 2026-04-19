// Tests for d3-interpolate color/date/array interpolation

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class InterpolateColorTests
{
    [Fact]
    public void Rgb_InterpolatesChannels()
    {
        var interp = D3InterpolateColor.Rgb(
            new D3Color(0, 0, 0),
            new D3Color(255, 255, 255));

        var mid = interp(0.5);
        Assert.Equal(128, mid.R);
        Assert.Equal(128, mid.G);
        Assert.Equal(128, mid.B);
    }

    [Fact]
    public void Rgb_Endpoints()
    {
        var a = new D3Color(255, 0, 0);
        var b = new D3Color(0, 0, 255);
        var interp = D3InterpolateColor.Rgb(a, b);

        var start = interp(0);
        Assert.Equal(255, start.R);
        Assert.Equal(0, start.B);

        var end = interp(1);
        Assert.Equal(0, end.R);
        Assert.Equal(255, end.B);
    }

    [Fact]
    public void Rgb_FromStrings()
    {
        var interp = D3InterpolateColor.Rgb("#ff0000", "#0000ff");
        var mid = interp(0.5);
        Assert.Equal(128, mid.R);
        Assert.Equal(0, mid.G);
        Assert.Equal(128, mid.B);
    }

    [Fact]
    public void Hsl_InterpolatesHue()
    {
        var interp = D3InterpolateColor.Hsl(
            new D3Color(255, 0, 0),
            new D3Color(0, 0, 255));

        var mid = interp(0.5);
        Assert.False(mid.R == 255 && mid.G == 0 && mid.B == 0);
        Assert.False(mid.R == 0 && mid.G == 0 && mid.B == 255);
    }

    [Fact]
    public void Date_InterpolatesTime()
    {
        var a = new DateTime(2020, 1, 1);
        var b = new DateTime(2020, 12, 31);
        var interp = D3InterpolateColor.Date(a, b);

        Assert.Equal(a, interp(0));
        Assert.Equal(b, interp(1));
        var mid = interp(0.5);
        Assert.True(mid > a);
        Assert.True(mid < b);
    }

    [Fact]
    public void Array_InterpolatesElements()
    {
        var interp = D3InterpolateColor.Array([0, 10], [10, 20]);
        var mid = interp(0.5);
        Assert.Equal(5, mid[0]);
        Assert.Equal(15, mid[1]);
    }

    [Fact]
    public void Array_DifferentLengths()
    {
        var interp = D3InterpolateColor.Array([0], [10, 20]);
        var mid = interp(0.5);
        Assert.Equal(2, mid.Length);
        Assert.Equal(5, mid[0]);
        Assert.Equal(10, mid[1]);
    }
}
