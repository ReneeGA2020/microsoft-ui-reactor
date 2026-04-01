using Duct;
using Duct.Core;
using Duct.D3;
using Duct.D3.Charts;
using Microsoft.UI.Xaml;
using static Duct.D3.Charts.D3;
using static Duct.UI;

namespace DuctD3.Gallery;

public sealed class ClusterDendrogramSample : GallerySample
{
    public override string Title => "Cluster Dendrogram";
    public override string Description =>
        "A cluster (dendrogram) layout showing animal taxonomy. All leaf nodes are " +
        "aligned at the same depth, making it easy to compare lineages. Uses ClusterLayout.";
    public override string Category => "Hierarchies";

    public override string SourceCode => """
        var layout = ClusterLayout.Create<TaxNode>().Size(660, 440);
        var root = layout.Hierarchy(data, n => n.Children);
        layout.Layout(root);

        var links = nodes.SelectMany(node =>
            node.Children.Select(child => {
                double my = (node.Y + child.Y) / 2;
                var pb = new PathBuilder(3);
                pb.MoveTo(node.X, node.Y);
                pb.BezierCurveTo(node.X, my, child.X, my, child.X, child.Y);
                return D3Path(pb.ToString(), linkStroke, null, 1.2);
            }));
        var circles = nodes.Select(node =>
            D3Circle(node.X, node.Y, isLeaf ? 3.5 : 4.5) with { Fill = fill });
        D3Canvas(W, H, [..links, ..circles, ..labels]);
        """;

    record TaxNode(string Name, TaxNode[]? Children = null);

    public override Element Render()
    {
        const double W = 700, H = 500;

        // Taxonomy hierarchy
        var data = new TaxNode("Animalia", [
            new("Chordata", [
                new("Mammalia", [
                    new("Primates", [
                        new("Human"),
                        new("Chimpanzee"),
                        new("Gorilla"),
                    ]),
                    new("Carnivora", [
                        new("Lion"),
                        new("Wolf"),
                        new("Bear"),
                    ]),
                    new("Cetacea", [
                        new("Dolphin"),
                        new("Blue Whale"),
                    ]),
                ]),
                new("Aves", [
                    new("Passeriformes", [
                        new("Sparrow"),
                        new("Robin"),
                    ]),
                    new("Accipitriformes", [
                        new("Eagle"),
                        new("Hawk"),
                    ]),
                ]),
            ]),
            new("Arthropoda", [
                new("Insecta", [
                    new("Coleoptera", [
                        new("Ladybug"),
                        new("Beetle"),
                    ]),
                    new("Lepidoptera", [
                        new("Monarch"),
                        new("Swallowtail"),
                    ]),
                ]),
            ]),
        ]);

        var layout = ClusterLayout.Create<TaxNode>().Size(660, 440);
        var root = layout.Hierarchy(data, n => n.Children);
        layout.Layout(root);

        var nodes = root.Descendants().ToList();

        var linkStroke = Gray(190);

        return D3Canvas(W, H,
        [
            .. nodes.SelectMany(node =>
                node.Children.Select(child =>
                    D3Link(node.X, node.Y, child.X, child.Y, linkStroke, 1.2))),
            .. nodes.SelectMany(node =>
            {
                bool isLeaf = node.Children.Count == 0;
                double r = isLeaf ? 3.5 : 4.5;

                int branchColor = root.Children.IndexOf(node.TopAncestor);
                var fill = isLeaf
                    ? Brush(Palette[branchColor % Palette.Length])
                    : Brush("#666666");

                Element label = isLeaf
                    ? D3Text(node.X + 7, node.Y - 7, node.Data.Name, 8.5, Gray(50))
                    : node.Parent == null
                        ? D3Text(node.X - 4, node.Y - 18, node.Data.Name, 11, Brush("#222222"))
                            .Set(tb => tb.TextAlignment = TextAlignment.Center)
                        : D3Text(node.X - 4, node.Y - 16, node.Data.Name, 9, Gray(80));

                return new Element[]
                {
                    D3Circle(node.X, node.Y, r) with { Fill = fill },
                    label,
                };
            }),
        ]);
    }

}
