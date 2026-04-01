using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public sealed class BubbleChartSample : GallerySample
{
    public override string Title => "Bubble Chart";
    public override string Description => "A bubble chart where circle size encodes a third variable, showing 30 data points with x, y, and size dimensions.";
    public override string Category => "Dots";

    public override string SourceCode => """
        var xs = new LinearScale([0, 100], [left, left+pw]).Nice();
        var ys = new LinearScale([0, 100], [top+ph, top]).Nice();
        var rs = new LinearScale([5, 45], [3, 20]);
        var bubbles = points.Select((p, i) =>
            D3Circle(xs.Map(p.x), ys.Map(p.y), rs.Map(p.size)) with {
                Fill = Brush(Palette[i % Palette.Length], opacity: 0.45),
                Stroke = Brush(Palette[i % Palette.Length])
            });
        D3Canvas(width, height,
            ..D3Grid(ys, left, pw), ..D3Axes(xs, ys, left, top, pw, ph),
            ..bubbles)
        """;

    public override Element Render()
    {
        const double width = 700, height = 400;
        const double left = 50, top = 30, right = 20, bottom = 30;
        double pw = width - left - right;
        double ph = height - top - bottom;

        var rng = new Random(123);
        var points = Enumerable.Range(0, 30)
            .Select(_ => (x: rng.NextDouble() * 100, y: rng.NextDouble() * 100,
                          size: 5 + rng.NextDouble() * 40))
            .ToArray();

        var xs = new LinearScale([0, 100], [left, left + pw]).Nice();
        var ys = new LinearScale([0, 100], [top + ph, top]).Nice();
        var rs = new LinearScale([5, 45], [3, 20]);

        var bubbles = new Element[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var p = points[i];
            int ci = i % Palette.Length;
            var fill = Brush(Palette[ci], opacity: 0.45);
            var stroke = Brush(Palette[ci]);
            bubbles[i] = D3Circle(xs.Map(p.x), ys.Map(p.y), rs.Map(p.size)) with { Fill = fill, Stroke = stroke };
        }

        return D3Canvas(width, height,
            [.. D3Grid(ys, left, pw),
             .. D3Axes(xs, ys, left, top, pw, ph),
             .. bubbles,
             D3Text(left, 6, "Bubble Chart (size encodes third variable)", 14, Gray(40))]
        );
    }
}
