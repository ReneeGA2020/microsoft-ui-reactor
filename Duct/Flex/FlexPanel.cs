// Standalone FlexPanel for WinUI3, powered by Duct.Layout (Yoga engine).
// No dependency on Duct.Core — usable in any WinUI3 app.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Duct.Layout;
using Duct.Flex;
using Windows.Foundation;

namespace Duct.Flex;

/// <summary>
/// A WinUI3 Panel that implements CSS Flexbox layout using the Yoga layout engine.
/// Can be used standalone in XAML or through the Duct framework.
/// </summary>
public partial class FlexPanel : Panel
{
    // ── Yoga node cache: one YogaNode per UIElement child ──
    private readonly Dictionary<UIElement, YogaNode> _nodeCache = new();
    private readonly YogaNode _rootNode = new();

    public FlexPanel()
    {
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Clear Yoga node cache when removed from the visual tree to avoid leaking references
        foreach (var node in _nodeCache.Values)
            _rootNode.RemoveChild(node);
        _nodeCache.Clear();
    }

    // ── Container dependency properties ──

    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register(nameof(Direction), typeof(FlexDirection), typeof(FlexPanel),
            new PropertyMetadata(FlexDirection.Row, OnContainerPropertyChanged));

    public static readonly DependencyProperty JustifyContentProperty =
        DependencyProperty.Register(nameof(JustifyContent), typeof(FlexJustify), typeof(FlexPanel),
            new PropertyMetadata(FlexJustify.FlexStart, OnContainerPropertyChanged));

    public static readonly DependencyProperty AlignItemsProperty =
        DependencyProperty.Register(nameof(AlignItems), typeof(FlexAlign), typeof(FlexPanel),
            new PropertyMetadata(FlexAlign.Stretch, OnContainerPropertyChanged));

    public static readonly DependencyProperty AlignContentProperty =
        DependencyProperty.Register(nameof(AlignContent), typeof(FlexAlign), typeof(FlexPanel),
            new PropertyMetadata(FlexAlign.FlexStart, OnContainerPropertyChanged));

    public static readonly DependencyProperty WrapProperty =
        DependencyProperty.Register(nameof(Wrap), typeof(FlexWrap), typeof(FlexPanel),
            new PropertyMetadata(FlexWrap.NoWrap, OnContainerPropertyChanged));

    public static readonly DependencyProperty LayoutDirectionProperty =
        DependencyProperty.Register(nameof(LayoutDirection), typeof(FlexLayoutDirection), typeof(FlexPanel),
            new PropertyMetadata(FlexLayoutDirection.LTR, OnContainerPropertyChanged));

    public static readonly DependencyProperty ColumnGapProperty =
        DependencyProperty.Register(nameof(ColumnGap), typeof(double), typeof(FlexPanel),
            new PropertyMetadata(0.0, OnContainerPropertyChanged));

    public static readonly DependencyProperty RowGapProperty =
        DependencyProperty.Register(nameof(RowGap), typeof(double), typeof(FlexPanel),
            new PropertyMetadata(0.0, OnContainerPropertyChanged));

    public static readonly DependencyProperty FlexPaddingProperty =
        DependencyProperty.Register(nameof(FlexPadding), typeof(Thickness), typeof(FlexPanel),
            new PropertyMetadata(default(Thickness), OnContainerPropertyChanged));

