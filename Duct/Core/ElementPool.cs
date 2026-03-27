using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Duct.Core;

/// <summary>
/// Per-CLR-type pool (cap 32) that recycles unmounted WinUI FrameworkElement instances.
/// V1: pools only non-interactive controls (no event handlers to worry about).
/// </summary>
public sealed class ElementPool
{
    private const int MaxPerType = 32;

    private static readonly HashSet<Type> PoolableTypes = new()
    {
        typeof(TextBlock),
        typeof(WinUI.RichTextBlock),
        typeof(WinUI.StackPanel),
        typeof(WinUI.Grid),
        typeof(WinUI.Border),
        typeof(WinUI.ScrollViewer),
        typeof(WinUI.Canvas),
        typeof(WinUI.Viewbox),
        typeof(WinUI.ProgressBar),
        typeof(WinUI.ProgressRing),
        typeof(WinUI.Image),
        typeof(WinUI.InfoBadge),
        typeof(Monaco.MonacoEditor),
    };

    private readonly Dictionary<Type, Stack<FrameworkElement>> _pools = new();

    /// <summary>
    /// Try to rent an element of the given type from the pool.
    /// Returns null if the pool is empty or the type is not poolable.
    /// </summary>
    public FrameworkElement? TryRent(Type type)
    {
        if (!PoolableTypes.Contains(type)) { System.Diagnostics.Debug.WriteLine($"[Pool] TryRent({type.Name}) — not poolable"); return null; }
        if (!_pools.TryGetValue(type, out var stack) || stack.Count == 0) { System.Diagnostics.Debug.WriteLine($"[Pool] TryRent({type.Name}) — pool empty"); return null; }
        var item = stack.Pop();
        System.Diagnostics.Debug.WriteLine($"[Pool] TryRent({type.Name}) — GOT {item.GetHashCode()}, {stack.Count} remaining");
        return item;
    }

    /// <summary>
    /// Return an element to the pool after unmount. Cleans it first.
    /// Silently drops if the type is not poolable or the pool is full.
    /// </summary>
    public void Return(FrameworkElement element)
    {
        var type = element.GetType();
        if (!PoolableTypes.Contains(type)) { System.Diagnostics.Debug.WriteLine($"[Pool] Return({type.Name} {element.GetHashCode()}) — not poolable, DROPPED"); return; }

        if (!_pools.TryGetValue(type, out var stack))
        {
            stack = new Stack<FrameworkElement>();
            _pools[type] = stack;
        }

        if (stack.Count >= MaxPerType) { System.Diagnostics.Debug.WriteLine($"[Pool] Return({type.Name} {element.GetHashCode()}) — pool full, DROPPED"); return; }

        // Detach from parent before pooling — WinUI doesn't allow an element in two parents.
        // Use FrameworkElement.Parent (works even for detached trees, unlike VisualTreeHelper).
        DetachFromParent(element);

        System.Diagnostics.Debug.WriteLine($"[Pool] Return({type.Name} {element.GetHashCode()}) — POOLED, {stack.Count + 1} in pool");
        CleanElement(element);
        stack.Push(element);
    }

    /// <summary>
    /// Remove an element from its current parent so it can be safely re-parented.
    /// Uses FrameworkElement.Parent which works even for detached trees
    /// (unlike VisualTreeHelper.GetParent which requires a live visual tree).
    /// </summary>
    private static void DetachFromParent(FrameworkElement element)
    {
        var parent = element.Parent;
        switch (parent)
        {
            case WinUI.Panel panel:
                panel.Children.Remove(element);
                break;
            case WinUI.Border border when ReferenceEquals(border.Child, element):
                border.Child = null;
                break;
            case WinUI.ScrollViewer sv when ReferenceEquals(sv.Content, element):
                sv.Content = null;
                break;
            case WinUI.ContentControl cc when ReferenceEquals(cc.Content, element):
                cc.Content = null;
                break;
            case WinUI.UserControl uc when ReferenceEquals(uc.Content, element):
                uc.Content = null;
                break;
        }
    }

    /// <summary>
    /// Reset an element to a clean state suitable for reuse.
    /// </summary>
    internal static void CleanElement(FrameworkElement fe)
    {
        // Common properties
        fe.Tag = null;
        fe.Margin = new Thickness(0);
        fe.Width = double.NaN;
        fe.Height = double.NaN;
        fe.MinWidth = 0;
        fe.MinHeight = 0;
        fe.MaxWidth = double.PositiveInfinity;
        fe.MaxHeight = double.PositiveInfinity;
        fe.HorizontalAlignment = HorizontalAlignment.Stretch;
        fe.VerticalAlignment = VerticalAlignment.Stretch;
        fe.Opacity = 1.0;
        fe.Visibility = Visibility.Visible;

        // Type-specific cleanup
        switch (fe)
        {
            case WinUI.Panel panel:
                panel.Children.Clear();
                break;
            case WinUI.Border border:
                border.Child = null;
                border.Background = null;
                border.BorderBrush = null;
                border.BorderThickness = new Thickness(0);
                border.CornerRadius = new CornerRadius(0);
                border.Padding = new Thickness(0);
                break;
            case WinUI.ScrollViewer sv:
                sv.Content = null;
                break;
            case WinUI.Viewbox vb:
                vb.Child = null;
                break;
            case TextBlock tb:
                tb.Text = "";
                tb.FontSize = 14; // WinUI default
                tb.ClearValue(TextBlock.FontWeightProperty);
                break;
            case WinUI.RichTextBlock rtb:
                rtb.Blocks.Clear();
                break;
            case WinUI.ProgressBar pb:
                pb.IsIndeterminate = false;
                pb.Value = 0;
                pb.Minimum = 0;
                pb.Maximum = 100;
                pb.ShowError = false;
                pb.ShowPaused = false;
                break;
            case WinUI.ProgressRing pr:
                pr.IsIndeterminate = false;
                pr.IsActive = true;
                pr.Value = 0;
                pr.Minimum = 0;
                pr.Maximum = 100;
                break;
            case WinUI.Image img:
                img.Source = null;
                break;
            case WinUI.InfoBadge badge:
                badge.Value = -1; // WinUI default (hidden)
                break;
            case Monaco.MonacoEditor:
                // Keep the WebView2 alive — just clear the tag.
                // Properties will be updated on next mount via dependency properties.
                break;
        }
    }
}
