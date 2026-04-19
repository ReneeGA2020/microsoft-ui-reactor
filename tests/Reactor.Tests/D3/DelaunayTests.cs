// Tests for d3-delaunay triangulation and Voronoi diagrams

using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

public class DelaunayTests
{
    [Fact]
    public void Delaunay_TriangulatesPoints()
    {
        var points = new (double x, double y)[]
        {
            (0, 0), (100, 0), (50, 100), (50, 50)
        };

        var d = Delaunay.From(points);
        Assert.Equal(4, d.Points.Length);
        Assert.True(d.Triangles.Length > 0);
        Assert.True(d.Triangles.Length % 3 == 0);
    }

    [Fact]
    public void Delaunay_Find_ClosestPoint()
    {
        var points = new (double x, double y)[]
        {
            (0, 0), (100, 0), (0, 100), (100, 100)
        };

        var d = Delaunay.From(points);
        Assert.Equal(0, d.Find(1, 1));
        Assert.Equal(3, d.Find(99, 99));
    }

    [Fact]
    public void Delaunay_Neighbors()
    {
        var points = new (double x, double y)[]
        {
            (0, 0), (100, 0), (50, 100)
        };

        var d = Delaunay.From(points);
        var neighbors = d.Neighbors(0).ToList();
        Assert.True(neighbors.Count > 0);
        Assert.Contains(1, neighbors);
        Assert.Contains(2, neighbors);
    }

    [Fact]
    public void Delaunay_Hull()
    {
        var points = new (double x, double y)[]
        {
            (0, 0), (100, 0), (100, 100), (0, 100), (50, 50)
        };

        var d = Delaunay.From(points);
        Assert.True(d.Hull.Length >= 4);
    }

    [Fact]
    public void Delaunay_TwoPoints()
    {
        var points = new (double x, double y)[] { (0, 0), (1, 1) };
        var d = Delaunay.From(points);
        Assert.Equal(2, d.Points.Length);
    }

    [Fact]
    public void Delaunay_SinglePoint()
    {
        var points = new (double x, double y)[] { (5, 5) };
        var d = Delaunay.From(points);
        Assert.Single(d.Points);
    }
}

public class VoronoiTests
{
    [Fact]
    public void Voronoi_CreatesFromDelaunay()
    {
        var points = new (double x, double y)[]
        {
            (10, 10), (90, 10), (50, 90)
        };

        var d = Delaunay.From(points);
        var v = d.Voronoi(0, 0, 100, 100);

        Assert.NotNull(v);
        Assert.True(v.Circumcenters.Length > 0);
    }

    [Fact]
    public void Voronoi_CellPolygon()
    {
        var points = new (double x, double y)[]
        {
            (25, 50), (75, 50)
        };

        var d = Delaunay.From(points);
        var v = d.Voronoi(0, 0, 100, 100);

        var cell = v.CellPolygon(0);
    }

    [Fact]
    public void Voronoi_Contains()
    {
        var points = new (double x, double y)[]
        {
            (25, 50), (75, 50)
        };

        var d = Delaunay.From(points);
        var v = d.Voronoi(0, 0, 100, 100);

        Assert.True(v.Contains(0, 10, 50));
        Assert.True(v.Contains(1, 90, 50));
    }

    [Fact]
    public void Voronoi_ThreePoints()
    {
        var points = new (double x, double y)[]
        {
            (10, 10), (90, 10), (50, 90)
        };

        var d = Delaunay.From(points);
        var v = d.Voronoi(0, 0, 100, 100);

        Assert.True(v.Contains(0, 10, 10));
        Assert.True(v.Contains(1, 90, 10));
        Assert.True(v.Contains(2, 50, 90));
    }
}
