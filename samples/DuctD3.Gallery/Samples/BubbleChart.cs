using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class BubbleChartSample : GallerySample
{
    public override string Title => "Bubble Chart";
    public override string Description => "A bubble chart where circle size encodes a third variable, showing 30 data points with x, y, and size dimensions.";
    public override string Category => "Dots";

    public override string SourceCode => """
        var rng = new Random(123);
        var points = Enumerable.Range(0, 30)
            .Select(_ => (x: rng.NextDouble()*100, y: rng.NextDouble()*100,
                          size: 5 + rng.NextDouble()*40))
            .ToArray();
        var xs = new LinearScale([0, 100], [left, left+pw]).Nice();
        var ys = new LinearScale([0, 100], [top+ph, top]).Nice();
        var rs = new LinearScale([5, 45], [3, 20]);
        foreach (var p in points)
            G.AddEllipse(canvas, xs.Map(p.x), ys.Map(p.y), rs.Map(p.size), fill, stroke);
        """;

    public override FrameworkElement Render()
    {
        const double width = 700, height = 400;
        const double left = 50, top = 30, right = 20, bottom = 30;
        double pw = width - left - right;
        double ph = height - top - bottom;

        var canvas = new Canvas { Width = width, Height = height };

        var rng = new Random(123);
        var points = Enumerable.Range(0, 30)
            .Select(_ => (x: rng.NextDouble() * 100, y: rng.NextDouble() * 100,
                          size: 5 + rng.NextDouble() * 40))
            .ToArray();

        var xs = new LinearScale([0, 100], [left, left + pw]).Nice();
        var ys = new LinearScale([0, 100], [top + ph, top]).Nice();
        var rs = new LinearScale([5, 45], [3, 20]);

        G.DrawGrid(canvas, ys, left, pw);
        G.DrawAxes(canvas, xs, ys, left, top, pw, ph);

        for (int i = 0; i < points.Length; i++)
        {
            var p = points[i];
            int ci = i % G.Palette.Length;
            var fill = G.Brush(G.Palette[ci], 0.45);
            var stroke = G.Brush(G.Palette[ci]);
            G.AddEllipse(canvas, xs.Map(p.x), ys.Map(p.y), rs.Map(p.size), fill, stroke);
        }

        G.AddText(canvas, left, 6, "Bubble Chart (size encodes third variable)", 14, G.Gray(40));

        return canvas;
    }
}
