using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class PieChartSample : GallerySample
{
    public override string Title => "Pie Chart";
    public override string Description => "A classic pie chart showing market share across five companies, using PieGenerator and ArcGenerator.";
    public override string Category => "Radial";

    public override string SourceCode => """
        var data = new[] {
            ("Chrome", 65.0), ("Safari", 18.0), ("Firefox", 7.0),
            ("Edge", 5.0), ("Other", 5.0)
        };
        var pie = PieGenerator.Create<(string Name, double Value)>(d => d.Value)
            .SetSortValues(null);
        var arc = new ArcGenerator().SetOuterRadius(150).SetInnerRadius(0);
        var arcs = pie.Generate(data);
        // Draw each slice and label at centroid
        """;

    public override FrameworkElement Render()
    {
        const double width = 700, height = 400;
        double cx = width / 2, cy = height / 2;

        var canvas = new Canvas { Width = width, Height = height };

        var data = new (string Name, double Value)[]
        {
            ("Chrome", 65.0), ("Safari", 18.0), ("Firefox", 7.0),
            ("Edge", 5.0), ("Other", 5.0)
        };

        var pie = PieGenerator.Create<(string Name, double Value)>(d => d.Value)
            .SetSortValues(null);
        var arc = new ArcGenerator().SetOuterRadius(150).SetInnerRadius(0);
        var arcs = pie.Generate(data);

        for (int i = 0; i < arcs.Length; i++)
        {
            var a = arcs[i];
            var pathData = arc.Generate(a);
            if (pathData == null) continue;

            var path = G.MakePath(pathData, fill: G.Brush(G.Palette[i % G.Palette.Length]));
            path.RenderTransform = new TranslateTransform { X = cx, Y = cy };
            canvas.Children.Add(path);

            // Label at centroid
            var (lx, ly) = arc.Centroid(a.StartAngle, a.EndAngle);
            var labelArc = new ArcGenerator().SetOuterRadius(180).SetInnerRadius(180);
            var (ox, oy) = labelArc.Centroid(a.StartAngle, a.EndAngle);
            G.AddText(canvas, cx + ox - 20, cy + oy - 7,
                $"{a.Data.Name} ({a.Data.Value}%)", 11, G.Brush(G.Palette[i % G.Palette.Length]));
        }

        G.AddText(canvas, cx - 60, 10, "Market Share", 16, G.Gray(40));

        return canvas;
    }
}
