using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Duct;

/// <summary>
/// Lookup helpers for WinUI theme resources.
/// These resolve against Application.Current.Resources, which includes
/// all merged dictionaries (XamlControlsResources, custom themes, etc.).
///
/// Usage:
///   var brush = ThemeResource.Brush("TextFillColorSecondaryBrush");
///   var radius = ThemeResource.CornerRadius("OverlayCornerRadius");
/// </summary>
public static class ThemeResource
{
    public static Brush Brush(string key) =>
        (Brush)Application.Current.Resources[key];

    public static double Double(string key) =>
        (double)Application.Current.Resources[key];

    public static CornerRadius CornerRadius(string key) =>
        (CornerRadius)Application.Current.Resources[key];

    public static Thickness Thickness(string key) =>
        (Thickness)Application.Current.Resources[key];

    /// <summary>
    /// Try to look up a resource, returning default if not found.
    /// </summary>
    public static T Get<T>(string key, T defaultValue = default!)
    {
        if (Application.Current.Resources.TryGetValue(key, out var value) && value is T typed)
            return typed;
        return defaultValue;
    }
}
