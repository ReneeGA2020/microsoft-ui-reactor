# WinUI Gallery Migration Spec

This document tracks the features needed to incrementally migrate the WinUI Gallery app from XAML to Duct. The Gallery is a 122-page XAML app with a NavigationView shell, Frame-based page navigation, and a repeating `ControlExample` pattern on every page. The goal is to migrate pages one at a time without rewriting the shell or navigation infrastructure upfront.

---

## Feature Status Key

- **Not started** — Design only, no code written
- **In progress** — Implementation underway
- **Complete** — Implemented and tested

---

## Feature 1: Extensible Reconciler (`RegisterType`)

**Status: Complete**

**Priority: P0 (prerequisite for all other migration work)**

### Problem

The reconciler's `Mount()` and `Update()` methods are hardcoded switch expressions over ~59 built-in element types. There is no way to add support for a new control without modifying the reconciler source. This blocks wrapping Gallery custom controls (`ControlExample`, `PageHeader`, `SampleCodePresenter`) and third-party controls (Community Toolkit, etc.).

### Design

Add a type registry to the `Reconciler` that is checked before the built-in switch expressions.

#### Registration API

```csharp
public void RegisterType<TElement, TControl>(
    Func<TElement, Action, TControl> mount,
    Func<TElement, TElement, TControl, Action, UIElement?> update,
    Action<TControl>? unmount = null)
    where TElement : Element
    where TControl : UIElement;
```

#### Internal storage

```csharp
private readonly Dictionary<Type, ITypeRegistration> _typeRegistry = new();

private interface ITypeRegistration
{
    UIElement Mount(Element element, Action requestRerender);
    UIElement? Update(Element oldEl, Element newEl, UIElement control, Action requestRerender);
    void Unmount(UIElement control);
    bool HasUnmount { get; }
}
```

A generic `TypeRegistration<TElement, TControl>` implements `ITypeRegistration`, performs the casts, and delegates to the user-provided lambdas.

#### Integration points

**Mount** (`Reconciler.Mount.cs`) — add registry lookup before the existing switch:

```csharp
public UIElement? Mount(Element element, Action requestRerender)
{
    if (_typeRegistry.TryGetValue(element.GetType(), out var reg))
        return reg.Mount(element, requestRerender);

    return element switch { /* existing 59 cases */ };
}
```

**Update** (`Reconciler.Update.cs`) — same pattern:

```csharp
private UIElement? Update(Element oldEl, Element newEl, UIElement control, Action requestRerender)
{
    if (_typeRegistry.TryGetValue(newEl.GetType(), out var reg))
        return reg.Update(oldEl, newEl, control, requestRerender);

    return (oldEl, newEl, control) switch { /* existing cases */ };
}
```

**CanUpdate** (`Reconciler.cs`) — no changes needed. Already compares `GetType()` equality, which works for registered types.

**Unmount** (`Reconciler.cs`) — look up the element type from `control.Tag` to find the registry entry. If found and it has an unmount handler, call it. Otherwise fall through to default Panel/Border/ScrollViewer child traversal.

#### Public API surface changes

`UpdateChild` and `UnmountChild` (currently `internal`) need to be accessible to registered mount/update handlers so they can recursively reconcile children. Make them `public`.

#### Design notes

