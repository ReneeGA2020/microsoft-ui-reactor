using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public sealed class DotPlotSample : GallerySample
{
    public override string Title => "Dot Plot";
    public override string Description => "A strip chart (dot plot) showing values for five categories along a shared x axis, with multiple dots per category.";
    public override string Category => "Dots";

    public override string SourceCode => """
        var xs = new LinearScale([0, 100], [left, left+pw]).Nice();
        D3Canvas(width, height,
            [..categories.SelectMany((cat, ci) =>
                 data.Where(d => d.cat == cat)
                     .Select(d => D3Circle(xs.Map(d.value), rowY, 5)
                         with { Fill = fill, Stroke = stroke })),
             D3Text(left, 6, "Dot Plot", 14, Gray(40))]
        )
        """;

    public override Element Render()
    {
        const double width = 700, height = 400;
        const double left = 80, top = 30, right = 20, bottom = 30;
        double pw = width - left - right;
        double ph = height - top - bottom;

        var categories = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };

        var rng = new Random(7);
        var data = categories.SelectMany((cat, ci) =>
            Enumerable.Range(0, 6 + ci).Select(_ =>
                (cat, value: 20 + rng.NextDouble() * 60 + ci * 5))
        ).ToArray();

        var xs = new LinearScale([0, 100], [left, left + pw]).Nice();

        double axisY = top + ph;
        double rowHeight = ph / categories.Length;

        return D3Canvas(width, height,
            [// X axis
             D3Line(left, axisY, left + pw, axisY) with { Stroke = Gray(100, alpha: 180) },
             .. xs.Ticks(6).Select(t =>
                 D3Text(xs.Map(t) - 12, axisY + 4, Fmt(t), 10, Gray(100))),
             // Category rows and dots
             .. categories.SelectMany((cat, ci) =>
             {
                 double rowY = top + ci * rowHeight + rowHeight / 2;
                 var fill = Brush(Palette[ci % Palette.Length], opacity: 0.6);
                 var stroke = Brush(Palette[ci % Palette.Length]);
                 return ((Element[])
                 [
                     D3Text(4, rowY - 7, cat, 11, Gray(60)),
                     D3Line(left, rowY, left + pw, rowY) with { Stroke = Gray(128, alpha: 30) },
                     .. data.Where(d => d.cat == cat)
                         .Select(d => D3Circle(xs.Map(d.value), rowY, 5) with { Fill = fill, Stroke = stroke }),
                 ]);
             }),
             D3Text(left, 6, "Dot Plot (strip chart)", 14, Gray(40))]
        );
    }
}
