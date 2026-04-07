# Migration Guide: Declarative Modifiers & Hooks

This guide covers four new Duct APIs that replace common `.Set()` and `.OnMount()` escape hatches with first-class declarative equivalents.

---

## 1. Typography Modifiers (FontFamily, FontSize, FontWeight)

**Problem:** Setting font properties on non-Text elements required `.Set()` with a cast:

```csharp
// BEFORE — imperative, bypasses reconciliation
Button("New", onClick)
    .Set(fe =>
    {
        var b = (Microsoft.UI.Xaml.Controls.Button)fe;
        b.FontFamily = new FontFamily("Segoe Fluent Icons");
        b.FontSize = 10;
    })
```

**After:** Use the chainable `.FontFamily()`, `.FontSize()`, `.FontWeight()` modifiers. These work on **any** element (Button, Border, CheckBox, etc.), not just `TextElement`:

```csharp
// AFTER — declarative, participates in reconciliation
Button("New", onClick)
    .FontFamily("Segoe Fluent Icons")
    .FontSize(10)
```

### API

```csharp
// String overload (creates FontFamily from name)
.FontFamily("Segoe Fluent Icons")

// Instance overload (reuse a FontFamily object)
.FontFamily(myFontFamily)

// Font size (double)
.FontSize(10)

// Font weight
.FontWeight(new FontWeight(700))
```

### What to migrate

Search for `.Set()` calls that set `FontFamily`, `FontSize`, or `FontWeight` on the control:

```
// Find candidates:
.Set(fe => { ... fe.FontFamily = ... })
.Set(fe => { ... fe.FontSize = ... })
.Set(b => b.FontFamily = ...)
.Set(b => b.FontSize = ...)
```

Replace with the chainable modifier equivalents above.

### Note on TextElement

`TextElement` already had its own `.FontSize()` and `.FontFamily()` extensions that set properties on the `TextElement` record directly. Those still work. The new general modifiers set properties via `ElementModifiers`, which the reconciler applies to the underlying WinUI control. For `TextElement`, prefer the existing typed extensions since they participate in the bitmask diff optimization.

---

## 2. Declarative Event Handlers

**Problem:** Event handlers required `.OnMount()` with manual event subscription, leading to stale closures and no cleanup on re-render:

```csharp
// BEFORE — imperative, stale closures, no cleanup
Border(content)
    .OnMount(fe =>
    {
        fe.SizeChanged += (s, e) =>
        {
            if (e.NewSize.Width > 0)
                setViewportWidth(e.NewSize.Width);  // captures stale viewportWidth!
        };
    })
```

**After:** Use declarative event handler modifiers. These re-attach on every update, so closures always capture fresh state. The reconciler automatically detaches the previous handler before attaching the new one:

```csharp
// AFTER — declarative, always-fresh closures, auto-cleanup
Border(content)
    .OnSizeChanged((s, e) =>
    {
        if (e.NewSize.Width > 0)
            setViewportWidth(e.NewSize.Width);  // always current!
    })
```

### Available handlers

| Modifier | WinUI Event | Signature |
|----------|-------------|-----------|
| `.OnSizeChanged(handler)` | `SizeChanged` | `Action<object, SizeChangedEventArgs>` |
| `.OnPointerPressed(handler)` | `PointerPressed` | `Action<object, PointerRoutedEventArgs>` |
| `.OnPointerMoved(handler)` | `PointerMoved` | `Action<object, PointerRoutedEventArgs>` |
| `.OnPointerReleased(handler)` | `PointerReleased` | `Action<object, PointerRoutedEventArgs>` |
| `.OnTapped(handler)` | `Tapped` | `Action<object, TappedRoutedEventArgs>` |
| `.OnKeyDown(handler)` | `KeyDown` | `Action<object, KeyRoutedEventArgs>` |

### Migration examples

**SizeChanged:**
```csharp
// BEFORE
.OnMount(fe =>
{
    fe.SizeChanged += (s, e) => setViewportWidth(e.NewSize.Width);
})

// AFTER
.OnSizeChanged((s, e) => setViewportWidth(e.NewSize.Width))
```

