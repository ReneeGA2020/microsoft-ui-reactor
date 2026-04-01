using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class ScatterplotSample : GallerySample
{
    public override string Title => "Scatterplot";
    public override string Description => "A scatterplot of 50 random points with linear scales on both axes.";
    public override string Category => "Dots";

    public override string SourceCode => """
        // Generate 50 seeded random points
        var rng = new Random(42);
        var points = Enumerable.Range(0, 50)
            .Select(_ => (x: rng.NextDouble() * 100, y: rng.NextDouble() * 100))
            .ToArray();
        var xs = new LinearScale(new[] { 0.0, 100 }, new[] { left, left + pw });
        var ys = new LinearScale(new[] { 0.0, 100 }, new[] { top + ph, top });
        G.DrawGrid(canvas, ys, left, pw);
        G.DrawAxes(canvas, xs, ys, left, top, pw, ph);
        foreach (var p in points)
            G.AddEllipse(canvas, xs.Map(p.x), ys.Map(p.y), 4, fill, stroke);
        """;

    public override FrameworkElement Render()
    {
        const double width = 700, height = 400;
        const double left = 50, top = 30, right = 20, bottom = 30;
        double pw = width - left - right;
        double ph = height - top - bottom;

        var canvas = new Canvas { Width = width, Height = height };

        // Generate 50 seeded-random points
        var rng = new Random(42);
        var points = Enumerable.Range(0, 50)
            .Select(_ => (x: rng.NextDouble() * 100, y: rng.NextDouble() * 100))
            .ToArray();

        var xs = new LinearScale([0, 100], [left, left + pw]).Nice();
        var ys = new LinearScale([0, 100], [top + ph, top]).Nice();

        G.DrawGrid(canvas, ys, left, pw);
        G.DrawAxes(canvas, xs, ys, left, top, pw, ph);

        var fill = G.Brush(G.Palette[0], 0.6);
        var stroke = G.Brush(G.Palette[0]);

        foreach (var p in points)
        {
            G.AddEllipse(canvas, xs.Map(p.x), ys.Map(p.y), 4, fill, stroke);
        }

        G.AddText(canvas, left, 6, "Scatterplot (50 random points)", 14, G.Gray(40));

        return canvas;
    }
}
