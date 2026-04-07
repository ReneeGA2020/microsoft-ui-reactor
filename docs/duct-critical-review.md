# Duct Framework — Critical Review

A skeptic's component-by-component analysis of Duct against React, SwiftUI, and
Jetpack Compose. This document catalogs architectural weaknesses, API design
problems, missing capabilities, and places where Duct is a hack on top of WinUI
rather than a principled declarative UI framework.

---

## Executive Summary

Duct is an ambitious attempt to bring React-style declarative UI to WinUI 3.
It has impressive breadth of control coverage (94% of WinUI controls wrapped) and
a reasonable hooks system. However, it suffers from several fundamental problems
that would prevent any serious team from choosing it over the established
frameworks:

1. **Theming is architecturally broken** — any use of Duct's styling API silently
   destroys dark mode and high contrast support
2. **Navigation is non-existent** — the core navigation scenario (pages with back
   stack) is blocked
3. **No global state mechanism** — no Context, no EnvironmentObject, no
   CompositionLocal
4. **The DSL is constrained by C# language limitations** — verbose, repetitive,
   and leaky compared to JSX, SwiftUI's result builders, or Compose's Kotlin DSL
5. **Accessibility is a checkbox, not a feature** — 1 out of 12 accessibility
   properties exposed
6. **Animation story is thin** — implicit transitions exist, but no declarative
   animation system
7. **The `.Set()` escape hatch is load-bearing** — a huge fraction of WinUI's
   functionality is only accessible through it, making Duct a thin wrapper rather
   than an abstraction

**Verdict:** Duct is a clever proof-of-concept that maps React's component model
onto WinUI. It is not a production-ready UI framework. The gap between "works in
a demo" and "ships in a real app" is enormous, and many of the gaps are
architectural, not incremental.

---

## Table of Contents

