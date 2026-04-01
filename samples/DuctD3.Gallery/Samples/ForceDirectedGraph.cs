using Duct;
using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.D3.Charts.D3;
using static Duct.UI;

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

        var edges = sim.Links.Select(link => {
            var s = sim.Nodes[link.Source];
            var t = sim.Nodes[link.Target];
            return D3Line(s.X, s.Y, t.X, t.Y) with
                { Stroke = edgeStroke, StrokeThickness = 1.2 };
        });
        var nodes = sim.Nodes.Select((n, i) =>
            D3Circle(n.X, n.Y, 10) with
                { Fill = Brush(Palette[categories[i]]), Stroke = white });

        return D3Canvas(W, H, [..edges, ..nodes, ..labels]);
        """;

    public override Element Render()
    {
        const double W = 700, H = 500;

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

        // -- draw --
        var edgeStroke = Gray(180);
        var white = Brush("#ffffff");

        return D3Canvas(W, H,
        [
            .. sim.Links.Select(link =>
            {
                var s = sim.Nodes[link.Source];
                var t = sim.Nodes[link.Target];
                return D3Line(s.X, s.Y, t.X, t.Y) with { Stroke = edgeStroke, StrokeThickness = 1.2 };
            }),
            .. sim.Nodes.SelectMany((n, i) => new Element[]
            {
                D3Circle(n.X, n.Y, 10) with
                {
                    Fill = Brush(Palette[categories[i] % Palette.Length]),
                    Stroke = white,
                    StrokeThickness = 1.5,
                },
                D3TextCenter(n.X - 16, n.Y + 12, labels[i], 32, 9, Gray(60)),
            }),
            D3Text(12, 6, "Force-Directed Graph", 14, Brush("#333333")),
        ]);
    }
}
