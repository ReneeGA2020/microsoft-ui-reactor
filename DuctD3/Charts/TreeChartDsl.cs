// Tree and Force Graph chart DSL for Duct integration

using Duct.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinShapes = Microsoft.UI.Xaml.Shapes;

namespace Duct.D3.Charts;

public static partial class ChartDsl
{
    /// <summary>
    /// Creates a tree diagram from hierarchical data.
    /// </summary>
    public static TreeChartElement<T> TreeChart<T>(
        T root,
        Func<T, IEnumerable<T>?> children,
        Func<T, string>? label = null) =>
        new()
        {
            Root = root,
            ChildrenAccessor = children,
            LabelAccessor = label,
        };

    /// <summary>
    /// Creates a force-directed graph.
    /// </summary>
    public static ForceGraphElement ForceGraph(
        IReadOnlyList<ForceNode> nodes,
        IReadOnlyList<ForceLink> links) =>
        new()
        {
            InputNodes = nodes,
            InputLinks = links,
        };
}

/// <summary>
/// Tree diagram element for Duct's virtual tree.
/// </summary>
public sealed class TreeChartElement<T>
{
    internal T Root { get; init; } = default!;
    internal Func<T, IEnumerable<T>?> ChildrenAccessor { get; init; } = _ => null;
    internal Func<T, string>? LabelAccessor { get; init; }

    private double _width = 600;
    private double _height = 400;
    private string _linkColor = "#999999";
    private string _nodeColor = "#4285f4";
    private double _nodeRadius = 6;
    private Action<TreeChartHandle>? _onReady;

    public TreeChartElement<T> Width(double w) { _width = w; return this; }
    public TreeChartElement<T> Height(double h) { _height = h; return this; }
    public TreeChartElement<T> LinkColor(string c) { _linkColor = c; return this; }
    public TreeChartElement<T> NodeColor(string c) { _nodeColor = c; return this; }
    public TreeChartElement<T> NodeRadius(double r) { _nodeRadius = r; return this; }
    public TreeChartElement<T> OnReady(Action<TreeChartHandle> callback) { _onReady = callback; return this; }

    public Element ToElement() => new XamlHostElement(BuildCanvas, UpdateCanvas) { TypeKey = "DuctD3Tree" };
    public static implicit operator Element(TreeChartElement<T> chart) => chart.ToElement();

    private FrameworkElement BuildCanvas()
    {
        var canvas = new Canvas { Width = _width, Height = _height };
        FullRender(canvas, Root);
        _onReady?.Invoke(new TreeChartHandle(canvas, root => FullRender(canvas, (T)root)));
        return canvas;
    }

    private void UpdateCanvas(FrameworkElement fe)
    {
        if (fe is Canvas canvas)
        {
            FullRender(canvas, Root);
            _onReady?.Invoke(new TreeChartHandle(canvas, root => FullRender(canvas, (T)root)));
        }
    }

    private void FullRender(Canvas canvas, T rootData)
    {
        canvas.Children.Clear();
        var layout = TreeLayout.Create<T>().Size(_width, _height);
        var root = layout.Hierarchy(rootData, ChildrenAccessor);
        layout.Layout(root);

        var linkBrush = ChartElement<object>.ColorToBrush(_linkColor);
        var nodeBrush = ChartElement<object>.ColorToBrush(_nodeColor);
        var textBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 60, 60, 60));

        // Draw links first (behind nodes)
        Visit(root, node =>
        {
            foreach (var child in node.Children)
            {
                // Draw curved link using a path
                var pb = new PathBuilder(3);
                pb.MoveTo(node.X, node.Y);
                double midY = (node.Y + child.Y) / 2;
                pb.BezierCurveTo(node.X, midY, child.X, midY, child.X, child.Y);

                var path = new WinShapes.Path
                {
                    Data = PathDataParser.Parse(pb.ToString()),
                    Stroke = linkBrush,
                    StrokeThickness = 1.5,
                };
                canvas.Children.Add(path);
            }
        });

        // Draw nodes
        Visit(root, node =>
        {
            var ellipse = new WinShapes.Ellipse
            {
                Width = _nodeRadius * 2,
                Height = _nodeRadius * 2,
                Fill = node.Children.Count > 0 ? nodeBrush : new SolidColorBrush(Microsoft.UI.Colors.White),
                Stroke = nodeBrush,
                StrokeThickness = 2,
            };
            Canvas.SetLeft(ellipse, node.X - _nodeRadius);
            Canvas.SetTop(ellipse, node.Y - _nodeRadius);
            canvas.Children.Add(ellipse);

            // Label
            if (LabelAccessor != null)
            {
                var label = new TextBlock
                {
                    Text = LabelAccessor(node.Data),
                    FontSize = 10,
                    Foreground = textBrush,
                };
                double labelX = node.Children.Count > 0 ? node.X - 15 : node.X + _nodeRadius + 4;
                double labelY = node.Children.Count > 0 ? node.Y - _nodeRadius - 14 : node.Y - 6;
                Canvas.SetLeft(label, labelX);
                Canvas.SetTop(label, labelY);
                canvas.Children.Add(label);
            }
        });
    }

    private static void Visit(TreeNode<T> node, Action<TreeNode<T>> action)
    {
        action(node);
        foreach (var child in node.Children) Visit(child, action);
    }
}