**Pointer events (drag handling):**
```csharp
// BEFORE
.OnMount(fe =>
{
    fe.PointerPressed += (s, e) =>
    {
        ((UIElement)s!).CapturePointer(e.Pointer);
        dragging = true;
        e.Handled = true;
    };
    fe.PointerMoved += (s, e) => { /* drag logic */ };
    fe.PointerReleased += (s, e) => { /* end drag */ };
})

// AFTER
.OnPointerPressed((s, e) =>
{
    ((UIElement)s!).CapturePointer(e.Pointer);
    dragging = true;
    e.Handled = true;
})
.OnPointerMoved((s, e) => { /* drag logic */ })
.OnPointerReleased((s, e) => { /* end drag */ })
```

**Tapped (close button):**
```csharp
// BEFORE
.OnMount(fe =>
{
    fe.Tapped += (s, e) =>
    {
        e.Handled = true;
        onAction?.Invoke(new ClosePanel(panel));
    };
})

// AFTER
.OnTapped((s, e) =>
{
    e.Handled = true;
    onAction?.Invoke(new ClosePanel(panel));
})
```

**KeyDown:**
```csharp
// BEFORE
.OnMount(fe =>
{
    fe.KeyDown += (s, e) =>
    {
        if (e.Key == VirtualKey.Escape)
        {
            DispatchAction(new DragCancelledAction());
            e.Handled = true;
        }
    };
})

// AFTER
.OnKeyDown((s, e) =>
{
    if (e.Key == VirtualKey.Escape)
    {
        DispatchAction(new DragCancelledAction());
        e.Handled = true;
    }
})
```

### When to keep `.OnMount()`

`.OnMount()` is still appropriate for one-time setup that isn't an event handler:
- Setting up `XamlRoot` references
- One-time visual state changes
- Attaching to events not covered by the declarative modifiers

### When to keep `.Set()`

`.Set()` is still appropriate for properties not covered by modifiers:
- `FlowDirection`
- `IsTabStop`
- Control-specific properties (e.g., `TickFrequency` on Slider — already has a typed extension)

---

## 3. Action-Based UseReducer

**Problem:** Complex state mutations required a large imperative `DispatchAction` switch with manual version bumping:

```csharp
// BEFORE — imperative mutations, manual version tracking
var versionRef = UseRef(0);
var (version, setVersion) = UseState(0);

void DispatchAction(DockingAction action)
{
    switch (action)
    {
        case ClosePanel close:
            editor.RemovePanel(close.Panel);    // mutate model
            versionRef.Current++;               // manual version bump
            host.DispatcherQueue.TryEnqueue(    // deferred re-render
                () => setVersion(versionRef.Current));
            break;
        case ActivateTab activate:
            activate.TabGroup.SetActiveIndex(activate.Index);
            versionRef.Current++;
            // ... same boilerplate
            break;
        // 10+ more cases...
    }
}
```

**After:** Use the action-based `UseReducer` to manage state functionally. The reducer returns new immutable state, and re-renders happen automatically:

```csharp
// AFTER — functional reducer, automatic re-renders
abstract record DockingAction;
record ClosePanel(PanelNode Panel) : DockingAction;
record ActivateTab(TabGroupNode TabGroup, int Index) : DockingAction;

record DockingState(DockNode Root, /* ... */);

static DockingState Reducer(DockingState state, DockingAction action) => action switch
{
    ClosePanel close => state with { Root = RemovePanel(state.Root, close.Panel) },
    ActivateTab activate => state with { Root = SetActiveTab(state.Root, activate) },
    _ => state,
};

// In component:
var (state, dispatch) = UseReducer<DockingState, DockingAction>(Reducer, initialState);

// Dispatch actions — re-render is automatic when state changes
dispatch(new ClosePanel(panel));
dispatch(new ActivateTab(tabGroup, 2));
```

### API

```csharp
// In a Component class:
protected (TState Value, Action<TAction> Dispatch) UseReducer<TState, TAction>(
    Func<TState, TAction, TState> reducer,
    TState initialState)

// In a RenderContext (function components):
ctx.UseReducer<TState, TAction>(reducer, initialState)
```

### Key differences from the functional-updater UseReducer

The existing `UseReducer<T>(initialValue)` returns `(T, Action<Func<T, T>>)` — the updater takes a function from old state to new state. The new overload takes a **separate reducer function** and dispatches **typed actions**:

```csharp
// Existing (functional updater):
var (count, update) = UseReducer(0);
update(prev => prev + 1);

// New (action-based):
var (count, dispatch) = UseReducer<int, string>(
    (state, action) => action == "increment" ? state + 1 : state,
    0);
dispatch("increment");
```

Use the action-based variant when:
- You have multiple distinct operations on the same state
- State transitions are complex (multiple fields, validation)
- You want to centralize state logic in a pure reducer function
- You're porting Redux/Flux patterns

---

## 4. Grid Layout Builders

**Problem:** Building grids with interspersed separators required manual index math:

```csharp
// BEFORE — manual index calculation, error-prone
var sizes = new List<string>();
var children = new List<Element>();
for (int i = 0; i < node.Children.Count; i++)
{
    sizes.Add($"{node.Proportions[i]:F6}*");
    children.Add(childElement.Grid(row: 0, column: i * 2));  // i * 2!
    if (i < node.Children.Count - 1)
    {
        sizes.Add($"{SplitterThickness}");
        children.Add(splitter.Grid(row: 0, column: i * 2 + 1));  // i * 2 + 1!
    }
}
return Grid(isHorizontal ? columns: sizes.ToArray() : ..., children.ToArray());
```

**After:** Use `InterspersedGrid` which handles the index math, column/row definitions, and child placement:

```csharp
// AFTER — declarative, no manual indexing
return InterspersedGrid(
    Orientation.Horizontal,
    items: node.Children.Select(c => RenderNode(c)).ToArray(),
    proportions: node.Proportions.ToArray(),
    separatorSize: SplitterThickness,
    separatorFactory: i => Splitter(i)
);
```

### API

```csharp
// Items interspersed with separators (split panels, toolbars with dividers)
GridElement InterspersedGrid(
    Orientation orientation,       // Horizontal or Vertical
    Element[] items,               // The content items
    double[] proportions,          // Star proportions for each item
    double separatorSize,          // Fixed pixel size for separators
    Func<int, Element> separatorFactory)  // Creates separator at index i

// Equal-sized cells (simple uniform distribution)
GridElement UniformGrid(Orientation orientation, params Element?[] items)
```

### Examples

**Split panel with splitters:**
```csharp
InterspersedGrid(
    Orientation.Horizontal,
    [LeftPane(), RightPane()],
    [0.3, 0.7],
    6.0,
    i => Splitter()
)
// Produces: columns=["0.300000*", "6", "0.700000*"], rows=["*"]
```

**Three-column equal layout:**
```csharp
UniformGrid(Orientation.Horizontal, Col1(), Col2(), Col3())
// Produces: columns=["*", "*", "*"], rows=["*"]
```

**Vertical split with proportional panes:**
```csharp
InterspersedGrid(
    Orientation.Vertical,
    [TopPane(), MiddlePane(), BottomPane()],
    [0.5, 0.3, 0.2],
    4.0,
    i => HorizontalRule()
)
// Produces: rows=["0.500000*", "4", "0.300000*", "4", "0.200000*"], columns=["*"]
```

---

## Quick reference: What to search for

| Pattern to find | Replace with |
|-----------------|-------------|
| `.Set(fe => { ... fe.FontFamily = ... })` | `.FontFamily("name")` |
| `.Set(fe => { ... fe.FontSize = ... })` | `.FontSize(size)` |
| `.Set(fe => { ... fe.FontWeight = ... })` | `.FontWeight(weight)` |
| `.OnMount(fe => { fe.SizeChanged += ... })` | `.OnSizeChanged((s, e) => ...)` |
| `.OnMount(fe => { fe.PointerPressed += ... })` | `.OnPointerPressed((s, e) => ...)` |
| `.OnMount(fe => { fe.PointerMoved += ... })` | `.OnPointerMoved((s, e) => ...)` |
| `.OnMount(fe => { fe.PointerReleased += ... })` | `.OnPointerReleased((s, e) => ...)` |
| `.OnMount(fe => { fe.Tapped += ... })` | `.OnTapped((s, e) => ...)` |
| `.OnMount(fe => { fe.KeyDown += ... })` | `.OnKeyDown((s, e) => ...)` |
| `BumpVersion()` + `UseRef` + `setVersion` | `UseReducer<TState, TAction>(reducer, init)` |
| Manual `i * 2` grid index math | `InterspersedGrid(...)` |
