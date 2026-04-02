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

**Status: Complete**

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

**Status: Complete**

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
| 7. Reverse Embedding | XAML pages inside Duct tree (pattern on top of Feature 1) | Complete |
| 8. Animation Support | Transitions, implicit animations, connected animations | Complete |

### Page Migration Order (once Phase 1 is complete)

- [x] **Proof of concept**: Migrate one simple control page (`ButtonPage`) end-to-end.
- [x] **Build Duct ControlExample component**: Wrap the existing XAML `ControlExample` custom control using Feature 1, so migrated pages can reuse it.
- [x] **Migrate simple pages in batches**: Pages with 1-3 `ControlExample` sections and minimal interactivity (105 control pages migrated).
    - [x] AcrylicPage (3 examples, default/custom/luminosity acrylic brush)
    - [x] AnimatedVisualPlayerPage (1 example, Lottie animation with play/pause/stop/reverse)
    - [x] AnnotatedScrollBarPage (1 example, ScrollView + ItemsRepeater + label sections)
    - [x] AppBarButtonPage (6 examples, symbol/bitmap/font/path icon + keyboard accelerator + flyout)
    - [x] AppBarSeparatorPage (1 example, static CommandBar)
    - [x] AppBarToggleButtonPage (4 examples, symbol/bitmap/font/path icon three-state)
    - [x] AppNotificationPage (5 examples, toast notification scenarios)
    - [x] AutoSuggestBoxPage (2 examples, cat breed suggestions + control search)
    - [x] BadgeNotificationManagerPage (2 examples, badge count + badge glyph)
    - [x] BorderPage (1 example, configurable thickness/background/border brush)
    - [x] BreadcrumbBarPage (2 examples, string-based + custom Folder object)
    - [x] ButtonPage (proof of concept — 3 examples, click events + disable toggle)
    - [x] CalendarDatePickerPage (1 example, date selection with header)
    - [x] CalendarViewPage (1 example, selection mode/group labels/calendar identifier)
    - [x] CanvasPage (1 example, positioned colored rectangles with sliders)
    - [x] CheckBoxPage (3 examples, 2-state + 3-state + select-all pattern)
    - [x] ColorPickerPage (1 example, spectrum/slider/channel options)
    - [x] ComboBoxPage (3 examples, inline items + font family + editable)
    - [x] CommandBarFlyoutPage (1 example, CommandBarFlyout on image button)
    - [x] CommandBarPage (1 example, primary/secondary commands + open/close + add/remove)
    - [x] ContentDialogPage (2 examples, basic dialog + custom content dialog)
    - [x] DatePickerPage (2 examples, simple + custom format)
    - [x] DropDownButtonPage (1 example, dropdown menu)
    - [x] ExpanderPage (2 examples, basic + direction option)
    - [x] FlipViewPage (3 examples, inline images + data template + vertical)
    - [x] FlyoutPage (1 example, confirmation flyout with state-driven dismiss)
    - [x] GridPage (1 example, 3x3 grid with spacing/positioning sliders)
    - [x] HyperlinkButtonPage (2 examples, URI nav + click event)
    - [x] IconElementPage (6 examples, BitmapIcon/FontIcon/ImageIcon/PathIcon/SymbolIcon)
    - [x] ImagePage (5 examples, basic + decode + stretch + nine-grid + SVG)
    - [x] InfoBadgePage (4 examples, nav badge + styles + button badge + dynamic value)
    - [x] InfoBarPage (3 examples, severity + message/button + closable/icon)
    - [x] ListBoxPage (2 examples, color items + font items)
    - [x] MediaPlayerElementPage (2 examples, transport controls + autoplay)
    - [x] MenuBarPage (3 examples, simple + keyboard accelerators + submenus)
    - [x] MenuFlyoutPage (6 examples, sort/toggle/cascade/icons/accelerators/radio items)
    - [x] NumberBoxPage (3 examples, expression + spin buttons + formatted)
    - [x] PasswordBoxPage (3 examples, simple + header/char + reveal mode)
    - [x] PersonPicturePage (1 example, radio button profile type selection)
    - [x] PipsPagerPage (2 examples, FlipView integration + standalone with options)
    - [x] PivotPage (1 example, basic Pivot with PivotItems)
    - [x] PopupPage (1 example, Popup with light dismiss + offset options)
    - [x] ProgressBarPage (2 examples, indeterminate + determinate)
    - [x] ProgressRingPage (2 examples, indeterminate + determinate with background)
    - [x] PullToRefreshPage (2 examples, basic RefreshContainer + custom visualizer)
    - [x] RadialGradientBrushPage (1 example, gradient with 6 configurable sliders)
    - [x] RadioButtonPage (2 examples, options group + color selection)
    - [x] RatingControlPage (2 examples, basic + placeholder value)
    - [x] RelativePanelPage (1 example, positioned elements via attached properties)
    - [x] RepeatButtonPage (1 example, click counter)
    - [x] RichTextBlockPage (4 examples, simple/selection highlight/overflow/text highlighting)
    - [x] ScrollViewerPage (1 example, zoom/scroll mode options)
    - [x] SelectorBarPage (3 examples, basic + frame navigation + ItemsView)
    - [x] SemanticZoomPage (1 example, grouped GridView + ListView zoom levels)
    - [x] SliderPage (4 examples, simple + range + ticks + vertical)
    - [x] SoundPage (3 examples, sound toggle + spatial audio + system sounds)
    - [x] SplitButtonPage (2 examples, color picker flyout + text with color grid)
    - [x] SplitViewPage (1 example, configurable display mode/placement/background)
    - [x] StackPanelPage (1 example, orientation + spacing options)
    - [x] TextBlockPage (5 examples, text styling/inlines/selection)
    - [x] TextBoxPage (4 examples, simple/header/read-only/multi-line)
    - [x] ThemeShadowPage (1 example, Z-translation shadow on Border)
    - [x] TimePickerPage (3 examples, simple + minute increment + 24-hour)
    - [x] ToggleButtonPage (1 example, toggle with disable option)
    - [x] ToggleSplitButtonPage (1 example, list type toggle with flyout)
    - [x] ToggleSwitchPage (2 examples, toggle + ProgressRing binding)
    - [x] ToolTipPage (3 examples, simple + offset + placement rect)
    - [x] TreeViewPage (4 examples, drag-drop/multi-select/ItemsSource/template selector)
    - [x] VariableSizedWrapGridPage (1 example, orientation options)
    - [x] ViewboxPage (1 example, stretch/direction options)
    - [x] AnimatedIconPage (2 examples, animated icon states + playback)
    - [x] AppWindowPage (7 examples, window creation/sizing/positioning/presenter modes)
    - [x] AppWindowTitleBarPage (3 examples, title bar customization + drag regions)
    - [x] CaptureElementPreviewPage (1 example, camera capture + snapshot gallery)
    - [x] ClipboardPage (6 examples, text/HTML/bitmap/file copy-paste)
    - [x] CompactSizingPage (1 example, compact density Frame navigation)
    - [x] ConnectedAnimationPage (3 examples, connected animation configurations)
    - [x] ContentIslandPage (1 example, composition hosting + ChildSiteLink)
    - [x] CreateMultipleWindowsPage (1 example, multi-window creation)
    - [x] EasingFunctionPage (4 examples, easing curves with Storyboard animations)
    - [x] GridViewPage (3 examples, basic/selection/drag-drop GridView)
    - [x] ImplicitTransitionPage (6 examples, opacity/offset/rotation/scale/size/theme transitions)
    - [x] ItemsRepeaterPage (6 examples, layouts + phased rendering + animated scrolling)
    - [x] ItemsViewPage (3 examples, basic/swappable layouts/selection modes)
    - [x] JumpListPage (2 examples, jump list group/task items)
    - [x] LinePage (4 examples, line/polyline/path/polygon shapes)
    - [x] ListViewPage (8 examples, basic/selection/drag-drop/grouped/filtered/messaging/images/context)
    - [x] MapControlPage (1 example, interactive map control)
    - [x] NavigationViewPage (8 examples, default/top/adaptive/tabs/data-binding/footer/hierarchical/API)
    - [x] PageTransitionPage (1 example, Frame navigation transitions)
    - [x] ParallaxViewPage (2 examples, parallax scrolling with ListView/image)
    - [x] RichEditBoxPage (5 examples, formatting/spell-check/file-open/math-mode)
    - [x] ScrollViewPage (3 examples, ScrollView zoom/scroll/anchoring)
    - [x] ShapePage (3 examples, rectangle/ellipse/polygon shapes)
    - [x] StandardUICommandPage (1 example, delete command with SwipeControl + ListView)
    - [x] StoragePickersPage (4 examples, file/folder open/save pickers)
    - [x] SwipeControlPage (5 examples, reveal/execute/ListView/gradient/custom icons)
    - [x] SystemBackdropsPage (3 examples, Mica/MicaAlt/DesktopAcrylic backdrop windows)
    - [x] TabViewPage (10 examples, basic/markup/data-bound/keyboard/header-footer/width/close/icons/accent/windowing)
    - [x] TeachingTipPage (3 examples, targeted/non-targeted/hero content teaching tips)
    - [x] ThemeTransitionPage (5 examples, reposition/add-delete/content/entrance/pane transitions)
    - [x] TitleBarPage (2 examples, custom title bar configuration)
    - [x] WebView2Page (1 example, basic WebView2 navigation)
    - [x] XamlCompInteropPage (5 examples, composition animations + Spring/Expression)
    - [x] XamlUICommandPage (1 example, XamlUICommand with KeyboardAccelerator)
