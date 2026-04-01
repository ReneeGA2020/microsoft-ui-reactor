using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class ForceDirectedGraphSample : GallerySample
{
    public override string Title => "Force-Directed Graph";
    public override string Description => "A force-directed network layout with 15 nodes and ~20 edges. Nodes are colored by category and positioned using charge, link, and center forces over 300 iterations.";
    public override string Category => "Networks";

    public override string SourceCode => """
        var sim = new ForceSimulation()
            .SetNodes(nodes).SetLinks(links)
            .Center(W / 2, H / 2)
            .ChargeStrength(-120).LinkDistance(60)
            .CollisionRadius(14)
            .InitializePositions().Run(300);

        foreach (var link in sim.Links)
        {
            var s = sim.Nodes[link.Source];
            var t = sim.Nodes[link.Target];
            G.AddLine(canvas, s.X, s.Y, t.X, t.Y, edgeStroke, 1.2);
        }
        for (int i = 0; i < sim.Nodes.Count; i++)
        {
            var n = sim.Nodes[i];
            var fill = G.Brush(G.Palette[categories[i] % G.Palette.Length]);
            G.AddEllipse(canvas, n.X, n.Y, 10, fill, white, 1.5);
        }
        """;

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        var canvas = new Canvas { Width = W, Height = H };

        // -- data: 15 nodes in 4 categories --
        string[] labels =
        [
            "Alice", "Bob", "Carol", "Dave", "Eve",
            "Frank", "Grace", "Heidi", "Ivan", "Judy",
            "Karl", "Liam", "Mia", "Nina", "Oscar"
        ];
        int[] categories = [0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3];

        var nodes = new ForceNode[labels.Length];
        for (int i = 0; i < labels.Length; i++)
            nodes[i] = new ForceNode { Label = labels[i], Radius = 10 };

        // ~20 edges connecting related nodes
        var links = new ForceLink[]
        {
            new(0, 1), new(0, 2), new(1, 2), new(1, 3),
            new(2, 4), new(3, 4), new(3, 5), new(4, 6),
            new(5, 6), new(5, 7), new(6, 8), new(7, 8),
            new(7, 9), new(8, 10), new(9, 10), new(9, 11),
            new(10, 12), new(11, 13), new(12, 13), new(13, 14),
            new(14, 0), new(11, 14),
        };

        // -- simulation --
        var sim = new ForceSimulation()
            .SetNodes(nodes)
            .SetLinks(links)
            .Center(W / 2, H / 2)
            .ChargeStrength(-120)
            .LinkDistance(60)
            .CollisionRadius(14)
            .InitializePositions()
            .Run(300);

        // -- draw edges --
        var edgeStroke = G.Gray(180);
        foreach (var link in sim.Links)
        {
            var s = sim.Nodes[link.Source];
            var t = sim.Nodes[link.Target];
            G.AddLine(canvas, s.X, s.Y, t.X, t.Y, edgeStroke, 1.2);
        }

        // -- draw nodes --
        var white = G.Brush("#ffffff");
        for (int i = 0; i < sim.Nodes.Count; i++)
        {
            var n = sim.Nodes[i];
            int cat = categories[i];
            var fill = G.Brush(G.Palette[cat % G.Palette.Length]);
            G.AddEllipse(canvas, n.X, n.Y, 10, fill, white, 1.5);

            // label offset below node
            G.AddText(canvas, n.X - 16, n.Y + 12, labels[i], 9, G.Gray(60),
                      TextAlignment.Center, 32);
        }

        // -- title --
        G.AddText(canvas, 12, 6, "Force-Directed Graph", 14, G.Brush("#333333"));

        return canvas;
    }
}
