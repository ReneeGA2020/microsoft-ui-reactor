using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace DuctD3.Gallery;

public sealed class SankeyDiagramSample : GallerySample
{
    public override string Title => "Sankey Diagram";
    public override string Description => "A Sankey diagram visualizing energy flow from 5 sources through 3 intermediate stages to 2 outputs. Node height encodes throughput; curved bands show flow magnitude.";
    public override string Category => "Networks";

    public override string SourceCode => """
        var layout = new SankeyLayout()
            .Size(plotW, plotH)
            .SetNodeWidth(20).SetNodePadding(14);
        layout.Layout(graph);

        foreach (var link in graph.Links)
        {
            string? pathData = SankeyLayout.LinkPath(link);
            if (pathData == null) continue;
            var path = G.MakePath(pathData, fill: G.Brush(color, 0.35));
            path.RenderTransform = new TranslateTransform { X = pad, Y = pad };
            canvas.Children.Add(path);
        }
        foreach (var node in graph.Nodes)
        {
            var fill = G.Brush(G.Palette[ci % G.Palette.Length]);
            double nh = node.Y1 - node.Y0;
            G.AddRect(canvas, pad + node.X0, pad + node.Y0,
                node.X1 - node.X0, nh, fill, 2);
        }
        """;

    public override FrameworkElement Render()
    {
        const double W = 700, H = 500;
        const double pad = 40;
        double plotW = W - pad * 2;
        double plotH = H - pad * 2;
        var canvas = new Canvas { Width = W, Height = H };

        // -- nodes: 5 sources -> 3 intermediate -> 2 outputs --
        var graph = new SankeyGraph
        {
            Nodes =
            [
                // Sources (column 0)
                new SankeyNode { Id = "solar",   Label = "Solar" },
                new SankeyNode { Id = "wind",    Label = "Wind" },
                new SankeyNode { Id = "hydro",   Label = "Hydro" },
                new SankeyNode { Id = "gas",     Label = "Natural Gas" },
                new SankeyNode { Id = "coal",    Label = "Coal" },
                // Intermediate (column 1)
                new SankeyNode { Id = "electric", Label = "Electricity" },
                new SankeyNode { Id = "heat",     Label = "Heat" },
                new SankeyNode { Id = "losses",   Label = "Losses" },
                // Outputs (column 2)
                new SankeyNode { Id = "residential", Label = "Residential" },
                new SankeyNode { Id = "industrial",  Label = "Industrial" },
            ],
            Links =
            [
                // Sources -> Intermediate
                new SankeyLink { SourceId = "solar",  TargetId = "electric", Value = 120 },
                new SankeyLink { SourceId = "wind",   TargetId = "electric", Value = 90 },
                new SankeyLink { SourceId = "hydro",  TargetId = "electric", Value = 70 },
                new SankeyLink { SourceId = "gas",    TargetId = "electric", Value = 60 },
                new SankeyLink { SourceId = "gas",    TargetId = "heat",     Value = 50 },
                new SankeyLink { SourceId = "coal",   TargetId = "heat",     Value = 80 },
                new SankeyLink { SourceId = "coal",   TargetId = "losses",   Value = 40 },
                new SankeyLink { SourceId = "gas",    TargetId = "losses",   Value = 20 },
                // Intermediate -> Outputs
                new SankeyLink { SourceId = "electric", TargetId = "residential", Value = 180 },
                new SankeyLink { SourceId = "electric", TargetId = "industrial",  Value = 160 },
                new SankeyLink { SourceId = "heat",     TargetId = "residential", Value = 70 },
                new SankeyLink { SourceId = "heat",     TargetId = "industrial",  Value = 60 },
                new SankeyLink { SourceId = "losses",   TargetId = "industrial",  Value = 60 },
            ],
        };

        // -- layout --
        var layout = new SankeyLayout()
            .Size(plotW, plotH)
            .SetNodeWidth(20)
            .SetNodePadding(14);
        layout.Layout(graph);

        // Assign a color index to each node for consistent coloring
        var nodeColors = new Dictionary<string, int>();
        for (int i = 0; i < graph.Nodes.Count; i++)
            nodeColors[graph.Nodes[i].Id] = i;

        // -- draw links --
        foreach (var link in graph.Links)
        {
            string? pathData = SankeyLayout.LinkPath(link);
            if (pathData == null) continue;

            int ci = nodeColors.GetValueOrDefault(link.SourceId, 0);
            var color = G.Palette[ci % G.Palette.Length];
            var path = G.MakePath(pathData, fill: G.Brush(color, 0.35));
            path.RenderTransform = new TranslateTransform { X = pad, Y = pad };
            canvas.Children.Add(path);
        }

        // -- draw nodes --
        foreach (var node in graph.Nodes)
        {
            int ci = nodeColors[node.Id];
            var fill = G.Brush(G.Palette[ci % G.Palette.Length]);
            double nh = node.Y1 - node.Y0;
            G.AddRect(canvas, pad + node.X0, pad + node.Y0, node.X1 - node.X0, nh, fill, 2);

            // Label: right side for source/intermediate, left side for outputs
            bool isOutput = node.SourceLinks.Count == 0;
            double tx = isOutput
                ? pad + node.X0 - 6
                : pad + node.X1 + 6;
            var align = isOutput ? TextAlignment.Right : TextAlignment.Left;
            double tw = isOutput ? 90 : 0;
            double labelX = isOutput ? tx - 90 : tx;
            G.AddText(canvas, labelX, pad + node.Y0 + nh / 2 - 7,
                      node.Label ?? node.Id, 10, G.Gray(40), align, isOutput ? 90 : null);
        }

        // -- title --
        G.AddText(canvas, 12, 6, "Sankey Diagram — Energy Flow", 14, G.Brush("#333333"));

        return canvas;
    }
}
