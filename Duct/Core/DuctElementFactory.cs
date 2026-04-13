using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Duct.Core;

/// <summary>
/// Bridges WinUI's ItemsRepeater/ElementFactory to Duct's Reconciler.
/// GetElementCore calls the view builder then mounts; RecycleElementCore unmounts.
/// </summary>
#pragma warning disable CS8305 // ElementFactory is experimental; we coordinate with WinUI team
public sealed partial class DuctElementFactory<T> : ElementFactory
#pragma warning restore CS8305
{
    private readonly IReadOnlyList<T> _items;
    private readonly Func<T, int, Element> _viewBuilder;
    private readonly Reconciler _reconciler;
    private readonly Action _requestRerender;
    private readonly ElementPool? _pool;

    public DuctElementFactory(
        IReadOnlyList<T> items,
        Func<T, int, Element> viewBuilder,
        Reconciler reconciler,
        Action requestRerender,
        ElementPool? pool = null)
    {
        _items = items;
        _viewBuilder = viewBuilder;
        _reconciler = reconciler;
        _requestRerender = requestRerender;
        _pool = pool;
    }

    protected override UIElement GetElementCore(ElementFactoryGetArgs args)
    {
        var index = args.Data is int i ? i : 0;
        if (index < 0 || index >= _items.Count)
            return new TextBlock { Text = "" };

        var item = _items[index];
        var element = _viewBuilder(item, index);
        var control = _reconciler.Mount(element, _requestRerender);
        return control ?? new TextBlock { Text = "" };
    }

    protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
    {
        if (args.Element is null) return;

        // Clean up Duct state (component contexts, effects).
        _reconciler.UnmountChild(args.Element);

        // Pool interactive leaf controls for reuse. Layout containers (Panel, Border)
        // are NOT pooled here because ItemsRepeater may still reference the root element
        // during its layout pass and modifying children causes COMExceptions. Interactive
        // controls are safe to detach and pool because they are leaves with no children.
        if (_pool is not null)
            PoolInteractiveLeaves(args.Element);
    }

    /// <summary>
    /// Walk the recycled subtree and pool interactive leaf controls (Button, TextBox,
    /// ToggleSwitch). These are the most expensive controls to create and benefit most
    /// from pooling. Detaches each from its parent panel before returning to the pool.
    /// </summary>
    private void PoolInteractiveLeaves(UIElement root)
    {
        if (root is Microsoft.UI.Xaml.Controls.Panel panel)
        {
            // Walk children in reverse so removal doesn't shift indices
            for (int i = panel.Children.Count - 1; i >= 0; i--)
                PoolInteractiveLeaves(panel.Children[i]);
        }
        else if (root is Microsoft.UI.Xaml.Controls.Border border && border.Child is not null)
        {
            PoolInteractiveLeaves(border.Child);
        }
        else if (root is FrameworkElement fe && IsPoolableInteractive(fe))
        {
            _pool!.Return(fe);
        }
    }

    private static bool IsPoolableInteractive(FrameworkElement fe) =>
        fe is Microsoft.UI.Xaml.Controls.Button
        or TextBox
        or Microsoft.UI.Xaml.Controls.ToggleSwitch;
}