- [x] **Migrate shell pages**: Application-level pages migrated to Duct components.
    - [x] AllControlsPage (flat grid of all controls, responsive layout)
    - [x] SectionPage (category page with async data loading)
    - [x] HomePage (SelectorBar tabs, recent/favorites, HomePageHeader XAML embed)
    - [x] SearchResultsPage (multi-token search, filter NavigationView, dynamic results)
    - [x] SettingsPage (CommunityToolkit SettingsCard/SettingsExpander, theme/nav/sound settings)
    - [x] ItemPage (PageHeader embed, Frame navigation to control pages, theme toggle, reflection cleanup)
- [x] **Migrate shell**: NavigationView chrome, TitleBar, search box.
    - [x] ShellComponent renders NavigationView + Frame via Duct DSL
    - [x] TitleBar now available as a first-class DSL element (`TitleBar(title)`)
    - [x] Search box can be embedded in TitleBar via `Content` property
    - [x] Dynamic control group items populated from ControlInfoDataSource
    - [x] Navigation selection handling moved to ShellComponent
    - [x] Test automation helpers preserved in XAML

---

## Duct Framework Gaps (discovered during migration)

These gaps were found during the migration of 105 control pages, 6 shell pages, and the NavigationView shell, representing areas where the Duct DSL forced workarounds via `.Set()`, `.OnMount()`, or full imperative construction. Fixed gaps have been removed. Ordered by impact.

