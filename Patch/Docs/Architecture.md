# Patch Architecture

Patch (Functional UI) is a React/SwiftUI/Compose-inspired functional projection for WinUI 3. It lets you write WinUI apps in pure C# with a declarative, component-based approach — no XAML, no data binding, no templates.

## Core Concepts

### Virtual Element Tree

The central idea is **virtual elements** — lightweight, immutable C# records that *describe* what the UI should look like, without directly creating WinUI controls. This is the same concept as React's virtual DOM.

```
Element (abstract record)
├── TextElement
├── ButtonElement
├── TextFieldElement
├── CheckBoxElement
├── SliderElement
├── ToggleSwitchElement
├── ProgressElement
├── StackElement          (children: Element[])
├── GridElement
├── ScrollViewElement
├── BorderElement
├── ExpanderElement
├── ComponentElement      (wraps a Component class)
├── FuncElement           (inline function component)
├── ModifiedElement       (wraps element + layout modifiers)
└── EmptyElement          (renders nothing)
```

Elements are:
- **Immutable** (C# records with `with` expressions for modification)
- **Cheap to create** (no WinUI objects allocated)
- **Diffable** (the reconciler can compare old vs new trees)

### Components

Components are where state lives. There are two flavors:

**Class components** — extend `Component` and override `Render()`:
```csharp
class Counter : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        return VStack(
            Text($"Count: {count}"),
            Button("+1", () => setCount(count + 1))
        );
    }
}
```

**Function components** — inline via `Func(ctx => ...)`:
```csharp
var widget = Func(ctx =>
{
    var (on, setOn) = ctx.UseState(false);
    return ToggleSwitch(on, setOn);
});
```

### Hooks (State Management)

Patch uses a hooks system inspired by React. Hooks are methods called during `Render()` that provide stateful behavior:

| Hook | Purpose |
|------|---------|
| `UseState<T>(initial)` | Declares a piece of state. Returns `(value, setter)`. |
| `UseReducer<T>(initial)` | Like UseState but the setter takes `Func<T,T>` (previous → next). |
| `UseEffect(action, deps)` | Runs side effects after render when deps change. |
| `UseMemo<T>(factory, deps)` | Memoizes a computed value. |
| `UseCallback(action, deps)` | Returns a stable callback reference. |
| `UseRef<T>(initial)` | Mutable reference that persists across renders. |

**Hook rules** (same as React):
1. Call hooks in the same order every render (no conditional hooks)
2. Only call hooks from within `Render()` or a function component body

### Reconciler

The reconciler is the engine that makes declarative UI work efficiently. When state changes:

1. The component re-renders, producing a **new** element tree
2. The reconciler **diffs** the old and new trees
3. Only the **differences** are applied to real WinUI controls

```
┌─────────────┐     ┌─────────────┐
│  Old Tree   │     │  New Tree   │
│  (Elements) │     │  (Elements) │
└──────┬──────┘     └──────┬──────┘
       │                   │
       └───────┬───────────┘
               │
        ┌──────▼──────┐
        │  Reconciler │
        │   (diff)    │
        └──────┬──────┘
               │
        ┌──────▼──────┐
        │  WinUI      │
        │  Controls   │
        │  (patched)  │
        └─────────────┘
```

**Reconciliation strategy:**
- **Same type, same position** → update the existing control's properties in place
- **Different type** → unmount old control, mount new one
- **Keyed elements** → matched by key for stable identity across list reorderings
- **Null / EmptyElement** → control is removed (enables conditional rendering)

### PatchHost & Render Loop

`PatchHost` bridges the virtual tree to a real WinUI `Window`:

1. It holds the root component and the current element tree
2. When any state setter is called, it schedules a re-render via `DispatcherQueue`
3. Multiple state changes in the same synchronous block are **batched** into a single render
4. After rendering, effects are flushed

```
State setter called
    → RequestRender()
    → (batched via DispatcherQueue)
    → Render()
        → component.Render()  [produces new element tree]
        → reconciler.Reconcile(old, new)  [patches WinUI]
        → context.FlushEffects()
```

## Architecture Diagram

```
┌────────────────────────────────────────────────────┐
│                   Your App Code                     │
│  class MyApp : Component { Render() => VStack(...) }│
└────────────────────┬───────────────────────────────┘
                     │ uses
┌────────────────────▼───────────────────────────────┐
│                    Patch Library                       │
│                                                      │
│  ┌──────────┐  ┌───────────┐  ┌──────────────────┐ │
│  │ Elements │  │ Component │  │  Element          │ │
│  │ (DSL)    │  │ (State +  │  │  Extensions       │ │
│  │          │  │  Hooks)   │  │  (.Bold(), etc)   │ │
│  └────┬─────┘  └─────┬─────┘  └────────┬─────────┘ │
│       │              │                  │            │
│       └──────┬───────┘──────────────────┘            │
│              │                                       │
│       ┌──────▼──────┐                                │
│       │  Reconciler │                                │
│       │  (tree diff)│                                │
│       └──────┬──────┘                                │
│              │                                       │
│       ┌──────▼──────┐     ┌──────────────┐          │
│       │   PatchHost   │────▶│  PatchApp      │          │
│       │ (render     │     │  (bootstrap) │          │
│       │  loop)      │     └──────────────┘          │
│       └──────┬──────┘                                │
└──────────────┼───────────────────────────────────────┘
               │ creates/patches
┌──────────────▼───────────────────────────────────────┐
│              WinUI 3 Controls                         │
│  (TextBlock, Button, StackPanel, Grid, ...)           │
└──────────────────────────────────────────────────────┘
```

## Design Decisions

### Why records for elements?
C# records give us immutability, structural equality, and `with` expressions for free. This makes elements safe to diff and cheap to create variations of.

### Why hooks instead of MVVM/INotifyPropertyChanged?
Hooks keep state co-located with the render logic. There's no separate ViewModel class, no property-changed boilerplate, no binding markup. State is just a local variable in your render method.

### Why factory methods instead of constructors?
`Text("hello")` reads better than `new TextElement("hello")`. Combined with `using static Patch.UI`, you get a clean DSL that looks almost like a markup language:
```csharp
VStack(
    Heading("Settings"),
    ToggleSwitch(darkMode, setDarkMode),
    Button("Save", onSave)
)
```

### Why no XAML?
XAML adds a layer of indirection (markup ↔ codebehind) and requires tooling support. Patch's approach keeps everything in C# where you get full IDE support — IntelliSense, refactoring, type safety, and the full power of the language for control flow.

### Tag-based event handler pattern
WinUI event handlers are wired once at mount time. To update closures (which capture state), the reconciler stores the current element in the control's `Tag` property. Event handlers read from `Tag` at invocation time, ensuring they always use the latest closure. This avoids re-subscribing events on every render.
