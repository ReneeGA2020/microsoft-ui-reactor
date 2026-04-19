// Port of d3-shape/test/pie-test.js

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class PieTests
{
    private const double Tolerance = 1e-10;

    [Fact]
    public void Pie_Default_ReturnsArcsInInputOrder()
    {
        var p = PieGenerator.Create();
        var arcs = p.Generate([1, 3, 2]);

        Assert.Equal(3, arcs.Length);

        Assert.Equal(1.0, arcs[0].Data);
        Assert.Equal(1.0, arcs[0].Value);
        Assert.Equal(2, arcs[0].Index);
        Assert.InRange(arcs[0].StartAngle, 5.235987 - 0.001, 5.235988 + 0.001);
        Assert.InRange(arcs[0].EndAngle, 6.283185 - 0.001, 6.283186 + 0.001);

        Assert.Equal(3.0, arcs[1].Data);
        Assert.Equal(3.0, arcs[1].Value);
        Assert.Equal(0, arcs[1].Index);
        Assert.Equal(0.0, arcs[1].StartAngle, Tolerance);
        Assert.InRange(arcs[1].EndAngle, Math.PI - 0.001, Math.PI + 0.001);

        Assert.Equal(2.0, arcs[2].Data);
        Assert.Equal(2.0, arcs[2].Value);
        Assert.Equal(1, arcs[2].Index);
    }

    [Fact]
    public void Pie_NegativeValues_TreatedAsZero()
    {
        var p = PieGenerator.Create();
        var arcs = p.Generate([1, 0, -1]);

        Assert.Equal(1.0, arcs[0].Value);
        Assert.Equal(0.0, arcs[1].Value);
        Assert.Equal(-1.0, arcs[2].Value);

        double fullCircle = 2 * Math.PI;
        Assert.Equal(0.0, arcs[0].StartAngle, Tolerance);
        Assert.Equal(fullCircle, arcs[0].EndAngle, Tolerance);
    }

    [Fact]
    public void Pie_AllZeros_AllAtStartAngle()
    {
        var p = PieGenerator.Create();
        var arcs = p.Generate([0.0, 0.0]);

        Assert.Equal(0.0, arcs[0].StartAngle);
        Assert.Equal(0.0, arcs[0].EndAngle);
        Assert.Equal(0.0, arcs[1].StartAngle);
        Assert.Equal(0.0, arcs[1].EndAngle);
    }

    [Fact]
    public void Pie_CustomStartAngle()
    {
        var p = PieGenerator.Create().SetStartAngle(Math.PI);
        var arcs = p.Generate([1, 2, 3]);

        Assert.Equal(3, arcs.Length);

        var arc3 = arcs[2];
        Assert.Equal(3.0, arc3.Value);
    }

    [Fact]
    public void Pie_CustomEndAngle()
    {
        var p = PieGenerator.Create().SetEndAngle(Math.PI);
        var arcs = p.Generate([1, 2, 3]);

        Assert.Equal(3, arcs.Length);
    }

    [Fact]
    public void Pie_PadAngle()
    {
        var p = PieGenerator.Create().SetPadAngle(0.1);
        var arcs = p.Generate([1, 2, 3]);

        Assert.Equal(3, arcs.Length);
        Assert.Equal(0.1, arcs[0].PadAngle);
        Assert.Equal(0.1, arcs[1].PadAngle);
        Assert.Equal(0.1, arcs[2].PadAngle);
    }

    [Fact]
    public void Pie_SortValuesAscending()
    {
        var p = PieGenerator.Create().SetSortValues((a, b) => a.CompareTo(b));
        var arcs = p.Generate([1, 3, 2]);

        Assert.Equal(0, arcs[0].Index);
        Assert.Equal(2, arcs[1].Index);
        Assert.Equal(1, arcs[2].Index);
    }
}
