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

    // MeasureOverride and ArrangeOverride use different Yoga strategies:
    //
    // MEASURE: We need to answer "how big does my content need to be?" — this is
    // the WinUI measure contract. We pass NaN (undefined) to CalculateLayout so
    // Yoga computes the root's size from its children's intrinsic sizes, matching
    // CSS default behavior where a flex container without explicit width/height is
    // content-sized. We set MaxWidth/MaxHeight on the root node so Yoga won't
    // exceed the available space (equivalent to CSS max-width/max-height).
    //
    // ARRANGE: We know the final allocated size, so we pass it as definite
    // dimensions to CalculateLayout. This lets Yoga distribute extra space to
    // children with flex-grow, fill stretched cross-axis items, etc.
    //
    // Without this split, a nested FlexPanel (e.g. a FlexRow inside a Border)
    // would expand to fill all available height during measure, because passing
    // availableSize as a definite constraint tells Yoga "my root IS this tall"
    // rather than "my root can be AT MOST this tall."

    protected override Size MeasureOverride(Size availableSize)
    {
        SyncYogaTree();
        SetRootConstraints(availableSize);

        // Set max constraints so Yoga content-sizes the root but won't overflow.
        _rootNode.MaxWidth = float.IsInfinity((float)availableSize.Width)
            ? YogaValue.Undefined
            : YogaValue.Point((float)availableSize.Width);
        _rootNode.MaxHeight = float.IsInfinity((float)availableSize.Height)
            ? YogaValue.Undefined
            : YogaValue.Point((float)availableSize.Height);

        // NaN = undefined: let Yoga compute content size rather than filling.
        _rootNode.CalculateLayout(float.NaN, float.NaN, LayoutDirection);

        // Measure each child at the size Yoga computed for it
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (_nodeCache.TryGetValue(child, out var childNode))
            {
                child.Measure(new Size(childNode.LayoutWidth, childNode.LayoutHeight));
            }
        }

        return new Size(_rootNode.LayoutWidth, _rootNode.LayoutHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Clear max constraints — finalSize is the definite allocated size.
        _rootNode.MaxWidth = YogaValue.Undefined;
        _rootNode.MaxHeight = YogaValue.Undefined;

        // Definite dimensions let Yoga distribute space via grow/shrink/stretch.
        _rootNode.CalculateLayout(
            (float)finalSize.Width,
            (float)finalSize.Height,
            LayoutDirection);

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (_nodeCache.TryGetValue(child, out var childNode))
            {
                child.Arrange(new Rect(
                    childNode.LayoutX,
                    childNode.LayoutY,
                    childNode.LayoutWidth,
                    childNode.LayoutHeight));
            }
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

                // Set measure function: delegates to WinUI Measure
                var capturedChild = child;
                childNode.MeasureFunction = (node, w, wMode, h, hMode) =>
                {
                    var constraintW = wMode == YogaMeasureMode.Undefined ? double.PositiveInfinity : w;
                    var constraintH = hMode == YogaMeasureMode.Undefined ? double.PositiveInfinity : h;
                    capturedChild.Measure(new Size(constraintW, constraintH));
                    return new YogaSize((float)capturedChild.DesiredSize.Width, (float)capturedChild.DesiredSize.Height);
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