1. [The DSL: C# as a UI Language](#1-the-dsl-c-as-a-ui-language)
2. [Component Model](#2-component-model)
3. [State Management](#3-state-management)
4. [The Reconciler](#4-the-reconciler)
5. [Layout System](#5-layout-system)
6. [Styling and Theming](#6-styling-and-theming)
7. [Navigation](#7-navigation)
8. [Lists and Collections](#8-lists-and-collections)
9. [Animation](#9-animation)
10. [Accessibility](#10-accessibility)
11. [Input Handling and Events](#11-input-handling-and-events)
12. [Developer Experience](#12-developer-experience)
13. [The .Set() Problem](#13-the-set-problem)
14. [Component-by-Component Scorecard](#14-component-by-component-scorecard)
15. [Conclusion](#15-conclusion)

---

## 1. The DSL: C# as a UI Language

### The fundamental problem

React has JSX. SwiftUI has result builders. Compose has Kotlin's trailing lambdas
and compiler plugin. Duct has... C# method calls with `params` arrays.

This matters enormously. The quality of the UI DSL determines whether writing UI
code feels natural or feels like fighting the language.

### What Duct looks like

```csharp
VStack(
    Text("Hello").Bold().FontSize(24),
    Button("Click me", () => setCount(count + 1))
        .Background("#0078D4")
        .Padding(12, 8),
    count > 5 ? Text("Wow!") : null
)
```

### What the competition looks like

**React (JSX):**
```jsx
<VStack>
  <Text bold fontSize={24}>Hello</Text>
  <Button onClick={() => setCount(c => c + 1)}
          style={{ background: '#0078D4', padding: '12px 8px' }}>
    Click me
  </Button>
  {count > 5 && <Text>Wow!</Text>}
</VStack>
```

**SwiftUI:**
```swift
VStack {
    Text("Hello").bold().font(.title)
    Button("Click me") { count += 1 }
        .background(.blue)
        .padding(.horizontal, 12)
    if count > 5 { Text("Wow!") }
}
```

**Compose:**
```kotlin
Column {
    Text("Hello", fontWeight = FontWeight.Bold, fontSize = 24.sp)
    Button(onClick = { count++ }) {
        Text("Click me")
    }
    if (count > 5) { Text("Wow!") }
}
```

### Specific DSL weaknesses

**1. No block syntax for children.** SwiftUI and Compose use trailing closures
/ lambdas that visually separate children from the container. In Duct, children
are positional arguments mixed into the constructor call. For deeply nested UIs,
this becomes bracket-counting hell:

```csharp
VStack(
    HStack(
        VStack(
            Text("Label"),
            Text("Value")
        ).Width(200),
        VStack(
            Text("Label 2"),
            Text("Value 2")
        ).Width(200)
    ),
    HStack(
        Button("OK", onOk),
        Button("Cancel", onCancel)
    )
)  // Which closing paren belongs to which?
```

**2. Modifier chains allocate on every render.** Every `.Margin(10)` call does
`el with { Modifiers = ... }` creating a new record copy. A chain of 5 modifiers
creates 5 intermediate records. SwiftUI avoids this via opaque return types and
view builder inlining. Compose avoids it via `Modifier` composition that chains
without copying. Duct's approach generates measurable GC pressure in hot render
paths — every element with modifiers allocates multiple short-lived objects per
render cycle.

**3. The `with` expression problem.** Duct's element records use C# records
with `init` properties. The fluent modifier pattern uses either extension methods
that create `ElementModifiers` records and merge them, or `with` expressions on
the concrete record. This means *every modifier call allocates a new
ElementModifiers record*, even for a single property change:

```csharp
// This creates a new ElementModifiers for just one property,
// then merges it (another allocation) with the existing modifiers
public static T Margin<T>(this T el, double uniform) where T : Element =>
    Modify(el, new ElementModifiers { Margin = new Thickness(uniform) });
```

**4. String-typed APIs where enums or types should be used.** Grid columns are
`string[]`: `Grid(["*", "Auto", "200"], ...)`. There's no compile-time
validation that `"*"` or `"Auto"` are valid values. SwiftUI uses
`GridItem(.flexible())`, `GridItem(.fixed(200))` — type-safe and discoverable.
Compose uses `GridCells.Adaptive(minSize)`. Duct borrowed CSS grid syntax but
lost type safety in the process.

**5. No children as content blocks.** In React, JSX children are a natural part
of the markup. In SwiftUI, `@ViewBuilder` closures provide block syntax. In
Compose, `content: @Composable () -> Unit` trailing lambdas do the same. Duct
forces all children into `params Element?[]` constructor arguments — there's no
visual distinction between container properties and container children.

**6. Implicit string conversion is a code smell, not a feature.** `Element` has
`public static implicit operator Element(string text) => new TextElement(text)`.
This means `VStack("Hello", "World")` works. But it also means any string
accidentally passed where an Element is expected silently becomes text. This is
the kind of convenience that causes bugs in large codebases.

**7. Null-based conditional rendering is fragile.** Duct uses nullable elements
for conditional rendering: `condition ? Text("yes") : null`. This works but
requires `FilterChildren` to strip nulls on every render. SwiftUI uses `if`
directly in the view builder. Compose uses `if` naturally. React uses
`{condition && <Component/>}`. Duct's approach works but is less readable and
requires runtime filtering.

---

## 2. Component Model

### What Duct has

- **Class components** extending `Component` with `Render()` method
- **Props via generics**: `Component<TProps>` for typed props
- **Function components**: `Func(ctx => { ... })` inline lambdas
- **Error boundaries**: `ErrorBoundary(child, fallback)`
- **No slots/named children** — no mechanism for multi-slot composition

### What's missing vs the competition

**No global state sharing mechanism.** React has Context + useContext. SwiftUI has
@EnvironmentObject and @Environment. Compose has CompositionLocal. Duct has...
nothing. There's no way to pass data through the component tree without threading
it through every intermediate component's props. This is a *fundamental*
architectural gap — theming, localization, auth state, navigation state, and
virtually every cross-cutting concern in a real app needs some form of ambient
state.

The `UseObservable` hook bridges to INotifyPropertyChanged, which could serve as
a workaround, but it requires every consumer to know about and hold a reference
to the view model object — it's not ambient, it's explicit dependency passing.

**LocaleProvider is a one-off hack that proves the need.** The localization
system implements `LocaleContext.Current` as thread-static state — effectively a
hand-rolled CompositionLocal for just one use case. If the framework needed this
for localization, it needs it as a general-purpose mechanism.

**No Suspense or lazy loading.** React's `Suspense` + `React.lazy()` for
code-split components has no equivalent. In a large app, this means either
loading everything eagerly or building your own loading state management per
component. SwiftUI and Compose also lack this, so Duct is not uniquely deficient
here — but React has had it since 2018.

**Component props are untyped at the element level.** `ComponentElement` stores
props as `object?`. Props are set via `IPropsReceiver.SetProps(object props)` with
a cast. This means:
- No compile-time validation that the right props type is passed
- A runtime `InvalidCastException` if you get it wrong
- The generic `Component<T, TProps>(props)` factory hides this, but the
  underlying infrastructure is stringly-typed

**No shouldComponentUpdate / React.memo equivalent.** There's no way to tell the
framework "this component's output hasn't changed, skip the re-render." Every
state change re-renders the entire subtree. React's `React.memo()`, SwiftUI's
`EquatableView`, and Compose's automatic skipping via stable parameters all
provide this. Duct will render every component on every state change in the
ancestor chain, regardless of whether the component's inputs changed.

**Component identity is based on position + type, not key.** Like early React,
Duct matches components by their position in the tree. If you conditionally
render different components at the same position, state is lost. Keys exist on
elements but the component lifecycle implications aren't well-defined — there's
no documentation on what happens to component state when keys change.

**FuncElement creates a new RenderContext per instance, but identity is
problematic.** `Func(ctx => ...)` creates a `FuncElement` with a lambda. But
since the lambda is recreated every render (it captures outer state), the
function reference changes every time. The reconciler must have special handling
to avoid remounting FuncElement instances on every parent re-render — and the
`ShallowEquals` method doesn't even attempt to compare FuncElements (they always
return false, forcing an update).

---

## 3. State Management

### What Duct has

- `UseState<T>` — React's useState equivalent
- `UseReducer<T>` — functional updater variant
- `UseReducer<TState, TAction>` — Redux-style reducer
- `UseEffect` — side effects with dependency tracking
- `UseMemo<T>` — memoized computation
- `UseCallback` — stable callback reference
- `UseRef<T>` — mutable reference
- `UseObservable<T>` — INotifyPropertyChanged bridge
- `UseCollection<T>` — ObservableCollection bridge
- `UseWindowSize` / `UseBreakpoint` — responsive hooks

### Critiques

**1. Hook state is `object`-typed internally.** `HookState.Value` is `object`.
Every value type (int, bool, double) is boxed on every render. React hooks in
JavaScript don't have this problem (no boxing). SwiftUI's @State uses generics
throughout. Compose's `MutableState<T>` is generic. Duct pays a boxing penalty
for every piece of primitive state, on every render.

```csharp
private class HookState
{
    public object Value = default!;  // Boxing for value types
}
```

**2. Dependency comparison uses `object.Equals`.** `UseEffect`, `UseMemo`, and
other dependency-tracking hooks compare deps with `Equals(prev[i], next[i])`.
This means:
- Value types are boxed into the `object[]` dependency array (allocation)
- Reference types use reference equality by default unless they override Equals
- Collections as dependencies will compare by reference, not by content
- No warning or guidance about what makes a good dependency

React has the same issue (shallow comparison) but has ESLint rules that catch
common mistakes. Duct has no tooling support.

**3. No state persistence across component unmount/remount.** SwiftUI has
`@SceneStorage`. Compose has `rememberSaveable`. React has various persistence
solutions. In Duct, if a component is unmounted and remounted (e.g., navigating
away and back), all state is lost. For a desktop app framework where window
restoration is expected, this is a significant gap.

**4. UseCallback is just UseMemo returning the same Action.** The implementation
is literally:

```csharp
public Action UseCallback(Action callback, params object[] dependencies)
{
    return UseMemo(() => callback, dependencies);
}
```

This doesn't actually stabilize the callback reference in the way React's
useCallback does. The `() => callback` lambda captures `callback`, so if
`callback` is a new closure each render (which it always is), the memoized
value is the closure captured at first render — meaning it has stale captures
unless the deps change. This is correct behavior but the documentation doesn't
explain this nuance, and the implementation is subtly different from React's
behavior.

**5. Effect cleanup timing is synchronous and during render.** Effects run
their cleanup during `UseEffect` (the hook call itself, during render), not
during `FlushEffects`. This means cleanup runs synchronously as part of the
render phase. React runs cleanups asynchronously after render. This could cause
issues with effects that have expensive cleanup (network cancellation, timer
disposal) blocking the render.

Looking at the code:
```csharp
if (hook.Dependencies is null || !DepsEqual(hook.Dependencies, dependencies))
{
    hook.Cleanup?.Invoke();  // Cleanup runs HERE, during render
    hook.Dependencies = dependencies.ToArray();
    hook.Effect = effect;
    hook.Pending = true;
}
```

**6. No batching control or transition API.** React 18 has `startTransition`
for marking non-urgent updates. Duct batches via DispatcherQueue, which is
all-or-nothing. There's no way to say "this state update is low priority" or
"this update should not show a loading state."

**7. No global state at all.** This bears repeating because it's so important.
Every real app needs to share state across the component tree: user session,
theme preference, feature flags, router state. Duct provides zero mechanism for
this. The workarounds are:

- Thread props through every intermediate component (prop drilling — exactly what
  Context was invented to solve)
- Use static/singleton state (breaks component isolation, no re-render on change)
- Use `UseObservable` with a shared view model (requires explicit wiring, no
  tree scoping)

None of these are acceptable for a production framework.

---

## 4. The Reconciler

### Architecture

The reconciler follows React's model: diff old and new element trees, apply
minimal patches. It's split across `Reconciler.cs` (orchestration),
`Reconciler.Mount.cs` (~40 mount handlers), `Reconciler.Update.cs` (~30 update
handlers), and `ChildReconciler.cs` (keyed/unkeyed lists).

### Critiques

**1. Massive switch/dispatch for every element type.** The Mount and Update
methods are giant type-based dispatches. Adding a new element type requires
modifying both `Mount()` and `Update()` — a violation of the open/closed
principle. The `RegisterType<>` extensibility API exists but the built-in types
don't use it; they're hardcoded switch arms. This means the reconciler is a
monolithic class with ~70+ individual handler methods across its partial files.

React's reconciler is element-type-agnostic — it calls `createElement` and the
component decides how to render. SwiftUI's diffing is automatic via the View
protocol. Compose's compiler plugin handles recomposition transparently. Duct's
reconciler is essentially a hand-maintained mapping table from element types to
WinUI control operations.

**2. The Tag-based event dispatch is a fragile workaround.** WinUI controls
have a `Tag` property (type `object`) intended for user data. Duct repurposes it
to store the current element, so event handlers can read fresh state:

```csharp
// At mount time: wire event handler once
button.Click += (sender, _) => {
    var el = GetElementTag<ButtonElement>(sender);
    el?.OnClick?.Invoke();
};

// At update time: update the tag to point to the new element
SetElementTag(button, newElement);
```

This is clever but fragile:
- If anything else sets `Tag` (e.g., WinUI internals, user code via `.Set()`),
  event handlers silently break with no error
- It prevents users from using `Tag` for their own purposes
- It couples event dispatch to a WinUI implementation detail that could change
- There's a race condition: if an event fires between `Tag` being cleared and
  updated during reconciliation, the handler gets `null`

**3. Element Pool only handles non-interactive controls.** The element pool
recycles unmounted controls: TextBlock, StackPanel, Grid, Border, ScrollViewer,
Canvas, Image. But interactive controls (Button, TextBox, etc.) are NOT pooled
because "resetting their event state safely is more complex." In a real app,
interactive controls are the majority of the UI. This means the pool optimizes
the cheap case (layout containers) while leaving the expensive case (controls
with event subscriptions and visual state) unoptimized.

**4. ShallowEquals is conservative to the point of being useless.** The
`Element.ShallowEquals` optimization returns `false` for any element that has
Setters, event handlers, or is of an unknown type. Since virtually every
interactive element has at least one event handler, ShallowEquals will return
false for all of them, meaning the optimization only helps for static text and
images. The method explicitly says "Conservative: returns false for unknown
element types" — which means custom element types never benefit.

**5. Every component adds a hidden Border to the visual tree.** The reconciler
wraps every `Component` and `FuncElement` in a WinUI `Border` as an identity
anchor:

```csharp
var wrapper = new Border { Child = childControl };
_componentNodes[wrapper] = new ComponentNode { ... };
```

This means the WinUI visual tree is NOT 1:1 with the Duct element tree. A
component that renders a single `Text("Hello")` actually produces
`Border > TextBlock`. In a deeply nested component tree (which is how
well-structured Duct apps should look), you accumulate invisible Borders at
every component boundary. These are not free — each Border participates in
WinUI's measure/arrange layout cycle. React's Fiber architecture, SwiftUI's
view protocol, and Compose's slot table all avoid this overhead.

**6. The element pool uses a ForceDetach hack.** Returning a control to the pool
requires round-tripping it through a scratch `StackPanel`:

```csharp
_scratchPanel ??= new StackPanel();
_scratchPanel.Children.Add(element);    // Re-parent
_scratchPanel.Children.Remove(element);  // Detach
```

WinUI has internal parent tracking that doesn't fully release on logical removal.
Without this workaround, re-parenting a pooled control causes `COMException`.
This is the kind of hack that works until a WinUI update changes the internal
behavior, and it betrays how much the framework is fighting the platform rather
than working with it.

**7. No concurrent/interruptible rendering.** React 18's concurrent mode allows
rendering to be interrupted and resumed, preventing long renders from blocking
user interaction. Duct's render cycle is fully synchronous and runs to
completion. For complex UIs with many components, a state change triggers a full
synchronous re-render of the entire dirty subtree, which blocks the UI thread.
There is no mechanism to yield back to the event loop mid-render, prioritize
urgent updates, or time-slice work.

---

## 5. Layout System

### What Duct has

- `VStack` / `HStack` (StackPanel)
- `Grid` with string-based column/row definitions
- `Canvas` with absolute positioning
- `RelativePanel` with named references
- `FlexPanel` (CSS Flexbox via Yoga port) — **Duct-exclusive**
- `WrapGrid` (VariableSizedWrapGrid)
- `ScrollView`
- `Border`

### Critiques

**1. Grid definitions are stringly-typed.** `Grid(["*", "Auto", "200"], ["*"])`
uses string arrays. A typo like `"Atuo"` silently fails at runtime. SwiftUI uses
`GridItem(.flexible())`, `GridItem(.fixed(200))`. Even WinUI's own XAML
validates `ColumnDefinition` values at parse time. Duct's pure-string approach
is a regression in type safety.

**2. No Spacer equivalent.** SwiftUI's `Spacer()` is one of its most-used
layout tools — a flexible element that expands to fill available space. Duct has
no equivalent. You'd need to use `.HAlign(HorizontalAlignment.Stretch)` or Flex
layout with `grow`, neither of which is as intuitive as `Spacer()`.

**3. FlexPanel is impressive but duplicates WinUI's layout system.** Duct
ships a full CSS Flexbox implementation via a Yoga port. This is a significant
engineering effort that creates a parallel layout system alongside WinUI's
native layout. It means:
- Two layout systems with different mental models
- Flex children can't participate in Grid layout and vice versa
- Performance overhead of running Yoga's layout algorithm on top of WinUI's
- Debugging layout issues requires understanding which system is in play

SwiftUI and Compose don't have this problem because they own the entire layout
pipeline. Duct bolted Flexbox onto a platform that has its own (different) layout
model.

**4. No safe area or inset handling.** WinUI desktop apps have title bars,
task bars, and potentially custom chrome. Duct has no `SafeArea` concept. SwiftUI
has `.ignoresSafeArea()` and `.safeAreaInset()`. Compose has `WindowInsets`. Duct
developers must manually account for title bar height and other insets.

**5. Responsive layout is hook-based, which forces full re-renders.**
`UseWindowSize()` and `UseBreakpoint()` trigger a full component re-render
when the window resizes. SwiftUI's `@Environment(\.horizontalSizeClass)` only
invalidates views that read it. Compose's `BoxWithConstraints` only recomposes
the contained scope. Duct's approach means resizing a window re-renders the
entire component tree that uses any responsive hook.

---

## 6. Styling and Theming

### The theming disaster

This is Duct's most critical architectural flaw. The project's own gap analysis
labels it **P0** and "blocked for any real-world app."

**The core problem:** Every Duct styling modifier (`.Background()`,
`.Foreground()`, etc.) does a local-value property set on the WinUI control.
In WinUI's dependency property system, local values have the *highest* precedence
— higher than styles, higher than theme resources, higher than everything. Once
you set `.Background("#FF5733")`, that control will be `#FF5733` forever,
regardless of dark mode, high contrast, or any theme change.

**The impossible choice:**
- Don't use any Duct color modifiers → your app looks plain but themes correctly
- Use Duct color modifiers → your app looks custom but dark mode is broken

**What React does:** CSS custom properties (`var(--primary-color)`) that resolve
from theme context. Material UI's `ThemeProvider` + `useTheme()`. Styled-components
theme prop. All allow themed values that react to context changes.

**What SwiftUI does:** Semantic colors (`Color.primary`, `.accentColor`) that
automatically resolve per theme. Asset catalogs with dark/light/HC variants. The
system just works.

**What Compose does:** `MaterialTheme` provides `colorScheme.primary`, etc.
`isSystemInDarkTheme()` for detection. Theming is built into the foundation.

**What Duct does:** The theming design spec proposes a `ThemeRef` token system:
`.Background(Theme.Accent)`. This is partially implemented (ThemeRef records
exist, ThemeBindings dictionary exists on Element) but the reconciler support
and theme change detection are incomplete. Until this ships and is the default,
every Duct app with custom colors is broken in dark mode.

**The high contrast nightmare:** High contrast mode is even worse. A Duct app
that hard-codes *any* colors via modifiers violates Windows accessibility
requirements. In high contrast mode, users expect all UI elements to use the
system's high contrast palette. Duct makes it trivially easy to break this
with no warning.

### Other styling issues

**No style composition or reuse.** SwiftUI has `ViewModifier` for creating
reusable style bundles. Compose has `Modifier` which is composable. Duct has no
mechanism to group modifiers into reusable units. You can create extension methods
as a workaround, but there's no framework-level concept of a "style."

**No lightweight styling (theme resource key overrides).** WinUI's lightweight
styling lets you override specific theme resource keys per-control to customize
appearance while preserving theme reactivity. Duct has no DSL for this. It's
listed as a P1 gap.

**ApplyStyle is a string-based runtime lookup.** `.ApplyStyle("AccentButtonStyle")`
does a dictionary lookup in `Application.Current.Resources` at runtime. A typo
in the style name silently fails (returns null, sets Style to null). No
compile-time validation.

---

## 7. Navigation

### The gap

Navigation is the second most critical gap after theming. The gap analysis says
it plainly: "Navigation is generally broken."

**The core problem:** WinUI's Frame/Page navigation requires `Page` subclasses,
which require XAML code-behind files. Duct is pure C# — no XAML files. Therefore,
Frame/Page navigation is *architecturally blocked*.

**The workaround:**
```csharp
var (currentPage, setCurrentPage) = UseState("home");
return NavigationView(menuItems,
    currentPage switch {
        "home" => Component<HomePage>(),
        "settings" => Component<SettingsPage>(),
        _ => Text("404")
    }
);
```

**What this loses:**
- No back stack — pressing Back does nothing unless you manually implement a
  stack
- No navigation transitions — page changes are instant with no animation
- No parameter passing framework — you hand-roll props
- No deep linking — can't restore navigation state from a URL or activation arg
- No navigation lifecycle (willAppear, willDisappear)
- No nested navigation (nav stacks within tabs)

**What the competition provides:**

React: React Router with URL-based routing, nested routes, loaders, lazy loading,
transitions, `useNavigate()`, `useParams()`, `useSearchParams()`.

SwiftUI: `NavigationStack` with value-type routing, `navigationDestination(for:)`,
`NavigationPath` (serializable for state restoration), animated transitions.

Compose: `NavHost` + `NavController`, route-based navigation with arguments,
deep link support, back stack management, `AnimatedNavHost` for transitions.

**Duct:** Roll your own string-based switch statement.

---

## 8. Lists and Collections

### What Duct has

- `ListView(items)` / `GridView(items)` — WinUI controls
- `LazyVStack<T>` / `LazyHStack<T>` — virtualized via ItemsRepeater
- `TemplatedListView<T>` / `TemplatedGridView<T>` — typed templates
- `ForEach` — non-virtualized iteration
- `TreeView` with drag support
- `FlipView`, `SemanticZoom`

### Critiques

**1. ForEach is not virtualized.** `ForEach(items, item => ...)` produces a
`GroupElement` with ALL items rendered. For 1000 items, this creates 1000
elements in memory, diffs all 1000, and mounts all 1000 WinUI controls. React
has the same problem (it needs react-window), but SwiftUI's `ForEach` inside
`List` is virtualized, and Compose's `items()` inside `LazyColumn` is virtualized.
Duct requires explicitly choosing `LazyVStack<T>` for virtualization — a common
footgun for developers who reach for the simpler `ForEach` API first.

**2. LazyVStack requires a key selector.** `LazyVStack<T>(items, keySelector,
viewBuilder)` forces you to provide a key extraction function. React, SwiftUI,
and Compose all have default behaviors for keyless lists (positional matching).
Duct's LazyVStack won't compile without a key selector. While keys are a best
practice, making them mandatory adds friction for prototyping.

**3. No sections or grouping in lists.** SwiftUI has `Section(header:)` inside
`List`. Compose has `stickyHeader {}` in `LazyColumn`. Duct has nothing — to
create a sectioned list, you'd need to manually interleave header elements with
content items and handle all the layout yourself.

**4. No pull-to-refresh.** `RefreshContainer` exists but it's a separate
element, not integrated with ListView. SwiftUI has `.refreshable { }` on List.
Compose has `PullToRefreshBox`. Duct requires manually wrapping a list in a
RefreshContainer and wiring the callback.

**5. No drag-and-drop reordering for lists.** TreeView has drag support, but
ListView and GridView have no built-in reordering. SwiftUI has `.onMove` on
`ForEach`. This must be manually implemented in Duct.

**6. No empty state handling.** All frameworks require manual `if list.isEmpty`
checks, so this isn't a competitive disadvantage — but a production framework
could provide a convenience API like `ListView(items, template, emptyState)`.

---

## 9. Animation

### What Duct has

- **Implicit transitions:** `.OpacityTransition()`, `.ScaleTransition()`,
  `.RotationTransition()`, `.TranslationTransition()`, `.BackgroundTransition()`
- **Theme transitions:** entrance, content, reposition, etc.
- **Passthrough:** composition animations, storyboards, spring via `.Set()`

### Critiques

**1. No declarative animation API.** React has Framer Motion and React Spring.
SwiftUI has `withAnimation { }` and `.animation(.spring, value: x)`. Compose has
`animateXAsState`, `AnimatedVisibility`, `AnimatedContent`, `Transition`. Duct has
implicit transitions (property changes animate smoothly) and that's it.

There is no way in Duct to:
- Animate a value from A to B with a custom curve
- Coordinate multiple property animations
- Run a sequence of animations
- Drive an animation from a gesture
- Animate the appearance/disappearance of an element (enter/exit transitions)

All of these require dropping down to WinUI's composition layer via `.Set()`.

**2. No enter/exit transitions.** When an element conditionally appears or
disappears (e.g., `isVisible ? Text("Hello") : null`), it just pops in and out.
SwiftUI's `.transition(.slide)` and Compose's `AnimatedVisibility` solve this
elegantly. Duct has no mechanism for animating element insertion and removal.

**3. No connected animations.** Moving an element from one position to another
(e.g., a shared element transition between "pages") is a common pattern. WinUI
supports this via `ConnectedAnimationService`. Duct doesn't wrap it and there's
no cross-component coordination mechanism to support it.

**4. Implicit transitions are limited to 5 properties.** Opacity, rotation,
scale, translation, and background. That's it. You can't implicitly animate
width, height, corner radius, margin, or any other property. SwiftUI animates
*any* state change that produces a different value. Duct's 5-property limit is a
severe restriction.

**5. The VSM replacement is expensive.** Duct replaces WinUI's Visual State
Manager with state + conditional rendering. The gap analysis acknowledges the
trade-off: "VSM transitions are declarative and run on the composition thread;
Duct's approach requires a full re-render cycle for state changes." A hover
effect that changes background color triggers a full reconciliation cycle in
Duct. In WinUI XAML, it's handled entirely on the composition thread with no
managed code involved.

---

## 10. Accessibility

### Current state

**1 out of 12+ accessibility properties have first-class modifiers.**

| Property | Duct Status |
|---|---|
| AutomationProperties.Name | `.AutomationName()` — **Exposed** |
| AutomationProperties.AutomationId | `.AutomationId()` — **Exposed** |
| AutomationProperties.HelpText | Missing — `.Set()` only |
| AutomationProperties.HeadingLevel | Missing |
| AutomationProperties.LandmarkType | Missing |
| AutomationProperties.LiveSetting | Missing |
| AutomationProperties.AccessibilityView | Missing |
| AutomationProperties.IsRequiredForForm | Missing |
| AutomationProperties.LabeledBy | Missing |
| AutomationProperties.FullDescription | Missing |
| AutomationProperties.PositionInSet/SizeOfSet | Missing |
| Custom AutomationPeer | **Blocked** — components aren't Controls |
| IsTabStop / TabIndex | Missing |
| Access keys | Missing |

### Critiques

**1. Accessibility is an afterthought, not a design principle.** SwiftUI makes
every standard control accessible by default and provides `.accessibilityLabel()`,
`.accessibilityHint()`, `.accessibilityValue()`, `.accessibilityAddTraits()` as
first-class modifiers. Compose has `Modifier.semantics { }` as a core concept.
Duct exposes 2 out of 12+ properties and requires `.Set()` for everything else.

**2. Custom AutomationPeer is architecturally blocked.** WinUI controls provide
accessibility by overriding `OnCreateAutomationPeer()` on their `Control` base
class. Duct components don't subclass `Control` — they're pure C# classes that
output element trees. There's no way to create a custom automation peer for a
Duct component. This means:
- Custom composite controls (e.g., a color picker built from primitives) can't
  describe their accessible role
- Screen readers see the individual primitives, not the semantic component
- There's no way to create accessible custom controls

**3. Focus management is completely missing.** No `IsTabStop` modifier, no
`TabIndex`, no `TabFocusNavigation`, no `XYFocusUp/Down/Left/Right`. Keyboard
navigation in a Duct app depends entirely on WinUI's default behavior, with no
way to customize it through the framework's API. In SwiftUI, `@FocusState` and
`.focused()` provide programmatic focus control. In Compose, `FocusRequester`
and `Modifier.focusable()` do the same.

**4. No accessibility diagnostics or linting.** The accessibility design spec
proposes a diagnostic system, but it's not implemented. There's no way to detect
at build time or runtime that a control is missing an accessible name. React has
`eslint-plugin-jsx-a11y`. SwiftUI has Xcode's accessibility inspector. Duct has
nothing.

**5. Zero accessibility tests in a 2,400+ test suite.** The test suite has
extensive coverage of the reconciler, layout, hooks, and even localization. But
there are exactly zero tests for accessibility — no tests for AutomationId
coverage, screen reader announcements, keyboard navigation, high contrast mode,
or focus management. When a framework has 2,400+ tests and none of them touch
accessibility, that tells you where accessibility sits in the priority stack.

---

## 11. Input Handling and Events

### What Duct has

- **Semantic events on controls:** `OnClick`, `OnChanged`, `OnSelectionChanged`
  — well-covered for all wrapped controls
- **Declarative event modifiers:** `.OnPointerPressed()`, `.OnPointerMoved()`,
  `.OnPointerReleased()`, `.OnTapped()`, `.OnKeyDown()`, `.OnSizeChanged()`
- **Keyboard accelerators:** `Accelerator(key, modifiers)` data records
- **Everything else:** `.Set()` passthrough

### Critiques

**1. No gesture system.** SwiftUI has `.gesture()` with `DragGesture`,
`TapGesture`, `LongPressGesture`, and gesture composition (`.simultaneously`,
`.sequenced`). Compose has `Modifier.pointerInput { detectDragGestures {} }`.
Duct has individual pointer events with no abstraction — you're back to manual
hit testing and state tracking for any gesture more complex than a tap.

**2. Event handler re-attachment is wasteful.** Declarative event handlers
(`.OnPointerPressed()` etc.) "re-attach on every update" per the documentation.
The reconciler detaches the previous handler and attaches the new one on every
render cycle. This is O(n) COM interop calls per render per element with event
handlers. React avoids this with event delegation (one handler on the document).
SwiftUI and Compose handle it at the framework level.

**3. No command abstraction.** The gap analysis identifies this as P0. WinUI's
`ICommand` bundles execute + canExecute + change notification. Duct has bare
`Action` callbacks. This means:
- No automatic button disabling when a command can't execute
- No reusable command objects (label + icon + accelerator + action)
- No `StandardUICommand` equivalents (Cut/Copy/Paste)
- Every button's enabled state must be separately managed

**4. Six pointer events but no PointerEntered/Exited modifiers.** The
declarative event handlers include pressed/moved/released but not entered/exited.
Hover effects — one of the most common interaction patterns — require `.Set()`
to wire `PointerEntered`/`PointerExited`. This is an odd omission given that
hover is more common than pointer-move tracking.

**5. No RightTapped, DoubleTapped, or Holding modifiers.** These common
interactions are passthrough only, requiring `.Set()`. Context menus need
right-tap. Double-click is common in desktop apps. Long-press is common in
touch apps.

---

## 12. Developer Experience

### What's good

- **Full IntelliSense and refactoring** — the C# DSL gets IDE support for free
- **Type safety** — mismatched types are caught at compile time
- **No XAML parsing errors** — a common WinUI pain point eliminated
- **No DataContext confusion** — data flows explicitly through props and state

### What's bad

**1. Hot reload exists but with .NET's inherent limitations.** Duct hooks into
.NET's `MetadataUpdateHandler` via `HotReloadService.cs` — when code changes,
`DuctApp.ActiveHost?.RequestRender()` fires and the UI updates while preserving
hook state (UseState values survive because the RenderContext stays in memory).
This works with both Visual Studio's hot reload and `dotnet watch`.

This is genuinely good — and better than "no hot reload." However, .NET hot
reload has well-known limitations: adding new fields, changing type hierarchies,
adding new classes, and lambda changes often require a full restart. These are
exactly the kinds of changes you make during UI development (adding a new
component, restructuring a layout). React's Fast Refresh and Compose's Live Edit
are purpose-built for UI changes and handle a wider range of edits.

**2. Preview system is functional but not a visual designer.** Duct has a
`--preview [ComponentName]` CLI flag that launches a component in isolation, a
`--preview-list` to discover all components, and a `PreviewCaptureServer` that
streams JPEG frames over HTTP. A VS Code extension (`vscode-duct`) consumes this
to show a live preview panel beside the editor, with a component dropdown
selector and automatic switching when the active editor changes.

This is a thoughtful system — the HTTP capture server, frame streaming, and
dynamic component switching without process restart are well-engineered. It's
more than many frameworks provide.

However, it's fundamentally a **screenshot stream** of a real WinUI window, not
an interactive preview. You can't click or interact with the preview in VS Code.
SwiftUI's Xcode Previews are interactive — you can click buttons, type in fields,
and see state changes in the canvas. Compose's Preview renders actual composables
inline in the IDE. Duct's approach is closer to a live screenshot — useful for
visual iteration but not for testing interactions.

The bigger gap is the lack of a **visual designer**. None of the competition
really has this either (SwiftUI's canvas is previews, not a designer), but for
WinUI developers coming from XAML's Visual Studio designer, this is a step
backward. A property inspector, element picker, or layout visualizer would
significantly close this gap.

**3. Error messages from the reconciler are runtime-only.** If you violate hook
rules (calling hooks conditionally), you get a runtime `InvalidOperationException`:
"Hook at index N is X, expected Y. Hooks must be called in the same order every
render." This is a runtime crash, not a compile-time warning. React has the
`eslint-plugin-react-hooks` that catches this at edit time. Duct has no static
analysis for hook rule violations.

**4. Debugging the reconciler requires deep framework knowledge.** When a UI
update doesn't look right, there's no equivalent of React DevTools (component
tree inspector, state viewer, profiler) or Compose's Layout Inspector. You're
left with `System.Diagnostics.Debug.WriteLine` sprinkled through the reconciler
code and the `IDuctLogger` interface.

**5. No performance profiling.** There's no built-in way to measure:
- How long a render takes
- How many elements were diffed
- How many WinUI controls were touched
- Which components are re-rendering unnecessarily
- Memory allocation per render cycle

React has the React Profiler. Compose has recomposition counts in Layout
Inspector. Duct has nothing.

---

## 13. The .Set() Problem

### The escape hatch that carries the framework

`.Set()` is Duct's escape hatch for accessing any WinUI property that doesn't
have a first-class modifier. It takes a lambda that receives the underlying WinUI
control:

```csharp
Button("Click", onClick)
    .Set(b => b.FlowDirection = FlowDirection.RightToLeft)
    .Set(b => {
        b.PointerEntered += (_, _) => setHovered(true);
        b.PointerExited += (_, _) => setHovered(false);
    })
```

### Why this is a problem

**1. It breaks the declarative model.** The entire point of Duct is declarative
UI. `.Set()` is imperative mutation. When you use `.Set()`, you're bypassing the
reconciler — the framework doesn't know what you changed and can't diff it. If
a control is recycled from the pool, your `.Set()` mutations from the previous
owner are still there (the pool only resets properties the reconciler knows
about).

**2. Event handlers wired in .Set() leak.** If you do
`.Set(b => b.PointerEntered += handler)`, that handler is wired on every update
(Set runs during mount AND update). You'll accumulate duplicate event handlers
unless you manually track and remove them. The framework provides no lifecycle
hook for this — no "cleanup on unmount" for Set-based side effects.

**3. A huge fraction of WinUI is .Set()-only.** From the gap analysis:
- All pointer events (entered, exited, pressed, released, moved)
- All gesture events (tapped, double-tapped, right-tapped, holding)
- All manipulation events
- All keyboard events (except OnKeyDown modifier)
- Focus management (IsTabStop, TabIndex, TabFocusNavigation)
- Access keys
- Drag and drop
- 9 out of 12 accessibility properties
- Custom storyboard animations
- Composition layer access
- Materials and effects
- Most windowing APIs

When this much of the platform requires the escape hatch, the abstraction is
too thin.

**4. .Set() runs on every render.** The documentation for `OnMount` says it runs
once at mount time, but `.Set()` Setters are `Action<TControl>[]` arrays stored
on the element. These arrays are compared by length in `ShallowEquals`, not by
content — meaning any element with Setters always updates, and the Set callbacks
run on every render. This is both wasteful and surprising.

**5. There's no .Get() — no way to read control state.** `.Set()` lets you
mutate the control, but there's no way to read from it back into the component's
render logic. `.OnMount(control => ...)` captures a reference, but by the time
you use it in a render, you might get stale data. There's no reactive bridge
from control properties back to component state.

---

## 14. Component-by-Component Scorecard

How Duct's individual feature areas compare to the mature frameworks (React,
SwiftUI, Compose).

| Feature Area | Duct | React | SwiftUI | Compose | Notes |
|---|---|---|---|---|---|
| **Component Model** | C | A | A | A | No global state, no slots, no memo |
| **Local State** | B+ | A | A | A | Hooks work, boxing overhead |
| **Global State** | F | A | A | A | Nothing — no Context equivalent |
| **Reconciler** | B- | A | A- | A | Works but monolithic, no concurrent mode |
| **Layout** | B+ | B+ | A | A | Flex is good; Grid is stringly-typed |
| **Theming** | F | B+ | A | A | Architecturally broken |
| **Navigation** | F | A | A | A | Blocked; roll your own |
| **Lists/Collections** | B | B+ | A | A | Virtualization exists, no sections |
| **Animation** | D | B | A | A | 5 implicit transitions, nothing else |
| **Accessibility** | D- | B | A | A | 2/12+ properties, custom peers blocked |
| **Input/Events** | C+ | B | A | A | Semantic events good, rest is .Set() |
| **Commands** | F | N/A | N/A | N/A | No ICommand equivalent |
| **Styling** | D | B+ | A | A | Broken theming undermines everything |
| **Developer Experience** | C+ | A | B+ | B+ | Hot reload works; preview is screenshot-only; no devtools |
| **Control Coverage** | A | N/A | A | A | 94% of WinUI wrapped |
| **Error Handling** | B | B+ | D | D | ErrorBoundary exists (rare feature) |
| **Localization** | B+ | B | B | B | ICU-based, full system |
| **Responsive Layout** | B | B+ | A | A | Hooks work but force full re-render |

---

## 15. Conclusion

### The sample apps are telling

Duct has ambitious sample apps: an Outlook clone with email and calendar views,
a file manager with async file watching and lazy TreeView loading, a registry
editor with multiple edit dialogs, and a word puzzle game. These demonstrate that
non-trivial UIs *can* be built with Duct.

But they also reveal the pain:
- **DuctFiles** requires manual `SynchronizationContext` capture for off-thread
  state updates — the framework provides no async state management
- **Outlook clone** uses string-based view switching (`currentPage switch { ... }`)
  because there's no navigation system
- **Every sample** that uses custom colors is silently broken in dark mode
- **None of the samples** demonstrate accessibility, theming, or animation
  beyond basic implicit transitions

These are showcase apps for the framework, and they can't demonstrate theming,
navigation, accessibility, or animation because the framework doesn't support
them.

### What Duct gets right

1. **Control coverage is impressive.** 94% of WinUI controls wrapped with
   clean factory APIs. This is a huge amount of tedious work done well.

2. **The hooks system is faithful to React.** UseState, UseReducer, UseEffect,
   UseMemo, UseRef — all work as expected. The React mental model transfers
   cleanly.

3. **ErrorBoundary exists.** Neither SwiftUI nor Compose has this. Duct's error
   boundary is a genuine differentiator for resilient UIs.

4. **FlexPanel is ambitious and useful.** A full Flexbox implementation on WinUI
   provides layout capabilities that WinUI itself doesn't have.

5. **Type safety over XAML.** No more binding errors, DataContext confusion, or
   resource-not-found runtime failures. The C# compiler catches real mistakes.

6. **Observable interop.** UseObservable and UseCollection bridge cleanly to
   MVVM, which is essential for incremental adoption.

7. **Hot reload and preview tooling.** The `MetadataUpdateHandler` integration
   preserves hook state across code edits, `--preview` isolates components, and
   the VS Code extension with HTTP frame streaming is a thoughtful developer
   experience investment. This is more than most new frameworks ship with.

### What prevents Duct from being production-ready

1. **Theming is broken.** Any styled control silently breaks in dark mode and
   high contrast. This alone disqualifies Duct for any app that ships to users.

2. **No navigation system.** The core scenario of multi-page apps with back
   stack is blocked. Building your own router is not acceptable for a framework.

3. **No global state.** Without Context/EnvironmentObject/CompositionLocal, Duct
   can't handle cross-cutting concerns that every real app has.

4. **Accessibility is almost entirely missing.** 2 out of 12+ properties
   exposed, custom automation peers blocked. This fails WCAG compliance.

5. **No animation system.** Five implicit transitions don't constitute an
   animation framework. Everything else requires `.Set()` escape to WinUI's
   imperative animation API.

6. **.Set() carries too much weight.** When the escape hatch is required for the
   majority of platform features, the abstraction isn't thick enough.

### The fundamental question

Is Duct a *framework* or a *wrapper*?

A framework provides opinions, abstractions, and capabilities that make you more
productive than using the underlying platform directly. React, SwiftUI, and
Compose all provide this: you think in the framework's model, not the platform's.

A wrapper provides a different syntax for the same platform with a thinner API.
You still need to understand the underlying platform, and you reach through the
wrapper constantly via escape hatches.

Duct is currently closer to a wrapper. The reconciler and hooks system are
genuine framework-level abstractions. But theming, navigation, accessibility,
animation, global state, and a large fraction of input handling are all
"just use WinUI through .Set()." That's not a framework — it's a different way
to call the same APIs, with fewer capabilities.

To become a production framework, Duct needs to either:
1. Build real abstractions for theming, navigation, accessibility, animation,
   and global state (massive effort), or
2. Accept that it's a thin wrapper and optimize for that (embrace .Set(), provide
   better escape hatches, focus on the reconciler as the value-add)

The current position — claiming to be a declarative framework while requiring
imperative escape hatches for most real-world scenarios — is the worst of both
worlds.