public sealed class TreeChartHandle
{
    private readonly Canvas _canvas;
    private readonly Action<object> _redraw;
    internal TreeChartHandle(Canvas canvas, Action<object> redraw) { _canvas = canvas; _redraw = redraw; }
    public Canvas Canvas => _canvas;
    public void Redraw<T>(T root) => _redraw(root!);
}

/// <summary>
/// Force-directed graph element for Duct's virtual tree.
/// Pure renderer — draws nodes, links, labels from a ForceSimulation's current state.
/// Interaction (drag, animation) is the caller's responsibility.
/// </summary>
public sealed class ForceGraphElement
{
    internal IReadOnlyList<ForceNode> InputNodes { get; init; } = [];
    internal IReadOnlyList<ForceLink> InputLinks { get; init; } = [];

    private double _width = 600;
    private double _height = 400;
    private string _linkColor = "#cccccc";
    private string _nodeColor = "#4285f4";
    private double _chargeStrength = -100;
    private double _linkDistance = 60;
    private int _iterations = 300;
    private Action<ForceGraphHandle>? _onReady;

    public ForceGraphElement Width(double w) { _width = w; return this; }
    public ForceGraphElement Height(double h) { _height = h; return this; }
    public ForceGraphElement LinkColor(string c) { _linkColor = c; return this; }
    public ForceGraphElement NodeColor(string c) { _nodeColor = c; return this; }
    public ForceGraphElement Charge(double strength) { _chargeStrength = strength; return this; }
    public ForceGraphElement Distance(double d) { _linkDistance = d; return this; }
    public ForceGraphElement Iterations(int n) { _iterations = n; return this; }

    /// <summary>
    /// Called after the graph is rendered with a handle that exposes the simulation,
    /// WinUI elements, and a <c>SyncPositions</c> method. Use this to wire up
    /// drag behaviour, animation timers, or anything else from the call site.
    /// </summary>
    public ForceGraphElement OnReady(Action<ForceGraphHandle> callback) { _onReady = callback; return this; }

    public Element ToElement() => new XamlHostElement(BuildCanvas, UpdateCanvas) { TypeKey = "DuctD3Force" };
    public static implicit operator Element(ForceGraphElement chart) => chart.ToElement();

    private ForceSimulation? _sim;

    private FrameworkElement BuildCanvas()
    {
        var canvas = new Canvas
        {
            Width = _width,
            Height = _height,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
        };
        RenderForceGraph(canvas);
        return canvas;
    }

    private void UpdateCanvas(FrameworkElement fe)
    {
        if (fe is Canvas canvas)
        {
            canvas.Children.Clear();
            canvas.Width = _width;
            canvas.Height = _height;
            RenderForceGraph(canvas);
        }
    }

