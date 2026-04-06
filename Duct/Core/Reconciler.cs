using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUI = Microsoft.UI.Xaml.Controls;
using WinPrim = Microsoft.UI.Xaml.Controls.Primitives;

namespace Duct.Core;

/// <summary>
/// The reconciler diffs old and new element trees and patches the real WinUI control tree.
///
/// Split across partial classes:
///   - Reconciler.cs           — orchestration, children, unmount, helpers
///   - Reconciler.Mount.cs     — Mount() dispatch + per-control MountXxx methods
///   - Reconciler.Update.cs    — Update() dispatch + per-control UpdateXxx methods
/// </summary>
public sealed partial class Reconciler : IDisposable
{
    private readonly Dictionary<UIElement, ComponentNode> _componentNodes = new();
    private readonly Dictionary<UIElement, ErrorBoundaryNode> _errorBoundaryNodes = new();
    private readonly ElementPool _pool = new();
    private readonly Dictionary<Type, ITypeRegistration> _typeRegistry = new();
    private readonly IDuctLogger _logger;
    private int _errorBoundaryDepth;

    /// <summary>
    /// The element pool used by this reconciler. Disable via Pool.Enabled = false
    /// to prevent recycled controls from retaining stale property state.
    /// </summary>
    public ElementPool Pool => _pool;

    /// <summary>
    /// EXP-2: When true, UpdateText uses bitmask diff (old vs new Element comparison)
    /// instead of reading WinUI control properties via COM interop to guard writes.
    /// </summary>
    public static bool EnableBitmaskDiff { get; set; }

    public Reconciler() : this(NullDuctLogger.Instance) { }

    public Reconciler(IDuctLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Associates a control with its current element via Tag.
    /// Only call for interactive controls that need the Tag-based event handler pattern.
    /// Layout-only controls (Border, StackPanel, TextBlock, etc.) should NOT set Tag
    /// to avoid expensive COM DependencyProperty calls on the hot path.
    /// </summary>
    /// <summary>
    /// A shared DataTemplate containing a ContentControl shell.
    /// Parsed once via XamlReader.Load, reused across all items controls (ListView, GridView, FlipView).
    /// </summary>
    internal static readonly Lazy<DataTemplate> SharedContentControlTemplate = new(() =>
        (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
            "<ContentControl HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch'/>" +
            "</DataTemplate>"));

    internal static void SetElementTag(FrameworkElement control, Element element) => control.Tag = element;

    /// <summary>
    /// Retrieves the element associated with a control via Tag, or null.
    /// </summary>
    internal static Element? GetElementTag(UIElement control) =>
        control is FrameworkElement fe ? fe.Tag as Element : null;

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
        {
            // Guard against control type mismatch (e.g., recycled from pool or element type changed at this position).
            // If the existing control isn't our expected type, force a fresh mount instead of crashing.
            if (control is not TControl typedControl || oldEl is not TElement typedOldEl)
                return _mount(reconciler, (TElement)newEl, requestRerender);

            return _update(reconciler, typedOldEl, (TElement)newEl, typedControl, requestRerender);
        }

        public void Unmount(UIElement control, Reconciler reconciler)
        {
            if (control is TControl typedControl)
                _unmount?.Invoke(reconciler, typedControl);
        }
    }

    public UIElement? Reconcile(
        Element? oldElement,
        Element? newElement,
        UIElement? existingControl,
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
            // If Update returned a completely new control (full remount path),
            // unmount the old control to clean up event handlers and component state.
            if (replacement is not null && replacement != existingControl)
                UnmountAndPool(existingControl);
            return replacement ?? existingControl;
        }

        // Type changed — unmount+pool old tree, then mount new tree
        // (so pooled controls are available for rent during mount).
        UnmountAndPool(existingControl);
        return Mount(newElement, requestRerender);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Component reconciliation
    // ════════════════════════════════════════════════════════════════════

