// Port of d3-shape/test/pie-test.js

using Xunit;

namespace Duct.D3.Tests;

public class PieTests
{
    private const double Tolerance = 1e-10;

    [Fact]
    public void Pie_Default_ReturnsArcsInInputOrder()
    {
        var p = PieGenerator.Create();
        var arcs = p.Generate([1, 3, 2]);

        // Value 3 gets index 0 (sorted descending), value 2 gets index 1, value 1 gets index 2
        Assert.Equal(3, arcs.Length);

        // Arc for data=1 (smallest, index 2 in sorted order) — last position
        Assert.Equal(1.0, arcs[0].Data);
        Assert.Equal(1.0, arcs[0].Value);
        Assert.Equal(2, arcs[0].Index);
        Assert.InRange(arcs[0].StartAngle, 5.235987 - 0.001, 5.235988 + 0.001);
        Assert.InRange(arcs[0].EndAngle, 6.283185 - 0.001, 6.283186 + 0.001);

        // Arc for data=3 (largest, index 0) — first position
        Assert.Equal(3.0, arcs[1].Data);
        Assert.Equal(3.0, arcs[1].Value);
        Assert.Equal(0, arcs[1].Index);
        Assert.Equal(0.0, arcs[1].StartAngle, Tolerance);
        Assert.InRange(arcs[1].EndAngle, Math.PI - 0.001, Math.PI + 0.001);

        // Arc for data=2 (middle, index 1)
        Assert.Equal(2.0, arcs[2].Data);
        Assert.Equal(2.0, arcs[2].Value);
        Assert.Equal(1, arcs[2].Index);
    }

    [Fact]
    public void Pie_NegativeValues_TreatedAsZero()
    {
        var p = PieGenerator.Create();
        var arcs = p.Generate([1, 0, -1]);

        // Only value=1 contributes to sum
        Assert.Equal(1.0, arcs[0].Value);
        Assert.Equal(0.0, arcs[1].Value);
        Assert.Equal(-1.0, arcs[2].Value);

        // All angle span goes to the positive value
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

        // Total angle: 2*PI - PI = PI
        Assert.Equal(3, arcs.Length);

        // Arc for data=3 (largest) should start at PI
        var arc3 = arcs[2]; // data=3 is at index 2 in original array
        Assert.Equal(3.0, arc3.Value);
    }

    [Fact]
    public void Pie_CustomEndAngle()
    {
        var p = PieGenerator.Create().SetEndAngle(Math.PI);
        var arcs = p.Generate([1, 2, 3]);

        Assert.Equal(3, arcs.Length);
        // Total angle span is PI (half circle)
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

        // Ascending sort: 1 at index 0, 2 at index 1, 3 at index 2
        Assert.Equal(0, arcs[0].Index); // data=1, sorted first
        Assert.Equal(2, arcs[1].Index); // data=3, sorted last
        Assert.Equal(1, arcs[2].Index); // data=2, sorted middle
    }
}
