using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public sealed class DonutChartSample : GallerySample
{
    public override string Title => "Donut Chart";
    public override string Description => "A donut chart (pie with inner radius) showing six expense categories, using PieGenerator and ArcGenerator with innerRadius=80.";
    public override string Category => "Radial";

    public override string SourceCode => """
        var data = new[] {
            ("Housing", 1800.0), ("Food", 650.0), ("Transport", 400.0),
            ("Utilities", 250.0), ("Health", 300.0), ("Savings", 600.0)
        };
        var pie = PieGenerator.Create<(string, double)>(d => d.Item2)
            .SetSortValues(null).SetPadAngle(0.02);
        var arc = new ArcGenerator().SetOuterRadius(150).SetInnerRadius(80);
        var arcs = pie.Generate(data);
        var slices = arcs.Select((a, i) =>
            D3PathTranslated(arc.Generate(a), cx, cy,
                fill: Brush(Palette[i % Palette.Length])));
        D3Canvas(width, height, [..slices, ..labels, ..centerText, title]);
        """;

    public override Element Render()
    {
        const double width = 700, height = 400;
        double cx = width / 2, cy = height / 2;

        var data = new (string Name, double Value)[]
        {
            ("Housing", 1800.0), ("Food", 650.0), ("Transport", 400.0),
            ("Utilities", 250.0), ("Health", 300.0), ("Savings", 600.0)
        };

        var pie = PieGenerator.Create<(string Name, double Value)>(d => d.Value)
            .SetSortValues(null)
            .SetPadAngle(0.02);
        var arc = new ArcGenerator().SetOuterRadius(150).SetInnerRadius(80);
        var arcs = pie.Generate(data);

        var labelArc = new ArcGenerator().SetOuterRadius(185).SetInnerRadius(185);
        double total = data.Sum(d => d.Value);

        return D3Canvas(width, height,
        [
            .. arcs.SelectMany((a, i) =>
                {
                    var (lx, ly) = labelArc.Centroid(a.StartAngle, a.EndAngle);
                    return new Element[]
                    {
                        D3PathTranslated(arc.Generate(a), cx, cy,
                            fill: Brush(Palette[i % Palette.Length])),
                        D3Text(cx + lx - 24, cy + ly - 7,
                            $"{a.Data.Name}", 10, Brush(Palette[i % Palette.Length])),
                    };
                }),
            D3Text(cx - 24, cy - 12, $"${total:N0}", 14, Gray(40)),
            D3Text(cx - 16, cy + 6, "/ month", 10, Gray(120)),
            D3Text(cx - 80, 10, "Monthly Expenses", 16, Gray(40)),
        ]);
    }
}
