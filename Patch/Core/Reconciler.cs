using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;

namespace Patch.Core;

/// <summary>
/// The reconciler diffs old and new element trees and patches the real WinUI control tree.
///
/// Split across partial classes:
///   - Reconciler.cs        — orchestration, children, unmount, helpers
///   - Reconciler.Mount.cs  — Mount() dispatch + per-control MountXxx methods
///   - Reconciler.Update.cs — Update() dispatch + per-control UpdateXxx methods
/// </summary>
public sealed partial class Reconciler
{
    private readonly Dictionary<UIElement, ComponentNode> _componentNodes = new();

    public UIElement? Reconcile(
        Element? oldElement,
        Element? newElement,
        UIElement? existingControl,
        WinUI.Panel? parent,
        int childIndex,
        Action requestRerender)
    {
        if (newElement is null or EmptyElement)
        {
            if (existingControl is not null)
                Unmount(existingControl);
            return null;
        }

        if (oldElement is null or EmptyElement || existingControl is null)
            return Mount(newElement, requestRerender);

        if (CanUpdate(oldElement, newElement))
        {
            var replacement = Update(oldElement, newElement, existingControl, requestRerender);
            return replacement ?? existingControl;
        }

        Unmount(existingControl);
        return Mount(newElement, requestRerender);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Component reconciliation
    // ════════════════════════════════════════════════════════════════════

    private void ReconcileComponent(Element oldEl, Element newEl, UIElement control, Action requestRerender)
    {
        if (!_componentNodes.TryGetValue(control, out var node)) return;

        Element newChildElement;
        if (node.Component is not null)
        {
            node.Component.Context.BeginRender(requestRerender);
            newChildElement = node.Component.Render();
            node.Component.Context.FlushEffects();
        }
        else if (node.Context is not null && newEl is FuncElement func)
        {
            node.Context.BeginRender(requestRerender);
            newChildElement = func.RenderFunc(node.Context);
            node.Context.FlushEffects();
        }
        else return;

        var newControl = Reconcile(node.RenderedElement, newChildElement, control, null, 0, requestRerender);
        if (newControl is not null && newControl != control)
        {
            var parentEl = VisualTreeHelper.GetParent(control);
            if (parentEl is WinUI.Panel panel)
            {
                var idx = panel.Children.IndexOf(control);
                if (idx >= 0) panel.Children[idx] = newControl;
            }
            else if (parentEl is WinUI.Border border)
            {
                border.Child = newControl;
            }
            else if (parentEl is WinUI.ScrollViewer sv)
            {
                sv.Content = newControl;
            }

            _componentNodes.Remove(control);
            _componentNodes[newControl] = node;
        }

        node.RenderedElement = newChildElement;
        node.Element = newEl;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Children reconciliation (keyed LIS + positional)
    // ════════════════════════════════════════════════════════════════════

    private void ReconcileChildren(
        Element[] oldChildren, Element[] newChildren,
        WinUI.Panel panel, Action requestRerender)
    {
        var childCollection = new PanelChildCollection(panel);
        ChildReconciler.Reconcile(oldChildren, newChildren, childCollection, this, requestRerender);
    }

    private void ReconcileItemsChildren(
        Element[] oldChildren, Element[] newChildren,
        WinUI.ItemsControl itemsControl, Action requestRerender)
    {
        var childCollection = new ItemsControlChildCollection(itemsControl);
        ChildReconciler.Reconcile(oldChildren, newChildren, childCollection, this, requestRerender);
    }

    /// <summary>
    /// Called by ChildReconciler to update a single child element.
    /// Returns non-null if the child needs to be replaced (new control returned).
    /// </summary>
    internal UIElement? UpdateChild(Element oldEl, Element newEl, UIElement control, Action requestRerender)
    {
        return Update(oldEl, newEl, control, requestRerender);
    }

    /// <summary>
    /// Called by ChildReconciler to unmount a child.
    /// </summary>
    internal void UnmountChild(UIElement control)
    {
        Unmount(control);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Unmount
    // ════════════════════════════════════════════════════════════════════

    private void Unmount(UIElement control)
    {
        if (_componentNodes.TryGetValue(control, out var node))
        {
            node.Component?.Context.RunCleanups();
            node.Context?.RunCleanups();
            _componentNodes.Remove(control);
        }
        if (control is WinUI.Panel panel)
        {
            foreach (var child in panel.Children)
                Unmount(child);
        }
        else if (control is WinUI.Border border && border.Child is not null)
        {
            Unmount(border.Child);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  CanUpdate
    // ════════════════════════════════════════════════════════════════════

    internal bool CanUpdate(Element oldEl, Element newEl)
    {
        if (oldEl.GetType() != newEl.GetType()) return false;
        if (oldEl is ComponentElement oldComp && newEl is ComponentElement newComp)
            return oldComp.ComponentType == newComp.ComponentType;
        return true;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Shared helpers (used by Mount + Update)
    // ════════════════════════════════════════════════════════════════════

    internal static void ApplySetters<T>(Action<T>[] setters, T control) where T : class
    {
        foreach (var setter in setters) setter(control);
    }

    internal static void ApplyModifiers(FrameworkElement fe, ElementModifiers m)
    {
        if (m.Margin.HasValue) fe.Margin = m.Margin.Value;
        if (m.Width.HasValue) fe.Width = m.Width.Value;
        if (m.Height.HasValue) fe.Height = m.Height.Value;
        if (m.MinWidth.HasValue) fe.MinWidth = m.MinWidth.Value;
        if (m.MinHeight.HasValue) fe.MinHeight = m.MinHeight.Value;
        if (m.MaxWidth.HasValue) fe.MaxWidth = m.MaxWidth.Value;
        if (m.MaxHeight.HasValue) fe.MaxHeight = m.MaxHeight.Value;
        if (m.HorizontalAlignment.HasValue) fe.HorizontalAlignment = m.HorizontalAlignment.Value;
        if (m.VerticalAlignment.HasValue) fe.VerticalAlignment = m.VerticalAlignment.Value;
        if (m.Opacity.HasValue) fe.Opacity = m.Opacity.Value;
        if (m.IsVisible.HasValue)
            fe.Visibility = m.IsVisible.Value ? Visibility.Visible : Visibility.Collapsed;
        if (m.ToolTip is not null) WinUI.ToolTipService.SetToolTip(fe, m.ToolTip);
    }

    // ── Enum conversions removed — Patch now uses WinUI types directly ──

    internal static Symbol ParseSymbol(string name)
    {
        if (Enum.TryParse<Symbol>(name, ignoreCase: true, out var symbol)) return symbol;
        return Symbol.Placeholder;
    }

    // ── Grid definition parsing ─────────────────────────────────────

    internal static ColumnDefinition ParseColumnDef(string def) => def switch
    {
        "*" => new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
        "Auto" or "auto" => new ColumnDefinition { Width = GridLength.Auto },
        _ when double.TryParse(def, out var px) => new ColumnDefinition { Width = new GridLength(px) },
        _ when def.EndsWith('*') && double.TryParse(def[..^1], out var stars) =>
            new ColumnDefinition { Width = new GridLength(stars, GridUnitType.Star) },
        _ => new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
    };

    internal static RowDefinition ParseRowDef(string def) => def switch
    {
        "*" => new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
        "Auto" or "auto" => new RowDefinition { Height = GridLength.Auto },
        _ when double.TryParse(def, out var px) => new RowDefinition { Height = new GridLength(px) },
        _ when def.EndsWith('*') && double.TryParse(def[..^1], out var stars) =>
            new RowDefinition { Height = new GridLength(stars, GridUnitType.Star) },
        _ => new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
    };

    internal class ComponentNode
    {
        public Component? Component;
        public RenderContext? Context;
        public Element? RenderedElement;
        public Element? Element;
    }
}
