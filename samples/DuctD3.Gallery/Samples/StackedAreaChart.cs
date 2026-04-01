using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class StackedAreaChart : GallerySample
{
    public override string Title => "Stacked Area Chart";
    public override string Description =>
        "A stacked area chart showing 3 data series across 12 months. " +
        "Uses StackGenerator to compute cumulative baselines, then AreaGenerator " +
        "to render each layer with a distinct palette color.";
    public override string Category => "Areas";

    public override string SourceCode => @"
var stack = StackGenerator.Create<Dictionary<string, double>>()
    .SetKeys(keys)
    .SetValue((d, key) => d[key]);
StackSeries[] series = stack.Generate(months);

for (int si = 0; si < series.Length; si++)
{
    var s = series[si];
    var pts = new (double x, double y0, double y1)[s.Points.Length];
    for (int j = 0; j < s.Points.Length; j++)
        pts[j] = (j, s.Points[j].Y0, s.Points[j].Y1);

    var area = AreaGenerator.Create<(double x, double y0, double y1)>(
        d => xScale.Map(d.x),
        d => yScale.Map(d.y0),
        d => yScale.Map(d.y1));
    string? path = area.Generate(pts);
}";

    public override FrameworkElement Render()
    {
        const double W = 700, H = 400;
        const double marginLeft = 50, marginTop = 20, marginRight = 20, marginBottom = 40;
        double plotW = W - marginLeft - marginRight;
        double plotH = H - marginTop - marginBottom;

        string[] keys = ["Product", "Service", "Support"];
        string[] monthLabels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

        // Sample data: 3 series over 12 months
        var months = new List<Dictionary<string, double>>();
        double[] bases = [40, 25, 15];
        double[] growth = [2.5, 1.8, 1.0];
        for (int m = 0; m < 12; m++)
        {
            var row = new Dictionary<string, double>();
            for (int k = 0; k < keys.Length; k++)
            {
                double val = bases[k] + growth[k] * m
                    + 8 * Math.Sin((m + k * 2) * 0.7)
                    + 3 * Math.Cos((m + k) * 1.1);
                row[keys[k]] = Math.Max(val, 2);
            }
            months.Add(row);
        }

        // Stack
        var stack = StackGenerator.Create<Dictionary<string, double>>()
            .SetKeys(keys)
            .SetValue((d, key) => d[key]);
        StackSeries[] series = stack.Generate(months);

        // Compute max y
        double maxY = 0;
        foreach (var s in series)
            foreach (var p in s.Points)
                if (p.Y1 > maxY) maxY = p.Y1;

        var xScale = new LinearScale([0, 11], [marginLeft, marginLeft + plotW]);
        var yScale = new LinearScale([0, maxY * 1.1], [marginTop + plotH, marginTop]);
        yScale.Nice();

        var canvas = new Canvas { Width = W, Height = H };

        // Grid + axes
        G.DrawGrid(canvas, yScale, marginLeft, plotW);
        G.DrawAxes(canvas, xScale, yScale, marginLeft, marginTop, plotW, plotH);

        // Render each stacked layer
        for (int si = 0; si < series.Length; si++)
        {
            var s = series[si];
            var pts = new (double x, double y0, double y1)[s.Points.Length];
            for (int j = 0; j < s.Points.Length; j++)
                pts[j] = (j, s.Points[j].Y0, s.Points[j].Y1);

            var area = AreaGenerator.Create<(double x, double y0, double y1)>(
                d => xScale.Map(d.x),
                d => yScale.Map(d.y0),
                d => yScale.Map(d.y1));
            string? path = area.Generate(pts);

            if (path != null)
            {
                canvas.Children.Add(G.MakePath(path,
                    fill: G.Brush(G.Palette[si], 0.75)));
            }
        }

        // Month labels along X axis
        for (int m = 0; m < 12; m += 2)
        {
            G.AddText(canvas, xScale.Map(m) - 10, marginTop + plotH + 6,
                monthLabels[m], 9, G.Gray(120));
        }

        // Legend
        for (int k = 0; k < keys.Length; k++)
        {
            double lx = marginLeft + plotW - 100;
            double ly = marginTop + 8 + k * 18;
            G.AddRect(canvas, lx, ly, 12, 12, G.Brush(G.Palette[k], 0.75));
            G.AddText(canvas, lx + 16, ly - 1, keys[k], 11, G.Gray(60));
        }

        // Title
        G.AddText(canvas, marginLeft, 2, "Stacked Area Chart", 14, G.Gray(40));

        return canvas;
    }
}