    private void RenderForceGraph(Canvas canvas)
    {
        if (InputNodes.Count == 0) return;

        if (_sim == null)
        {
            _sim = new ForceSimulation()
                .SetNodes(InputNodes)
                .SetLinks(InputLinks)
                .ChargeStrength(_chargeStrength)
                .Center(_width / 2, _height / 2)
                .LinkDistance(_linkDistance)
                .CollisionRadius(12)
                .InitializePositions()
                .Run(_iterations);
        }

        var linkBrush = ChartElement<object>.ColorToBrush(_linkColor);
        var palette = D3Color.Category10;

        var lines = new WinShapes.Line[_sim.Links.Count];
        var ellipses = new WinShapes.Ellipse[_sim.Nodes.Count];
        var labels = new TextBlock?[_sim.Nodes.Count];

        // Draw links
        for (int li = 0; li < _sim.Links.Count; li++)
        {
            var link = _sim.Links[li];
            if (link.Source < 0 || link.Source >= _sim.Nodes.Count ||
                link.Target < 0 || link.Target >= _sim.Nodes.Count) continue;

            var s = _sim.Nodes[link.Source];
            var t = _sim.Nodes[link.Target];
            var line = new WinShapes.Line
            {
                X1 = s.X, Y1 = s.Y,
                X2 = t.X, Y2 = t.Y,
                Stroke = linkBrush,
                StrokeThickness = 1,
            };
            lines[li] = line;
            canvas.Children.Add(line);
        }

        // Draw nodes
        for (int i = 0; i < _sim.Nodes.Count; i++)
        {
            var node = _sim.Nodes[i];
            var color = palette[i % palette.Length];
            var brush = new SolidColorBrush(Windows.UI.Color.FromArgb(
                (byte)(color.Opacity * 255), color.R, color.G, color.B));

            var ellipse = new WinShapes.Ellipse
            {
                Width = node.Radius * 2,
                Height = node.Radius * 2,
                Fill = brush,
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.White),
                StrokeThickness = 1.5,
            };
            Canvas.SetLeft(ellipse, node.X - node.Radius);
            Canvas.SetTop(ellipse, node.Y - node.Radius);
            ellipses[i] = ellipse;
            canvas.Children.Add(ellipse);

            if (node.Label != null)
            {
                var label = new TextBlock
                {
                    Text = node.Label,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 60, 60, 60)),
                    IsHitTestVisible = false,
                };
                Canvas.SetLeft(label, node.X + node.Radius + 3);
                Canvas.SetTop(label, node.Y - 7);
                labels[i] = label;
                canvas.Children.Add(label);
            }
        }

        // Hand the live references to the caller
        _onReady?.Invoke(new ForceGraphHandle(_sim, canvas, ellipses, labels, lines));
    }
}

/// <summary>
/// Exposes the live simulation and WinUI elements so callers can implement
/// drag, animation, hover, or any other interaction outside the library.
/// </summary>
public sealed class ForceGraphHandle
{
    public ForceSimulation Simulation { get; }
    public Canvas Canvas { get; }
    public WinShapes.Ellipse[] NodeEllipses { get; }
    public TextBlock?[] NodeLabels { get; }
    public WinShapes.Line[] LinkLines { get; }

    internal ForceGraphHandle(
        ForceSimulation sim, Canvas canvas,
        WinShapes.Ellipse[] ellipses, TextBlock?[] labels, WinShapes.Line[] lines)
    {
        Simulation = sim;
        Canvas = canvas;
        NodeEllipses = ellipses;
        NodeLabels = labels;
        LinkLines = lines;
    }

    /// <summary>
    /// Pushes current ForceNode positions into the WinUI elements (ellipses, labels, link endpoints).
    /// Call this after Simulation.Tick() to animate the display.
    /// </summary>
    public void SyncPositions()
    {
        var nodes = Simulation.Nodes;
        var links = Simulation.Links;

        for (int i = 0; i < nodes.Count && i < NodeEllipses.Length; i++)
        {
            var n = nodes[i];
            Canvas.SetLeft(NodeEllipses[i], n.X - n.Radius);
            Canvas.SetTop(NodeEllipses[i], n.Y - n.Radius);

            if (i < NodeLabels.Length && NodeLabels[i] is TextBlock lbl)
            {
                Canvas.SetLeft(lbl, n.X + n.Radius + 3);
                Canvas.SetTop(lbl, n.Y - 7);
            }
        }

        for (int li = 0; li < links.Count && li < LinkLines.Length; li++)
        {
            var link = links[li];
            if (LinkLines[li] == null) continue;
            if (link.Source >= 0 && link.Source < nodes.Count)
            {
                LinkLines[li].X1 = nodes[link.Source].X;
                LinkLines[li].Y1 = nodes[link.Source].Y;
            }
            if (link.Target >= 0 && link.Target < nodes.Count)
            {
                LinkLines[li].X2 = nodes[link.Target].X;
                LinkLines[li].Y2 = nodes[link.Target].Y;
            }
        }
    }
}