### High Priority — Missing DSL elements (required full imperative construction)

| Gap | Pages Affected | Workaround |
|-----|---------------|------------|
| ~~**No TitleBar DSL element**~~ | ~~MainWindow shell~~ | ~~Resolved — `TitleBar(title)` is now a first-class DSL element. Automatically calls `Window.SetTitleBar()` on mount.~~ |
| **No ContentIsland / ChildSiteLink / composition hosting** | ContentIslandPage | Entire page is imperative — 3D model loading, composition tree (skip) |
| **No CaptureElement / MediaCapture** | CaptureElementPreviewPage | Camera capture + snapshot gallery entirely imperative (skip) |
| **No SettingsCard / SettingsExpander DSL elements** | SettingsPage | CommunityToolkit controls built entirely imperatively via Border + `.Set()` — header, description, icons, nested cards all manual |
| **No way to embed existing XAML UserControls as Duct elements** | HomePage, ItemPage | HomePageHeader, PageHeader embedded via Border + `.Set()` workaround; requires RegisterType for clean integration |

### Medium Priority — Missing DSL properties/modifiers

| Gap | Pages Affected | Workaround |
|-----|---------------|------------|
| **NavigationViewItem icon only supports Symbol enum** | ShellComponent, NavigationViewPage | `NavItem` icon param takes `Symbol` — cannot use FontIcon glyphs (e.g. `"\uE8F1"`); must apply icons imperatively via `.Set()` |
| **No NavigationViewItemHeader DSL element** | ShellComponent | "Controls" section header must be inserted imperatively into NavigationView.MenuItems |
| **No NavigationView.DisplayModeChanged event in DSL** | ShellComponent | Pane toggle button visibility depends on display mode; requires imperative event wiring |
| **No NavigationView.IsBackButtonVisible / IsPaneToggleButtonVisible in DSL** | ShellComponent | Must use `.Set()` for these NavigationView properties |
| **No NavigationView.IsTabStop in DSL** | ShellComponent | Must use `.Set()` to prevent NavigationView from receiving keyboard focus |
| **No NavigationView.FooterMenuItems in DSL** | NavigationViewPage | Footer items added imperatively via `.Set()` |
| **No NavigationView.MenuItemsSource / MenuItemTemplate** | NavigationViewPage, SearchResultsPage, ShellComponent | Dynamic menu items from data require `.Set()`; no data-driven menu pattern |
| **No ContextFlyout on NavigationViewItems in DSL** | ShellComponent | Copy-link flyouts on nav items require imperative construction |
| **No AutomationProperties on NavigationViewItems in DSL** | ShellComponent | AutomationId/Name must be set via imperative loop after mount |
| **No Frame.Navigated / Frame.Navigating events in DSL** | ShellComponent, ItemPage | Test automation and back button visibility require imperative Frame event handlers |
| **No NavigationView.IsPaneOpen property change callback in DSL** | ShellComponent | Accessibility announcements for pane open/close require `RegisterPropertyChangedCallback()` imperatively |
| **No Storyboard / DoubleAnimation / EasingFunction DSL** | EasingFunctionPage | Animations built imperatively (skip) |
| **No Composition animation DSL (SpringVector3, ExpressionAnimation)** | XamlCompInteropPage | All composition animations + StartAnimation() imperative |
| **No ConnectedAnimationService DSL** | ConnectedAnimationPage | PrepareAnimationForConnectedAnimation / TryStartConnectedAnimation imperative |
| **No TabView.TabItemsSource / DataTemplate binding** | TabViewPage | Data-bound tab scenario uses XamlReader.Load for DataTemplate |
| **No TabView.TabStripHeader / TabStripFooter** | TabViewPage | `.Set()` to assign header/footer content |
| **No TabView.Resources for theme dictionaries** | TabViewPage | Accent-colored tab strip built imperatively with ResourceDictionary |
| **No XamlUICommand / StandardUICommand DSL** | XamlUICommandPage, StandardUICommandPage | Command binding + KeyboardAccelerator entirely imperative |
| **No RichEditBox document API (ITextDocument)** | RichEditBoxPage, ClipboardPage | Formatting, file open/save, SetText/GetText all via `.Set()` |
| **No PointerEntered / PointerExited event DSL** | XamlCompInteropPage | Pointer events set imperatively; no declarative handler props |
| **No Polygon DSL element** | ShapePage, LinePage | Polygon/Polyline/Path built imperatively (Rectangle/Ellipse/Line exist) |
| **No WrapPanel layout element** | ContentIslandPage | Built imperatively |
| **No GridView ContainerContentChanging in DSL** | AllControlsPage, SectionPage, HomePage, SearchResultsPage | Workaround: filter data before rendering; event still unavailable |
| **No GridView ItemContainerStyle in DSL** | AllControlsPage, SectionPage | Custom item styles require `.Set()` on the GridView Resources |
| **No implicit show/hide animations (Community Toolkit)** | HomePage | `animations:Implicit.ShowAnimations` / `HideAnimations` not expressible |
| **No NavigationView.SelectedItem programmatic set** | SearchResultsPage, SectionPage, ShellComponent | Must reach into NavigationView imperatively |
| **No visual tree traversal (GetDescendantsOfType)** | ItemPage | Theme toggle on SampleThemeListener descendants requires imperative tree walk |
| **`.CornerRadius()` only accepts `double`, not `CornerRadius` struct** | ControlItemTemplateHelper | Cannot pass theme resource `CornerRadius` values; must use `.Set()` instead |
| **`.WithBorder()` only accepts `(string, double)`, not `(Brush, Thickness)`** | ControlItemTemplateHelper | Cannot pass theme resource brushes; must use `.Set()` for border properties |

