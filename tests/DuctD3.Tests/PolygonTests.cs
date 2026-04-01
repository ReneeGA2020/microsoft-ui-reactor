// Port of d3-polygon tests

using Xunit;

namespace Duct.D3.Tests;

public class PolygonTests
{
    private const double Tol = 1e-6;

    [Fact]
    public void Area_Square()
    {
        var square = new (double x, double y)[]
        {
            (0, 0), (1, 0), (1, 1), (0, 1)
        };
        Assert.Equal(-1.0, D3Polygon.Area(square), Tol); // clockwise = negative
    }

    [Fact]
    public void Area_CounterClockwise()
    {
        var square = new (double x, double y)[]
        {
            (0, 0), (0, 1), (1, 1), (1, 0)
        };
        Assert.Equal(1.0, D3Polygon.Area(square), Tol);
    }

    [Fact]
    public void Centroid_Square()
    {
        var square = new (double x, double y)[]
        {
            (0, 0), (1, 0), (1, 1), (0, 1)
        };
        var (cx, cy) = D3Polygon.Centroid(square);
        Assert.Equal(0.5, cx, Tol);
        Assert.Equal(0.5, cy, Tol);
    }

    [Fact]
    public void Contains_Inside()
    {
        var triangle = new (double x, double y)[]
        {
            (0, 0), (10, 0), (5, 10)
        };
        Assert.True(D3Polygon.Contains(triangle, (5, 5)));
    }

    [Fact]
    public void Contains_Outside()
    {
        var triangle = new (double x, double y)[]
        {
            (0, 0), (10, 0), (5, 10)
        };
        Assert.False(D3Polygon.Contains(triangle, (20, 20)));
    }

    [Fact]
    public void Length_Square()
    {
        var square = new (double x, double y)[]
        {
            (0, 0), (1, 0), (1, 1), (0, 1)
        };
        Assert.Equal(4.0, D3Polygon.Length(square), Tol);
    }

    [Fact]
    public void Hull_Triangle()
    {
        var points = new (double x, double y)[]
        {
            (0, 0), (10, 0), (5, 10), (5, 3) // inner point
        };
        var hull = D3Polygon.Hull(points);
        Assert.NotNull(hull);
        Assert.Equal(3, hull.Length); // triangle hull, inner point excluded
    }

    [Fact]
    public void Hull_TooFewPoints()
    {
        Assert.Null(D3Polygon.Hull([(0, 0), (1, 1)]));
    }

    [Fact]
    public void Hull_Square()
    {
        var points = new (double x, double y)[]
        {
            (0, 0), (10, 0), (10, 10), (0, 10)
        };
        var hull = D3Polygon.Hull(points);
        Assert.NotNull(hull);
        Assert.Equal(4, hull.Length);
    }
}
