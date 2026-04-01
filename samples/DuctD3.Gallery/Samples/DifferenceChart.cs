using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

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
string? redPath = areaGreen.Generate(redPts);";

    private record struct Point(double X, double A, double B);

    public override FrameworkElement Render()
    {
        const double W = 700, H = 400;
        const double marginLeft = 55, marginTop = 25, marginRight = 20, marginBottom = 40;
        double plotW = W - marginLeft - marginRight;
        double plotH = H - marginTop - marginBottom;

        // Generate two crossing series over 24 data points (months)
        const int n = 24;
        var data = new Point[n];
        for (int i = 0; i < n; i++)
        {
            double t = i / (double)(n - 1);
            double revenue = 60 + 30 * Math.Sin(t * Math.PI * 2.2 + 0.3)
                               + 10 * Math.Cos(t * Math.PI * 4);
            double expenses = 55 + 20 * Math.Sin(t * Math.PI * 1.8 + 1.5)
                                + 15 * Math.Cos(t * Math.PI * 3 + 0.8);
            data[i] = new Point(i, revenue, expenses);
        }

        var (yMinA, yMaxA) = D3Extent.Extent(data, d => d.A);
        var (yMinB, yMaxB) = D3Extent.Extent(data, d => d.B);
        double yLo = Math.Min(yMinA, yMinB);
        double yHi = Math.Max(yMaxA, yMaxB);

        var xScale = new LinearScale([0, n - 1], [marginLeft, marginLeft + plotW]);
        var yScale = new LinearScale([yLo * 0.85, yHi * 1.1], [marginTop + plotH, marginTop]);
        yScale.Nice();

        var canvas = new Canvas { Width = W, Height = H };

        // Grid + axes
        G.DrawGrid(canvas, yScale, marginLeft, plotW);
        G.DrawAxes(canvas, xScale, yScale, marginLeft, marginTop, plotW, plotH);

        // Green fill: where A > B, show area from B up to A
        // We approximate by sampling and clamping
        var greenPts = new (double x, double y0, double y1)[n];
        var redPts = new (double x, double y0, double y1)[n];
        for (int i = 0; i < n; i++)
        {
            double a = data[i].A;
            double b = data[i].B;
            // Green region: baseline = max(a,b), top = a (visible only if a > b)
            greenPts[i] = (i, Math.Min(a, b), a);
            // Red region: baseline = max(a,b), top = b (visible only if b > a)
            redPts[i] = (i, Math.Min(a, b), b);
        }

        // Green area (Revenue > Expenses)
        var areaGreen = AreaGenerator.Create<(double x, double y0, double y1)>(
            d => xScale.Map(d.x),
            d => yScale.Map(d.y0),
            d => yScale.Map(d.y1));
        string? greenPath = areaGreen.Generate(greenPts);
        if (greenPath != null)
        {
            canvas.Children.Add(G.MakePath(greenPath,
                fill: G.Brush(D3Color.Parse("#2ca02c"), 0.35)));
        }

        // Red area (Expenses > Revenue)
        var areaRed = AreaGenerator.Create<(double x, double y0, double y1)>(
            d => xScale.Map(d.x),
            d => yScale.Map(d.y0),
            d => yScale.Map(d.y1));
        string? redPath = areaRed.Generate(redPts);
        if (redPath != null)
        {
            canvas.Children.Add(G.MakePath(redPath,
                fill: G.Brush(D3Color.Parse("#d62728"), 0.35)));
        }

        // Line for series A (Revenue)
        var lineA = LineGenerator.Create<Point>(d => xScale.Map(d.X), d => yScale.Map(d.A))
            .SetCurve(D3Curve.MonotoneX);
        string? lineAPath = lineA.Generate(data);
        if (lineAPath != null)
        {
            canvas.Children.Add(G.MakePath(lineAPath,
                stroke: G.Brush(D3Color.Parse("#2ca02c")), strokeWidth: 2));
        }

        // Line for series B (Expenses)
        var lineB = LineGenerator.Create<Point>(d => xScale.Map(d.X), d => yScale.Map(d.B))
            .SetCurve(D3Curve.MonotoneX);
        string? lineBPath = lineB.Generate(data);
        if (lineBPath != null)
        {
            canvas.Children.Add(G.MakePath(lineBPath,
                stroke: G.Brush(D3Color.Parse("#d62728")), strokeWidth: 2));
        }

        // Legend
        double lx = marginLeft + plotW - 130;
        G.AddRect(canvas, lx, marginTop + 6, 12, 12, G.Brush(D3Color.Parse("#2ca02c")));
        G.AddText(canvas, lx + 16, marginTop + 5, "Revenue", 11, G.Gray(60));
        G.AddRect(canvas, lx, marginTop + 24, 12, 12, G.Brush(D3Color.Parse("#d62728")));
        G.AddText(canvas, lx + 16, marginTop + 23, "Expenses", 11, G.Gray(60));

        // Title
        G.AddText(canvas, marginLeft, 4, "Difference Chart (Revenue vs Expenses)", 14, G.Gray(40));

        return canvas;
    }
}
