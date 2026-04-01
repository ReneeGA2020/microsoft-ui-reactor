using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class ChordDiagramSample : GallerySample
{
    public override string Title => "Chord Diagram";
    public override string Description => "A chord diagram showing trade flow between five world regions. Outer arcs represent each region's total flow; inner ribbons show pairwise connections.";
    public override string Category => "Networks";

    public override string SourceCode => """
        var chord = new ChordLayout().SetPadAngle(0.05);
        var data = chord.Generate(matrix);
        var ribbon = new RibbonGenerator().SetRadius(innerR);

        foreach (var g in data.Groups)
        {
            var pb = new PathBuilder(3);
            pb.Arc(cx, cy, outerR, a0, a1);
            pb.Arc(cx, cy, innerR, a1, a0, ccw: true);
            pb.ClosePath();
            canvas.Children.Add(G.MakePath(pb.ToString(), fill: fill));
        }
        foreach (var c in data.Chords)
        {
            string? pathData = ribbon.Generate(c);
            if (pathData == null) continue;
            var path = G.MakePath(pathData, fill: G.Brush(color, 0.55));
            path.RenderTransform = new TranslateTransform { X = cx, Y = cy };
            canvas.Children.Add(path);
        }
        """;

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        double cx = W / 2, cy = H / 2;
        double outerR = 200, innerR = 190;
        var canvas = new Canvas { Width = W, Height = H };

        // -- regions and flow matrix (5x5) --
        string[] regions = ["N. America", "Europe", "Asia", "S. America", "Africa"];
        double[][] matrix =
        [
            [0,   120, 200, 80,  40],
            [90,  0,   160, 50,  30],
            [180, 140, 0,   60,  70],
            [60,  40,  50,  0,   20],
            [30,  25,  55,  15,  0 ],
        ];

        // -- compute layout --
        var chord = new ChordLayout().SetPadAngle(0.05);
        var data = chord.Generate(matrix);

        // -- outer arcs (groups) --
        var arcGen = new ArcGenerator();
        foreach (var g in data.Groups)
        {
            var color = G.Palette[g.Index % G.Palette.Length];
            var fill = G.Brush(color);

            // Build arc path using PathBuilder
            var pb = new PathBuilder(3);
            double a0 = g.StartAngle - Math.PI / 2;
            double a1 = g.EndAngle - Math.PI / 2;
            pb.Arc(cx, cy, outerR, a0, a1);
            pb.Arc(cx, cy, innerR, a1, a0, ccw: true);
            pb.ClosePath();

            var arcPath = G.MakePath(pb.ToString(), fill: fill);
            canvas.Children.Add(arcPath);

            // Label at midpoint angle
            double mid = (g.StartAngle + g.EndAngle) / 2 - Math.PI / 2;
            double lx = cx + (outerR + 16) * Math.Cos(mid);
            double ly = cy + (outerR + 16) * Math.Sin(mid);
            double labelW = 70;
            G.AddText(canvas, lx - labelW / 2, ly - 7, regions[g.Index], 10,
                      G.Brush(color), TextAlignment.Center, labelW);
        }

        // -- ribbons (chords) --
        var ribbon = new RibbonGenerator().SetRadius(innerR);
        foreach (var c in data.Chords)
        {
            var color = G.Palette[c.Source.Index % G.Palette.Length];
            string? pathData = ribbon.Generate(c);
            if (pathData == null) continue;

            // Translate ribbon path to canvas center
            var path = G.MakePath(pathData, fill: G.Brush(color, 0.55));
            path.RenderTransform = new TranslateTransform { X = cx, Y = cy };
            canvas.Children.Add(path);
        }

        // -- title --
        G.AddText(canvas, 12, 6, "Chord Diagram — Regional Trade Flow", 14, G.Brush("#333333"));

        return canvas;
    }
}