    private void ReconcileComponent(Element oldEl, Element newEl, UIElement control, Action requestRerender)
    {
        if (!_componentNodes.TryGetValue(control, out var node))
        {
            _logger.Log(DuctLogLevel.Warning, "ReconcileComponent: component node not found for control — component will not update");
            return;
        }

        Element newChildElement;
        try
        {
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
        }
        catch (Exception ex) when (_errorBoundaryDepth == 0)
        {
            _logger.Log(DuctLogLevel.Error, $"Component Render() threw: {newEl.GetType().Name}", ex);
            newChildElement = new TextElement($"⚠ Render error: {ex.Message}");
        }

        // Dereference the Border wrapper to get the actual child control.
        // Each component is wrapped in a Border as an identity anchor, so we
        // reconcile the child inside the wrapper, not the wrapper itself.
        var existingChild = (control as Border)?.Child;
        var newControl = Reconcile(node.RenderedElement, newChildElement, existingChild, requestRerender);
        if (control is Border border)
        {
            if (newControl != existingChild)
                border.Child = newControl; // handles both replacement and null (child removed)
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
        UnmountRecursive(control);
    }

    private void UnmountRecursive(UIElement control)
    {
        if (_componentNodes.TryGetValue(control, out var node))
        {
            node.Component?.Context.RunCleanups();
            node.Context?.RunCleanups();
            _componentNodes.Remove(control);
        }

        _errorBoundaryNodes.Remove(control);

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
                UnmountRecursive(child);
        }
        else if (control is WinUI.Border border && border.Child is not null)
        {
            UnmountRecursive(border.Child);
        }
        else if (control is WinUI.ScrollViewer sv && sv.Content is UIElement svChild)
        {
            UnmountRecursive(svChild);
        }
        else if (control is WinUI.UserControl uc && uc.Content is UIElement ucChild)
        {
            UnmountRecursive(ucChild);
        }
        else if (control is WinUI.ContentControl cc && cc.Content is UIElement ccChild)
        {
            UnmountRecursive(ccChild);
        }
    }

    /// <summary>
    /// Unmounts and returns all descendants + root to the pool.
    /// Call AFTER the root has been detached from the visual tree.
    /// Collects all controls first, then pools bottom-up so DetachFromParent
    /// removes children before parents clear their collections.
    /// </summary>
    internal void UnmountAndPool(UIElement control)
    {
        var toPool = new List<FrameworkElement>();
        UnmountAndCollect(control, toPool);

        // Pool top-down: parent's CleanElement calls Children.Clear() which
        // detaches children, so by the time children are pooled they're parentless.
        for (int i = 0; i < toPool.Count; i++)
            _pool.Return(toPool[i]);
    }

    private void UnmountAndCollect(UIElement control, List<FrameworkElement> toPool)
    {
        // Run cleanup logic (component teardown, etc.)
        if (_componentNodes.TryGetValue(control, out var node))
        {
            node.Component?.Context.RunCleanups();
            node.Context?.RunCleanups();
            _componentNodes.Remove(control);
        }

        if (control is FrameworkElement fe && fe.Tag is Element tagEl
            && _typeRegistry.TryGetValue(tagEl.GetType(), out var reg) && reg.HasUnmount)
        {
            reg.Unmount(control, this);
            // Collect this control for pooling, but do NOT recurse into children —
            // they were created outside Duct's tree and must not be pooled.
            // (Mirrors UnmountRecursive which returns early in this case.)
            if (control is FrameworkElement poolCandidate2)
                toPool.Add(poolCandidate2);
            return;
        }

        // Recurse into children.
        if (control is WinUI.Panel panel)
        {
            foreach (var child in panel.Children)
                UnmountAndCollect(child, toPool);
        }
        else if (control is WinUI.Border border && border.Child is not null)
        {
            UnmountAndCollect(border.Child, toPool);
        }
        else if (control is WinUI.ScrollViewer sv && sv.Content is UIElement svChild)
        {
            UnmountAndCollect(svChild, toPool);
        }
        else if (control is WinUI.UserControl uc && uc.Content is UIElement ucChild)
        {
            UnmountAndCollect(ucChild, toPool);
        }
        else if (control is WinUI.ContentControl cc && cc.Content is UIElement ccChild)
        {
            UnmountAndCollect(ccChild, toPool);
            cc.Content = null; // Detach so pooled child has no parent
        }

        if (control is FrameworkElement poolCandidate)
            toPool.Add(poolCandidate);
    }

