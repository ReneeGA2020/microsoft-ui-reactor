namespace Duct.Core.Navigation;

/// <summary>
/// Provides a static <see cref="DuctContext{T}"/> instance per TRoute type
/// for sharing a <see cref="NavigationHandle{TRoute}"/> through the element tree.
/// Uses the static-generic-class pattern so each TRoute gets its own singleton context
/// without per-render allocation.
/// </summary>
internal static class NavigationContext<TRoute> where TRoute : notnull
{
    internal static readonly DuctContext<NavigationHandle<TRoute>?> Instance =
        new(defaultValue: null, name: $"NavigationContext<{typeof(TRoute).Name}>");
}
