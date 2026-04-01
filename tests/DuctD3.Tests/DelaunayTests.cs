// Tests for Delaunay triangulation and Voronoi diagrams

using Xunit;

namespace Duct.D3.Tests;

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
        Assert.Equal(0, d.Find(1, 1));       // closest to (0,0)
        Assert.Equal(3, d.Find(99, 99));     // closest to (100,100)
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
        Assert.True(d.Hull.Length >= 4); // At least the 4 corners
    }

    [Fact]
    public void Delaunay_TwoPoints()
    {
        var points = new (double x, double y)[] { (0, 0), (1, 1) };
        var d = Delaunay.From(points);
        Assert.Equal(2, d.Points.Length);
        // Can't triangulate 2 points, but should not throw
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

        // Even with 2 points (degenerate triangulation), should handle gracefully
        // The cell may be null or a simple polygon
        var cell = v.CellPolygon(0);
        // Don't assert non-null — 2 points is degenerate for Delaunay
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

        // Point (10, 50) should be in cell 0 (closer to 25,50)
        Assert.True(v.Contains(0, 10, 50));
        // Point (90, 50) should be in cell 1 (closer to 75,50)
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

        // The simplified triangulation should still enable contains queries
        Assert.True(v.Contains(0, 10, 10)); // Closest to point 0
        Assert.True(v.Contains(1, 90, 10)); // Closest to point 1
        Assert.True(v.Contains(2, 50, 90)); // Closest to point 2
    }
}
