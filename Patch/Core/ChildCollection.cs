using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Patch.Core;

/// <summary>
/// Abstraction over a collection of UIElement children.
/// Allows the reconciler to work with any container type (Panel, ItemsControl, etc.).
/// </summary>
internal interface IChildCollection
{
    int Count { get; }
    UIElement Get(int index);
    void Insert(int index, UIElement element);
    void RemoveAt(int index);
    void Move(int oldIndex, int newIndex);
    void Replace(int index, UIElement element);
}

/// <summary>
/// Wraps Panel.Children (StackPanel, Grid, Canvas, etc.).
/// </summary>
internal sealed class PanelChildCollection : IChildCollection
{
    private readonly UIElementCollection _children;

    public PanelChildCollection(WinUI.Panel panel)
    {
        _children = panel.Children;
    }

    public int Count => _children.Count;
    public UIElement Get(int index) => _children[index];
    public void Insert(int index, UIElement element) => _children.Insert(index, element);
    public void RemoveAt(int index) => _children.RemoveAt(index);

    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex) return;
        var item = _children[oldIndex];
        _children.RemoveAt(oldIndex);
        // newIndex is the final desired position — no adjustment needed.
        // After RemoveAt, inserting at newIndex places the item at that index
        // in the resulting collection.
        _children.Insert(newIndex, item);
    }

    public void Replace(int index, UIElement element)
    {
        _children[index] = element;
    }
}

/// <summary>
/// Wraps ItemsControl.Items (ListView, GridView, FlipView, etc.).
/// Items in these controls are objects (not necessarily UIElement), but we store UIElements.
/// </summary>
internal sealed class ItemsControlChildCollection : IChildCollection
{
    private readonly ItemCollection _items;

    public ItemsControlChildCollection(WinUI.ItemsControl itemsControl)
    {
        _items = itemsControl.Items;
    }

    public int Count => _items.Count;
    public UIElement Get(int index) => (UIElement)_items[index];
    public void Insert(int index, UIElement element) => _items.Insert(index, element);
    public void RemoveAt(int index) => _items.RemoveAt(index);

    public void Move(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex) return;
        var item = _items[oldIndex];
        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, item);
    }

    public void Replace(int index, UIElement element)
    {
        _items[index] = element;
    }
}
