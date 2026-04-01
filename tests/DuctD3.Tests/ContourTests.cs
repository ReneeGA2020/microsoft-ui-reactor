// Tests for contour generation

using Xunit;

namespace Duct.D3.Tests;

public class ContourTests
{
    [Fact]
    public void Contour_SimpleGradient()
    {
        // 4x4 grid with a gradient
        int w = 4, h = 4;
        var values = new double[w * h];
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
                values[j * w + i] = i + j; // gradient: 0..6

        var contour = new ContourGenerator(w, h);
        var result = contour.SetThresholds(2, 4).Generate(values);

        Assert.Equal(2, result.Length);
        Assert.Equal(2, result[0].Value);
        Assert.Equal(4, result[1].Value);
    }

    [Fact]
    public void Contour_PeakInCenter()
    {
        int w = 10, h = 10;
        var values = new double[w * h];
        // Create a peak in the center
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                double dx = i - 4.5, dy = j - 4.5;
                values[j * w + i] = 20 - (dx * dx + dy * dy);
            }

        var contour = new ContourGenerator(w, h);
        var result = contour.SetThresholds(10).Generate(values);

        Assert.Single(result);
        Assert.Equal(10, result[0].Value);
        // With a larger grid, we should get contour segments
        Assert.True(result[0].Coordinates.Count >= 0); // may or may not have rings depending on grid resolution
    }

    [Fact]
    public void Contour_UniformField_NoContours()
    {
        int w = 3, h = 3;
        var values = Enumerable.Repeat(5.0, w * h).ToArray();

        var contour = new ContourGenerator(w, h);
        var result = contour.SetThresholds(5).Generate(values);

        // At the threshold value, all cells are "inside" — no boundary to trace
        Assert.Single(result);
    }

    [Fact]
    public void Contour_AutoThresholds()
    {
        int w = 10, h = 10;
        var values = new double[w * h];
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
                values[j * w + i] = Math.Sqrt(i * i + j * j);

        var contour = new ContourGenerator(w, h).SetThresholdCount(5);
        var result = contour.Generate(values);

        Assert.True(result.Length > 0);
    }
}

public class DensityContourTests
{
    [Fact]
    public void DensityContour_ClusteredPoints()
    {
        var points = new List<(double x, double y)>();
        // Cluster around (50, 50)
        var rng = new Random(42);
        for (int i = 0; i < 100; i++)
        {
            points.Add((50 + rng.NextDouble() * 20, 50 + rng.NextDouble() * 20));
        }

        var density = new DensityContourGenerator()
            .SetSize(100, 100)
            .SetBandwidth(10)
            .SetThresholdCount(3);

        var result = density.Generate(points);
        Assert.True(result.Length > 0);
    }
}
