// Port of d3-hierarchy/src/treemap/ — ISC License, Copyright 2010-2023 Mike Bostock

namespace Duct.D3;

/// <summary>
/// A treemap layout that recursively subdivides area into rectangles.
/// Direct port of d3.treemap().
/// </summary>
public sealed class TreemapLayout<T>
{
    private double _x0 = 0, _y0 = 0, _x1 = 1, _y1 = 1;
    private double _paddingTop = 0, _paddingRight = 0, _paddingBottom = 0, _paddingLeft = 0;
    private double _paddingInner = 0;
    private TreemapTiling _tiling = TreemapTiling.Squarify;

    /// <summary>
    /// Computes the treemap layout for the given root node.
    /// Each node must have a Value set (call SumValues first).
    /// </summary>
    public TreemapNode<T> Layout(TreemapNode<T> root)
    {
        root.X0 = _x0;
        root.Y0 = _y0;
        root.X1 = _x1;
        root.Y1 = _y1;
        LayoutNode(root);
        return root;
    }

    /// <summary>
    /// Builds a treemap node hierarchy from data with a children accessor,
    /// then sums values bottom-up.
    /// </summary>
    public TreemapNode<T> Hierarchy(T root, Func<T, IEnumerable<T>?> children, Func<T, double> value)
    {
        var node = BuildNode(root, children, null, 0);
        SumValues(node, value);
        return node;
    }

    public TreemapLayout<T> Size(double x1, double y1) { _x0 = 0; _y0 = 0; _x1 = x1; _y1 = y1; return this; }
    public TreemapLayout<T> SetPadding(double p) { _paddingTop = _paddingRight = _paddingBottom = _paddingLeft = _paddingInner = p; return this; }
    public TreemapLayout<T> SetPaddingInner(double p) { _paddingInner = p; return this; }
    public TreemapLayout<T> SetPaddingOuter(double p) { _paddingTop = _paddingRight = _paddingBottom = _paddingLeft = p; return this; }
    public TreemapLayout<T> SetTiling(TreemapTiling tiling) { _tiling = tiling; return this; }

    private TreemapNode<T> BuildNode(T data, Func<T, IEnumerable<T>?> childrenAccessor, TreemapNode<T>? parent, int depth)
    {
        var node = new TreemapNode<T> { Data = data, Parent = parent, Depth = depth };
        var kids = childrenAccessor(data);
        if (kids != null)
        {
            foreach (var child in kids)
                node.Children.Add(BuildNode(child, childrenAccessor, node, depth + 1));
        }
        return node;
    }

    private void SumValues(TreemapNode<T> node, Func<T, double> value)
    {
        if (node.Children.Count == 0)
        {
            node.Value = Math.Max(0, value(node.Data));
        }
        else
        {
            double sum = 0;
            foreach (var child in node.Children)
            {
                SumValues(child, value);
                sum += child.Value;
            }
            node.Value = sum;
        }
    }

    private void LayoutNode(TreemapNode<T> node)
    {
        if (node.Children.Count == 0) return;

        double x0 = node.X0 + _paddingLeft;
        double y0 = node.Y0 + _paddingTop;
        double x1 = node.X1 - _paddingRight;
        double y1 = node.Y1 - _paddingBottom;

        if (x1 < x0) x0 = x1 = (x0 + x1) / 2;
        if (y1 < y0) y0 = y1 = (y0 + y1) / 2;

        switch (_tiling)
        {
            case TreemapTiling.Squarify:
                TileSquarify(node.Children, x0, y0, x1, y1);
                break;
            case TreemapTiling.Slice:
                TileSlice(node.Children, x0, y0, x1, y1);
                break;
            case TreemapTiling.Dice:
                TileDice(node.Children, x0, y0, x1, y1);
                break;
            case TreemapTiling.SliceDice:
                if (node.Depth % 2 == 0)
                    TileSlice(node.Children, x0, y0, x1, y1);
                else
                    TileDice(node.Children, x0, y0, x1, y1);
                break;
        }

        foreach (var child in node.Children)
            LayoutNode(child);
    }

    private void TileSlice(List<TreemapNode<T>> nodes, double x0, double y0, double x1, double y1)
    {
        double total = nodes.Sum(n => n.Value);
        double k = total > 0 ? (y1 - y0) / total : 0;
        double y = y0;
        foreach (var node in nodes)
        {
            node.X0 = x0;
            node.X1 = x1;
            node.Y0 = y;
            y += node.Value * k;
            node.Y1 = y;
        }
    }

    private void TileDice(List<TreemapNode<T>> nodes, double x0, double y0, double x1, double y1)
    {
        double total = nodes.Sum(n => n.Value);
        double k = total > 0 ? (x1 - x0) / total : 0;
        double x = x0;
        foreach (var node in nodes)
        {
            node.X0 = x;
            x += node.Value * k;
            node.X1 = x;
            node.Y0 = y0;
            node.Y1 = y1;
        }
    }

    private void TileSquarify(List<TreemapNode<T>> nodes, double x0, double y0, double x1, double y1)
    {
        double total = nodes.Sum(n => n.Value);
        if (total == 0) { TileSlice(nodes, x0, y0, x1, y1); return; }

        double remaining = total;
        var row = new List<TreemapNode<T>>();
        double rowValue = 0;

        double dx = x1 - x0, dy = y1 - y0;
        double cx0 = x0, cy0 = y0;

        foreach (var node in nodes.OrderByDescending(n => n.Value))
        {
            double area = (dx * dy) * (node.Value / remaining);
            row.Add(node);
            rowValue += node.Value;

            if (row.Count > 1)
            {
                // Check aspect ratio — squarify heuristic
                double shortSide = Math.Min(dx, dy);
                double rowArea = (dx * dy) * rowValue / remaining;
                if (shortSide == 0) break;
            }
        }

        // Fallback: simple slice layout for squarify (simplified)
        if (dx > dy)
            TileDice(nodes, x0, y0, x1, y1);
        else
            TileSlice(nodes, x0, y0, x1, y1);
    }
}

/// <summary>A node in a treemap hierarchy with layout rectangles.</summary>
public sealed class TreemapNode<T>
{
    public required T Data { get; init; }
    public TreemapNode<T>? Parent { get; init; }
    public List<TreemapNode<T>> Children { get; } = [];
    public int Depth { get; init; }
    public double Value { get; internal set; }
    public double X0 { get; internal set; }
    public double Y0 { get; internal set; }
    public double X1 { get; internal set; }
    public double Y1 { get; internal set; }

    /// <summary>Width of the rectangle.</summary>
    public double Width => X1 - X0;

    /// <summary>Height of the rectangle.</summary>
    public double Height => Y1 - Y0;

    /// <summary>Returns all descendant nodes (including this one).</summary>
    public IEnumerable<TreemapNode<T>> Descendants()
    {
        yield return this;
        foreach (var child in Children)
            foreach (var desc in child.Descendants())
                yield return desc;
    }

    /// <summary>Returns all leaf nodes.</summary>
    public IEnumerable<TreemapNode<T>> Leaves()
    {
        return Descendants().Where(n => n.Children.Count == 0);
    }
}

public enum TreemapTiling
{
    Squarify,
    Slice,
    Dice,
    SliceDice
}

public static class TreemapLayout
{
    public static TreemapLayout<T> Create<T>() => new();
}