    public FlexDirection Direction
    {
        get => (FlexDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public FlexJustify JustifyContent
    {
        get => (FlexJustify)GetValue(JustifyContentProperty);
        set => SetValue(JustifyContentProperty, value);
    }

    public FlexAlign AlignItems
    {
        get => (FlexAlign)GetValue(AlignItemsProperty);
        set => SetValue(AlignItemsProperty, value);
    }

    public FlexAlign AlignContent
    {
        get => (FlexAlign)GetValue(AlignContentProperty);
        set => SetValue(AlignContentProperty, value);
    }

    public FlexWrap Wrap
    {
        get => (FlexWrap)GetValue(WrapProperty);
        set => SetValue(WrapProperty, value);
    }

    public FlexLayoutDirection LayoutDirection
    {
        get => (FlexLayoutDirection)GetValue(LayoutDirectionProperty);
        set => SetValue(LayoutDirectionProperty, value);
    }

    public double ColumnGap
    {
        get => (double)GetValue(ColumnGapProperty);
        set => SetValue(ColumnGapProperty, value);
    }

    public double RowGap
    {
        get => (double)GetValue(RowGapProperty);
        set => SetValue(RowGapProperty, value);
    }

    public Thickness FlexPadding
    {
        get => (Thickness)GetValue(FlexPaddingProperty);
        set => SetValue(FlexPaddingProperty, value);
    }

    private static void OnContainerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FlexPanel panel)
            panel.InvalidateMeasure();
    }

    // ── Attached properties (for children) ──

    public static readonly DependencyProperty GrowProperty =
        DependencyProperty.RegisterAttached("Grow", typeof(double), typeof(FlexPanel),
            new PropertyMetadata(0.0, OnChildPropertyChanged));

    public static readonly DependencyProperty ShrinkProperty =
        DependencyProperty.RegisterAttached("Shrink", typeof(double), typeof(FlexPanel),
            new PropertyMetadata(1.0, OnChildPropertyChanged));

    public static readonly DependencyProperty BasisProperty =
        DependencyProperty.RegisterAttached("Basis", typeof(double), typeof(FlexPanel),
            new PropertyMetadata(double.NaN, OnChildPropertyChanged));

    public static readonly DependencyProperty AlignSelfProperty =
        DependencyProperty.RegisterAttached("AlignSelf", typeof(FlexAlign), typeof(FlexPanel),
            new PropertyMetadata(FlexAlign.Auto, OnChildPropertyChanged));

    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.RegisterAttached("Position", typeof(FlexPositionType), typeof(FlexPanel),
            new PropertyMetadata(FlexPositionType.Relative, OnChildPropertyChanged));

    public static readonly DependencyProperty LeftProperty =
        DependencyProperty.RegisterAttached("Left", typeof(double), typeof(FlexPanel),
            new PropertyMetadata(double.NaN, OnChildPropertyChanged));

    public static readonly DependencyProperty TopProperty =
        DependencyProperty.RegisterAttached("Top", typeof(double), typeof(FlexPanel),
            new PropertyMetadata(double.NaN, OnChildPropertyChanged));

    public static readonly DependencyProperty RightProperty =
        DependencyProperty.RegisterAttached("Right", typeof(double), typeof(FlexPanel),
            new PropertyMetadata(double.NaN, OnChildPropertyChanged));

    public static readonly DependencyProperty BottomProperty =
        DependencyProperty.RegisterAttached("Bottom", typeof(double), typeof(FlexPanel),
            new PropertyMetadata(double.NaN, OnChildPropertyChanged));

    // Attached property static accessors
    public static void SetGrow(UIElement el, double value) => el.SetValue(GrowProperty, value);
    public static double GetGrow(UIElement el) => (double)el.GetValue(GrowProperty);

    public static void SetShrink(UIElement el, double value) => el.SetValue(ShrinkProperty, value);
    public static double GetShrink(UIElement el) => (double)el.GetValue(ShrinkProperty);

    public static void SetBasis(UIElement el, double value) => el.SetValue(BasisProperty, value);
    public static double GetBasis(UIElement el) => (double)el.GetValue(BasisProperty);

    public static void SetAlignSelf(UIElement el, FlexAlign value) => el.SetValue(AlignSelfProperty, value);
    public static FlexAlign GetAlignSelf(UIElement el) => (FlexAlign)el.GetValue(AlignSelfProperty);

