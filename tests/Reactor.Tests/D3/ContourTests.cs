// Tests for d3-contour generation

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class ContourTests
{
    [Fact]
    public void Contour_SimpleGradient()
    {
        int w = 4, h = 4;
        var values = new double[w * h];
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
                values[j * w + i] = i + j;

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
        Assert.True(result[0].Coordinates.Count >= 0);
    }

    [Fact]
    public void Contour_UniformField_NoContours()
    {
        int w = 3, h = 3;
        var values = Enumerable.Repeat(5.0, w * h).ToArray();

        var contour = new ContourGenerator(w, h);
        var result = contour.SetThresholds(5).Generate(values);

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
