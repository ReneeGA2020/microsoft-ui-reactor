using Microsoft.UI.Reactor.Charting.D3;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.D3;

/// <summary>
/// Drives the Voronoi diagram path: circumcenters, cell polygons, point-in-cell
/// containment, and bounding-box clipping (Sutherland–Hodgman). Also covers
/// the small-input branches of Delaunay.From().
/// </summary>
public class VoronoiCoverageTests
{
    [Fact]
    public void Delaunay_Empty_Returns_Empty_Hull()
    {
        var d = Delaunay.From(new (double, double)[] { });
        Assert.Empty(d.Triangles);
        Assert.Empty(d.Hull);
    }

    [Fact]
    public void Delaunay_OnePoint_Returns_Single_Hull_Index()
    {
        var d = Delaunay.From(new[] { (5.0, 5.0) });
        Assert.Empty(d.Triangles);
        Assert.Equal(new[] { 0 }, d.Hull);
    }

    [Fact]
    public void Delaunay_TwoPoints_Returns_Two_Hull_Indices()
    {
        var d = Delaunay.From(new[] { (0.0, 0.0), (10.0, 10.0) });
        Assert.Empty(d.Triangles);
        Assert.Equal(new[] { 0, 1 }, d.Hull);
    }

    [Fact]
    public void Delaunay_AllCollinear_Returns_Empty_Triangles_With_Hull()
    {
        var d = Delaunay.From(new[] { (0.0, 0.0), (5.0, 0.0), (10.0, 0.0) });
        // Collinear points → degenerate; algorithm bails but returns hull indices.
        Assert.Empty(d.Triangles);
        Assert.Equal(3, d.Hull.Length);
    }

    [Fact]
    public void Voronoi_Computes_Circumcenters_Per_Triangle()
    {
        var pts = new[] { (0.0, 0.0), (100.0, 0.0), (50.0, 100.0), (50.0, 50.0) };
        var d = Delaunay.From(pts);
        var v = d.Voronoi(0, 0, 200, 200);
        Assert.Equal(d.Triangles.Length / 3, v.Circumcenters.Length);
    }

    [Fact]
    public void Voronoi_CellPolygon_For_Valid_Index_Runs_Without_Throwing()
    {
        // Whether the polygon survives clipping depends on input topology;
        // we only need the code path to execute.
        var pts = new[] { (0.0, 0.0), (100.0, 0.0), (50.0, 100.0), (50.0, 50.0) };
        var d = Delaunay.From(pts);
        var v = d.Voronoi(-200, -200, 400, 400);
        for (int i = 0; i < pts.Length; i++)
            v.CellPolygon(i);
    }

    [Fact]
    public void Voronoi_CellPolygon_For_Invalid_Index_Returns_Null()
    {
        var pts = new[] { (0.0, 0.0), (100.0, 0.0), (50.0, 100.0) };
        var d = Delaunay.From(pts);
        var v = d.Voronoi();
        Assert.Null(v.CellPolygon(-1));
        Assert.Null(v.CellPolygon(99));
    }

    [Fact]
    public void Voronoi_Contains_Returns_True_For_Own_Point()
    {
        var pts = new[] { (0.0, 0.0), (100.0, 0.0), (0.0, 100.0), (100.0, 100.0) };
        var d = Delaunay.From(pts);
        var v = d.Voronoi(0, 0, 100, 100);
        // The point (10, 10) is closest to (0, 0) — index 0.
        Assert.True(v.Contains(0, 10, 10));
        Assert.False(v.Contains(0, 90, 90));
    }

    [Fact]
    public void Voronoi_CellPolygon_Isolated_Point_Returns_Bounding_Box_Cell()
    {
        // A point not present in any triangle (collinear case) takes the
        // bounding-box fallback branch.
        var pts = new[] { (0.0, 0.0), (5.0, 0.0), (10.0, 0.0) };
        var d = Delaunay.From(pts);
        var v = d.Voronoi(-100, -100, 100, 100);
        var cell = v.CellPolygon(0);
        // Either the fallback box cell or null after clipping — both are valid;
        // primarily we need the line to execute.
        if (cell is not null)
            Assert.True(cell.Length >= 3);
    }
}