- Registry is checked first, so registered types take priority over built-in types (allows overriding built-in behavior for testing).
- `ModifiedElement` wrapping is handled before the registry check (it's the first case in both switches), so `.Margin()`, `.Width()`, etc. work on registered types automatically.
- No special `Element` subclass required. Users define their own `record : Element` with whatever properties they need.
- Estimated scope: ~50-60 lines of new code. Fully backwards compatible.

#### Usage example

```csharp
// 1. Define a custom element
public record ControlExampleElement(
    string HeaderText,
    Element? Example = null,
    string? XamlSource = null
) : Element;

// 2. Register with reconciler
reconciler.RegisterType<ControlExampleElement, ControlExample>(
    mount: (el, rerender) =>
    {
        var ctrl = new ControlExample();
        ctrl.HeaderText = el.HeaderText;
        ctrl.XamlSource = el.XamlSource;
        if (el.Example is not null)
            ctrl.Example = reconciler.Mount(el.Example, rerender);
        ctrl.Tag = el;
        return ctrl;
    },
    update: (oldEl, newEl, ctrl, rerender) =>
    {
        ctrl.HeaderText = newEl.HeaderText;
        ctrl.XamlSource = newEl.XamlSource;
        if (newEl.Example is not null && oldEl.Example is not null)
        {
            var childCtrl = ctrl.Example as UIElement;
            reconciler.UpdateChild(oldEl.Example, newEl.Example, childCtrl, rerender);
        }
        ctrl.Tag = newEl;
        return null; // null = updated in place
    }
);

// 3. Use in Duct UI code
public override Element Render()
{
    return new ControlExampleElement(
        HeaderText: "Basic Button",
        Example: Button("Click me", () => { }),
        XamlSource: "Samples/ButtonSample.txt"
    );
}
```

---

## Feature 2: DuctHost Control (Embed Duct inside XAML)

**Status: Complete**

**Priority: P0 (prerequisite for incremental migration)**

### Problem

Duct currently owns the entire window content tree via `DuctApp`/`DuctHost`. There is no way to embed a Duct component tree inside a XAML page or control. This means migration is all-or-nothing — you must rewrite the entire app at once.

### Design

Create a `DuctHost` class that is a WinUI `ContentControl` (or `UserControl`) which can be placed in XAML and renders a Duct component tree inside itself.

```xml
<!-- In any XAML page -->
<local:DuctHost Component="typeof(MyDuctComponent)" />
```

#### Requirements

- `DuctHost` is a `FrameworkElement` that XAML can instantiate.
- It accepts a component type (and optionally props) and renders the Duct tree as its content.
- It owns its own `Reconciler` instance and render loop.
- It stretches to fill its parent by default.
- It propagates the WinUI `ActualTheme` to the Duct tree so theme resources resolve correctly.
- It handles cleanup (unmount, hook cleanups) when the host is unloaded from the visual tree.
- Multiple `DuctHost` instances can coexist on the same page or across pages.

#### API sketch

```csharp
public class DuctHost : ContentControl
{
    // Set the root component type
    public Type ComponentType { get; set; }

    // Optional: pass props to the root component
    public object? Props { get; set; }

    // Internal
    private Reconciler _reconciler;
    private Element? _currentElement;
    private UIElement? _currentControl;

    // Render loop triggered by state changes
    private void RequestRerender() { ... }

    // Wire up on Loaded, tear down on Unloaded
    protected override void OnApplyTemplate() { ... }
}
```

#### Open questions

- Should `DuctHost` accept a `Component` instance or a `Type`? A `Type` is simpler for XAML (no instantiation in markup). A `Func<Element>` or `FuncElement` could also work for lightweight usage.
- Should it share a `Reconciler` with other `DuctHost` instances, or each get their own? Separate is safer (no cross-contamination of component state).

---

## Feature 3: Duct Page Adapter (Frame Navigation)

**Status: Complete**

**Priority: P0 (required to migrate Gallery pages)**

### Problem

The Gallery navigates between pages using `Frame.Navigate(typeof(SomePage))`. Every control page is a WinUI `Page` subclass. To migrate a single page to Duct, we need a `Page` that hosts a Duct component — essentially `DuctHost` wrapped in a `Page` with navigation parameter support.

### Design

```csharp
public class DuctPage<TComponent> : Page where TComponent : Component, new()
{
    private DuctHost _host;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _host = new DuctHost { ComponentType = typeof(TComponent) };
        // Pass navigation parameter as props
        if (e.Parameter is not null)
            _host.Props = e.Parameter;
        Content = _host;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // Unmount Duct tree, run cleanups
        _host.Dispose();
    }
}
```

#### Usage in Gallery

Replace a page registration:

```csharp
// Before (XAML page):
{ "ButtonPage", typeof(ButtonPage) }

// After (Duct page):
{ "ButtonPage", typeof(DuctPage<ButtonPageComponent>) }
```

The rest of the Gallery navigation infrastructure (NavigationView, Frame, back/forward) works unchanged.

#### Requirements

- Must support navigation parameters (Gallery passes `UniqueId` strings).
- The Duct component receives the parameter as a prop (needs a convention — e.g., `INavigationAware` interface or a `Props` property on `Component`).
- `OnNavigatedFrom` must trigger full cleanup (unmount, effect cleanups).
- Navigation transitions (`DrillInNavigationTransitionInfo`) should still work since the Page itself participates in the Frame's transition system.

---

## Feature 4: Theme Resource Access

**Status: Complete**

**Priority: P1 (required for visual correctness of migrated pages)**

### Problem

The Gallery uses WinUI theme resources extensively: `TextFillColorSecondaryBrush`, `ControlFillColorDefaultBrush`, `OverlayCornerRadius`, text styles like `BodyTextBlockStyle`, etc. These resolve differently in light vs. dark mode. Duct has no way to reference named theme resources — you'd have to hardcode colors, which breaks theme switching.

### Design

#### Resource lookup helper

```csharp
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
}
```

#### Style application modifier

```csharp
// Apply a named WinUI Style to any element
Text("Hello").ApplyStyle("BodyTextBlockStyle")

// Implementation: a modifier or setter that does:
// control.Style = (Style)Application.Current.Resources[styleName];
```

#### Theme-reactive hook (stretch goal)

```csharp
// Re-renders when theme changes
var theme = UseActualTheme(); // ElementTheme.Light or Dark
```

This would subscribe to `FrameworkElement.ActualThemeChanged` on the nearest ancestor and trigger a re-render. Useful for conditional rendering based on theme.

#### Notes

- The simple `Application.Current.Resources[key]` lookup works for most cases but doesn't automatically re-resolve when the theme changes at runtime. For brushes defined as `ThemeResource` in XAML dictionaries, WinUI handles re-resolution internally when the control is in the visual tree. So as long as we apply the brush object (not a color value), theme switching should work.
- `ApplyStyle` is high-value — one call replaces dozens of individual property settings.

---

## Feature 5: Responsive Layout Hook (`UseWindowSize` / `UseBreakpoint`)

**Status: Complete**

**Priority: P1 (required for responsive Gallery pages)**

### Problem

Nearly every Gallery page uses `VisualStateManager` with `AdaptiveTrigger MinWindowWidth="641"` to switch between wide (side-by-side) and narrow (stacked) layouts. Duct components render the same regardless of window size — there's no equivalent of adaptive triggers.

### Design

#### `UseWindowSize` hook

```csharp
var (width, height) = UseWindowSize();
```

Returns current window dimensions. Triggers re-render when the window resizes. Internally subscribes to `Window.SizeChanged` via `UseEffect`.

#### `UseBreakpoint` hook (convenience)

```csharp
var isWide = UseBreakpoint(641);
// true when window width >= 641

// Usage:
return isWide
    ? HStack(12, example, options)   // side-by-side
    : VStack(12, example, options);  // stacked
```

#### Implementation notes

- Needs access to the `Window` instance. Could be provided via a context/ambient value set by `DuctHost` or `DuctApp`.
- Should debounce or throttle resize events to avoid excessive re-renders during drag-resize.
- The Gallery's breakpoint is almost always 641px, so a simple `UseBreakpoint(double minWidth)` covers the vast majority of cases.

---

## Feature 6: Observable/Binding Interop Hook

**Status: Complete**

**Priority: P1 (enables reuse of existing Gallery data layer)**

### Problem

The Gallery's data layer uses `INotifyPropertyChanged` (`SettingsHelper`, `ItemsPageBase`) and `ObservableCollection<T>` (`ControlInfoDataItem` collections). Migrated Duct pages need to consume these data sources without rewriting them. Duct's hook-based state model has no bridge to the .NET observable pattern.

### Design

#### `UseObservable<T>` hook

```csharp
// Subscribe to an INotifyPropertyChanged source
var settings = UseObservable(SettingsHelper.Instance);
// Returns the same object, but triggers re-render when any property changes

// Subscribe to a specific property
var theme = UseObservableProperty(SettingsHelper.Instance, s => s.SelectedAppTheme);
// Returns the property value, re-renders only when that property changes
```

#### `UseCollection<T>` hook

```csharp
// Subscribe to an ObservableCollection
var items = UseCollection(dataSource.Items);
// Returns IReadOnlyList<T>, triggers re-render on Add/Remove/Reset
```

#### Implementation

These are essentially `UseEffect` + `UseState` combos:

```csharp
public T UseObservable<T>(T source) where T : INotifyPropertyChanged
{
    var (_, forceRender) = UseState(0);
    UseEffect(() =>
    {
        void handler(object? s, PropertyChangedEventArgs e) => forceRender(v => v + 1);
        source.PropertyChanged += handler;
        return () => source.PropertyChanged -= handler;
    }, source);
    return source;
}
```

Could be provided as extension methods on `RenderContext` or as static helpers in a `Duct.Interop` namespace.

---

## Feature 7: Reverse Embedding (XAML inside Duct)

**Status: Not started**

**Priority: P2 (needed when migrating the shell)**

### Problem

If the shell (NavigationView, TitleBar, search) is eventually migrated to Duct, any remaining XAML pages need to be embeddable as children in the Duct tree. This is the inverse of Feature 2.

### Design

This is largely solved by Feature 1 (extensible reconciler). A XAML `UserControl` or `Page` is just another `FrameworkElement`. With `RegisterType`, you can wrap it:

```csharp
public record XamlPageElement(Type PageType, object? Parameter = null) : Element;

reconciler.RegisterType<XamlPageElement, Frame>(
    mount: (el, rerender) =>
    {
        var frame = new Frame();
        frame.Navigate(el.PageType, el.Parameter);
        frame.Tag = el;
        return frame;
    },
    update: (oldEl, newEl, frame, rerender) =>
    {
        if (oldEl.PageType != newEl.PageType || oldEl.Parameter != newEl.Parameter)
            frame.Navigate(newEl.PageType, newEl.Parameter);
        frame.Tag = newEl;
        return null;
    }
);
```

No additional framework feature needed beyond Feature 1, but documenting the pattern is valuable.

---

## Feature 8: Animation and Transition Support

**Status: Not started**

**Priority: P2 (polish, not blocking migration)**

### Problem

The Gallery uses `DrillInNavigationTransitionInfo`, `RepositionThemeTransition`, implicit animations (Community Toolkit), and connected animations. Duct has no built-in animation support.

### Design

#### Navigation transitions

Handled by the `Frame` itself (Feature 3), not by Duct. The `DuctPage<T>` participates in Frame transitions because it's a real `Page`. No Duct work needed.

#### Theme transitions

Could be applied via `.Set()` on container elements:

```csharp
VStack(12, children).Set(sp =>
{
    sp.ChildrenTransitions = new TransitionCollection
    {
        new RepositionThemeTransition()
    };
})
```

This works today. No new feature needed, but a convenience DSL method could help:

```csharp
VStack(12, children).WithTransitions(new RepositionThemeTransition())
```

#### Implicit animations (Community Toolkit)

These attach to controls via attached properties. Accessible through `.Set()`:

```csharp
element.Set(ctrl => Implicit.Animations.SetAnimations(ctrl, ...))
```

#### Connected animations

Would require a `UseRef`-like mechanism to get a handle to the mounted control, then call `ConnectedAnimationService` APIs. Possible today with `.Set()` capturing a reference, but awkward.

### Recommendation

Defer built-in animation support. `.Set()` covers the immediate needs. A convenience modifier like `.WithTransitions()` can be added incrementally.

---

## Feature 9: Component Props System

**Status: Complete**

**Priority: P1 (needed for page parameter passing)**

### Problem

The Gallery passes navigation parameters to pages (e.g., `UniqueId` strings). Feature 3 (`DuctPage<T>`) needs a way to pass these parameters into the Duct component. Currently, `ComponentElement` has an `object? Props` field, but `Component` has no typed way to receive props.

### Design

#### Typed props via generic base class

```csharp
public abstract class Component<TProps> : Component
{
    public TProps Props { get; internal set; }
}

// Usage
public class ButtonPageComponent : Component<string>
{
    public override Element Render()
    {
        var uniqueId = Props; // the navigation parameter
        // ...
    }
}
```

#### Integration with DuctPage

```csharp
public class DuctPage<TComponent, TProps> : Page
    where TComponent : Component<TProps>, new()
{
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        var component = new TComponent { Props = (TProps)e.Parameter };
        // mount...
    }
}
```

#### Alternative: Props via hooks

```csharp
var props = UseProps<NavigationParams>();
```

This keeps the functional style consistent but requires threading props through the render context.

---

## Migration Roadmap

### Phase 1: Foundation (Features 1-3)

These are prerequisites. Without them, no migration can begin.

| Feature | Description | Status |
|---------|-------------|--------|
| 1. Extensible Reconciler | `RegisterType` API on Reconciler | Complete |
| 2. DuctHost Control | Embed Duct component tree inside XAML | Complete |
| 3. DuctPage Adapter | `Page` subclass for Frame navigation | Complete |

### Phase 2: Practical Migration (Features 4-6, 9)

These make migrating pages practical at scale — without them, each page is painful.

| Feature | Description | Status |
|---------|-------------|--------|
| 4. Theme Resource Access | Lookup helpers + `.ApplyStyle()` modifier | Complete |
| 5. Responsive Layout Hook | `UseWindowSize` / `UseBreakpoint` | Complete |
| 6. Observable Interop Hook | `UseObservable` / `UseCollection` | Complete |
| 9. Component Props System | Typed props for navigation parameters | Complete |

### Phase 3: Shell Migration & Polish (Features 7-8)

Only needed once most pages are migrated and the shell itself is being converted.

| Feature | Description | Status |
|---------|-------------|--------|
| 7. Reverse Embedding | XAML pages inside Duct tree (pattern on top of Feature 1) | Not started |
| 8. Animation Support | Transitions, implicit animations, connected animations | Not started |

### Page Migration Order (once Phase 1 is complete)

1. **Proof of concept**: Migrate one simple control page (e.g., `ButtonPage`) end-to-end.
2. **Build Duct ControlExample component**: Wrap the existing XAML `ControlExample` custom control using Feature 1, so migrated pages can reuse it.
3. **Migrate simple pages in batches**: Pages with 1-3 `ControlExample` sections and minimal interactivity.
4. **Migrate complex pages**: Pages with heavy state, custom controls, or responsive layouts (after Phase 2 features land).
5. **Migrate shell**: NavigationView, TitleBar, search (after Phase 3 features land).
