using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

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
            D3PathTranslated(pathData, pad, pad, fill: Brush(color, opacity: 0.35))
        }
        foreach (var node in graph.Nodes)
        {
            D3Rect(pad + node.X0, pad + node.Y0, nw, nh)
                with { Fill = fill, RadiusX = 2, RadiusY = 2 }
        }
        """;

    public override Element Render()
    {
        const double W = 700, H = 500;
        const double pad = 40;
        double plotW = W - pad * 2;
        double plotH = H - pad * 2;

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

        return D3Canvas(W, H,
            [.. graph.Links
                .Select(link =>
                {
                    int ci = nodeColors.GetValueOrDefault(link.SourceId, 0);
                    var color = Palette[ci % Palette.Length];
                    return (Element)D3PathTranslated(SankeyLayout.LinkPath(link), pad, pad,
                        fill: Brush(color, opacity: 0.35));
                }),
             .. graph.Nodes.SelectMany(node =>
             {
                 int ci = nodeColors[node.Id];
                 var fill = Brush(Palette[ci % Palette.Length]);
                 double nh = node.Y1 - node.Y0;
                 bool isOutput = node.SourceLinks.Count == 0;
                 double labelY = pad + node.Y0 + nh / 2 - 7;
                 string labelText = node.Label ?? node.Id;

                 var label = isOutput
                     ? D3TextRight(pad + node.X0 - 6 - 90, labelY, labelText, 90, 10, Gray(40))
                     : D3Text(pad + node.X1 + 6, labelY, labelText, 10, Gray(40));

                 return new Element[]
                 {
                     D3Rect(pad + node.X0, pad + node.Y0, node.X1 - node.X0, nh) with
                         { Fill = fill, RadiusX = 2, RadiusY = 2 },
                     label,
                 };
             }),
             D3Text(12, 6, "Sankey Diagram \u2014 Energy Flow", 14, Brush("#333333")),
            ]
        );
    }
}
