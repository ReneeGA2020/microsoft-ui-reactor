using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Duct.Core;

/// <summary>
/// Bridges WinUI's ItemsRepeater/ElementFactory to Duct's Reconciler.
/// GetElementCore calls the view builder then mounts; RecycleElementCore unmounts.
/// </summary>
public sealed class DuctElementFactory<T> : ElementFactory
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

        _reconciler.UnmountChild(args.Element);

        if (_pool is not null && args.Element is FrameworkElement fe)
            _pool.Return(fe);
    }
}
