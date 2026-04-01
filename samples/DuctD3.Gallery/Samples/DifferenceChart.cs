using Duct.Core;
using Duct.D3;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public class DifferenceChart : GallerySample
{
    public override string Title => "Difference Chart";
    public override string Description =>
        "A surplus/deficit difference chart showing two series (Revenue vs Expenses). " +
        "Green fill appears where Revenue exceeds Expenses, red where Expenses exceed Revenue. " +
        "Approximated with two clipped area regions.";
    public override string Category => "Areas";

    public override string SourceCode => @"
var greenPts = new (double x, double y0, double y1)[n];
var redPts = new (double x, double y0, double y1)[n];
for (int i = 0; i < n; i++)
{
    double a = data[i].A, b = data[i].B;
    greenPts[i] = (i, Math.Min(a, b), a);
    redPts[i] = (i, Math.Min(a, b), b);
}

var areaGreen = AreaGenerator.Create<(double x, double y0, double y1)>(
    d => xScale.Map(d.x),
    d => yScale.Map(d.y0),
    d => yScale.Map(d.y1));
string? greenPath = areaGreen.Generate(greenPts);
string? redPath = areaGreen.Generate(redPts);
D3Canvas(W, H, [..grid, ..axes, greenArea, redArea, lineA, lineB, ..legend, title]);";

    private record struct Point(double X, double A, double B);

    public override Element Render()
    {
        const double W = 700, H = 400;
        const double marginLeft = 55, marginTop = 25, marginRight = 20, marginBottom = 40;
        double plotW = W - marginLeft - marginRight;
        double plotH = H - marginTop - marginBottom;

        // Generate two crossing series over 24 data points (months)
        const int n = 24;
        var data = Enumerable.Range(0, n).Select(i =>
        {
            double t = i / (double)(n - 1);
            double revenue = 60 + 30 * Math.Sin(t * Math.PI * 2.2 + 0.3)
                               + 10 * Math.Cos(t * Math.PI * 4);
            double expenses = 55 + 20 * Math.Sin(t * Math.PI * 1.8 + 1.5)
                                + 15 * Math.Cos(t * Math.PI * 3 + 0.8);
            return new Point(i, revenue, expenses);
        }).ToArray();

        var (yMinA, yMaxA) = D3Extent.Extent(data, d => d.A);
        var (yMinB, yMaxB) = D3Extent.Extent(data, d => d.B);
        double yLo = Math.Min(yMinA, yMinB);
        double yHi = Math.Max(yMaxA, yMaxB);

        var xScale = new LinearScale([0, n - 1], [marginLeft, marginLeft + plotW]);
        var yScale = new LinearScale([yLo * 0.85, yHi * 1.1], [marginTop + plotH, marginTop]);
        yScale.Nice();

        // Grid + axes
        var grid = D3Grid(yScale, marginLeft, plotW);
        var axes = D3Axes(xScale, yScale, marginLeft, marginTop, plotW, plotH);

        // Green fill: where A > B, show area from B up to A
        var greenPts = data.Select((d, i) => (x: (double)i, y0: Math.Min(d.A, d.B), y1: d.A)).ToArray();
        var redPts = data.Select((d, i) => (x: (double)i, y0: Math.Min(d.A, d.B), y1: d.B)).ToArray();

        // Green area (Revenue > Expenses)
        var areaGreen = AreaGenerator.Create<(double x, double y0, double y1)>(
            d => xScale.Map(d.x),
            d => yScale.Map(d.y0),
            d => yScale.Map(d.y1));
        string? greenPath = areaGreen.Generate(greenPts);

        // Red area (Expenses > Revenue)
        var areaRed = AreaGenerator.Create<(double x, double y0, double y1)>(
            d => xScale.Map(d.x),
            d => yScale.Map(d.y0),
            d => yScale.Map(d.y1));
        string? redPath = areaRed.Generate(redPts);

        // Line for series A (Revenue)
        var lineA = LineGenerator.Create<Point>(d => xScale.Map(d.X), d => yScale.Map(d.A))
            .SetCurve(D3Curve.MonotoneX);
        string? lineAPath = lineA.Generate(data);

        // Line for series B (Expenses)
        var lineB = LineGenerator.Create<Point>(d => xScale.Map(d.X), d => yScale.Map(d.B))
            .SetCurve(D3Curve.MonotoneX);
        string? lineBPath = lineB.Generate(data);

        // Legend
        double lx = marginLeft + plotW - 130;

        return D3Canvas(W, H,
        [
            .. grid,
            .. axes,
            D3Path(greenPath, fill: Brush(D3Color.Parse("#2ca02c"), opacity: 0.35)),
            D3Path(redPath, fill: Brush(D3Color.Parse("#d62728"), opacity: 0.35)),
            D3Path(lineAPath, stroke: Brush(D3Color.Parse("#2ca02c")), strokeWidth: 2),
            D3Path(lineBPath, stroke: Brush(D3Color.Parse("#d62728")), strokeWidth: 2),
            .. D3Legend(lx, marginTop + 6, [("Revenue", Brush(D3Color.Parse("#2ca02c"))), ("Expenses", Brush(D3Color.Parse("#d62728")))]),
            D3Text(marginLeft, 4, "Difference Chart (Revenue vs Expenses)", 14, Gray(40)),
        ]);
    }
}
