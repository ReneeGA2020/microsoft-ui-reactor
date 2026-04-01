using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class DotPlotSample : GallerySample
{
    public override string Title => "Dot Plot";
    public override string Description => "A strip chart (dot plot) showing values for five categories along a shared x axis, with multiple dots per category.";
    public override string Category => "Dots";

    public override string SourceCode => """
        var categories = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };
        // Each category has several numeric values
        var rng = new Random(7);
        var data = categories.SelectMany((cat, ci) =>
            Enumerable.Range(0, 6 + ci).Select(_ =>
                (cat, value: 20 + rng.NextDouble() * 60 + ci * 5))
        ).ToArray();
        var xs = new LinearScale([0, 100], [left, left+pw]).Nice();
        // Map categories to evenly spaced y positions
        foreach (var (cat, value) in data)
            G.AddEllipse(canvas, xs.Map(value), yPos, 4, fill, stroke);
        """;

    public override FrameworkElement Render()
    {
        const double width = 700, height = 400;
        const double left = 80, top = 30, right = 20, bottom = 30;
        double pw = width - left - right;
        double ph = height - top - bottom;

        var canvas = new Canvas { Width = width, Height = height };

        var categories = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };

        var rng = new Random(7);
        var data = categories.SelectMany((cat, ci) =>
            Enumerable.Range(0, 6 + ci).Select(_ =>
                (cat, value: 20 + rng.NextDouble() * 60 + ci * 5))
        ).ToArray();

        var xs = new LinearScale([0, 100], [left, left + pw]).Nice();

        // Draw x axis
        double axisY = top + ph;
        G.AddLine(canvas, left, axisY, left + pw, axisY, G.Gray(100, 180));
        foreach (var t in xs.Ticks(6))
            G.AddText(canvas, xs.Map(t) - 12, axisY + 4, G.Fmt(t), 10, G.Gray(100));

        // Draw horizontal grid lines and category rows
        double rowHeight = ph / categories.Length;

        for (int ci = 0; ci < categories.Length; ci++)
        {
            double rowY = top + ci * rowHeight + rowHeight / 2;

            // Category label
            G.AddText(canvas, 4, rowY - 7, categories[ci], 11, G.Gray(60));

            // Horizontal guide
            G.AddLine(canvas, left, rowY, left + pw, rowY, G.Gray(128, 30));

            // Dots for this category
            var fill = G.Brush(G.Palette[ci % G.Palette.Length], 0.6);
            var stroke = G.Brush(G.Palette[ci % G.Palette.Length]);

            foreach (var (cat, value) in data)
            {
                if (cat != categories[ci]) continue;
                G.AddEllipse(canvas, xs.Map(value), rowY, 5, fill, stroke);
            }
        }

        G.AddText(canvas, left, 6, "Dot Plot (strip chart)", 14, G.Gray(40));

        return canvas;
    }
}
