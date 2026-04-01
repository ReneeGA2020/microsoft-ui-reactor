using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public sealed class ScatterplotSample : GallerySample
{
    public override string Title => "Scatterplot";
    public override string Description => "A scatterplot of 50 random points with linear scales on both axes.";
    public override string Category => "Dots";

    public override string SourceCode => """
        var xs = new LinearScale([0, 100], [left, left + pw]).Nice();
        var ys = new LinearScale([0, 100], [top + ph, top]).Nice();
        D3Canvas(width, height,
            ..D3Grid(ys, left, pw),
            ..D3Axes(xs, ys, left, top, pw, ph),
            ..points.Select(p => D3Circle(xs.Map(p.x), ys.Map(p.y), 4)
                with { Fill = fill, Stroke = stroke })
        )
        """;

    public override Element Render()
    {
        const double width = 700, height = 400;
        const double left = 50, top = 30, right = 20, bottom = 30;
        double pw = width - left - right;
        double ph = height - top - bottom;

        var rng = new Random(42);
        var points = Enumerable.Range(0, 50)
            .Select(_ => (x: rng.NextDouble() * 100, y: rng.NextDouble() * 100))
            .ToArray();

        var xs = new LinearScale([0, 100], [left, left + pw]).Nice();
        var ys = new LinearScale([0, 100], [top + ph, top]).Nice();

        var fill = Brush(Palette[0], opacity: 0.6);
        var stroke = Brush(Palette[0]);

        return D3Canvas(width, height,
            [.. D3Grid(ys, left, pw),
             .. D3Axes(xs, ys, left, top, pw, ph),
             .. points.Select(p => (Element)(D3Circle(xs.Map(p.x), ys.Map(p.y), 4) with { Fill = fill, Stroke = stroke })),
             D3Text(left, 6, "Scatterplot (50 random points)", 14, Gray(40))]
        );
    }
}
