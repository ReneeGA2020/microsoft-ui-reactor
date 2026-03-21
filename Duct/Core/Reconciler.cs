using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;

namespace Duct.Core;

/// <summary>
/// Controls which reconciliation engine is used for tree diffing.
/// </summary>
public enum ReconcileMode
{
    /// <summary>Use native DiffTrees when available, fall back to C#.</summary>
    Auto,
    /// <summary>Force native Rust DiffTrees path (throws if DLL not available).</summary>
    NativeDiffTree,
    /// <summary>Force pure C# imperative reconciliation.</summary>
    CSharpFallback,
}

/// <summary>
/// The reconciler diffs old and new element trees and patches the real WinUI control tree.
///
/// Split across partial classes:
///   - Reconciler.cs           — orchestration, children, unmount, helpers
///   - Reconciler.Mount.cs     — Mount() dispatch + per-control MountXxx methods
///   - Reconciler.Update.cs    — Update() dispatch + per-control UpdateXxx methods
///   - Reconciler.DiffTrees.cs — native Rust DiffTrees reconciliation path
/// </summary>
public sealed partial class Reconciler : IDisposable
{
    private readonly Dictionary<UIElement, ComponentNode> _componentNodes = new();
    private readonly ElementPool _pool = new();
    private readonly Dictionary<Type, ITypeRegistration> _typeRegistry = new();

    /// <summary>
    /// Associates a control with its current element via Tag.
    /// Only call for interactive controls that need the Tag-based event handler pattern.
    /// Layout-only controls (Border, StackPanel, TextBlock, etc.) should NOT set Tag
    /// to avoid expensive COM DependencyProperty calls on the hot path.
    /// </summary>
    internal static void SetElementTag(FrameworkElement control, Element element) => control.Tag = element;

    /// <summary>
    /// Retrieves the element associated with a control via Tag, or null.
    /// </summary>
    internal static Element? GetElementTag(UIElement control) =>
        control is FrameworkElement fe ? fe.Tag as Element : null;
    private ViewDiffer? _differ;
    private bool _differChecked;

    /// <summary>
    /// Controls which diffing engine is used. Default is Auto (native when available).
    /// Set to CSharpFallback or NativeDiffTree to force a specific path for A/B testing.
    /// </summary>
    public ReconcileMode Mode { get; set; } = ReconcileMode.Auto;