### Low Priority — Nice-to-have improvements

| Gap | Pages Affected | Notes |
|-----|---------------|-------|
| **ContentDialog is inherently imperative** | ContentDialogPage, SettingsPage | `ShowAsync()` doesn't fit declarative model (skip) |
| **StoragePicker needs XamlRoot access** | StoragePickersPage | `ContentIslandEnvironment.AppWindowId` requires imperative access via `.Set()` |
| **No ComboBox with ComboBoxItem children (Content + Tag)** | StoragePickersPage, SettingsPage | Typed combo items with separate display/value require `.Set()` |
| **SplitView.PaneBackground / PanePlacement not in DSL** | SplitViewPage | Must use `.Set()` for these properties |
| **No ContextFlyout on TabViewItems** | TabViewPage | Requires imperative `.Set()` |
| **SelectorBarItem icon takes string glyph, not Symbol enum** | HomePage | Must use glyph codes like `"\uE823"` instead of `Symbol.Clock` |
| **No HorizontalScrollContainer (Gallery custom control)** | HomePage | Gallery-specific control must be embedded imperatively |
| **No ElementSoundPlayer DSL** | SettingsPage | Sound/spatial audio state management is entirely imperative |
| **No Clipboard API integration** | SettingsPage | Copy-to-clipboard requires imperative DataPackage construction |
