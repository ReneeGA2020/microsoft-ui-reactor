using Duct.D3;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public class StreamgraphChart : GallerySample
{
    public override string Title => "Streamgraph";
    public override string Description =>
        "A streamgraph (wiggle offset) with 4 layers of smooth data centered around " +
        "the horizontal midline. Uses StackGenerator then applies a manual centering " +
        "offset to produce the characteristic symmetric shape.";
    public override string Category => "Areas";

    public override string SourceCode => @"
// Stack normally, then center (wiggle-like offset)
StackSeries[] series = stack.Generate(rows);

// Compute total per column and shift to center
for (int j = 0; j < n; j++)
{
    double total = series[^1].Points[j].Y1;
    double offset = -total / 2;
    foreach (var s in series)
    {
        s.Points[j] = new StackPoint(
            s.Points[j].Y0 + offset,
            s.Points[j].Y1 + offset);
    }
}";

    public override FrameworkElement Render()
    {
        const double W = 700, H = 400;
        const double marginLeft = 50, marginTop = 30, marginRight = 20, marginBottom = 30;
        double plotW = W - marginLeft - marginRight;
        double plotH = H - marginTop - marginBottom;

        const int n = 30; // data points per layer
        string[] keys = ["Alpha", "Beta", "Gamma", "Delta"];

        // Generate smooth bumpy data for each layer
        var rows = new List<Dictionary<string, double>>();
        for (int j = 0; j < n; j++)
        {
            var row = new Dictionary<string, double>();
            double t = j / (double)(n - 1);
            row["Alpha"] = 12 + 18 * Math.Exp(-Math.Pow((t - 0.3) * 4, 2))
                             + 6 * Math.Sin(t * Math.PI * 3);
            row["Beta"]  = 10 + 14 * Math.Exp(-Math.Pow((t - 0.55) * 3.5, 2))
                             + 4 * Math.Cos(t * Math.PI * 2.5);
            row["Gamma"] = 8  + 20 * Math.Exp(-Math.Pow((t - 0.7) * 4, 2))
                             + 5 * Math.Sin(t * Math.PI * 4 + 1);
            row["Delta"] = 6  + 10 * Math.Exp(-Math.Pow((t - 0.45) * 3, 2))
                             + 7 * Math.Cos(t * Math.PI * 1.8 + 2);
            // Ensure positive
            foreach (var k in keys)
                if (row[k] < 1) row[k] = 1;
            rows.Add(row);
        }

        // Stack
        var stack = StackGenerator.Create<Dictionary<string, double>>()
            .SetKeys(keys)
            .SetValue((d, key) => d[key]);
        StackSeries[] series = stack.Generate(rows);

        // Center the stack (wiggle-like offset)
        for (int j = 0; j < n; j++)
        {
            double total = series[^1].Points[j].Y1;
            double offset = -total / 2.0;
            foreach (var s in series)
            {
                s.Points[j] = new StackPoint(
                    s.Points[j].Y0 + offset,
                    s.Points[j].Y1 + offset);
            }
        }

        // Compute Y extent across all layers
        double yMin = double.MaxValue, yMax = double.MinValue;
        foreach (var s in series)
        {
            foreach (var p in s.Points)
            {
                if (p.Y0 < yMin) yMin = p.Y0;
                if (p.Y1 > yMax) yMax = p.Y1;
            }
        }

        var xScale = new LinearScale([0, n - 1], [marginLeft, marginLeft + plotW]);
        var yScale = new LinearScale([yMin * 1.1, yMax * 1.1], [marginTop + plotH, marginTop]);

        var canvas = new Canvas { Width = W, Height = H };

        // Center line
        G.AddLine(canvas, marginLeft, yScale.Map(0), marginLeft + plotW, yScale.Map(0),
            G.Gray(200), 1);

        // Render each layer
        for (int si = 0; si < series.Length; si++)
        {
            var s = series[si];
            var pts = new (double x, double y0, double y1)[n];
            for (int j = 0; j < n; j++)
                pts[j] = (j, s.Points[j].Y0, s.Points[j].Y1);

            var area = AreaGenerator.Create<(double x, double y0, double y1)>(
                d => xScale.Map(d.x),
                d => yScale.Map(d.y0),
                d => yScale.Map(d.y1));
            string? path = area.Generate(pts);

            if (path != null)
            {
                canvas.Children.Add(G.MakePath(path,
                    fill: G.Brush(G.Palette[si], 0.8)));
            }
        }

        // Legend
        for (int k = 0; k < keys.Length; k++)
        {
            double lx = marginLeft + 10;
            double ly = marginTop + 4 + k * 18;
            G.AddRect(canvas, lx, ly, 12, 12, G.Brush(G.Palette[k], 0.8));
            G.AddText(canvas, lx + 16, ly - 1, keys[k], 11, G.Gray(60));
        }

        // Title
        G.AddText(canvas, marginLeft + 100, 6, "Streamgraph (Centered Stack)", 14, G.Gray(40));

        return canvas;
    }
}
