using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.Controls;

/// <summary>
/// DSL factory methods for VirtualList.
/// </summary>
public static class VirtualListDsl
{
    /// <summary>
    /// Creates a VirtualList element — a count-based virtualized list.
    /// Items are rendered on demand via the renderItem callback.
    /// </summary>
    public static Element VirtualList(
        int itemCount,
        Func<int, Element> renderItem,
        Func<int, string>? getItemKey = null,
        double? itemHeight = null,
        double estimatedItemHeight = 40,
        double spacing = 0,
        Action<VirtualListRef>? @ref = null,
        Action<int, int>? onVisibleRangeChanged = null)
    {
        var props = new VirtualListElement
        {
            ItemCount = itemCount,
            RenderItem = renderItem,
            GetItemKey = getItemKey,
            ItemHeight = itemHeight,
            EstimatedItemHeight = estimatedItemHeight,
            Spacing = spacing,
            Ref = @ref,
            OnVisibleRangeChanged = onVisibleRangeChanged,
        };

        return Component<VirtualListComponent, VirtualListElement>(props)
            .WithKey($"vl-{itemCount}");
    }
}
