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
    };

    private readonly Dictionary<Type, Stack<FrameworkElement>> _pools = new();

    /// <summary>
    /// Try to rent an element of the given type from the pool.
    /// Returns null if the pool is empty or the type is not poolable.
    /// </summary>
    public FrameworkElement? TryRent(Type type)
    {
        if (!PoolableTypes.Contains(type)) return null;
        if (!_pools.TryGetValue(type, out var stack) || stack.Count == 0) return null;
        return stack.Pop();
    }

    /// <summary>
    /// Return an element to the pool after unmount. Cleans it first.
    /// Silently drops if the type is not poolable or the pool is full.
    /// </summary>
    public void Return(FrameworkElement element)
    {
        var type = element.GetType();
        if (!PoolableTypes.Contains(type)) return;

        if (!_pools.TryGetValue(type, out var stack))
        {
            stack = new Stack<FrameworkElement>();
            _pools[type] = stack;
        }

        if (stack.Count >= MaxPerType) return;

        CleanElement(element);
        stack.Push(element);
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
        }
    }
}
