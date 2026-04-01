using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

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
        // Draw slices and place labels at centroids
        """;

    public override FrameworkElement Render()
    {
        const double width = 700, height = 400;
        double cx = width / 2, cy = height / 2;

        var canvas = new Canvas { Width = width, Height = height };

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

        for (int i = 0; i < arcs.Length; i++)
        {
            var a = arcs[i];
            var pathData = arc.Generate(a);
            if (pathData == null) continue;

            var path = G.MakePath(pathData, fill: G.Brush(G.Palette[i % G.Palette.Length]));
            path.RenderTransform = new TranslateTransform { X = cx, Y = cy };
            canvas.Children.Add(path);

            // Label at centroid of a slightly larger arc for readability
            var labelArc = new ArcGenerator().SetOuterRadius(185).SetInnerRadius(185);
            var (ox, oy) = labelArc.Centroid(a.StartAngle, a.EndAngle);
            G.AddText(canvas, cx + ox - 24, cy + oy - 7,
                $"{a.Data.Name}", 10, G.Brush(G.Palette[i % G.Palette.Length]));
        }

        // Center label
        double total = 0;
        foreach (var d in data) total += d.Value;
        G.AddText(canvas, cx - 24, cy - 12, $"${total:N0}", 14, G.Gray(40));
        G.AddText(canvas, cx - 16, cy + 6, "/ month", 10, G.Gray(120));

        G.AddText(canvas, cx - 80, 10, "Monthly Expenses", 16, G.Gray(40));

        return canvas;
    }
}