    public static void SetPosition(UIElement el, FlexPositionType value) => el.SetValue(PositionProperty, value);
    public static FlexPositionType GetPosition(UIElement el) => (FlexPositionType)el.GetValue(PositionProperty);

    public static void SetLeft(UIElement el, double value) => el.SetValue(LeftProperty, value);
    public static double GetLeft(UIElement el) => (double)el.GetValue(LeftProperty);

    public static void SetTop(UIElement el, double value) => el.SetValue(TopProperty, value);
    public static double GetTop(UIElement el) => (double)el.GetValue(TopProperty);

    public static void SetRight(UIElement el, double value) => el.SetValue(RightProperty, value);
    public static double GetRight(UIElement el) => (double)el.GetValue(RightProperty);

    public static void SetBottom(UIElement el, double value) => el.SetValue(BottomProperty, value);
    public static double GetBottom(UIElement el) => (double)el.GetValue(BottomProperty);

    private static void OnChildPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement el && Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(el) is FlexPanel panel)
            panel.InvalidateMeasure();
    }

    // ── Layout ──

    // Two-pass MeasureOverride, modeled after WinUI Grid's multi-pass algorithm.
    //
    // Grid resolves star columns to pixel widths *during* MeasureOverride, then
    // measures children at those resolved widths. This lets text controls compute
    // correct wrapping before ArrangeOverride. We do the same with Yoga:
    //
    // PASS 1 (content-sizing): Run Yoga with NaN (undefined) root dimensions so
    // it computes the container's intrinsic size from children. This answers the
    // WinUI measure contract ("how big does my content need to be?") when the
    // parent offers infinite space.
    //
    // PASS 2 (flex distribution): If availableSize has definite dimensions, run
    // Yoga again with those dimensions so flex-grow/shrink can distribute space.
    // This is analogous to Grid's ResolveStar pass — it resolves proportional
    // sizes, then children are Measured at resolved widths so text wraps correctly.
    //
    // The desired size comes from the pass whose children were measured with
    // correct constraints — Pass 2 when it runs, Pass 1 otherwise. Returning
    // Pass 1's content size after Pass 2 would report heights distorted by
    // basis:0 children measured at near-zero width.

    // Cached child layout results from MeasureOverride, reused in ArrangeOverride
    // to avoid re-running Yoga (which calls child.Measure()) during the arrange
    // pass — calling Measure during Arrange can trigger LayoutCycleException.
    private struct ChildLayout { public float X, Y, Width, Height; }
    private readonly List<ChildLayout> _cachedChildLayouts = new();
    private Size _cachedDesiredSize;
    private bool _arranging;

    protected override Size MeasureOverride(Size availableSize)
    {
        SyncYogaTree();
        SetRootConstraints(availableSize);

        bool hasDefiniteWidth = !float.IsInfinity((float)availableSize.Width);
        bool hasDefiniteHeight = !float.IsInfinity((float)availableSize.Height);

        // ── Pass 1: Content-size layout ──
        // NaN = undefined: Yoga computes the root's size from children's intrinsic
        // sizes, matching CSS default where a flex container is content-sized.
        // MaxWidth/MaxHeight cap the result so it won't exceed available space.
        _rootNode.MaxWidth = hasDefiniteWidth
            ? YogaValue.Point((float)availableSize.Width)
            : YogaValue.Undefined;
        _rootNode.MaxHeight = hasDefiniteHeight
            ? YogaValue.Point((float)availableSize.Height)
            : YogaValue.Undefined;

        _rootNode.CalculateLayout(float.NaN, float.NaN, LayoutDirection);

        // ── Pass 2: Flex distribution layout ──
        // Like Grid's ResolveStar pass: distribute definite space on the MAIN
        // axis so grow/shrink work, but keep the CROSS axis as NaN so Yoga
        // content-sizes it from children measured at resolved main-axis widths.
        // This mirrors Grid where Auto rows (cross) get their height from cells
        // measured at resolved star column widths (main).
        //
        // Without this split, a FlexRow given 600×400 would claim 400px height
        // even if content only needs 30px — expanding to fill like a star row.
        bool isRow = Direction == FlexDirection.Row || Direction == FlexDirection.RowReverse;
        bool hasDefiniteMain = isRow ? hasDefiniteWidth : hasDefiniteHeight;

        if (hasDefiniteMain)
        {
            // Main axis: definite for grow/shrink distribution.
            // Cross axis: NaN for content-sizing, but capped by available space.
            if (isRow)
            {
                _rootNode.MaxWidth = YogaValue.Undefined;
                _rootNode.MaxHeight = hasDefiniteHeight
                    ? YogaValue.Point((float)availableSize.Height)
                    : YogaValue.Undefined;

                _rootNode.CalculateLayout(
                    (float)availableSize.Width, float.NaN, LayoutDirection);
            }
            else
            {
                _rootNode.MaxWidth = hasDefiniteWidth
                    ? YogaValue.Point((float)availableSize.Width)
                    : YogaValue.Undefined;
                _rootNode.MaxHeight = YogaValue.Undefined;

                _rootNode.CalculateLayout(
                    float.NaN, (float)availableSize.Height, LayoutDirection);
            }
        }

        // Capture desired size from whichever pass just ran. When Pass 2 ran,
        // its layout reflects children measured at resolved grow/shrink sizes —
        // the correct dimensions. Indefinite axes were passed as NaN, so Yoga
        // content-sized them from children's true measurements (not the Pass 1
        // measurements distorted by basis:0 → near-zero constraints).
        _cachedDesiredSize = new Size(_rootNode.LayoutWidth, _rootNode.LayoutHeight);

        // Cache child positions and measure children at Yoga's resolved sizes.
        // This fulfills the WinUI contract that all children must be Measured
        // during MeasureOverride, and caches positions for ArrangeOverride.
        _cachedChildLayouts.Clear();
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (_nodeCache.TryGetValue(child, out var childNode))
            {
                var layout = new ChildLayout
                {
                    X = childNode.LayoutX,
                    Y = childNode.LayoutY,
                    Width = childNode.LayoutWidth,
                    Height = childNode.LayoutHeight
                };
                _cachedChildLayouts.Add(layout);
                // Add margin back: Yoga's layout sizes are content-area,
                // but WinUI's Measure subtracts the child's Margin.
                var m = child is FrameworkElement cfe ? cfe.Margin : default;
                child.Measure(new Size(
                    layout.Width + m.Left + m.Right,
                    layout.Height + m.Top + m.Bottom));
            }
            else
            {
                _cachedChildLayouts.Add(default);
            }
        }

        return _cachedDesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // If finalSize matches what we measured for, use cached positions directly.
        // This avoids re-running Yoga (which would call child.Measure() via
        // MeasureFunction callbacks), preventing LayoutCycleException.
        bool sizeChanged =
            Math.Abs(finalSize.Width - _cachedDesiredSize.Width) > 0.5 ||
            Math.Abs(finalSize.Height - _cachedDesiredSize.Height) > 0.5;

        if (sizeChanged)
        {
            // Final size differs from measured size — re-run Yoga to redistribute
            // space, but suppress child.Measure() calls during this arrange pass.
            _arranging = true;
            try
            {
                _rootNode.MaxWidth = YogaValue.Undefined;
                _rootNode.MaxHeight = YogaValue.Undefined;
                _rootNode.CalculateLayout(
                    (float)finalSize.Width,
                    (float)finalSize.Height,
                    LayoutDirection);

                // Update cached positions from the new layout
                _cachedChildLayouts.Clear();
                for (int i = 0; i < Children.Count; i++)
                {
                    var child = Children[i];
                    if (_nodeCache.TryGetValue(child, out var childNode))
                    {
                        _cachedChildLayouts.Add(new ChildLayout
                        {
                            X = childNode.LayoutX,
                            Y = childNode.LayoutY,
                            Width = childNode.LayoutWidth,
                            Height = childNode.LayoutHeight
                        });
                    }
                    else
                    {
                        _cachedChildLayouts.Add(default);
                    }
                }
            }
            finally
            {
                _arranging = false;
            }
        }

        for (int i = 0; i < Children.Count && i < _cachedChildLayouts.Count; i++)
        {
            var layout = _cachedChildLayouts[i];
            var child = Children[i];
            // Expand arrange rect by margin: Yoga positions/sizes the content area,
            // but WinUI's Arrange subtracts the child's Margin from the rect.
            var m = child is FrameworkElement fe ? fe.Margin : default;
            child.Arrange(new Rect(
                layout.X - m.Left,
                layout.Y - m.Top,
                layout.Width + m.Left + m.Right,
                layout.Height + m.Top + m.Bottom));
        }

        return finalSize;
    }

    private void SetRootConstraints(Size availableSize)
    {
        // Container properties
        _rootNode.FlexDirection = Direction;
        _rootNode.JustifyContent = JustifyContent;
        _rootNode.AlignItems = AlignItems;
        _rootNode.AlignContent = AlignContent;
        _rootNode.FlexWrap = Wrap;
        _rootNode.SetGap(YogaGutter.Column, (float)ColumnGap);
        _rootNode.SetGap(YogaGutter.Row, (float)RowGap);

        // FlexPadding
        var p = FlexPadding;
        _rootNode.SetPadding(YogaEdge.Left, YogaValue.Point((float)p.Left));
        _rootNode.SetPadding(YogaEdge.Top, YogaValue.Point((float)p.Top));
        _rootNode.SetPadding(YogaEdge.Right, YogaValue.Point((float)p.Right));
        _rootNode.SetPadding(YogaEdge.Bottom, YogaValue.Point((float)p.Bottom));
    }

    private void SyncYogaTree()
    {
        // Remove nodes for children that are no longer present
        var currentChildren = new HashSet<UIElement>();
        foreach (UIElement child in Children)
            currentChildren.Add(child);

        var toRemove = new List<UIElement>();
        foreach (var kvp in _nodeCache)
        {
            if (!currentChildren.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }
        foreach (var el in toRemove)
        {
            if (_nodeCache.TryGetValue(el, out var node))
                _rootNode.RemoveChild(node);
            _nodeCache.Remove(el);
        }

        // Ensure each child has a YogaNode at the correct index
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (!_nodeCache.TryGetValue(child, out var childNode))
            {
                childNode = new YogaNode();
                _nodeCache[child] = childNode;

                // Set measure function: delegates to WinUI Measure.
                // During ArrangeOverride (_arranging=true), return the last
                // DesiredSize without calling Measure — calling Measure during
                // Arrange can trigger LayoutCycleException.
                //
                // Margin compensation: Yoga handles margins for positioning and
                // spacing between children (synced in ApplyAttachedProperties).
                // WinUI also subtracts Margin during Measure/Arrange. To avoid
                // double-counting, we add the margin back to Yoga's constraints
                // before calling WinUI Measure, and subtract it from DesiredSize
                // before returning to Yoga.
                var capturedChild = child;
                var panel = this;
                childNode.MeasureFunction = (node, w, wMode, h, hMode) =>
                {
                    var m = capturedChild is FrameworkElement cfe ? cfe.Margin : default;
                    double mH = m.Left + m.Right;
                    double mV = m.Top + m.Bottom;

                    if (panel._arranging)
                        return new YogaSize(
                            Math.Max(0, (float)(capturedChild.DesiredSize.Width - mH)),
                            Math.Max(0, (float)(capturedChild.DesiredSize.Height - mV)));

                    // Yoga's constraints are content-area (excluding margin).
                    // Add margin so WinUI's subtraction yields the correct content area.
                    var constraintW = wMode == YogaMeasureMode.Undefined ? double.PositiveInfinity : w + mH;
                    var constraintH = hMode == YogaMeasureMode.Undefined ? double.PositiveInfinity : h + mV;
                    capturedChild.Measure(new Size(constraintW, constraintH));
                    // Return content size (without margin) since Yoga tracks margins separately
                    return new YogaSize(
                        Math.Max(0, (float)(capturedChild.DesiredSize.Width - mH)),
                        Math.Max(0, (float)(capturedChild.DesiredSize.Height - mV)));
                };
            }

            // Apply attached properties from the UIElement to the YogaNode
            ApplyAttachedProperties(child, childNode);

            // Ensure correct child order in Yoga tree
            if (i < _rootNode.ChildCount)
            {
                if (_rootNode.GetChild(i) != childNode)
                {
                    // Remove if present elsewhere and re-insert at correct position
                    _rootNode.RemoveChild(childNode);
                    _rootNode.InsertChild(childNode, i);
                }
            }
            else
            {
                if (childNode.Owner != _rootNode)
                    _rootNode.InsertChild(childNode, i);
            }
        }

        // Remove extra Yoga children beyond current count
        while (_rootNode.ChildCount > Children.Count)
        {
            _rootNode.RemoveChild(_rootNode.ChildCount - 1);
        }
    }

    private static void ApplyAttachedProperties(UIElement el, YogaNode node)
    {
        var grow = GetGrow(el);
        var shrink = GetShrink(el);
        var basis = GetBasis(el);
        var alignSelf = GetAlignSelf(el);
        var position = GetPosition(el);

        node.Style.FlexGrow = (float)grow;
        node.Style.FlexShrink = (float)shrink;
        node.Style.FlexBasis = double.IsNaN(basis) ? YogaValue.Auto : YogaValue.Point((float)basis);
        node.Style.AlignSelf = alignSelf;
        node.Style.PositionType = position;

        // Position insets
        var left = GetLeft(el);
        var top = GetTop(el);
        var right = GetRight(el);
        var bottom = GetBottom(el);

        node.Style.Position[(int)YogaEdge.Left] = double.IsNaN(left) ? YogaValue.Undefined : YogaValue.Point((float)left);
        node.Style.Position[(int)YogaEdge.Top] = double.IsNaN(top) ? YogaValue.Undefined : YogaValue.Point((float)top);
        node.Style.Position[(int)YogaEdge.Right] = double.IsNaN(right) ? YogaValue.Undefined : YogaValue.Point((float)right);
        node.Style.Position[(int)YogaEdge.Bottom] = double.IsNaN(bottom) ? YogaValue.Undefined : YogaValue.Point((float)bottom);

        // If the child has explicit Width/Height set, pass them to Yoga
        if (el is FrameworkElement fe)
        {
            node.Width = double.IsNaN(fe.Width) ? YogaValue.Auto : YogaValue.Point((float)fe.Width);
            node.Height = double.IsNaN(fe.Height) ? YogaValue.Auto : YogaValue.Point((float)fe.Height);

            // Margins
            var margin = fe.Margin;
            if (margin.Left != 0 || margin.Top != 0 || margin.Right != 0 || margin.Bottom != 0)
            {
                node.SetMargin(YogaEdge.Left, YogaValue.Point((float)margin.Left));
                node.SetMargin(YogaEdge.Top, YogaValue.Point((float)margin.Top));
                node.SetMargin(YogaEdge.Right, YogaValue.Point((float)margin.Right));
                node.SetMargin(YogaEdge.Bottom, YogaValue.Point((float)margin.Bottom));
            }
            else
            {
                node.SetMargin(YogaEdge.Left, YogaValue.Undefined);
                node.SetMargin(YogaEdge.Top, YogaValue.Undefined);
                node.SetMargin(YogaEdge.Right, YogaValue.Undefined);
                node.SetMargin(YogaEdge.Bottom, YogaValue.Undefined);
            }
        }
    }
}
