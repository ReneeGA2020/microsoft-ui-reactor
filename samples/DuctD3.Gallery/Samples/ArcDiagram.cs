using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class ArcDiagramSample : GallerySample
{
    public override string Title => "Arc Diagram";
    public override string Description => "An arc diagram with 10 nodes arranged along a horizontal line. Connections are drawn as semicircular arcs above and below the baseline, with arc height proportional to the distance between nodes.";
    public override string Category => "Networks";

    public override string SourceCode => """
        double spacing = (W - padX * 2) / (nodeCount - 1);
        for (int i = 0; i < nodeCount; i++)
            nodeX[i] = padX + i * spacing;

        for (int ei = 0; ei < edges.Length; ei++)
        {
            var (si, ti) = edges[ei];
            double x1 = nodeX[si], x2 = nodeX[ti];
            if (x1 > x2) (x1, x2) = (x2, x1);
            double midX = (x1 + x2) / 2;
            double r = (x2 - x1) / 2;
            bool above = ei % 2 == 0;

            var pb = new PathBuilder(3);
            pb.MoveTo(x1, baseline);
            pb.Arc(midX, baseline, r, Math.PI, 0, ccw: !above);
            var stroke = G.Brush(G.Palette[colorIdx % G.Palette.Length], 0.6);
            canvas.Children.Add(G.MakePath(pb.ToString(), stroke: stroke,
                strokeWidth: 1.8));
        }
        """;

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        const double padX = 50, baseline = 320;
        var canvas = new Canvas { Width = W, Height = H };

        // -- data: 10 nodes --
        string[] labels =
        [
            "Alpha", "Bravo", "Charlie", "Delta", "Echo",
            "Foxtrot", "Golf", "Hotel", "India", "Juliet"
        ];
        int[] categories = [0, 0, 1, 1, 2, 2, 3, 3, 0, 1];
        int nodeCount = labels.Length;

        // ~12 edges (source, target)
        (int s, int t)[] edges =
        [
            (0, 2), (0, 5), (1, 3), (1, 4), (2, 6),
            (3, 7), (4, 8), (5, 9), (6, 8), (7, 9),
            (0, 9), (3, 6),
        ];

        // -- compute node x positions --
        double spacing = (W - padX * 2) / (nodeCount - 1);
        double[] nodeX = new double[nodeCount];
        for (int i = 0; i < nodeCount; i++)
            nodeX[i] = padX + i * spacing;

        // -- draw baseline --
        G.AddLine(canvas, padX - 10, baseline, W - padX + 10, baseline, G.Gray(200), 1);

        // -- draw arcs --
        // Alternate above/below: even-index edges go above, odd below
        for (int ei = 0; ei < edges.Length; ei++)
        {
            var (si, ti) = edges[ei];
            double x1 = nodeX[si];
            double x2 = nodeX[ti];
            if (x1 > x2) (x1, x2) = (x2, x1);

            double midX = (x1 + x2) / 2;
            double r = (x2 - x1) / 2;
            bool above = ei % 2 == 0;

            var pb = new PathBuilder(3);
            if (above)
            {
                // Arc above baseline: from x1 to x2, sweeping upward
                pb.MoveTo(x1, baseline);
                pb.Arc(midX, baseline, r, Math.PI, 0);
            }
            else
            {
                // Arc below baseline: from x1 to x2, sweeping downward
                pb.MoveTo(x1, baseline);
                pb.Arc(midX, baseline, r, Math.PI, 0, ccw: true);
            }

            int colorIdx = categories[edges[ei].s];
            var stroke = G.Brush(G.Palette[colorIdx % G.Palette.Length], 0.6);
            var path = G.MakePath(pb.ToString(), stroke: stroke, strokeWidth: 1.8);
            canvas.Children.Add(path);
        }

        // -- draw nodes --
        var white = G.Brush("#ffffff");
        for (int i = 0; i < nodeCount; i++)
        {
            var fill = G.Brush(G.Palette[categories[i] % G.Palette.Length]);
            G.AddEllipse(canvas, nodeX[i], baseline, 8, fill, white, 1.5);

            // Label below baseline
            G.AddText(canvas, nodeX[i] - 24, baseline + 14, labels[i], 9, G.Gray(60),
                      TextAlignment.Center, 48);
        }

        // -- title --
        G.AddText(canvas, 12, 6, "Arc Diagram", 14, G.Brush("#333333"));

        return canvas;
    }
}
