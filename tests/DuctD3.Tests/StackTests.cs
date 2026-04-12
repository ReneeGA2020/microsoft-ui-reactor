using Xunit;

namespace Duct.D3.Tests;

public class StackTests
{
    private const double Tolerance = 1e-10;

    // ── Empty data ─────────────────────────────────────────────────────

    [Fact]
    public void Generate_EmptyData_ReturnsEmptyPoints()
    {
        var stack = new StackGenerator<Dictionary<string, double>>()
            .SetKeys("a", "b")
            .SetValue((d, key) => d[key]);

        var series = stack.Generate([]);
        Assert.Equal(2, series.Length);
        Assert.Empty(series[0].Points);
        Assert.Empty(series[1].Points);
    }

    // ── Single series ──────────────────────────────────────────────────

    [Fact]
    public void Generate_SingleSeries_StartsAtZero()
    {
        var data = new List<Dictionary<string, double>>
        {
            new() { ["x"] = 10 },
            new() { ["x"] = 20 },
        };

        var stack = new StackGenerator<Dictionary<string, double>>()
            .SetKeys("x")
            .SetValue((d, key) => d[key]);

        var series = stack.Generate(data);
        Assert.Single(series);
        Assert.Equal("x", series[0].Key);
        Assert.Equal(0.0, series[0].Points[0].Y0, Tolerance);
        Assert.Equal(10.0, series[0].Points[0].Y1, Tolerance);
        Assert.Equal(0.0, series[0].Points[1].Y0, Tolerance);
        Assert.Equal(20.0, series[0].Points[1].Y1, Tolerance);
    }

    // ── Two series stacked ─────────────────────────────────────────────

    [Fact]
    public void Generate_TwoSeries_SecondStartsWhereFirstEnds()
    {
        var data = new List<Dictionary<string, double>>
        {
            new() { ["a"] = 5, ["b"] = 3 },
            new() { ["a"] = 10, ["b"] = 7 },
        };

        var stack = new StackGenerator<Dictionary<string, double>>()
            .SetKeys("a", "b")
            .SetValue((d, key) => d[key]);

        var series = stack.Generate(data);
        Assert.Equal(2, series.Length);

        // First series: [0,5] and [0,10]
        Assert.Equal(0.0, series[0].Points[0].Y0, Tolerance);
        Assert.Equal(5.0, series[0].Points[0].Y1, Tolerance);
        Assert.Equal(0.0, series[0].Points[1].Y0, Tolerance);
        Assert.Equal(10.0, series[0].Points[1].Y1, Tolerance);

        // Second series: [5,8] and [10,17]
        Assert.Equal(5.0, series[1].Points[0].Y0, Tolerance);
        Assert.Equal(8.0, series[1].Points[0].Y1, Tolerance);
        Assert.Equal(10.0, series[1].Points[1].Y0, Tolerance);
        Assert.Equal(17.0, series[1].Points[1].Y1, Tolerance);
    }

    // ── Three series ───────────────────────────────────────────────────

    [Fact]
    public void Generate_ThreeSeries_StacksCorrectly()
    {
        var data = new List<Dictionary<string, double>>
        {
            new() { ["x"] = 1, ["y"] = 2, ["z"] = 3 },
        };

        var stack = new StackGenerator<Dictionary<string, double>>()
            .SetKeys("x", "y", "z")
            .SetValue((d, key) => d[key]);

        var series = stack.Generate(data);
        Assert.Equal(3, series.Length);

        // x: [0, 1]
        Assert.Equal(0.0, series[0].Points[0].Y0, Tolerance);
        Assert.Equal(1.0, series[0].Points[0].Y1, Tolerance);

        // y: [1, 3]
        Assert.Equal(1.0, series[1].Points[0].Y0, Tolerance);
        Assert.Equal(3.0, series[1].Points[0].Y1, Tolerance);

        // z: [3, 6]
        Assert.Equal(3.0, series[2].Points[0].Y0, Tolerance);
        Assert.Equal(6.0, series[2].Points[0].Y1, Tolerance);
    }

    // ── NaN handling in prev ───────────────────────────────────────────

    [Fact]
    public void Generate_NaNInPreviousSeries_FallsBackToY0()
    {
        var data = new List<Dictionary<string, double>>
        {
            new() { ["a"] = double.NaN, ["b"] = 5 },
        };

        var stack = new StackGenerator<Dictionary<string, double>>()
            .SetKeys("a", "b")
            .SetValue((d, key) => d[key]);

        var series = stack.Generate(data);

        // series[0]: Y0=0, Y1=NaN
        Assert.Equal(0.0, series[0].Points[0].Y0, Tolerance);
        Assert.True(double.IsNaN(series[0].Points[0].Y1));

        // When prev Y1 is NaN, code uses prev Y0 (which is 0)
        Assert.Equal(0.0, series[1].Points[0].Y0, Tolerance);
        Assert.Equal(5.0, series[1].Points[0].Y1, Tolerance);
    }

    // ── Factory method ─────────────────────────────────────────────────

    [Fact]
    public void Create_ReturnsNewInstance()
    {
        var stack = StackGenerator.Create<Dictionary<string, double>>();
        Assert.NotNull(stack);
        Assert.IsType<StackGenerator<Dictionary<string, double>>>(stack);
    }
}