    /// <summary>
    /// Returns the native Rust differ if available, or null if the native DLL is not present.
    /// The differ instance is reused across reconciliation passes for amortized allocation.
    /// </summary>
    internal ViewDiffer? Differ
    {
        get
        {
            if (!_differChecked)
            {
                _differChecked = true;
                try
                {
                    _differ = new ViewDiffer();
                }
                catch (DllNotFoundException)
                {
                    // Native differ not available; fall back to C# implementation
                }
            }
            return _differ;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Extensible type registry (Feature 1: RegisterType API)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Registers a custom element type so the reconciler knows how to mount, update, and unmount it.
    /// Registered types take priority over built-in types.
    ///
    /// The mount and update handlers receive the Reconciler instance so they can
    /// recursively mount/update/unmount child elements without capturing external state.
    /// </summary>
    public void RegisterType<TElement, TControl>(
        Func<Reconciler, TElement, Action, TControl> mount,
        Func<Reconciler, TElement, TElement, TControl, Action, UIElement?> update,
        Action<Reconciler, TControl>? unmount = null)
        where TElement : Element
        where TControl : UIElement
    {
        _typeRegistry[typeof(TElement)] = new TypeRegistration<TElement, TControl>(mount, update, unmount);
    }

    internal interface ITypeRegistration
    {
        UIElement Mount(Element element, Action requestRerender, Reconciler reconciler);
        UIElement? Update(Element oldEl, Element newEl, UIElement control, Action requestRerender, Reconciler reconciler);
        void Unmount(UIElement control, Reconciler reconciler);
        bool HasUnmount { get; }
    }

    private sealed class TypeRegistration<TElement, TControl> : ITypeRegistration
        where TElement : Element
        where TControl : UIElement
    {
        private readonly Func<Reconciler, TElement, Action, TControl> _mount;
        private readonly Func<Reconciler, TElement, TElement, TControl, Action, UIElement?> _update;
        private readonly Action<Reconciler, TControl>? _unmount;

        public TypeRegistration(
            Func<Reconciler, TElement, Action, TControl> mount,
            Func<Reconciler, TElement, TElement, TControl, Action, UIElement?> update,
            Action<Reconciler, TControl>? unmount)
        {
            _mount = mount;
            _update = update;
            _unmount = unmount;
        }

        public bool HasUnmount => _unmount is not null;

        public UIElement Mount(Element element, Action requestRerender, Reconciler reconciler)
            => _mount(reconciler, (TElement)element, requestRerender);

        public UIElement? Update(Element oldEl, Element newEl, UIElement control, Action requestRerender, Reconciler reconciler)
            => _update(reconciler, (TElement)oldEl, (TElement)newEl, (TControl)control, requestRerender);

        public void Unmount(UIElement control, Reconciler reconciler)
            => _unmount?.Invoke(reconciler, (TControl)control);
    }

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

        // Top-level DiffTrees is not used on the hot path — components are opaque
        // to the serializer, so most real apps fall through to imperative anyway.
        // The Rust differ is used at the ChildReconciler level (ReconcileKeys)
        // which is controlled by the Mode property.

        return ReconcileImperative(oldElement, newElement, existingControl, requestRerender);
    }

    /// <summary>
    /// The original C# imperative reconciliation path.
    /// </summary>
    private UIElement? ReconcileImperative(
        Element oldElement, Element newElement,
        UIElement existingControl, Action requestRerender)
    {
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
            // Update props before re-rendering so the component sees fresh data
            if (newEl is ComponentElement compEl && compEl.Props is not null
                && node.Component is IPropsReceiver receiver)
            {
                receiver.SetProps(compEl.Props);
            }

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

    /// <summary>
    /// Returns the differ to use for child reconciliation, respecting ReconcileMode.
    /// CSharpFallback forces C# LIS even when native is available.
    /// </summary>
    private ViewDiffer? EffectiveDiffer => Mode == ReconcileMode.CSharpFallback ? null : Differ;

    private void ReconcileChildren(
        Element[] oldChildren, Element[] newChildren,
        WinUI.Panel panel, Action requestRerender)
    {
        var childCollection = new PanelChildCollection(panel);
        ChildReconciler.Reconcile(oldChildren, newChildren, childCollection, this, EffectiveDiffer, requestRerender);
    }

    private void ReconcileItemsChildren(
        Element[] oldChildren, Element[] newChildren,
        WinUI.ItemsControl itemsControl, Action requestRerender)
    {
        var childCollection = new ItemsControlChildCollection(itemsControl);
        ChildReconciler.Reconcile(oldChildren, newChildren, childCollection, this, EffectiveDiffer, requestRerender);
    }

    /// <summary>
    /// Updates a single child element. Returns non-null if the child control was replaced.
    /// Public so registered type handlers can recursively reconcile children.
    /// </summary>
    public UIElement? UpdateChild(Element oldEl, Element newEl, UIElement control, Action requestRerender)
    {
        return Update(oldEl, newEl, control, requestRerender);
    }

    /// <summary>
    /// Unmounts a child control. Public so registered type handlers can unmount children.
    /// </summary>
    public void UnmountChild(UIElement control)
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

        // Check registered type unmount handlers via Tag
        if (control is FrameworkElement fe && fe.Tag is Element tagEl
            && _typeRegistry.TryGetValue(tagEl.GetType(), out var reg) && reg.HasUnmount)
        {
            reg.Unmount(control, this);
            return;
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
        else if (control is WinUI.ScrollViewer sv && sv.Content is UIElement svChild)
        {
            Unmount(svChild);
        }
    }

    /// <summary>
    /// Unmounts and returns the element to the pool.
    /// Only call after the element has been detached from the visual tree.
    /// </summary>
    internal void UnmountAndPool(UIElement control)
    {
        Unmount(control);
        if (control is FrameworkElement fe)
            _pool.Return(fe);
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

    internal void ApplyModifiers(FrameworkElement fe, ElementModifiers m, Action requestRerender)
        => ApplyModifiers(fe, null, m, requestRerender);

    internal void ApplyModifiers(FrameworkElement fe, ElementModifiers? oldM, ElementModifiers m, Action requestRerender)
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
        if (m.RichToolTip is not null)
        {
            var oldTipEl = oldM?.RichToolTip;
            var existingTip = WinUI.ToolTipService.GetToolTip(fe) as UIElement;
            if (oldTipEl is not null && existingTip is not null && CanUpdate(oldTipEl, m.RichToolTip))
            {
                var replacement = Update(oldTipEl, m.RichToolTip, existingTip, requestRerender);
                if (replacement is not null)
                    WinUI.ToolTipService.SetToolTip(fe, replacement);
            }
            else
            {
                WinUI.ToolTipService.SetToolTip(fe, Mount(m.RichToolTip, requestRerender));
            }
        }
        else if (m.ToolTip is not null)
            WinUI.ToolTipService.SetToolTip(fe, m.ToolTip);

        if (m.AttachedFlyout is not null)
            ApplyFlyoutAttachment(fe, oldM?.AttachedFlyout, m.AttachedFlyout, requestRerender);

        if (m.ContextFlyout is not null)
        {
            var oldContextEl = oldM?.ContextFlyout;
            if (oldContextEl is not null && fe.ContextFlyout is WinPrim.FlyoutBase existingCtx)
                UpdateFlyoutInPlace(existingCtx, oldContextEl, m.ContextFlyout, requestRerender);
            else
                fe.ContextFlyout = CreateFlyoutFromElement(m.ContextFlyout, requestRerender);
        }
    }

    /// <summary>
    /// Sets or updates the flyout on a control. On first mount, creates a new flyout.
    /// On update, reconciles the content inside the existing flyout to keep it open.
    /// </summary>
    private void ApplyFlyoutAttachment(FrameworkElement fe, Element? oldFlyoutEl, Element newFlyoutEl, Action requestRerender)
    {
        // Try to get the existing flyout from the control.
        // SplitButton.Flyout and Button.Flyout are separate properties (different type hierarchies).
        WinPrim.FlyoutBase? existingFlyout = fe switch
        {
            WinUI.SplitButton sb => sb.Flyout,
            WinUI.Button btn => btn.Flyout,  // AppBarButton inherits from Button
            _ => WinPrim.FlyoutBase.GetAttachedFlyout(fe),
        };

        // If we have an existing flyout and old element, try to update in place
        if (oldFlyoutEl is not null && existingFlyout is not null)
        {
            UpdateFlyoutInPlace(existingFlyout, oldFlyoutEl, newFlyoutEl, requestRerender);
            return;
        }

        // First mount — create new flyout
        var flyout = CreateFlyoutFromElement(newFlyoutEl, requestRerender);
        if (flyout is null) return;

        SetFlyoutOnControl(fe, flyout);
    }

    /// <summary>
    /// Updates the content inside an existing flyout without replacing the flyout object.
    /// This keeps the flyout open while its content changes.
    /// </summary>
    private void UpdateFlyoutInPlace(WinPrim.FlyoutBase existingFlyout, Element oldEl, Element newEl, Action requestRerender)
    {
        // ContentFlyout → reconcile child content inside the existing Flyout
        if (newEl is ContentFlyoutElement newCf && existingFlyout is WinUI.Flyout flyout)
        {
            var oldContent = oldEl is ContentFlyoutElement oldCf ? oldCf.Content : null;
            if (oldContent is not null && flyout.Content is UIElement existingContent && CanUpdate(oldContent, newCf.Content))
            {
                var replacement = Update(oldContent, newCf.Content, existingContent, requestRerender);
                if (replacement is not null)
                    flyout.Content = replacement;
            }
            else
            {
                // Type changed — remount content
                flyout.Content = Mount(newCf.Content, requestRerender);
            }
            flyout.Placement = newCf.Placement;
            return;
        }

        // MenuFlyout → recreate items (lightweight, no open-state issue)
        if (newEl is MenuFlyoutContentElement newMf && existingFlyout is WinUI.MenuFlyout menuFlyout)
        {
            menuFlyout.Items.Clear();
            foreach (var item in newMf.Items) menuFlyout.Items.Add(CreateMenuFlyoutItem(item));
            if (newMf.Placement != WinPrim.FlyoutPlacementMode.Auto)
                menuFlyout.Placement = newMf.Placement;
            return;
        }

        // Fallback: plain element → reconcile inside existing Flyout
        if (existingFlyout is WinUI.Flyout plainFlyout && plainFlyout.Content is UIElement existingCtrl)
        {
            if (CanUpdate(oldEl, newEl))
            {
                var replacement = Update(oldEl, newEl, existingCtrl, requestRerender);
                if (replacement is not null)
                    plainFlyout.Content = replacement;
            }
            else
            {
                plainFlyout.Content = Mount(newEl, requestRerender);
            }
        }
    }

    private void SetFlyoutOnControl(FrameworkElement fe, WinPrim.FlyoutBase flyout)
    {
        // Check SplitButton before Button (SplitButton doesn't inherit from Button,
        // but DropDownButton does, so Button catch-all handles it).
        if (fe is WinUI.SplitButton sb)
            sb.Flyout = flyout;
        else if (fe is WinUI.Button btn)  // AppBarButton, DropDownButton inherit from Button
            btn.Flyout = flyout;
        else
            WinPrim.FlyoutBase.SetAttachedFlyout(fe, flyout);
    }

    /// <summary>
    /// Creates a WinUI FlyoutBase from a Duct element descriptor.
    /// Recognizes ContentFlyoutElement and MenuFlyoutContentElement for configured flyouts,
    /// and falls back to wrapping plain elements in a basic Flyout.
    /// Used by both ApplyModifiers (for .WithFlyout()/.WithContextFlyout()) and
    /// button mount methods (for direct Flyout parameter).
    /// </summary>
    internal WinPrim.FlyoutBase? CreateFlyoutFromElement(Element flyoutEl, Action requestRerender)
    {
        switch (flyoutEl)
        {
            case ContentFlyoutElement cf:
            {
                var content = Mount(cf.Content, requestRerender);
                return content is not null ? new WinUI.Flyout { Content = content, Placement = cf.Placement } : null;
            }
            case MenuFlyoutContentElement mf:
            {
                var menuFlyout = new WinUI.MenuFlyout();
                // Only set Placement if explicitly specified (Auto can cause assertions on MenuFlyout)
                if (mf.Placement != WinPrim.FlyoutPlacementMode.Auto)
                    menuFlyout.Placement = mf.Placement;
                foreach (var item in mf.Items) menuFlyout.Items.Add(CreateMenuFlyoutItem(item));
                return menuFlyout;
            }
            default:
            {
                var content = Mount(flyoutEl, requestRerender);
                return content is not null ? new WinUI.Flyout { Content = content } : null;
            }
        }
    }

    // ── Enum conversions removed — Duct now uses WinUI types directly ──

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

    public void Dispose()
    {
        _differ?.Dispose();
        _differ = null;
        _cachedOldSerialization = null;
        _cachedOldControls = null;
        _treeSerializer = null;
        _oldRegistry = null;
        _newRegistry = null;
        _inDiffTreesPass = false;
    }
}
