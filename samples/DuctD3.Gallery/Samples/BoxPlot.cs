using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class BoxPlotSample : GallerySample
{
    public override string Title => "Box Plot";
    public override string Description => "A box plot for four groups showing min, Q1, median, Q3, and max, computed with D3Statistics.QuantileSorted.";
    public override string Category => "Analysis";

    public override string SourceCode => """
        var ys = new LinearScale([yMin, yMax], [top + ph, top]).Nice();
        G.DrawGrid(canvas, ys, left, pw);

        for (int g = 0; g < groups.Length; g++)
        {
            var sorted = groupData[g];
            double min = sorted[0], max = sorted[^1];
            double q1 = D3Statistics.QuantileSorted(sorted, 0.25);
            double median = D3Statistics.QuantileSorted(sorted, 0.5);
            double q3 = D3Statistics.QuantileSorted(sorted, 0.75);

            double cx = left + g * groupWidth + groupWidth / 2;
            // Whiskers, caps, box (Q1-Q3), and median line
            G.AddLine(canvas, cx, ys.Map(min), cx, ys.Map(q1), stroke, 1.5);
            G.AddLine(canvas, cx, ys.Map(q3), cx, ys.Map(max), stroke, 1.5);
            G.AddRect(canvas, bx, ys.Map(q3), boxWidth,
                ys.Map(q1) - ys.Map(q3), fill, 2);
            G.AddLine(canvas, bx, ys.Map(median), bx + boxWidth,
                ys.Map(median), stroke, 2.5);
        }
        """;

    public override FrameworkElement Render()
    {
        const double width = 700, height = 400;
        const double left = 50, top = 30, right = 20, bottom = 40;
        double pw = width - left - right;
        double ph = height - top - bottom;

        var canvas = new Canvas { Width = width, Height = height };

        var groups = new[] { "A", "B", "C", "D" };
        var rng = new Random(55);

        // Generate data per group with different distributions
        double[][] groupData = new double[groups.Length][];
        for (int g = 0; g < groups.Length; g++)
        {
            double center = 30 + g * 15;
            double spread = 8 + g * 3;
            groupData[g] = Enumerable.Range(0, 40).Select(_ =>
            {
                double u1 = 1.0 - rng.NextDouble();
                double u2 = rng.NextDouble();
                return center + spread * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
            }).OrderBy(v => v).ToArray();
        }

        // Find overall extent for y scale
        double yMin = groupData.Min(d => d[0]);
        double yMax = groupData.Max(d => d[^1]);
        var ys = new LinearScale([yMin, yMax], [top + ph, top]).Nice();

        G.DrawGrid(canvas, ys, left, pw);

        // Y axis
        G.AddLine(canvas, left, top, left, top + ph, G.Gray(100, 180));
        foreach (var t in ys.Ticks(6))
            G.AddText(canvas, 0, ys.Map(t) - 7, G.Fmt(t), 10, G.Gray(100), TextAlignment.Right, left - 6);

        double groupWidth = pw / groups.Length;
        double boxWidth = groupWidth * 0.5;

        for (int g = 0; g < groups.Length; g++)
        {
            var sorted = groupData[g];
            double min = sorted[0];
            double max = sorted[^1];
            double q1 = D3Statistics.QuantileSorted(sorted, 0.25);
            double median = D3Statistics.QuantileSorted(sorted, 0.5);
            double q3 = D3Statistics.QuantileSorted(sorted, 0.75);

            double cx = left + g * groupWidth + groupWidth / 2;
            double bx = cx - boxWidth / 2;

            var color = G.Palette[g % G.Palette.Length];
            var fill = G.Brush(color, 0.35);
            var stroke = G.Brush(color);

            // Whisker: min to Q1
            G.AddLine(canvas, cx, ys.Map(min), cx, ys.Map(q1), stroke, 1.5);
            // Whisker: Q3 to max
            G.AddLine(canvas, cx, ys.Map(q3), cx, ys.Map(max), stroke, 1.5);

            // Min cap
            G.AddLine(canvas, cx - boxWidth * 0.3, ys.Map(min), cx + boxWidth * 0.3, ys.Map(min), stroke, 1.5);
            // Max cap
            G.AddLine(canvas, cx - boxWidth * 0.3, ys.Map(max), cx + boxWidth * 0.3, ys.Map(max), stroke, 1.5);

            // Box: Q1 to Q3
            double boxY = ys.Map(q3);
            double boxH = ys.Map(q1) - ys.Map(q3);
            G.AddRect(canvas, bx, boxY, boxWidth, boxH, fill, 2);

            // Box outline (left, right, top, bottom)
            G.AddLine(canvas, bx, boxY, bx + boxWidth, boxY, stroke, 1.5);
            G.AddLine(canvas, bx, boxY + boxH, bx + boxWidth, boxY + boxH, stroke, 1.5);
            G.AddLine(canvas, bx, boxY, bx, boxY + boxH, stroke, 1.5);
            G.AddLine(canvas, bx + boxWidth, boxY, bx + boxWidth, boxY + boxH, stroke, 1.5);

            // Median line
            G.AddLine(canvas, bx, ys.Map(median), bx + boxWidth, ys.Map(median), stroke, 2.5);

            // Group label
            G.AddText(canvas, cx - 8, top + ph + 8, $"Group {groups[g]}", 11, G.Gray(60));
        }

        G.AddText(canvas, left, 6, "Box Plot (four groups)", 14, G.Gray(40));

        return canvas;
    }
}