    // ════════════════════════════════════════════════════════════════════
    //  CanUpdate
    // ════════════════════════════════════════════════════════════════════

    internal bool CanUpdate(Element oldEl, Element newEl)
    {
        if (oldEl.GetType() != newEl.GetType()) return false;
        if (oldEl.Key != newEl.Key) return false;
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

    internal static void ApplyTransitions(UIElement uie, ImplicitTransitions? implicitT, ThemeTransitions? themeT)
    {
        if (implicitT is not null)
        {
            if (implicitT.Opacity is not null)
                uie.OpacityTransition = implicitT.Opacity;
            if (implicitT.Rotation is not null)
                uie.RotationTransition = implicitT.Rotation;
            if (implicitT.Scale is not null)
                uie.ScaleTransition = implicitT.Scale;
            if (implicitT.Translation is not null)
                uie.TranslationTransition = implicitT.Translation;
            if (implicitT.Background is not null)
            {
                switch (uie)
                {
                    case WinUI.Grid g: g.BackgroundTransition = implicitT.Background; break;
                    case WinUI.StackPanel sp: sp.BackgroundTransition = implicitT.Background; break;
                    case WinUI.ContentPresenter cp: cp.BackgroundTransition = implicitT.Background; break;
                }
            }
        }

        if (themeT?.Children is { Length: > 0 } children)
        {
            var tc = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection();
            foreach (var t in children) tc.Add(t);
            switch (uie)
            {
                case WinUI.StackPanel sp: sp.ChildrenTransitions = tc; break;
                case WinUI.Grid g: g.ChildrenTransitions = tc; break;
                case WinUI.Canvas c: c.ChildrenTransitions = tc; break;
                case WinUI.Border b: b.ChildTransitions = tc; break;
                case WinUI.ContentPresenter cp: cp.ContentTransitions = tc; break;
                case WinUI.ContentControl cc: cc.ContentTransitions = tc; break;
            }
        }

        if (themeT?.ItemContainer is { Length: > 0 } itemTransitions)
        {
            var tc = new Microsoft.UI.Xaml.Media.Animation.TransitionCollection();
            foreach (var t in itemTransitions) tc.Add(t);
            if (uie is WinUI.ListViewBase lvb)
                lvb.ItemContainerTransitions = tc;
        }
    }

    internal void ApplyModifiers(FrameworkElement fe, ElementModifiers m, Action requestRerender)
        => ApplyModifiers(fe, null, m, requestRerender);

    internal void ApplyModifiers(FrameworkElement fe, ElementModifiers? oldM, ElementModifiers m, Action requestRerender)
    {
        // Guard each property: only call into WinUI when the value actually changed.
        // Each WinUI property set is a managed→native interop call, so avoiding
        // unnecessary sets is critical for large element counts.

        // Apply physical margin, then overlay logical (BiDi-aware) inline margin
        var resolvedMargin = m.Margin ?? oldM?.Margin;
        if (m.MarginInlineStart.HasValue || m.MarginInlineEnd.HasValue)
        {
            var isRtl = fe.FlowDirection == FlowDirection.RightToLeft;
            var baseMargin = resolvedMargin ?? fe.Margin;
            var left = isRtl ? (m.MarginInlineEnd ?? baseMargin.Left) : (m.MarginInlineStart ?? baseMargin.Left);
            var right = isRtl ? (m.MarginInlineStart ?? baseMargin.Right) : (m.MarginInlineEnd ?? baseMargin.Right);
            resolvedMargin = new Thickness(left, baseMargin.Top, right, baseMargin.Bottom);
        }
        if (resolvedMargin.HasValue && resolvedMargin != oldM?.Margin) fe.Margin = resolvedMargin.Value;

        // Apply physical padding, then overlay logical (BiDi-aware) inline padding
        var resolvedPadding = m.Padding ?? oldM?.Padding;
        if (m.PaddingInlineStart.HasValue || m.PaddingInlineEnd.HasValue)
        {
            var isRtl = fe.FlowDirection == FlowDirection.RightToLeft;
            var basePad = resolvedPadding ?? (fe is WinUI.Control pc ? pc.Padding : fe is WinUI.Border pb ? pb.Padding : new Thickness());
            var left = isRtl ? (m.PaddingInlineEnd ?? basePad.Left) : (m.PaddingInlineStart ?? basePad.Left);
            var right = isRtl ? (m.PaddingInlineStart ?? basePad.Right) : (m.PaddingInlineEnd ?? basePad.Right);
            resolvedPadding = new Thickness(left, basePad.Top, right, basePad.Bottom);
        }
        if (resolvedPadding.HasValue && resolvedPadding != oldM?.Padding)
        {
            if (fe is WinUI.Control padCtrl) padCtrl.Padding = resolvedPadding.Value;
            else if (fe is WinUI.Border padBdr) padBdr.Padding = resolvedPadding.Value;
        }
        if (m.Width.HasValue && m.Width != oldM?.Width) fe.Width = m.Width.Value;
        if (m.Height.HasValue && m.Height != oldM?.Height) fe.Height = m.Height.Value;
        if (m.MinWidth.HasValue && m.MinWidth != oldM?.MinWidth) fe.MinWidth = m.MinWidth.Value;
        if (m.MinHeight.HasValue && m.MinHeight != oldM?.MinHeight) fe.MinHeight = m.MinHeight.Value;
        if (m.MaxWidth.HasValue && m.MaxWidth != oldM?.MaxWidth) fe.MaxWidth = m.MaxWidth.Value;
        if (m.MaxHeight.HasValue && m.MaxHeight != oldM?.MaxHeight) fe.MaxHeight = m.MaxHeight.Value;
        if (m.HorizontalAlignment.HasValue && m.HorizontalAlignment != oldM?.HorizontalAlignment) fe.HorizontalAlignment = m.HorizontalAlignment.Value;
        if (m.VerticalAlignment.HasValue && m.VerticalAlignment != oldM?.VerticalAlignment) fe.VerticalAlignment = m.VerticalAlignment.Value;
        if (m.Opacity.HasValue && m.Opacity != oldM?.Opacity) fe.Opacity = m.Opacity.Value;
        if (m.IsVisible.HasValue && m.IsVisible != oldM?.IsVisible)
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
        else if (m.ToolTip is not null && m.ToolTip != oldM?.ToolTip)
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

        // IsEnabled (on Control)
        if (m.IsEnabled.HasValue && m.IsEnabled != oldM?.IsEnabled && fe is WinUI.Control enCtrl)
            enCtrl.IsEnabled = m.IsEnabled.Value;

        // CornerRadius (on Control and Border)
        if (m.CornerRadius.HasValue && m.CornerRadius != oldM?.CornerRadius)
        {
            if (fe is WinUI.Control crCtrl) crCtrl.CornerRadius = m.CornerRadius.Value;
            else if (fe is WinUI.Border crBdr) crBdr.CornerRadius = m.CornerRadius.Value;
        }

        // BorderBrush / BorderThickness (on Control and Border)
        if (m.BorderBrush is not null && !ReferenceEquals(m.BorderBrush, oldM?.BorderBrush))
        {
            if (fe is WinUI.Control bbCtrl) bbCtrl.BorderBrush = m.BorderBrush;
            else if (fe is WinUI.Border bbBdr) bbBdr.BorderBrush = m.BorderBrush;
        }
        // Apply physical border thickness, then overlay logical (BiDi-aware) inline border
        var resolvedBorder = m.BorderThickness;
        if (m.BorderInlineStart.HasValue)
        {
            var isRtl = fe.FlowDirection == FlowDirection.RightToLeft;
            var baseBorder = resolvedBorder ?? (fe is WinUI.Control bc ? bc.BorderThickness : fe is WinUI.Border bb ? bb.BorderThickness : new Thickness());
            var inlineStartThickness = m.BorderInlineStart.Value;
            if (isRtl)
                resolvedBorder = new Thickness(baseBorder.Left, baseBorder.Top, inlineStartThickness.Left, baseBorder.Bottom);
            else
                resolvedBorder = new Thickness(inlineStartThickness.Left, baseBorder.Top, baseBorder.Right, baseBorder.Bottom);
        }
        if (resolvedBorder.HasValue && resolvedBorder != oldM?.BorderThickness)
        {
            if (fe is WinUI.Control btCtrl) btCtrl.BorderThickness = resolvedBorder.Value;
            else if (fe is WinUI.Border btBdr) btBdr.BorderThickness = resolvedBorder.Value;
        }

        // Background (Panel, Control, or Border)
        if (m.Background is not null && !ReferenceEquals(m.Background, oldM?.Background))
        {
            if (fe is WinUI.Panel panel) panel.Background = m.Background;
            else if (fe is WinUI.Control ctrl2) ctrl2.Background = m.Background;
            else if (fe is WinUI.Border bdr) bdr.Background = m.Background;
        }

        // Foreground (Control or TextBlock)
        if (m.Foreground is not null && !ReferenceEquals(m.Foreground, oldM?.Foreground))
        {
            if (fe is WinUI.Control fgCtrl) fgCtrl.Foreground = m.Foreground;
            else if (fe is TextBlock fgTb) fgTb.Foreground = m.Foreground;
        }

        // AutomationProperties.Name
        if (m.AutomationName is not null && m.AutomationName != oldM?.AutomationName)
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(fe, m.AutomationName);

        // AutomationProperties.AutomationId
        if (m.AutomationId is not null && m.AutomationId != oldM?.AutomationId)
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(fe, m.AutomationId);

        // ElementSoundMode (on Control, not FrameworkElement)
        if (m.ElementSoundMode.HasValue && m.ElementSoundMode != oldM?.ElementSoundMode && fe is WinUI.Control ctrl)
            ctrl.ElementSoundMode = m.ElementSoundMode.Value;

        // OnMountAction — only run on initial mount (oldM is null)
        if (m.OnMountAction is not null && oldM is null)
            m.OnMountAction(fe);
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

    /// <summary>
    /// Tracks the state of a mounted component in the tree.
    /// </summary>
    internal class ComponentNode
    {
        /// <summary>The class-based Component instance (null for function components).</summary>
        public Component? Component { get; set; }
        /// <summary>The RenderContext for function components (null for class components).</summary>
        public RenderContext? Context { get; set; }
        /// <summary>The element tree output from the last Render() call.</summary>
        public Element? RenderedElement { get; set; }
        /// <summary>The ComponentElement or FuncElement that created this node.</summary>
        public Element? Element { get; set; }
    }

    /// <summary>
    /// Tracks the state of a mounted ErrorBoundary in the tree.
    /// </summary>
    internal class ErrorBoundaryNode
    {
        public Element ChildElement { get; set; } = null!;
        public Element? RenderedElement { get; set; }
        public Exception? CaughtException { get; set; }
        public Func<Exception, Element> Fallback { get; set; } = null!;
    }

    public void Dispose()
    {
        foreach (var node in _componentNodes.Values)
        {
            node.Context?.RunCleanups();
            node.Component?.Context?.RunCleanups();
        }
        _componentNodes.Clear();
        _errorBoundaryNodes.Clear();
    }
}
