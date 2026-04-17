using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.Controls;

/// <summary>
/// DSL factory methods for PropertyGrid.
/// </summary>
public static class PropertyGridDsl
{
    /// <summary>
    /// Creates a PropertyGrid element for editing the target object's properties.
    /// Keyed by target type so switching between different target types creates
    /// a fresh component (avoiding broken reconciliation of different structures).
    /// </summary>
    public static Element PropertyGrid(
        object target,
        TypeRegistry registry,
        Action<object>? onRootChanged = null)
    {
        var props = new PropertyGridElement
        {
            Target = target,
            Registry = registry,
            OnRootChanged = onRootChanged,
        };

        return Component<PropertyGridComponent, PropertyGridElement>(props)
            .WithKey($"pg-{target.GetType().FullName}");
    }
}
