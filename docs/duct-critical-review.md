# Duct Framework — Critical Review

A skeptic's component-by-component analysis of Duct against React, SwiftUI, and
Jetpack Compose. This document catalogs architectural weaknesses, API design
problems, missing capabilities, and places where Duct is a hack on top of WinUI
rather than a principled declarative UI framework.

---

## Executive Summary

Duct is an ambitious attempt to bring React-style declarative UI to WinUI 3.
It has impressive breadth of control coverage (94% of WinUI controls wrapped),
a now-solid component model foundation, and a faithful hooks system. The two
largest gaps from the previous review — navigation and commanding — have both
shipped as comprehensive systems. But significant problems remain, and some of
the new features have implementation concerns that temper the enthusiasm:

1. **The component model is now a real framework foundation** — context system,
   memoization, generic hook state, persisted state, and post-render effect
   cleanup have all landed, closing the most fundamental gaps
2. **Navigation has matured beyond initial implementation** — type-safe routes,
   developer-owned back stack, composition-layer transitions, lifecycle guards
   (now including destination-side guards), LRU caching, serialization, and
   enhanced deep linking (query strings, optional params, wildcards). Navigation
   diagnostics provide observability. E2E test discovery was broken (44/46 tests
   invisible behind a filter bug) and is now fixed — 43 Appium tests pass across
   6 classes. But connected transitions are still a stub, and there's no adaptive
   multi-pane layout story
3. **Commanding is a genuine differentiator** — define-once commands with
   metadata bundling, 16 standard commands, async lifecycle, focus-scoped
   keyboard accelerators, and ICommand interop. No competing framework (React,
   SwiftUI, Compose) provides this out of the box. But accelerators rebuild on
   every render, standard command labels aren't localized, and there's no command
   routing to the focused view
4. **The DSL is constrained by C# language limitations** — verbose, repetitive,
   and leaky compared to JSX, SwiftUI's result builders, or Compose's Kotlin DSL
5. **Theming and styling have made a major leap** — style caching eliminates the
   XamlReader.Load-per-element perf concern, `.RequestedTheme()` modifier closes
   a gap, `UseColorScheme` hook exists (but reads app-level theme, not per-element
   effective theme), lightweight styling is Duct's first genuinely unique styling
   feature, and three Roslyn analyzers provide static guidance. But custom branded
   theme resources still don't exist, and the `UseColorScheme` bug undermines
   the RequestedTheme story
6. **Accessibility has crossed a meaningful threshold** — SemanticPanel +
   `.Semantics()` modifier solves the custom automation peer problem for
   composite components (the single hardest architectural limitation), a runtime
   WCAG scanner (AccessibilityScanner) and 3 Roslyn analyzers (DUCT_A11Y_001/
   002/003) provide both runtime and compile-time diagnostics, UseFocusTrap adds
   the first imperative accessibility hook, LabeledBy is now wired with deferred
   resolution, ElementPool clears 14 UIA properties on return, and a dedicated
   a11y-showcase sample demonstrates the full surface. But SemanticPanel adds
   another invisible wrapper, the scanner is DEBUG-only and runtime-only,
   UseFocusTrap doesn't cycle focus (it traps but doesn't wrap), and most
   imperative hooks (UseAnnounce, UseReducedMotion, UseScreenReaderActive)
   remain unbuilt
7. **Animation is now a fully operational compositor animation system** — 8
   features (curves, transitions, interaction states, keyframes, stagger,
   scroll-linked, WithAnimation scope) with solid abstractions; 4 prior
   integration bugs now fixed (exit transitions, Focused state, WithAnimation
   routing, stagger integration), plus 3 runtime fixes (async scope
   persistence, Opacity routing for .Animate(), pool crash prevention).
   Still compositor-property-bound, no arbitrary DP animation
8. **WinForms interop has landed as a new capability** — bidirectional hosting
   via `XamlIslandControl` (WinForms hosts Duct/WinUI) and `WinFormsHostElement`
   (Duct hosts WinForms), with E2E tests for Tab navigation, rendering, and
   accessibility across boundaries. One direction is blocked (Duct-primary
   hosting WinForms) due to WinUI's compositor-only rendering
9. **The `.Set()` escape hatch is still load-bearing** — navigation and commanding
   reduce its surface area, but the majority of input handling, gestures, advanced
   styling, and composition-layer access still require it

**Verdict:** Duct has crossed a third important threshold. The first was the
component model foundation (context, memoization, hooks). The second was the
application architecture layer (navigation, commanding). The third is the
accessibility semantic layer: SemanticPanel solves the hardest architectural
problem — composite components describing their own semantics to screen readers
— and the diagnostic tooling (runtime scanner, compile-time analyzers) moves
accessibility from "annotations on primitives" toward "a system with
guardrails." Navigation and commanding continue to mature with incremental
improvements (destination guards, deep link enhancements, diagnostics).

The distance from "impressive demo" to "ships to real users" has narrowed
further. Styling was the weakest mid-tier area and has received a focused
investment: style caching, per-element theme control, a color scheme hook,
lightweight styling (WinUI resource overrides through a fluent API), and Roslyn
analyzers. Accessibility was the weakest foundation-level area and has now
received the investment it needed: SemanticPanel, AccessibilityScanner,
UseFocusTrap, a11y Roslyn analyzers, and a dedicated showcase sample. WinForms
interop expands the adoption story for brownfield applications. The critical
path now runs through: custom branded theme definitions (still missing),
fixing the UseColorScheme/RequestedTheme composition bug, remaining imperative
accessibility hooks (UseAnnounce, UseReducedMotion), animation general-purpose
value support, and reducing the `.Set()` surface area further. The framework
is no longer blocked on any single P0 gap, and the sum of its P1/P2 gaps has
shrunk meaningfully — but it still adds up to a real distance from
production-readiness for apps that need polish.

---

## Table of Contents

1. [The DSL: C# as a UI Language](#1-the-dsl-c-as-a-ui-language)
2. [Component Model](#2-component-model)
3. [State Management](#3-state-management)
4. [The Reconciler](#4-the-reconciler)
5. [Layout System](#5-layout-system)
6. [Styling and Theming](#6-styling-and-theming)
7. [Navigation](#7-navigation)
8. [Commanding](#8-commanding)
9. [Lists and Collections](#9-lists-and-collections)
10. [Animation](#10-animation)
11. [Accessibility](#11-accessibility)
12. [Input Handling and Events](#12-input-handling-and-events)
13. [Developer Experience](#13-developer-experience)
14. [The .Set() Problem](#14-the-set-problem)
15. [Component-by-Component Scorecard](#15-component-by-component-scorecard)
16. [Conclusion](#16-conclusion)

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

### The component model story: from gaps to genuine framework foundation

The previous version of this review scored Component Model at C and Global State
at F — "no Context equivalent," "no memoization," "hooks box value types." A
significant component model diff has landed that addresses these systematically:
context system, default-on memoization, generic hook state, persisted state, and
post-render effect cleanup. The honest assessment now: **the component model
foundation is solid and competitive with the established frameworks on core
mechanics, but some secondary gaps remain.**

### What Duct has now

- **Class components** extending `Component` with `Render()` method
- **Props via generics**: `Component<TProps>` for typed props
- **Function components**: `Func(ctx => { ... })` inline lambdas
- **Memo function components**: `Memo(ctx => { ... }, deps)` with dependency tracking
- **Error boundaries**: `ErrorBoundary(child, fallback)`
- **DuctContext\<T\>** — tree-scoped ambient state (React Context equivalent)
- **Default-on memoization** — `ShouldUpdate()` on class components, dependency
  tracking on Memo elements
- **Generic hook state** — `ValueHookState<T>` eliminates boxing for value types
- **Persisted state** — `UsePersisted<T>(key, initial)` survives unmount/remount
- **Post-render effect cleanup** — cleanup runs in `FlushEffects`, not during render
- **No slots/named children** — no mechanism for multi-slot composition

### What shipped: Context system (DuctContext\<T\>)

```csharp
// Define: static, typed, named, with default
public static readonly DuctContext<ThemeConfig> ThemeContext =
    new(new ThemeConfig("light"));

// Provide: modifier on any element, scoped to subtree
VStack(children).Provide(ThemeContext, darkTheme)

// Consume: hook in any descendant component
var theme = UseContext(ThemeContext);
```

The design follows React's mental model with SwiftUI's fluent syntax. Contexts
are tree-scoped: inner providers shadow outer ones. The `ContextScope` class
maintains a stack during reconciler traversal — pushed on entering an element
with `ContextValues`, popped on leaving. `UseContext<T>` reads the nearest
ancestor's value by walking the stack backward.

The reconciler integrates context into the memo check: `HasConsumedContextChanged`
compares each `ContextHookState.LastValue` against the current scope value. A
component that consumes `ThemeContext` re-renders when the theme changes, even
if its props haven't changed. A component that doesn't consume any context skips
the check entirely — zero cost for the common case.

**LocaleProvider now uses DuctContext.** The localization system has been migrated
from the hand-rolled `LocaleContext.Current` thread-static hack to a proper
`DuctContext<IntlAccessor?>`. `UseIntl()` is now `UseContext(IntlContexts.Locale)`
under the hood. The legacy `LocaleContext.Current` is maintained for backward
compatibility but marked internal. This validates the context system with a
real use case — the framework's own localization depends on it.

### What shipped: Default-on memoization

```csharp
// Propless component: ShouldUpdate() defaults to false
// → only re-renders from own state changes or context changes
public class StatusBar : Component { ... }

// Props component: ShouldUpdate(old, new) defaults to Equals(old, new)
// → record props get structural comparison for free
public record DashboardProps(string Title, int Count);
public class Dashboard : Component<DashboardProps> { ... }

// Function component with memo: explicit dependency array
Memo(ctx => { ... }, count, theme)
```

Class components have `ShouldUpdate()` which defaults to `false` for propless
components (never re-render from parent) and `!Equals(oldProps, newProps)` for
`Component<TProps>` (re-render when props change structurally). This leverages
C# records — a `record` props type gets value equality for free, so the common
case requires zero developer effort. The reconciler also checks context changes
via `HasConsumedContextChanged`, so components re-render when consumed contexts
change regardless of the `ShouldUpdate` result.

The `ShouldUpdateWithProps` method in the reconciler uses reflection to call the
typed `ShouldUpdate(TProps?, TProps?)` through the untyped `Component` reference.
This is a one-time cost per component type (reflection calls are not cached), but
it runs on every parent re-render for every child component. For deep trees with
many components, this reflection overhead could add up.

### What shipped: Generic hook state (no boxing)

```csharp
// Before: HookState.Value was object — boxing for every int, bool, double
private class HookState { public object Value = default!; }

// After: ValueHookState<T> — no boxing, no allocation for value types
private class ValueHookState<T> : HookState { public T Value; }
```

`UseState<int>`, `UseReducer<bool>`, `UseMemo<double>` — none of these box
anymore. The generic `ValueHookState<T>` stores the value directly. The equality
check uses `EqualityComparer<T>.Default` instead of `object.Equals`, avoiding
both boxing for comparison and the allocation of intermediate `object` references.

This also means hook type mismatches (calling `UseState<int>` on one render and
`UseState<string>` on the next) produce clearer error messages — the exception
says exactly which generic types conflicted.

### What shipped: Persisted state

```csharp
var (scrollPos, setScrollPos) = UsePersisted("inbox-scroll", 0.0);
```

`UsePersisted<T>(key, initialValue)` works like `UseState<T>` but survives
unmount/remount. Values are stored in `PersistedStateCache` — a static
`Dictionary<string, object?>` — and saved to cache in `RunCleanups()` (on
unmount). On next mount, the cached value is used instead of `initialValue`.

### What shipped: Post-render effect cleanup

Previously, effect cleanup ran synchronously during `UseEffect()` — inside the
render phase. Now, cleanup is deferred: `UseEffect` sets `PendingCleanup` (the
old cleanup function) and `FlushEffects()` runs all pending cleanups *before*
running new effects, in a two-phase approach:

```
Phase 1: Run all PendingCleanup actions (from previous render)
Phase 2: Run all new pending effects
```

This matches React's behavior: cleanup from the previous render runs after the
new render completes, not during it. Expensive cleanup (network cancellation,
timer disposal) no longer blocks the render.

### What's actually good about this (credit where due)

The context system is well-designed. The `DuctContext<T>` type is simple — a
static field with a default value and a debug name (via `[CallerMemberName]`).
The provide mechanism uses the existing element `with` pattern via
`ContextExtensions.Provide()`, so it composes naturally with other modifiers.
The `ContextScope` stack is lightweight — a `List<(DuctContextBase, object?)>`
with version tracking. The consumer hook follows the existing hook pattern
exactly. This is the right API shape — it mirrors React Context closely enough
that the mental model transfers, while using Duct's fluent modifier convention
for providing.

The memoization default is the right call. Making propless components skip
re-renders by default (unless self-triggered or context-changed) is aggressive
but correct — it matches Compose's behavior where stable parameters cause
automatic skipping. Record-based props getting structural equality for free via
`Equals` is a clever use of C# language features that React and Compose can't
match (they need explicit `React.memo()` comparators or `@Stable` annotations).

The test coverage is thorough: 24 context unit tests, 12 context self-host tests,
19 memoization tests, 18 hook refactor tests, 17 persisted state tests, 15
integration tests exercising realistic multi-hook component patterns. The self-
host test pattern (manually driving BeginRender → Render → FlushEffects through
a ContextScope without WinUI controls) is a good strategy for testing framework
internals without platform dependencies.

### What's still concerning (skeptic's view)

**1. Context values are boxed in the scope stack.** `ContextScope` stores
`List<(DuctContextBase, object?)>` — the value is `object?`. A
`DuctContext<int>` provided with value `42` boxes that int. Every `UseContext<T>`
read casts back from `object?`. The hook state itself is now unboxed
(`ValueHookState<T>`), but the context delivery mechanism reintroduces boxing
for value types. For `DuctContext<string>` or `DuctContext<ThemeConfig>` (the
common cases), this doesn't matter. But the inconsistency is notable — hooks
went to the effort of eliminating boxing while the context system didn't.

**2. ShouldUpdateWithProps uses reflection on every memo check.** The reconciler
calls `ShouldUpdateWithProps` which does `compType.GetMethod("ShouldUpdate", ...)`
via reflection to find the typed `ShouldUpdate(TProps?, TProps?)` method. This
runs every time a parent re-renders and the child is a `Component<TProps>`. There's
no caching of the `MethodInfo` — each check does a fresh reflection lookup. For
a tree with 50 components and a root state change, that's 50 reflection calls per
render cycle. React's `React.memo()` stores the comparator as a direct function
reference. Compose's stability check is compile-time. Duct's is a runtime
reflection walk.

**3. PersistedStateCache is a static Dictionary with no eviction.** The cache
grows unboundedly — every `UsePersisted` key stays in memory for the process
lifetime. There's no LRU eviction, no size limit, no TTL. For a long-running
desktop app where users navigate through many views, the cache accumulates stale
state from components that will never remount. SwiftUI's `@SceneStorage`
serializes to disk with OS-managed lifecycle. Compose's `rememberSaveable` ties
to the `SaveableStateRegistry` which is scoped to the composition. Duct's cache
is a global singleton that only clears on process exit.

**4. PersistedStateCache keys are stringly-typed with no collision protection.**
`UsePersisted("scroll-pos", 0.0)` uses a bare string key. Two unrelated
components that happen to use the same key silently share state — a bug with no
diagnostic. The cache stores `object?`, so a type mismatch (one component persists
`int`, another reads `string` with the same key) produces a runtime
`InvalidCastException` on the next mount. There's no namespacing, no type
validation, no collision warning.

**5. No Suspense or lazy loading.** Still missing. React's `Suspense` +
`React.lazy()` for code-split components has no equivalent. SwiftUI and Compose
also lack this, so Duct is not uniquely deficient — but React has had it since
2018.

**6. Component props are still untyped at the element level.** `ComponentElement`
stores props as `object?`. Props are set via `IPropsReceiver.SetProps(object props)`
with a cast. The generic `Component<T, TProps>(props)` factory hides this, but the
underlying infrastructure is type-erased. A wrong props type produces a runtime
`InvalidCastException`, not a compile error.

**7. No slots/named children convention.** The design spec mentions documenting
a convention for named children using Element-typed props, with `Lazy<Element>`
as future work. This hasn't materialized. Multi-slot composition (header + body +
footer) requires ad-hoc props types. SwiftUI has `@ViewBuilder` parameters for
multiple content slots. Compose has multiple `@Composable` lambda parameters.
Duct has no convention, pattern, or framework support.

**8. FuncElement identity is still problematic.** `Func(ctx => ...)` creates a
`FuncElement` with a lambda. The lambda is recreated every render (it captures
outer state), so the function reference changes every time. `ShallowEquals` still
returns false for FuncElements, forcing an update. The new `Memo(ctx => ..., deps)`
provides a workaround — but `Func` remains the first thing developers reach for,
and it can't memo-skip.

**9. Context change detection compares boxed values with Equals.** The
`HasConsumedContextChanged` method compares `Equals(currentValue, ctxHook.LastValue)`
where both values are `object?`. For reference types that don't override `Equals`,
this is reference equality — meaning a new `ThemeConfig` record with the same
values still triggers re-renders unless the record type properly implements
`Equals`. C# records do implement value equality by default, so this works for
the common case. But a class-based context value type silently breaks the memo
optimization — every provide creates a new object, which is always "different"
by reference, defeating the point of memoization.

### Revised component model verdict

**Previously: Component Model C, Global State F, Local State B+.**
**Now: Component Model B+, Global State B+, Local State A-.**

The improvement is substantial and addresses the most critical gaps
identified in the previous review. The context system provides a real answer
to "how do I share state across the tree" — theming, auth, feature flags, and
localization all have a clean mechanism now. Default-on memoization means
components skip unnecessary re-renders without developer opt-in. Generic hook
state eliminates boxing. Post-render effect cleanup matches React's behavior.
Persisted state handles the unmount/remount scenario.

The grade is B+/B+ rather than A because of implementation concerns: reflection
for ShouldUpdate dispatch, boxing in the context scope stack, unbounded persisted
state cache, string-keyed persistence with no collision protection. These are
solvable problems — caching MethodInfo, using a generic scope entry, adding LRU
eviction — but they're real costs in the current implementation. The competition
doesn't have these particular issues: React's memo comparator is a direct
function reference, Compose's stability is compile-time, SwiftUI's environment
is type-safe throughout.

Local State moves to A- because the boxing is gone, effect cleanup timing is
correct, and persisted state exists. The remaining gap is the `object[]`
dependency array for `UseEffect`/`UseMemo` — dependencies are still boxed into
`object[]` and compared with `Equals`, which is the same issue React has (but
React has ESLint rules to catch common mistakes, and Duct has no tooling).

---

## 3. State Management

### What Duct has

- `UseState<T>` — React's useState equivalent, **now generic (no boxing)**
- `UseReducer<T>` — functional updater variant
- `UseReducer<TState, TAction>` — Redux-style reducer
- `UseEffect` — side effects with dependency tracking, **cleanup now post-render**
- `UseMemo<T>` — memoized computation
- `UseCallback` — stable callback reference
- `UseRef<T>` — mutable reference
- `UseObservable<T>` — INotifyPropertyChanged bridge
- `UseObservableTree<T>` — deep INotifyPropertyChanged bridge (recursive)
- `UseObservableProperty<T>` — single-property INotifyPropertyChanged bridge
- `UseCollection<T>` — ObservableCollection bridge
- `UseContext<T>` — **NEW** — reads nearest ancestor's context value
- `UsePersisted<T>` — **NEW** — state that survives unmount/remount
- `UseWindowSize` / `UseBreakpoint` — responsive hooks

### What's improved

**1. Hook state is now generic — no boxing.** Previously `HookState.Value` was
`object`, boxing every int/bool/double. Now `ValueHookState<T>` stores the value
directly. `EqualityComparer<T>.Default` handles comparison without boxing. This
is a clean fix for a real performance problem.

**2. Effect cleanup timing is now correct.** Previously, cleanup ran synchronously
during `UseEffect()` — inside the render phase. Now, `UseEffect` stores the old
cleanup as `PendingCleanup`, and `FlushEffects()` runs all pending cleanups in
Phase 1 before running new effects in Phase 2. This matches React's behavior:
cleanup from the previous render runs after the new render completes, not during
it. The fix is clean and the two-phase approach in FlushEffects is easy to reason
about.

**3. State persistence exists.** `UsePersisted<T>(key, initialValue)` stores
values in a static `PersistedStateCache` that survives unmount/remount. Values
are saved to cache during `RunCleanups()` (on unmount) and restored on next
mount. This closes the gap where navigating away and back lost all state.

**4. Global state exists via DuctContext.** Covered in detail in Section 2. The
`UseContext<T>` hook reads tree-scoped ambient state provided by any ancestor.
The old "no global state at all" critique is resolved.

### Remaining critiques

**1. Dependency comparison still uses `object.Equals` on `object[]`.** `UseEffect`,
`UseMemo`, and other dependency-tracking hooks compare deps with
`Equals(prev[i], next[i])` where deps are `params object[]`. This means:
- Value types are boxed into the `object[]` dependency array (allocation)
- Reference types use reference equality by default unless they override Equals
- Collections as dependencies compare by reference, not by content
- No warning or guidance about what makes a good dependency

The hook state itself is now unboxed, but the dependency arrays still box. React
has the same issue (shallow comparison) but has ESLint rules that catch common
mistakes. Duct has no tooling support.

**2. UseCallback is just UseMemo returning the same Action.** The implementation
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
explain this nuance.

**3. No batching control or transition API.** React 18 has `startTransition`
for marking non-urgent updates. Duct batches via DispatcherQueue, which is
all-or-nothing. There's no way to say "this state update is low priority" or
"this update should not show a loading state."

**4. PersistedStateCache limitations.** Covered in Section 2 — unbounded growth,
string keys with no collision protection, no disk persistence. For a desktop
framework where window/session restoration is expected, in-memory-only persistence
is a partial answer. SwiftUI's `@SceneStorage` and Compose's `rememberSaveable`
both tie into the platform's state restoration lifecycle. Duct's cache is process-
scoped only.

---

## 4. The Reconciler

### Architecture

The reconciler follows React's model: diff old and new element trees, apply
minimal patches. It's split across `Reconciler.cs` (orchestration + component
lifecycle + memo checks + context scope), `Reconciler.Mount.cs` (~40 mount
handlers), `Reconciler.Update.cs` (~30 update handlers), and `ChildReconciler.cs`
(keyed/unkeyed lists).

The reconciler now owns the `ContextScope` and drives context push/pop during
tree traversal — elements with `ContextValues` push on mount/update entry and
pop on exit. Components receive the scope via `BeginRender(requestRerender,
contextScope)` so their `UseContext` hooks read from the correct scope position.
The component update path includes a memo check that combines props comparison
(`ShouldUpdate`) with context change detection (`HasConsumedContextChanged`).

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

### The theming story: from functional-with-caveats to a real styling system

The previous version of this review graded theming at C+ — the ThemeRef system
closed the P0 blocker but had performance concerns (XamlReader.Load per element
per render), a narrow API surface (3 properties, no custom resources, no hooks),
no guardrails against hard-coded colors, and no lightweight styling. A focused
styling diff has landed that addresses several of these criticisms directly:
style caching, `.RequestedTheme()` modifier, `UseColorScheme` hook, lightweight
styling via `ResourceBuilder`, and three Roslyn analyzers. The honest assessment
now: **theming and styling have crossed from "functional for the common case"
to a genuinely capable system with one unique differentiator (lightweight
styling), but with a UseColorScheme implementation bug, still no custom theme
resources, and the ThemeRef/lightweight styling interaction story unresolved.**

### What shipped previously (ThemeRef foundation)

**ThemeRef tokens and modifier overloads.** ~40 semantic theme tokens
(`Theme.PrimaryText`, `Theme.Accent`, `Theme.CardBackground`, etc.) and
`Theme.Ref("AnyResourceKey")` for custom WinUI resources. Three-tier model:
unstyled elements theme automatically (Tier 3), theme tokens resolve reactively
(Tier 2), local concrete values override (Tier 1).

**Reconciler integration via XAML Style injection.** `ApplyThemeBindings`
constructs `{ThemeResource}` styles and assigns them to elements. WinUI handles
theme resolution natively.

### What shipped in the styling diff

**1. Style caching (Proposal 5 — P0 performance fix).** This directly addresses
the biggest criticism from the previous review: XamlReader.Load per element per
render. The implementation:

```csharp
private static readonly ConcurrentDictionary<string, Style> _styleCache = new();

private static string BuildCacheKey(string targetType, IReadOnlyDictionary<string, ThemeRef> bindings)
{
    var sortedKeys = bindings.Keys.ToArray();
    Array.Sort(sortedKeys, StringComparer.Ordinal);
    var sb = new StringBuilder(targetType);
    foreach (var key in sortedKeys)
        sb.Append('|').Append(key).Append('=').Append(bindings[key].ResourceKey);
    return sb.ToString();
}
```

Cache hit path: immediate Style assignment, zero XAML parsing. Cache miss:
build XAML → parse → store. `ApplyStyleToElement()` does a clever null-then-set
to force WinUI re-evaluation of `{ThemeResource}` setters when the same cached
Style reference is reapplied (without this, assigning the same reference is a
no-op). `ClearStyleCache()` runs on theme change as conservative memory cleanup.

This is well-engineered. The key generation is deterministic (sorted by
`StringComparer.Ordinal`), thread-safe (`ConcurrentDictionary`), and handles
dictionary enumeration order correctly. For a grid of 200 elements all using
`Theme.Accent`, `XamlReader.Load` fires once, not 200 times. The previous
review's primary performance critique is resolved.

**2. `.RequestedTheme()` modifier (Proposal 8A — P0 API gap).** Previously
required `.Set(b => b.RequestedTheme = ElementTheme.Dark)`. Now:

```csharp
VStack(children).RequestedTheme(ElementTheme.Dark)
```

The reconciler applies `RequestedTheme` **before** `ApplyThemeBindings` — the
ordering comment in the code is explicit about why this matters. The modifier
is clean: one property on `ElementModifiers`, one extension method, one line
in `ApplyModifiers` with a change guard. This is a textbook example of turning
a `.Set()` workaround into a first-class modifier — small scope, correct
integration, no surprises.

**3. `UseColorScheme` hook (Proposal 6 — P0 reactive hook).** Previously there
was no way to read the current theme in render logic. Now:

```csharp
var scheme = ctx.UseColorScheme();   // Light, Dark, or HighContrast
var isDark = ctx.UseIsDarkTheme();   // convenience wrapper
```

`ColorScheme` is a clean three-value enum. `ColorSchemeContext` handles the
`ElementTheme` → `ColorScheme` mapping with High Contrast detection via
`AccessibilitySettings.HighContrast`. The hook re-evaluates on every render, so
theme changes (which trigger a full re-render via DuctHost) are picked up
naturally.

**4. Lightweight Styling via ResourceBuilder (Proposal 2 — P0 unique
differentiator).** This is the most significant feature in the diff. WinUI's
"lightweight styling" lets you override specific theme resource keys per-control
(e.g., `ButtonBackground`, `ButtonBackgroundPointerOver`) without replacing the
control template. The VisualStateManager continues to work, so hover/pressed/
disabled states respect the overrides automatically. Duct now surfaces this:

```csharp
Button("Submit").Resources(r => r
    .Set("ButtonBackground", "#0078D4")
    .Set("ButtonBackgroundPointerOver", "#106EBE")
    .Set("ButtonBackgroundPressed", "#005A9E")
    .Set("ButtonForeground", "#FFFFFF"))

// Theme-reactive resource overrides
Button("Go").Resources(r => r
    .Set("ButtonBackground", Theme.Accent)
    .Set("ButtonBackgroundPointerOver", Theme.Ref("AccentButtonBackgroundPointerOver")))
```

`ResourceBuilder` separates literal values from `ThemeRef`-based entries.
`ResourceOverrides` is an immutable snapshot record. The reconciler's
`ApplyResourceOverrides()` tracks which keys Duct has set via a
`ConditionalWeakTable<FrameworkElement, HashSet<string>>` — ensuring cleanup
only removes Duct-managed keys, never interfering with XAML-set resources.
On update, old keys not present in the new overrides are removed. `ThemeRef`-
based resources are re-resolved on theme change via the re-render pipeline.

**No other C# declarative framework surfaces this.** React's CSS custom
properties are conceptually similar but operate at a different abstraction level.
SwiftUI's `.environment(\.font)` and Compose's `LocalContentColor` provide
ambient overrides but not per-control resource key targeting. Duct's
`ResourceBuilder` gives developers access to the exact same resource override
mechanism that WinUI's XAML lightweight styling uses — but through a fluent
builder instead of raw ResourceDictionary manipulation.

**5. Three Roslyn Analyzers.** A separate `Duct.Analyzers` project provides
static analysis:

| Analyzer | Severity | Detects | Suggests |
|---|---|---|---|
| DUCT001 | Warning | `.Background("#FFFFFF")` hard-coded colors | Use `Theme.*` tokens |
| DUCT002 | Info | `.Set(b => b.Background = brush)` on known controls | Use `.Resources()` lightweight styling |
| DUCT003 | Info | `.Set(fe => fe.RequestedTheme = ...)` pattern | Use `.RequestedTheme()` modifier |

Each has matching unit tests via `CSharpAnalyzerVerifier`. DUCT001 includes a
`UseThemeRefCodeFix` that maps known colors to tokens (`#FFFFFF` → `Theme.PrimaryBackground`,
`#0078D4` → `Theme.Accent`). DUCT003 has `RequestedThemeSetCodeFix` for
the `.Set()` → `.RequestedTheme()` transformation.

### What's actually good about this (credit where due)

**Style caching is the right fix for the right problem.** The previous review
called out XamlReader.Load-per-element as the heaviest possible implementation.
The cache eliminates repeated parses for identical binding sets — which covers
the vast majority of real-world usage (lists of identically-themed controls).
The implementation is clean: deterministic keys, thread-safe concurrent
dictionary, correct null-then-set for Style reapplication. The cache is
conservative — cleared on theme change even though `{ThemeResource}` setters
self-resolve. This is defense in depth, not a correctness requirement.

**Lightweight styling is Duct's first genuinely unique styling feature.** Every
other styling feature (ThemeRef, RequestedTheme, UseColorScheme) is Duct
catching up to what the competition provides natively. `ResourceBuilder` is
different — it surfaces a WinUI capability that no other declarative framework
wraps. A developer who writes `.Resources(r => r.Set("ButtonBackgroundPointerOver",
"#106EBE"))` gets hover/pressed/disabled states that respect the override, with
zero template replacement. This is the correct abstraction level: give
developers the resource key names (which are documented in WinUI docs), handle
the dictionary plumbing, track cleanup.

**The ConditionalWeakTable for managed-key tracking is smart.** The alternative
approaches (Tag, attached property, side dictionary keyed by identity) all have
GC or lifecycle problems. `ConditionalWeakTable` ties the tracking data's
lifetime to the FrameworkElement's GC lifetime, avoiding both leaks and
dangling references. When the element is collected, the tracking set goes with
it.

**The analyzers close the "pit of success" gap.** The previous review criticized
hard-coded colors as a trap with no guardrails. DUCT001 now warns at edit time
when a string literal is passed to `.Background()`, `.Foreground()`, or
`.WithBorder()`. It's not perfect (see concerns below), but it's the difference
between "no guidance" and "IDE squiggles on the suspicious line."

**RequestedTheme ordering is explicitly documented.** The comment at
Reconciler.cs:1615 says exactly why `RequestedTheme` must be set before
`ApplyThemeBindings`. This is the kind of ordering invariant that causes subtle
bugs when someone later refactors `ApplyModifiers`. The comment makes the
constraint visible.

### What's still concerning (skeptic's view)

**1. UseColorScheme reads the app-level theme, not the element's effective
theme.** This is a bug, not a design limitation. The implementation:

```csharp
public ColorScheme UseColorScheme()
{
    var theme = Microsoft.UI.Xaml.Application.Current?.RequestedTheme;
    // ...maps to ColorScheme...
}
```

The doc comment says "Automatically reflects [...] per-element RequestedTheme
overrides." The code reads `Application.Current.RequestedTheme` — the global
app theme. A component inside a `.RequestedTheme(ElementTheme.Dark)` subtree
will see `ColorScheme.Light` when the system is in Light mode. The spec
(Proposal 6, Option A) says to read `FrameworkElement.ActualTheme` from the
component's mounted control. The implementation doesn't do this.

This is worse than missing the feature entirely, because the API exists, the
doc comment claims it works, and the `StylingGallery` sample demonstrates it.
A developer who writes:

```csharp
var scheme = ctx.UseColorScheme();
VStack(
    scheme == ColorScheme.Dark ? Text("🌙") : Text("☀")
).RequestedTheme(ElementTheme.Dark)
```

expects the moon icon. They get the sun. The hook re-evaluates on system theme
change (via DuctHost re-render), so it "works" at the app level — but the
per-element awareness that makes it useful alongside `.RequestedTheme()` is
broken. The test suite doesn't catch this because `ColorSchemeTests` test the
enum mapping, not the `RequestedTheme` interaction.

SwiftUI's `@Environment(\.colorScheme)` correctly reads the effective color
scheme at the view's position in the hierarchy, including any
`.preferredColorScheme(.dark)` overrides from ancestors. Compose's
`isSystemInDarkTheme()` also reads from the local composition context. Duct's
hook reads the global.

**2. ThemeRef `{ThemeResource}` in dynamically-loaded Styles doesn't respect
per-element RequestedTheme.** The code itself documents this (Reconciler.cs:1970):
"Note: {ThemeResource} in dynamically-loaded Styles resolves against the app
theme, not per-element RequestedTheme overrides." This is a WinUI platform
behavior — `XamlReader.Load()` parses XAML in the app's default theme context,
not the element's local theme context. The `ApplyStyleToElement` null-then-set
trick forces WinUI to reprocess the Style assignment, which helps with system
theme changes, but doesn't bridge the gap for per-element `RequestedTheme`.

This means: `.RequestedTheme(ElementTheme.Dark)` correctly sets the native
`FrameworkElement.RequestedTheme` property, so WinUI's built-in control theming
works (buttons, text, etc. render in dark theme). But ThemeRef bindings on that
element (`.Background(Theme.CardBackground)`) resolve against the *app* theme,
not the element's requested theme. A dark sidebar with
`.Background(Theme.CardBackground)` in a Light-mode app may get the Light
variant of `CardBackground`, not the Dark one.

The workaround documented in the code ("rely on native WinUI control theming
instead of ThemeRef bindings") is valid — WinUI controls apply their own theme
resources based on `RequestedTheme`. But it means `.RequestedTheme()` +
`Theme.*` tokens don't compose as a developer would expect. The two features
work independently but not together.

**3. Lightweight styling resource keys are stringly-typed with no validation.**
`ResourceBuilder.Set("ButtonBackgrnd", "#0078D4")` — typo in the key —
silently sets a resource that no control reads. There's no compile-time
validation of resource key names, no IntelliSense for known keys, no runtime
warning when a key doesn't match any control template. WinUI's own XAML
suffers the same problem (lightweight styling keys are strings everywhere),
but Duct had the opportunity to provide type-safe constants:

```csharp
// What could exist (from the deferred items list):
Button("Submit").Resources(r => r
    .Set(ButtonResources.Background, "#0078D4")
    .Set(ButtonResources.BackgroundPointerOver, "#106EBE"))
```

The design spec lists "ButtonResources.Background constants" as future work.
Without them, `ResourceBuilder` inherits all the discoverability problems of
WinUI's stringly-typed XAML lightweight styling — you need to look up the
correct resource key names in the WinUI documentation. A framework that wraps
the platform should improve on this.

**4. Lightweight styling ThemeRef resources resolve at application level, not
element level.** `ApplyResourceOverrides` calls `ThemeRef.Resolve(resourceKey,
fe)`, which reads the effective theme for the element via `GetEffectiveThemeName`.
This is better than `UseColorScheme` (it does check the element). But the
resolved brush is a *snapshot* — a concrete `Brush` set into `fe.Resources`.
It's not a live `{ThemeResource}` binding. If the system theme changes, the
re-render pipeline calls `ApplyResourceOverrides` again and re-resolves. But
between renders, the resource is a frozen brush, not a theme-reactive binding.
For ThemeRef entries in `ResourceBuilder`, this means:

- System theme changes: re-resolved on next render (correct, with one-frame
  delay)
- Per-element RequestedTheme: resolved correctly at mount time (good — it uses
  `GetEffectiveThemeName(fe)`)
- VisualStateManager transitions: the resource is a frozen brush, not a
  `{ThemeResource}`. VSM state changes that reference the resource key will
  pick up the Duct-set brush, but it won't adapt to subsequent theme changes
  *between renders*

For the common case (set once, re-resolve on system theme change), this works.
For the edge case (rapid theme toggling or theme-dependent VSM transitions),
the snapshot model has a one-render-cycle delay.

**5. No custom theme resource definitions (unchanged).** `Theme.Ref("key")`
references existing WinUI resources, but there's no way to define new theme
resources from Duct. No branded colors that adapt to light/dark, no app-specific
semantic tokens. This was the most frequently cited remaining gap in the previous
review and it's still unaddressed. React's Material UI `createTheme()`, SwiftUI's
asset catalog named colors, and Compose's `lightColorScheme()`/`darkColorScheme()`
all provide this. Duct still only references the platform's built-in palette.

The lightweight styling `ResourceBuilder` makes this more pressing, not less.
A developer who discovers they can override `ButtonBackground` with a brand
color will immediately want a brand color that adapts to light/dark — which
requires custom theme resources. The feature that should unblock this (custom
theme definitions) is the feature that's still missing.

**6. Only three properties support ThemeRef bindings (unchanged).** Background,
Foreground, and BorderBrush via `GetDependencyPropertyName()`. No Fill/Stroke
on shapes, no PlaceholderForeground, no CaretBrush. The lightweight styling
`ResourceBuilder` partially compensates — you can override resource keys for
any property — but ThemeRef bindings on the element itself remain limited to
three.

**7. Style assignment still clobbers existing styles.** `ApplyThemeBindings`
does `fe.Style = cachedStyle` (via `ApplyStyleToElement`). The caching
eliminates the performance cost of building duplicate styles, but the
architectural concern remains: every theme-bound element gets a dynamically
assigned style that replaces whatever style WinUI would have naturally applied.
This interacts poorly with `.ApplyStyle("AccentButtonStyle")` and implicit
styles. The cache means this is now a *correctness* concern (style precedence)
rather than a *performance* concern.

**8. DUCT002 analyzer coverage is shallow.** The `UseLightweightStylingAnalyzer`
only knows about 3 properties (Background, Foreground, BorderBrush) and 6
control types (Button, ToggleButton, RepeatButton, SplitButton, AppBarButton,
HyperlinkButton). Missing: TextBox, CheckBox, ComboBox, Slider, ToggleSwitch.
Missing properties: PlaceholderForeground, BorderThickness, CornerRadius.
Missing resource keys: anything beyond the Button family. The analyzer detects
a fraction of the cases where lightweight styling would be beneficial. It's
better than nothing — but it won't nudge a developer who's using `.Set()` on a
TextBox to set its placeholder foreground, which is one of the most common
lightweight styling use cases.

**9. DUCT001 color-to-token mapping is incomplete.** The `UseThemeRefAnalyzer`
maps 5 specific colors (#FFFFFF, white, #000000, black, #0078D4) to theme
tokens. Every other hard-coded color gets a generic "use ThemeRef" message with
no specific token suggestion. In practice, developers use dozens of colors —
grays (#E5E5E5, #808080), blues (#005A9E, #106EBE), reds (#D13438), greens
(#107C10). None of these map. The analyzer is useful for the simplest cases but
doesn't provide actionable guidance for the majority of real-world hard-coded
colors.

### Revised theming verdict

**Previously: C+. Now: B-.**

The improvement is meaningful and addresses the right problems. Style caching
eliminates the XamlReader.Load performance concern that was the #1 critique.
Lightweight styling provides a genuinely unique capability. RequestedTheme
modifier removes a `.Set()` workaround. The analyzers provide static guidance
where none existed.

But the grade is B-, not B, because of the `UseColorScheme` bug (reads app
theme, not element effective theme), the `ThemeRef` + `RequestedTheme`
interaction gap (dynamically-loaded styles don't respect per-element theme),
missing custom theme resource definitions, stringly-typed resource keys with
no validation, and shallow analyzer coverage. The two most impactful new
features — RequestedTheme and UseColorScheme — don't compose correctly with
each other, which is exactly the scenario they were designed for (dark sidebar
in a light app with reactive rendering).

The competition has moved too. SwiftUI's styling story (semantic colors +
`@Environment(\.colorScheme)` + asset catalog + `.tint()` + `.preferredColorScheme()`)
is deeply integrated and consistent. Compose's Material 3 (`lightColorScheme` /
`darkColorScheme` / `dynamicColorScheme`) provides full custom theming with
one API surface. Duct's styling is now *functional* across more scenarios,
but the pieces don't always compose cleanly with each other.

### Other styling issues

**No style composition or reuse (partially addressed).** The design spec notes
that `Func<T, T>` extension methods serve as style bundles in practice. This is
valid C# — `static T BrandButton<T>(this T el) where T : Element => el
.Background(Theme.Accent).Foreground(Theme.PrimaryBackground)` — and is
documented in the styling spec. No new framework-level concept was added, which
is the right call: plain C# extension methods compose naturally with modifiers.

**Lightweight styling partially replaces the need for style composition.**
`.Resources()` lets a developer customize hover/pressed/disabled appearance
without needing to compose multiple modifiers. For the "brand button" pattern,
lightweight styling is strictly better than modifier chaining because it
preserves VisualStateManager integration.

**ApplyStyle is still string-based runtime lookup (unchanged).**
`.ApplyStyle("AccentButtonStyle")` does a dictionary lookup in
`Application.Current.Resources` at runtime. A typo silently fails.

---

## 7. Navigation

### The navigation story: from "architecturally blocked" to competitive and maturing

The previous version of this review scored Navigation at F — "Roll your own
string-based switch statement." A comprehensive navigation system shipped
previously, and a follow-up diff has added: destination-side guards
(`NavigatingToContext.Cancel()`), deep link enhancements (query strings, optional
params, wildcards), `NavigationDiagnostics` for observability, thread-safe cache,
configurable transition distance, and exposed `INavigationHandle`. Additionally,
a critical test infrastructure discovery was made: 44 of 46 E2E tests were
invisible behind a broken test filter (`ClassName=InteractiveTests` instead of
`ClassName!=SelfTestBatch`). This has been fixed — 43 Appium tests now pass across
6 test classes. The honest assessment now: **navigation is architecturally strong,
competitively mature, and now has real E2E validation across the full pipeline.**

### What Duct has now

```csharp
// Define routes as C# records — type-safe, serializable, pattern-matchable
record HomeRoute;
record DetailRoute(int Id);
record SettingsRoute;
record ProfileRoute(string UserId, string? Tab = null);

// Root: create navigation stack with initial route
var nav = UseNavigation<AppRoute>(initial: new HomeRoute());

// Child: retrieve ancestor's handle via DuctContext
var nav = UseNavigation<AppRoute>();

// Navigate with full type safety
nav.Navigate(new DetailRoute(42));
nav.GoBack();
nav.GoForward();
nav.Replace(new SettingsRoute());
nav.Reset(new HomeRoute());       // clear stack
nav.PopTo(r => r is HomeRoute);   // pop until match

// Render current route
NavigationHost(nav, route => route switch {
    HomeRoute => Component<HomePage>(),
    DetailRoute d => Component<DetailPage, int>(d.Id),
    SettingsRoute => Component<SettingsPage>(),
    _ => Text("Unknown route")
}) with {
    Transition = NavigationTransition.Slide(),
    CacheMode = NavigationCacheMode.Enabled,
    CacheSize = 10
}

// Lifecycle hooks with navigation guards (source AND destination)
UseNavigationLifecycle(
    onNavigatedTo: ctx => LoadData(ctx.Route),
    onNavigatingFrom: ctx => { if (hasUnsavedChanges) ctx.Cancel(); },
    onNavigatingTo: ctx => { if (!IsAuthorized(ctx.Route)) ctx.Cancel(); },  // NEW
    onNavigatedFrom: ctx => Cleanup()
);

// State serialization
var json = nav.GetState();   // full stack to JSON
nav.SetState(json);          // restore from JSON

// Deep linking (enhanced: query strings, optional params, wildcards)
var deepLinks = new DeepLinkMap<AppRoute>()
    .Map("/detail/{id:int}", args => new DetailRoute(args.Get<int>("id")))
    .Map("/profile/{userId}/{tab?}", args =>                                 // NEW
        new ProfileRoute(args.Get<string>("userId"), args.GetOrDefault<string>("tab")))
    .Map("/docs/**", args => new DocsRoute(args.GetWildcard()))              // NEW
    .Map("/settings", _ => new SettingsRoute());
// Query string access: args.Query<int>("page"), args.QueryString("search")  // NEW

// NavigationView integration
NavigationView(menuItems,
    NavigationHost(nav, routeMap)
).WithNavigation(nav, routeToTag, tagToRoute)
```

### What the competition provides now (2026 update)

**React Router v7** (stable, Nov 2024+): Three operating modes (declarative,
data, framework). Type safety via code generation (`npx react-router typegen`).
View Transitions API integration. Data loaders/actions for SSR. **But**: the
back stack is opaque — developers interact via `useNavigate()`, not by owning
the stack. Type safety is bolt-on (code-generated `.d.ts`), not intrinsic.

**TanStack Router** (v1.0 via TanStack Start, March 2026): 100% type-safe
routing with full inference — no code generation. Search params as typed state.
This is now the type-safety gold standard in the React ecosystem.

**SwiftUI NavigationStack** (iOS 16+, refreshed WWDC 2025): `NavigationPath`
is developer-owned and `Codable` (serializable). `navigationDestination(for:)`
maps types to views. WWDC 2025 added "Liquid Glass" visual refresh and improved
deep-linking. **But**: `NavigationPath` is type-erased — you can append and
count, but can't inspect or modify entries by type without workarounds.

**Compose Navigation 3** (stable Nov 2025 — **the biggest competitive shift
since the last review**): Ground-up rewrite that mirrors Duct's philosophy
almost exactly. Developer-owned back stack (`SnapshotStateList<T>`). Type-safe
routes via `@Serializable` data classes. `NavDisplay` observes the list and
renders content. `Scene`/`SceneStrategy` for adaptive multi-pane layouts
(list-detail on wide screens). **This is now the direct competitor to Duct's
navigation model.**

| Capability | Duct | React Router v7 | SwiftUI | Compose Nav3 |
|---|---|---|---|---|
| Type-safe routes | Native (C# records) | Codegen | Native (Swift types) | Native (@Serializable) |
| Developer-owned back stack | Yes (IReadOnlyList) | No (opaque) | Partial (type-erased) | Yes (SnapshotStateList) |
| GPU-accelerated transitions | Yes (composition layer) | View Transitions API | System-managed | Basic |
| Lifecycle guards (cancel nav) | Yes (source + destination) | useBlocker() | beforeRemove | Developer-managed |
| Page caching (LRU) | Yes | N/A | N/A | Developer-managed |
| State serialization | Yes (JSON) | N/A | Codable | Developer-managed |
| Deep linking | Yes (pattern matching) | Built-in (URL) | Built-in (URI) | Developer-managed |
| Adaptive multi-pane | No | N/A (web) | Limited | Yes (SceneStrategy) |
| Nested navigation | Yes (independent stacks) | Yes | Yes | Yes |
| NavigationView chrome | Yes (auto-sync) | N/A | Built-in | N/A |

### What's actually good about this (credit where due)

**The architectural decision to bypass WinUI Frame is correct and
well-justified.** The design spec documents exactly why Frame doesn't work:
XAML type metadata requirements, IPage interface hard-casts, parameterless
constructor constraints, no extension points. These are hard constraints in
WinUI's C++ code. Rather than fighting the platform, Duct built its own
navigation on a `ContentPresenter`/`Grid` host with the reconciler managing
content swap. This is the same architectural choice Compose Nav3 made —
own the stack, own the rendering, let the framework manage it.

**The developer-owned back stack is the right call.** Both Compose Nav3 and
SwiftUI converged on "navigation state is a list the developer controls." Duct
arrived at this independently. `NavigationStack<TRoute>` with `Navigate`,
`GoBack`, `GoForward`, `Replace`, `Reset`, `PopTo` is a clean, complete API
surface. The stack is an `IReadOnlyList<TRoute>` — inspectable, serializable,
testable. SwiftUI's `NavigationPath` is type-erased (you can't pattern-match on
entries), which is arguably worse.

**C# records as routes is clever.** Records give you structural equality,
immutability, `with` expressions, pattern matching in `switch`, and JSON
serialization for free. A `DetailRoute(int Id)` is more type-safe and more
ergonomic than Compose Nav3's `@Serializable data class Detail(val id: Int)`
(roughly equivalent) and strictly better than React Router's string-template
params (`"/detail/:id"`).

**Composition-layer transitions are a genuine differentiator.** The transition
engine uses `ElementCompositionPreview.GetElementVisual()` and runs slide/fade/
drill-in/spring animations on the compositor thread — zero managed-code
callbacks during animation, zero UI thread blocking. The automatic direction
reversal on GoBack (slide-from-right becomes slide-to-right) is a nice touch.
Per-navigation transition overrides (`nav.Navigate(route, new NavigateOptions {
Transition = NavigationTransition.DrillIn() })`) provide fine-grained control.
No competitor has GPU-accelerated transitions as a first-class navigation
feature.

**The lifecycle hook system is well-designed.** `UseNavigationLifecycle` with
`onNavigatedTo`, `onNavigatingFrom` (with cancellation), and `onNavigatedFrom`
follows the established navigation lifecycle pattern. The ordering is correct:
guard fires before stack mutation, mount and `onNavigatedTo` happen on the new
page, `onNavigatedFrom` fires after the old page is swapped out. The
cancellation mechanism (via `NavigatingFromContext.Cancel()`) is clean and
the guard runs synchronously before the stack mutates — no race conditions.

**The NavigationView integration is thoughtful.** `.WithNavigation(nav,
routeToTag, tagToRoute)` auto-syncs selected item, wires selection change to
navigate, and manages back button visibility/state. This is the kind of
framework integration that eliminates 30+ lines of boilerplate in every app.

**117 unit tests is substantial coverage.** The test suite covers stack
operations, host rendering, lifecycle ordering, caching, serialization/deep
linking, and NavigationView sync. The self-host test pattern (driving navigation
without WinUI controls) enables fast, reliable CI.

### What's still concerning (skeptic's view)

**1. ConnectedTransition is a stub.** The `NavigationTransition.Connected()`
factory exists and is documented, but `TransitionEngine.RunTransition` logs a
warning and falls back to `SlideTransition`. This is a spec-driven API that
doesn't work. A developer who writes `Transition = NavigationTransition.Connected()`
gets a slide instead of a connected animation, with the only indication being a
debug log message. Shipping a named API that doesn't do what the name says is
worse than not shipping it — it's a trap. SwiftUI's `matchedGeometryEffect` and
Compose's `SharedTransitionLayout` both work. Duct's doesn't.

**2. E2E tests were silently broken — now fixed, but the blind spot is telling.**
The previous review flagged E2E Appium tests as "fixtures only — not executed."
The truth was worse: 44 of 46 E2E tests existed and ran, but the test filter
(`ClassName=InteractiveTests`) made only 2 visible to `dotnet test`. The fix
(changing to `ClassName!=SelfTestBatch`) now surfaces 43 Appium tests across 6
test classes including `AccessibilityInteractionTests` (10 tests) and
`WinFormsInteropTests` (14 tests). This is a significant improvement — E2E
validation is real and running. But the fact that 44 tests were invisible for
the entire development period means prior E2E "we shipped tests" claims were
untested claims. How many other test configurations are silently misconfigured?

**3. No adaptive multi-pane layout.** Compose Nav3's `Scene`/`SceneStrategy`
abstraction handles the list-detail pattern: on a wide screen, show list and
detail side-by-side; on a narrow screen, push detail onto the stack. This is
increasingly important even for desktop apps (think adaptive panels in Outlook,
Teams, VS Code). Duct's navigation is strictly single-pane — one route renders
one page. Building a master-detail layout requires manual composition outside the
navigation system. For a desktop-first framework, this is an odd omission.

**4. Deep link patterns are stringly-typed at the boundary.** `DeepLinkMap.Map(
"/detail/{id:int}", args => ...)` uses string patterns with string parameter
names. A typo in `"{id:int}"` silently fails to match. The `RouteArgs.Get<T>(
"id")` call is a string-keyed dictionary lookup with a runtime cast. The irony
is thick — Duct chose C# records for routes specifically for type safety, then
introduced string-based pattern matching at the deep link boundary. The type
safety is only skin deep: the deep link system connects untyped URI strings to
typed routes through a stringly-typed middle layer.

**5. The Grid wrapper adds another invisible element.** `MountNavigationHost`
creates a Grid as the container for navigation content. This is the same
problem as the Border wrapper for components (Section 4, critique #5) — the
WinUI visual tree accumulates framework-internal containers that participate in
layout but serve no visual purpose. A NavigationHost inside a component inside
a NavigationView produces: NavigationView > ... > Border (component) >
Grid (nav host) > content. Every extra layout container costs measure/arrange
time.

**6. Cache restore may skip transitions.** When a cached page is restored, the
reconciler skips remounting (the cached `UIElement` is reattached directly).
It's unclear whether transitions run during cache restore. If they don't, the
user sees an instant snap when navigating back to a cached page but a smooth
slide when navigating to an uncached one — an inconsistency that would feel
broken.

**7. Destination-side guards now exist but data-dependent navigation is still
missing.** `NavigatingToContext.Cancel()` now allows the destination page to
reject navigation — closing the "outgoing guard only" gap flagged in the
previous review. This handles authorization checks and prerequisite validation.
But React Router's loaders (fetch data before navigation completes, show loading
state during fetch) still have no equivalent. The guard is synchronous — there's
no way to say "defer this navigation until the data loads, then decide." For
data-heavy apps where every page transition depends on an API call, the guard
is necessary but not sufficient.

**8. UseSystemBackButton is a separate opt-in hook.** The developer must
explicitly call `UseSystemBackButton(nav, window)` to wire Alt+Left and the
system back button. For a desktop framework where back-navigation is a core
interaction, this should be default behavior that you opt *out* of, not opt
*in* to. Every NavigationHost should respond to Alt+Left unless told not to.

**9. The sample app is the only integration test.** The NavigationDemo sample
app is comprehensive (routes, guards, transitions, caching, deep linking,
nested navigation). But it's a demo, not a test. There's no automated
verification that it works. The existing showcase apps (Outlook clone, file
manager) haven't been updated to use the new navigation system — meaning the
framework's most complex apps still use the hand-rolled `UseState` switch
pattern.

### Revised navigation verdict

**Previously: F → B+. Now: B+ (solid, trending toward A-).**

The B+ holds, with meaningful incremental progress. Destination-side guards
close a real gap (destination can now reject navigation). Deep link enhancements
(query strings, optional params, wildcards) make the deep linking system more
practical. `NavigationDiagnostics` provides the observability hooks that
production apps need. Most importantly, the E2E test discovery fix means 43
Appium tests are actually validating the full pipeline — the previous review's
"E2E tests are unexecuted" critique was correct at the `dotnet test` level
but the underlying problem was worse than assumed (broken filter, not missing
tests).

The grade stays B+ rather than moving to A- because: ConnectedTransition is
still a stub, no adaptive multi-pane layout exists, deep linking still has a
stringly-typed middle layer, the showcase apps still haven't adopted navigation,
and the destination guard is synchronous (no async data loading). The trajectory
is clear — each iteration closes real gaps — but the last 15% (adaptive layouts,
connected transitions, showcase adoption, async guards) separates "competitive"
from "best-in-class."

---

## 8. Commanding

### The commanding story: a novel framework feature with real gaps

The previous version of this review scored Commands at F — "No ICommand
equivalent." A comprehensive commanding system has now shipped: immutable
`DuctCommand` records that bundle execute + canExecute + label + icon +
description + keyboard accelerator, 16 standard commands, a `UseCommand` hook
for async lifecycle, focus-scoped keyboard accelerators via `CommandHost`,
command-aware DSL overloads for Button/AppBarButton/MenuItem, and ICommand
interop. The honest assessment: **commanding is a genuine framework
differentiator — no competing declarative framework provides this — but it
has performance concerns and missing capabilities that limit its claim to
being a complete solution.**

### Why this matters (the competitive context)

The commanding design spec's research is correct and worth repeating:

- **React** has no command abstraction. Third-party command palette libraries
  (`cmdk`, `kbar`) are UI components, not command registries.
- **SwiftUI** has `CommandMenu`/`CommandGroup` for macOS menu bars and (as of
  iPadOS 26) iPad menu bars. But there's no bundling — each menu item repeats
  its label, icon, shortcut, and action. No "define once, use everywhere."
- **Compose** has nothing. No commanding abstraction whatsoever.
- **Every serious app builds a custom command registry.** VS Code, Files App,
  Windows Terminal all rolled their own. This is a real, unsolved gap.

Duct filling this gap is the single most novel feature in the framework. It's
the one area where a skeptic has to concede: Duct provides something the
competition genuinely doesn't.

### What Duct has now

```csharp
// Define once — immutable record with all metadata
var saveCmd = new DuctCommand {
    Label = "Save",
    ExecuteAsync = async () => await SaveDocumentAsync(),
    Icon = new SymbolIconData("Save"),
    Accelerator = new KeyboardAcceleratorData(VirtualKey.S, VirtualKeyModifiers.Control),
    CanExecute = isDirty,
    Description = "Save the current document"
};

// Or use standard commands (16 built-in)
var cutCmd = StandardCommand.Cut(() => CutSelection(), canExecute: hasSelection);
var copyCmd = StandardCommand.Copy(() => CopySelection(), canExecute: hasSelection);
var pasteCmd = StandardCommand.Paste(() => PasteFromClipboard());

// UseCommand hook for async lifecycle (auto-debounce, IsExecuting tracking)
var save = UseCommand(saveCmd);
// save.IsExecuting is true during async operation
// save.IsEnabled is false while executing — buttons auto-disable

// Use in N surfaces from one definition
CommandBar(
    AppBarButton(cutCmd),    // auto-maps Label, Icon, Accelerator, IsEnabled
    AppBarButton(copyCmd),
    AppBarButton(pasteCmd)
)
MenuBar(
    MenuBarItem("Edit",
        MenuItem(cutCmd),    // same command, same metadata, different surface
        MenuItem(copyCmd),
        MenuItem(pasteCmd)
    )
)

// Per-site overrides via record with-expressions
MenuItem(deleteCmd with { Label = "Remove from list" })

// Focus-scoped keyboard accelerators
CommandHost([saveCmd, undoCmd, redoCmd],
    VStack(editorContent)    // Ctrl+S/Z/Y only work inside this region
)

// Parameterized commands
var deleteItem = new DuctCommand<Item> {
    Label = "Delete",
    Execute = item => RemoveItem(item),
    Icon = new SymbolIconData("Delete"),
    CanExecute = canDelete
};
MenuItem(deleteItem, selectedItem)  // binds parameter at use site

// ICommand interop for migration
var legacyCmd = CommandInterop.FromCommand(viewModel.SaveCommand, "Save");
```

### What's actually good about this (credit where due)

**The "CanExecute is a bool" design is brilliant in context.** Traditional
`ICommand` uses `CanExecuteChanged` events to notify the UI when enablement
changes. This is an event-based mechanism fighting a reactive framework. Duct's
approach: commands are created during `Render()`, which runs on every state
change. `CanExecute` is just a bool that naturally reflects current state
because `isDirty` or `hasSelection` are already reactive. No events needed.
The framework's re-render cycle IS the notification mechanism. This is the
insight that makes the whole design work — it eliminates the impedance mismatch
between ICommand's event model and Duct's declarative model.

**The "define once, use everywhere" pattern works.** One `DuctCommand` drives
`AppBarButton`, `MenuItem`, `Button`, keyboard accelerator, and tooltip from a
single definition. The DSL overloads (`Button(cmd)`, `AppBarButton(cmd)`,
`MenuItem(cmd)`) auto-map all metadata fields to the appropriate WinUI
properties. Per-site overrides via `cmd with { Label = "..." }` let you
customize without duplicating. This is exactly the capability VS Code, Files,
and Windows Terminal all built custom registries to achieve.

**StandardCommand is a nice convenience.** `StandardCommand.Cut(action)` gives
you the correct icon (SymbolIcon("Cut")), the correct keyboard accelerator
(Ctrl+X), and a label — ready to use. All 16 standard commands (Cut, Copy,
Paste, Undo, Redo, Delete, SelectAll, Save, Open, Close, Share, Play, Pause,
Stop, Forward, Backward) are implemented. This eliminates the "look up the
right icon and accelerator" dance that wastes time in every WinUI app.

**UseCommand for async is well-designed.** The hook wraps async commands with
`IsExecuting` tracking and re-entrance prevention. During `ExecuteAsync`, the
command auto-disables (buttons go gray). When it completes, it re-enables.
The implementation is clean: `UseState(false)` for the executing flag,
`UseMemo` for the wrapped execute with the guard. Sync-only commands skip hook
state entirely (no slot waste), which is a thoughtful optimization.

**Focus-scoped accelerators are the right model for desktop.** `CommandHost`
creates a region where keyboard accelerators are active only when focus is
within the host. This solves the "Ctrl+S means different things in different
panels" problem that desktop apps face. The implementation checks
`IsDescendantOf(focused, host)` before invoking — simple and correct.

### What's still concerning (skeptic's view)

**1. Accelerators are rebuilt on every render.** `UpdateCommandHost` clears and
recreates all `KeyboardAccelerator` objects on the WinUI `UIElement` every
render cycle. This is O(n) COM interop calls per render per CommandHost, where
n is the number of commands with accelerators. The reason is that accelerator
event handlers capture command closures that reference current state — so they
need fresh closures each render. This is functionally correct but
architecturally expensive. React avoids this with event delegation. Compose
avoids it by not having keyboard accelerators. Duct's approach is the most
direct and the most wasteful.

For a CommandHost with 20 commands (not unusual for a document editor's main
region), that's 20 accelerator creates + 20 accelerator destroys per render.
Each involves COM interop to the WinUI layer. If the component re-renders
frequently (e.g., on every keystroke in a text field), this compounds quickly.

**2. StandardCommand labels are English-only.** `StandardCommand.Cut` has
`Label = "Cut"`. Not `Label = Intl("Cut")`. Not `Label = loc.GetString("Cut")`.
Just a hard-coded English string. This is surprising given that Duct has a full
ICU-based localization system with `DuctContext<IntlAccessor?>` integration.
The framework's own standard commands don't use the framework's own
localization system. A developer localizing their app will discover that
toolbar buttons say "Cut"/"Copy"/"Paste" in English regardless of locale, and
the fix is to create their own command definitions instead of using
`StandardCommand`. This defeats the purpose of standard commands.

For comparison, WinUI's `StandardUICommand` has localized labels for all
supported Windows languages. Duct's `StandardCommand` is a regression from the
platform it's built on.

**3. No command routing to the focused view.** The design spec lists "command
routing to focused view" as a non-goal ("future work — needs focus management
first"). But this is the core use case for Cut/Copy/Paste in a multi-document
or multi-panel app. When the user presses Ctrl+C, which panel's selection gets
copied? `CommandHost` scopes accelerators to a region, but it doesn't solve
the routing problem — if two editors are both inside the same CommandHost, the
command fires on whatever closure was captured at render time, not on the
focused editor.

SwiftUI solves this with `FocusedValue`/`FocusedObject` — the focused view
publishes its cut/copy/paste handlers, and the menu bar reads them. WPF solved
this with `RoutedUICommand` and command routing through the visual tree. Duct
has neither. For a desktop framework, this is a conspicuous gap.

**4. No command palette UI.** The commanding design spec lists this as future
work. The registry data model (commands with labels, icons, accelerators) is
the perfect foundation for a VS Code-style command palette. But the framework
doesn't provide the palette itself. Given that Duct aims to be an opinionated
framework (not just a component library), shipping the registry without the
palette is like shipping a search index without a search box.

**5. CommandHost creates another invisible Grid wrapper.** `MountCommandHost`
creates a Grid to host the accelerators. This is the third invisible wrapper
(component Border, NavigationHost Grid, CommandHost Grid) that accumulates in
the visual tree. A component with a navigation host and a command host produces
Border > Grid > Grid > content — three extra layout containers.

**6. IsDescendantOf visual tree walk on every key press.** When any keyboard
accelerator fires inside a CommandHost, the handler walks the WinUI visual tree
upward from the focused element to check if it's a descendant of the host. For
deep visual trees (which Duct creates via its wrapper elements), this is O(d)
per key press where d is tree depth. Most key presses in a document editor fire
a KeyDown → accelerator check chain. This isn't catastrophic, but it's another
tax on a hot path.

**7. No batch command registration.** Each `CommandHost` independently manages
its accelerators. If multiple `CommandHost` elements exist (e.g., one for
document commands, one for panel commands), accelerators are registered on
separate WinUI elements. There's no global registry, no deduplication, no
priority system. If two CommandHosts register Ctrl+S, both fire (or one
shadows the other depending on focus). The design spec mentions a global
command registry as future work, but the current system is purely local.

**8. DuctCommand equality may defeat memoization.** `DuctCommand` is a record,
so its `Equals` compares all fields structurally — including `Execute` (an
`Action`) and `ExecuteAsync` (a `Func<Task>`). Delegate equality in C# compares
target + method — two lambdas that capture different closure state are never
equal, even if they do the same thing. This means a `DuctCommand` created in
`Render()` is *always* unequal to the one from the previous render (because the
lambda captures fresh state). Components that receive commands as props will
always fail the memo check, defeating the default-on memoization from Section 2.
The `UseCommand` hook doesn't address this — it wraps the command but returns a
new record each time.

### Revised commanding verdict

**Previously: F (no ICommand equivalent). Now: B+.**

The improvement is a leap, and the feature is genuinely novel. No competing
declarative framework provides define-once commands with metadata bundling,
standard commands, async lifecycle, and focus-scoped accelerators. Duct is
ahead of the entire competition here — not catching up, actually leading. The
API design is clean, the integration with the DSL is natural, and the
`UseCommand` hook for async is well-considered.

The grade is B+ rather than A because of implementation concerns: accelerator
rebuild on every render, un-localized standard commands (ironic given the
localization system exists), no command routing to the focused view, delegate
equality defeating memoization, and the missing command palette UI. The
foundation is strong — a command palette, localization, and routing can all be
built on top of what exists. But "the foundation enables it" and "it's shipped"
are different claims, and this review scores what's shipped.

---

## 9. Lists and Collections

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

## 10. Animation

### The animation story: from partially-wired to fully operational (within its ceiling)

The last version of this review graded animation at C+ — good API design across
8 new compositor-layer features, but four integration bugs where the API
promised behavior the reconciler didn't deliver: exit transitions were dead code,
Focused interaction state was silently ignored, WithAnimation only animated
Opacity, and stagger didn't integrate with enter transitions. The review
explicitly said: "Fix the four integration bugs and the compositor-property
ceiling is the only remaining structural concern — that would be a clear B-."

A focused bug-fix diff (commit d38c6ef, 3 sub-commits) has done exactly that —
plus fixed three additional runtime issues discovered during implementation.
The honest assessment now: **all four integration bugs are fixed, three runtime
issues are resolved, and the compositor-property ceiling is the only remaining
structural concern. The animation system now delivers what its API promises.**

### What Duct has now

**Tier 1: Implicit transitions (5 properties, unchanged).**

- `.OpacityTransition(duration?)` — ScalarTransition on UIElement.Opacity
- `.RotationTransition(duration?)` — ScalarTransition on UIElement.Rotation
- `.ScaleTransition(transition?)` — Vector3Transition on UIElement.Scale
- `.TranslationTransition(transition?)` — Vector3Transition on UIElement.Translation
- `.BackgroundTransition(duration?)` — BrushTransition on Grid/StackPanel only

These are thin wrappers over WinUI's built-in implicit transition properties.
Unchanged from before. They run on the composition thread with zero managed-code
involvement.

**Tier 2: Theme transitions (structural enter/exit).**

- `.WithTransitions(params Transition[])` — ChildrenTransitions on panels
- `.ItemContainerTransitions(params Transition[])` — ItemContainerTransitions
  on ListView/GridView

Also unchanged. Uses WinUI's theme transitions directly.

**Tier 3: Curve system (NEW — easing/spring DSL).**

```csharp
Curve.Spring(dampingRatio: 0.8f, period: 0.05f)
Curve.Ease(300, Easing.EaseInOut)
Curve.Ease(200, Easing.CubicBezier(0.2f, 0.9f, 0.3f, 1.0f))
Curve.Linear(150)
```

`Curve` is an abstract record with three sealed subtypes: `SpringCurve`,
`EaseCurve`, `LinearCurve`. `Easing` is a readonly record struct with 7 presets
(Linear, EaseIn, EaseOut, EaseInOut, Accelerate, Decelerate, Standard) plus
`CubicBezier()` for custom curves. Immutable, zero-allocation, shareable.
`AnimationHelper` maps these to WinUI compositor animations:
`CreateSpringScalarAnimation()` / `CreateSpringVector3Animation()` for springs,
`CreateScalarKeyFrameAnimation()` with `CreateCubicBezierEasingFunction()` for
eased, plain keyframe animation for linear.

This directly addresses the "no easing function DSL" critique from the previous
review. The design is clean — the curve types carry enough data for the
compositor bridge to create the right animation, and the bridge (`AnimationHelper`)
handles scalar vs. Vector3 variants uniformly across all curve types.

**Tier 4: `.Animate()` modifier (NEW — declaration-site implicit animations).**

```csharp
Border(child).Animate(Curve.Spring(), AnimateProperty.Opacity | AnimateProperty.Scale)
Border(child).Animate(Curve.Ease(200, Easing.Decelerate))  // default: All 5 properties
```

`AnimateProperty` is a flags enum covering Opacity, Offset, Scale, Rotation,
CenterPoint. The reconciler sets up `ImplicitAnimationCollection` entries on the
element's composition Visual for the specified properties. Any subsequent
compositor-level change to those properties animates automatically. This extends
beyond the 5 UIElement-level implicit transitions by targeting the composition
Visual directly, though it still only reaches compositor properties.

**Tier 5: Layout animations (previously documented — composition-layer
position/size).**

```csharp
Border(child).LayoutAnimation()
Border(child).SpringLayoutAnimation(dampingRatio: 0.8f, period: 0.1f)
```

Unchanged from last review. Sets up implicit animation on the composition
Visual's Offset and optionally Size. Well-engineered, spring physics, correct
reconciler integration.

**Tier 6: Enter/exit transitions (NEW — individual element mount/unmount
animations).**

```csharp
// Fade in on mount, fade out on unmount
Text("Hello").Transition(Transition.Fade)

// Slide in from bottom on mount, scale out on unmount
Panel.Transition(Transition.Slide(Edge.Bottom) | Transition.Scale(0.85f))

// Parallel: fade + slide together
Panel.Transition(Transition.Fade + Transition.Slide(Edge.Left))

// Asymmetric: different enter and exit
Panel.Transition(Transition.Fade | Transition.Slide(Edge.Bottom))
```

`Transition` is an abstract record with sealed subtypes: `FadeTransition`,
`SlideTransition(Edge)`, `ScaleTransition(float From)`, `CombinedTransition`,
`AsymmetricTransition`, `DirectionalTransition`. Two operators: `+` for
parallel composition, `|` for asymmetric enter/exit. `ElementTransition`
wraps the transition with an optional `Curve` override.

The reconciler integration is now complete. On mount:
`ApplyEnterTransition()` runs compositor animations (opacity/offset/scale) on
the element's Visual, respecting `AnimationScope.Current` if present (priority:
explicit curve > ambient scope > 300ms Decelerate default). On unmount:
`ApplyExitTransition()` creates a `CompositionScopedBatch`, runs exit
animations, and defers removal until `batch.Completed`. All three removal paths
in `ChildReconciler` (positional excess, keyed-only removals, keyed middle
unmatched) route through `RemoveChildWithExitTransition()`. Type-mismatch
replacements (e.g., `visible ? Border(...) : Text(...)`) route through
`ReplaceChildWithExitTransition()`, which inserts the new control, re-inserts
the old for its exit animation, then removes it on completion.

This fully addresses the "no enter/exit for individual elements" critique.
Both enter and exit transitions work. The composition-operator DSL
(`+` for parallel, `|` for asymmetric enter/exit) now has full runtime effect.

**Tier 7: Interaction states (NEW — zero-reconcile visual state machine).**

```csharp
Border(child).InteractionStates(s => s
    .PointerOver(scale: 1.05f, opacity: 0.9f)
    .Pressed(scale: 0.95f, background: hoverBrush)
    .Focused(borderBrush: focusBrush))
```

`InteractionStatesConfig` defines visual values for PointerOver, Pressed, and
Focused states. `InteractionStateValues` carries: 5 compositor properties
(Opacity, Scale, ScaleV, Translation, Rotation) + 3 brush properties
(Background, Foreground, BorderBrush). The reconciler registers pointer event
handlers that apply compositor animations for transform properties (zero-cost
during interaction) and direct brush swaps (~1μs) for brush properties. No
reconciler re-render. No state change. No virtual tree diff.

All three interaction states — PointerOver, Pressed, and Focused — are wired.
The reconciler's `ApplyInteractionStates()` registers PointerEntered/
PointerExited, PointerPressed/PointerReleased, and GotFocus/LostFocus handlers.
Focus styling values are properly applied and reverted through the same
transition system as pointer states.

This fully addresses the "VSM replacement is expensive" critique. Hover, press,
and focus effects using InteractionStates run entirely on the composition
thread (for transforms) or via pre-cached brush swap (for brushes), with zero
managed-code re-rendering.

The scope is intentionally narrow — `InteractionStateValues` is a closed record,
not an extensible property bag. No Width, Margin, FontSize, CornerRadius. The
record IS the boundary, and the boundary is the compositor + 3 brush properties.
This is honest design: it covers what can be done cheaply and stops there.

**Tier 8: Staggered children (NEW).**

```csharp
StackPanel(children).Stagger(TimeSpan.FromMilliseconds(50))
StackPanel(children).Stagger(TimeSpan.FromMilliseconds(50), Curve.Spring())
```

`StaggerConfig` applies progressive delay to child animations — layout
animations, property animations, and enter transitions — via
`CompositionAnimation.DelayTime`. Parent elements with `StaggerConfig` push a
`StaggerScope` before mounting children; each child's `ApplyEnterTransition`
consumes an incrementing index for stagger delay computation. The scope supports
nesting — inner stagger panels reset the index independently.

This fully integrates stagger with enter transitions. A staggered list with
`.Transition(Transition.Fade)` on each item produces a cascade fade-in effect
with each item delayed by the stagger interval.

**Tier 9: Keyframe animations (NEW — trigger-based multi-property keyframes).**

```csharp
Border(child).Keyframes("pulse", triggerValue, k => k
    .Duration(600).Loop()
    .At(0f, scale: Vector3.One)
    .At(0.5f, scale: new Vector3(1.1f, 1.1f, 1f), easing: Easing.EaseOut)
    .At(1f, scale: Vector3.One, easing: Easing.EaseIn))
```

`KeyframeBuilder` constructs a `KeyframeAnimationDef` with typed `KeyframeDef`
entries. Each keyframe can set Opacity, Scale, Translation, Rotation with
per-keyframe easing. Animations are trigger-based: when the `Trigger` value
changes (by reference), the reconciler starts new compositor animations. Looping
is supported. `ApplyKeyframeAnimations()` in the reconciler creates
`ScalarKeyFrameAnimation` / `Vector3KeyFrameAnimation` instances, inserts
keyframes with their easing functions, and calls `visual.StartAnimation()`.

This directly addresses the "no keyframe or sequenced animation DSL" critique.
The design is adequate — the builder pattern is familiar and the trigger
mechanism is sensible. The limitation: keyframes can only target the same
compositor properties everything else targets. No color keyframes, no layout
property keyframes.

**Tier 10: Scroll-linked animations (NEW).**

```csharp
ScrollViewer(content).ScrollAnimation(sv, b => b
    .Parallax(0.5f)
    .FadeOut(100, 400)
    .ScaleRange(0, 300, 1f, 0.8f)
    .Expression("Rotation", "scroll.Translation.Y * 0.001"))
```

`ScrollAnimationBuilder` produces `ScrollExpression[]` entries (property name +
expression string). The reconciler creates `ExpressionAnimation` instances on
the composition Visual, referencing the ScrollViewer's manipulation property
set. Preset helpers cover parallax, fade ranges, and scale ranges. Custom
expressions via `.Expression()` for anything else.

**Tier 11: AnimationScope.WithAnimation (NEW — ambient animation context).**

```csharp
// All property changes in this block animate with the given curve
AnimationScope.WithAnimation(Curve.Ease(300, Easing.Decelerate), () =>
{
    SetExpanded(!expanded);
});

// Async: returns Task that completes when all animations finish
await AnimationScope.WithAnimationAsync(Curve.Spring(), () =>
{
    SetStep(nextStep);
});
```

`AnimationScope` uses `[ThreadStatic]` fields to hold a `Curve?` and a
`bool HasScope`. `WithAnimation()` sets the ambient curve, runs the action,
and restores the previous curve in a `finally` block (nestable). The reconciler
routes all five compositor properties (Opacity, Scale, Rotation, Translation,
CenterPoint) through `AnimationHelper.SetOrAnimate()` / `SetOrAnimateVector3()`,
which check `AnimationScope.Current` for an ambient curve. The ambient curve
also propagates to enter transitions (overrides the default 300ms Decelerate).
Additionally, `SetOrAnimate` checks for the element's `AnimationConfig` (from
`.Animate()`) as a fallback when no ambient scope is present, creating explicit
compositor animations using the config's curve.

A critical runtime fix ensures the scope persists across the async render
boundary: state setters inside `WithAnimation` trigger an async re-render via
`DispatcherQueue`, and by the time `Reconcile()` runs the scope would normally
be gone. `DuctHost` now captures the ambient curve in `RequestRender()` and
restores it around the `Reconcile()` call via `AnimationScope.PushScope/PopScope`.

The async variant creates a `CompositionScopedBatch`, runs the action inside
the batch, calls `batch.End()`, and returns a `Task` that completes on
`batch.Completed`. This lets callers await animation completion.

This fully addresses the "no declarative value-driven animation API" critique.
It's close to SwiftUI's `withAnimation { }` pattern — wrap a state change, and
the resulting property updates animate. All compositor properties are covered.
Width changes, color changes, margin changes still don't animate (those are
layout/XAML properties, not compositor properties). But within the compositor
property set, `WithAnimation` works completely.

**Tier 12: Connected animations (previously documented).**

```csharp
Border(avatar).ConnectedAnimation("hero-image")
```

Unchanged from last review. Two-phase prepare-on-unmount / start-after-mount
pattern with deferred flush. Still string-keyed.

### What's actually good about this (credit where due)

The design quality across these 8 new features is consistently high. A few
things stand out:

**The Curve type system is well-modeled.** Three sealed record subtypes, each
carrying exactly the data the compositor bridge needs. Immutable, shareable,
zero-allocation. The presets cover standard motion needs. Custom bezier is
available. This is a clean API that will age well.

**The Transition composition operators are genuinely expressive.** `Fade +
Slide(Bottom)` for parallel, `Fade | Scale(0.85f)` for asymmetric enter/exit.
The record hierarchy (FadeTransition, SlideTransition, CombinedTransition,
AsymmetricTransition, DirectionalTransition) handles these combinations
correctly without combinatorial explosion. The reconciler decomposes them via
pattern matching in `GetEnterTransition()` / `GetExitTransition()`.

**InteractionStates solves a real performance problem correctly.** The previous
review flagged VSM replacement as expensive — hover effects triggering full
reconciliation. InteractionStates eliminates this for the common case. The
closed record design is honest: it covers what can be done at compositor speed
and doesn't pretend to be a general-purpose state machine for arbitrary
properties. The builder API is ergonomic.

**The reconciler lifecycle integration is now thorough and complete.** Every
feature has mount/update/unmount wiring:
- Enter transitions applied at mount, exit transitions defer removal via
  CompositionScopedBatch callback — all removal paths covered
- InteractionStates handlers registered at mount, cleared at unmount
  (PointerOver, Pressed, and Focused all wired)
- Scroll animations created at mount, cleared at unmount
- Keyframe animations triggered on mount and on trigger-value change
- Stagger delays applied to both implicit animations and enter transitions
  via StaggerScope
- Layout animations and `.Animate()` configs applied via implicit animation
  collections

On update, the reconciler correctly diffs: if a config changed, reapply; if
removed, clear. The structural wiring is complete — the API delivers what it
promises.

**AnimationScope.WithAnimation is the right abstraction, now fully wired.**
Ambient curve propagation via ThreadStatic fields, nestable with proper restore
in `finally`, async variant with CompositionScopedBatch completion tracking.
All five compositor properties route through `SetOrAnimate`/`SetOrAnimateVector3`.
The scope persists across the async render boundary via DuctHost capture/restore.
The pattern is architecturally sound (same model as SwiftUI's `withAnimation`)
and the implementation is complete.

### Previously-reported integration bugs: all four fixed

The previous version of this review identified four integration bugs where
the API promised behavior the reconciler didn't deliver. All four have been
fixed in commit d38c6ef (3 sub-commits, 767 lines added), along with three
additional runtime issues discovered during implementation:

**Bug 1 (exit transitions): FIXED.** All three removal paths in
`ChildReconciler` now route through `RemoveChildWithExitTransition()`.
Type-mismatch replacements route through `ReplaceChildWithExitTransition()`.

**Bug 2 (Focused state): FIXED.** `ApplyInteractionStates()` now registers
GotFocus/LostFocus handlers alongside pointer handlers.

**Bug 3 (WithAnimation scope): FIXED.** All five compositor properties now
route through `SetOrAnimate()`/`SetOrAnimateVector3()`. Additionally, the
scope persistence across the async render boundary was fixed (DuctHost captures
the ambient curve before DispatcherQueue dispatch and restores it around
`Reconcile()`).

**Bug 4 (stagger + enter transitions): FIXED.** `StaggerScope` is pushed
before mounting children; each child's `ApplyEnterTransition` consumes an
incrementing index for stagger delay computation, with proper nesting support.

**Additional runtime fixes:**
- **Opacity routing for `.Animate()`:** UIElement.Opacity is a XAML DP, not a
  compositor facade — setting it directly doesn't trigger implicit animations
  from `.Animate()`. `SetOrAnimate` now creates explicit compositor animations
  when an `AnimationConfig` is present.
- **Pool crash with compositor-tainted elements:** Elements that had
  `GetElementVisual()` called permanently lose XAML implicit transition API
  access. `ElementPool` now tracks "compositor-tainted" elements via
  `ConditionalWeakTable` and excludes them from pooling.
- **Exit transitions in type-mismatch replacement:** `visible ? Border : Text`
  scenarios now defer removal via `ReplaceChildWithExitTransition`.

**47 regression tests** in `AnimationBugTests.cs` (all pure logic, no WinUI
host) validate these fixes.

### What's still concerning (skeptic's view)

**1. Still compositor-property-bound.** This is the fundamental ceiling that
no amount of good API design can remove. Duct's animation system — all of it,
every tier — can only animate what the WinUI compositor exposes on the Visual:
Opacity, Offset (Translation), Scale, Rotation, CenterPoint, and Size
(cosmetic only). You cannot animate:

- Width, Height, MinWidth, MaxWidth (layout properties)
- CornerRadius, Margin, Padding (layout/shape properties)
- FontSize, FontWeight (text properties)
- Arbitrary brush colors or gradients (beyond the 3 InteractionStates swaps)
- Clip geometry, border thickness, any custom dependency property

SwiftUI animates *any* state change that produces a different view body.
Compose's `animateAsState` works for any type with a `TwoWayConverter`.
Flutter's `AnimatedBuilder` + `Tween` works for any property with `lerp`.
Duct can only animate the ~5 compositor properties plus 3 brush swaps. This
isn't a Duct design failure — it's a WinUI platform constraint. But the result
is real: a developer who wants a button's corner radius to animate on hover,
or a sidebar's width to animate on expand, has no declarative path. They need
`.Set()` and WinUI's `DoubleAnimation` / `Storyboard` API.

The `.Animate()` modifier, InteractionStates, keyframes, and
AnimationScope.WithAnimation all work within the same compositor property set.
They provide different *control models* (implicit, event-driven, trigger-based,
ambient) for the same narrow set of properties. This is good engineering — but
it's an increasingly sophisticated system for animating the same 5 things.

**2. No per-frame UseAnimation hook.** `AnimationScope.WithAnimation` wraps
state changes so the reconciler routes property updates through compositor
animations. This is the right model for reactive animation. But there's no hook
that yields interpolated values each frame for custom rendering:

```csharp
// Still doesn't exist:
var progress = UseAnimation(0.0, 1.0, duration: 300ms, easing: EaseOut);
// progress smoothly animates from 0 to 1, re-rendering at each frame

var spring = UseSpring(targetX, stiffness: 200, damping: 20);
// spring.Value drives layout or drawing on each frame
```

React Spring's `useSpring`, Framer Motion's `useAnimation`, and Compose's
`Animatable` all provide this. A game progress bar, a custom drawing that
interpolates between states, a physics simulation — these need per-frame values
driven from component code. Duct has no bridge between the hooks system and the
animation system. The compositor runs the animation; component code can't
observe the intermediate values.

In fairness, per-frame hooks would fight the compositor-thread model (they'd
require managed-code callbacks at display refresh rate), and WithAnimation
covers the common case of "animate this state transition." But the gap exists.

**3. Connected animations still use string-key coordination.** Source and
destination must use the same string key. A typo silently produces no animation
(the `try/catch` swallows the failure). SwiftUI's `matchedGeometryEffect`
uses typed Namespace objects; Compose's `SharedTransitionScope` uses typed keys.
Duct uses bare strings. This was called out in the previous review and is
unchanged.

**4. Scroll-linked expressions are stringly-typed.** The
`ScrollAnimationBuilder` presets (Parallax, FadeOut, FadeIn, ScaleRange) are
type-safe and cover common cases. But the escape hatch —
`.Expression("Rotation", "scroll.Translation.Y * 0.001")` — is two bare
strings with no validation. A typo in the property name or expression syntax
fails at runtime. This is inherent to WinUI's `ExpressionAnimation` API, but
the framework could validate property names against a known set.

**5. Keyframes can only target compositor properties.** The `KeyframeBuilder`
accepts Opacity, Scale, Translation, Rotation — the same compositor properties
as everything else. You can't keyframe a color transition, a width animation,
or a corner radius change. SwiftUI's `KeyframeAnimator` (iOS 17+) works with
any `Animatable` value. The keyframe system is useful within its bounds but
those bounds are the same narrow set.

**6. Layout animation limitations are inherent and unchanged.**
- Hit-testing uses the final layout position, not the animated visual
- Size animation is cosmetic — content doesn't re-layout during animation
- Elements need stable keys for reconciler matching across reorders

These are inherent to composition-layer visual animations. Layout animations
work well for list reorders and grid reflows, less well for "sidebar expands
from collapsed width."

**7. InteractionStates is closed to 8 properties.** The 5 compositor
transform properties + 3 brush swaps cover hover/press/focus for common UI
patterns. But a button that changes CornerRadius on hover, or a card that
changes BorderThickness on focus, needs `.Set()`. The closed record design is
honest, but the boundary is tight. WinUI's VSM can animate any dependency
property via Storyboard; InteractionStates can animate 8.

**8. No sequencing or orchestration.** Keyframes provide multi-step animation
on a single element. But there's no built-in way to orchestrate animations
across multiple elements in sequence — "first fade in the header, then slide
in the list items, then scale up the FAB." Stagger handles the simple case
(uniform delay per child), but general sequencing requires manual coordination
via `WithAnimationAsync` + `await` chains. Framer Motion's `staggerChildren`
and `delayChildren` variants, and Compose's `AnimatedContent` with
`SizeTransform`, provide richer orchestration primitives.

### Revised animation verdict

**Previously: C+ (good design, 4 integration bugs). Now: B-.**

The previous review explicitly said: "Fix the four integration bugs and the
compositor-property ceiling is the only remaining structural concern — that
would be a clear B-." The bugs are fixed. The grade moves to B-.

The animation system is now a fully operational multi-tier compositor animation
system:

- A clean curve/easing DSL with springs, bezier, and presets
- Enter AND exit transitions with composition operators — both work
- Zero-reconcile interaction states for hover/press/focus — all three wired
- Keyframe animations with per-frame easing and looping
- Staggered children integrated with enter transitions
- Scroll-linked expression animations
- Ambient animation context (WithAnimation) for ALL compositor properties
- Compositor-tainted element tracking prevents pool crashes

The design quality is real. Immutable record types, composition operators,
proper lifecycle wiring in mount/update/unmount, ThreadStatic ambient
propagation with correct nesting and async-boundary persistence. The API now
delivers what it promises — `.Focused(borderBrush: focusBrush)` works,
`.Transition(Transition.Fade | Transition.Scale(0.85f))` runs the exit, and
`WithAnimation` animates all five compositor properties.

The grade is B-, not B, because the compositor-property ceiling is real and
structural. The system can only animate Opacity, Scale, Rotation, Translation,
CenterPoint, and 3 brush swaps. Width, Height, CornerRadius, Margin, FontSize,
arbitrary brush colors — none of these animate. SwiftUI and Compose animate
any value. Duct has an increasingly sophisticated system for animating the same
narrow set of properties. This isn't a Duct design failure — it's a WinUI
platform constraint — but it's the ceiling that keeps the grade below B.

Additionally, per-frame animation hooks (`UseAnimation`, `UseSpring`) still
don't exist, connected animations still use string keys, and there's no
cross-element sequencing/orchestration beyond stagger. These are real gaps, but
they're design omissions rather than broken promises — the system does what it
says it does.

---

## 11. Accessibility

### The accessibility story: from credible foundation to a real system (with limits)

The previous version of this review graded accessibility at C+ — 16 modifiers
and 12 UIA tests, but "the harder problems — hooks, diagnostics, custom
automation peers, and focus management — remain unbuilt." A focused accessibility
diff has landed that attacks three of those four gaps directly: SemanticPanel +
`.Semantics()` modifier for custom automation peers, AccessibilityScanner for
runtime WCAG diagnostics, 3 Roslyn analyzers (DUCT_A11Y_001/002/003) for
compile-time checks, and UseFocusTrap for modal focus management. Additionally,
LabeledBy is now wired (with deferred resolution), ElementPool clears 14 UIA
properties on return to prevent stale state, Heading/SubHeading DSL functions
auto-set HeadingLevel, and a dedicated a11y-showcase sample demonstrates all
features. The honest assessment now: **accessibility has crossed from "annotations
on primitives" to a genuine accessibility system with semantic, diagnostic, and
behavioral layers — but the SemanticPanel approach has real architectural costs,
the scanner is runtime-only, UseFocusTrap doesn't cycle, and several imperative
hooks remain unbuilt.**

### What shipped

**16 first-class accessibility modifiers across two storage tiers:**

| Property | Modifier | Tier |
|---|---|---|
| AutomationProperties.Name | `.AutomationName()` | Tier 1 (inline) |
| AutomationProperties.AutomationId | `.AutomationId()` | Tier 1 (inline) |
| AutomationProperties.HeadingLevel | `.HeadingLevel()` | Tier 1 (inline) |
| Control.IsTabStop | `.IsTabStop()` | Tier 1 (inline) |
| Control.TabIndex | `.TabIndex()` | Tier 1 (inline) |
| UIElement.AccessKey | `.AccessKey()` | Tier 1 (inline) |
| AutomationProperties.HelpText | `.HelpText()` | Tier 2 (lazy) |
| AutomationProperties.FullDescription | `.FullDescription()` | Tier 2 (lazy) |
| AutomationProperties.LandmarkType | `.Landmark()` | Tier 2 (lazy) |
| AutomationProperties.AccessibilityView | `.AccessibilityView()` | Tier 2 (lazy) |
| Shorthand: hide from AT | `.AccessibilityHidden()` | Tier 2 (lazy) |
| AutomationProperties.IsRequiredForForm | `.Required()` | Tier 2 (lazy) |
| AutomationProperties.LiveSetting | `.LiveRegion()` | Tier 2 (lazy) |
| AutomationProperties.PositionInSet/SizeOfSet | `.PositionInSet()` | Tier 2 (lazy) |
| AutomationProperties.Level | `.HierarchyLevel()` | Tier 2 (lazy) |
| AutomationProperties.ItemStatus | `.ItemStatus()` | Tier 2 (lazy) |
| AutomationProperties.LabeledBy | `.LabeledBy()` | Tier 2 (lazy) — **defined but not reconciler-applied** |
| UIElement.TabFocusNavigation | `.TabNavigation()` | Tier 2 (lazy) |
| Custom AutomationPeer (composite) | `.Semantics()` | **NEW** — SemanticPanel wrapper |

**Lazy sub-record architecture.** Tier 1 properties (HeadingLevel, IsTabStop,
TabIndex, AccessKey) are stored inline on `ElementModifiers` — zero allocation
overhead for elements that don't use them. Tier 2/3 properties live in a
separate `AccessibilityModifiers` record that is only allocated when an advanced
modifier is first applied. A `ModifyA11y()` helper merges sub-records
automatically, and the developer sees a completely flat API surface — all
modifiers look identical at the call site:

```csharp
Button("Search", doSearch)
    .AutomationName("Search documents")
    .AccessKey("S")               // Tier 1 — inline
    .HelpText("Search all files") // Tier 2 — lazy sub-record
    .LiveRegion()                 // Tier 2 — same flat API
```

**12 end-to-end UIA tests via Appium/WinAppDriver.** These are real
out-of-process tests that read properties through the Windows UI Automation
client API — the same pipeline used by Narrator, NVDA, and automated testing
tools. Each test maps to a specific WCAG 2.1 success criterion:

| Test | WCAG | Validates |
|---|---|---|
| `A11y_1_1_1_IconButtonHasAccessibleName` | 1.1.1 | Name on icon-only buttons |
| `A11y_1_1_1_DecorativeImageHiddenFromUIA` | 1.1.1 | AccessibilityView.Raw hides decorative elements |
| `A11y_1_3_1_HeadingLevelsExposed` | 1.3.1 | HeadingLevel (Level1, Level2) |
| `A11y_1_3_1_LandmarksExposed` | 1.3.1 | Navigation & Main landmarks |
| `A11y_1_3_1_FormFieldRequired` | 1.3.1 | IsRequiredForForm |
| `A11y_1_3_1_HierarchyLevels` | 1.3.1 | Level property for tree structures |
| `A11y_2_1_1_AccessKeysExposed` | 2.1.1 | Access key shortcuts (Alt+F, Alt+E) |
| `A11y_3_3_2_FormFieldHasNameAndHelpText` | 3.3.2 | Name + HelpText on form fields |
| `A11y_3_3_2_FullDescriptionExposed` | 3.3.2 | FullDescription for complex elements |
| `A11y_4_1_2_ItemStatusExposed` | 4.1.2 | ItemStatus announcements |
| `A11y_4_1_2_PositionInSetExposed` | 4.1.2 | PositionInSet / SizeOfSet |
| `A11y_4_1_3_LiveRegionPolite` | 4.1.3 | Live region (Polite mode) |
| `A11y_4_1_3_LiveRegionAssertive` | 4.1.3 | Live region (Assertive mode) |

**Reconciler integration with change detection.** `ApplyAccessibilityModifiers()`
in the reconciler compares each property against the previous value before
calling the WinUI `AutomationProperties.Set*()` methods. This avoids redundant
COM interop calls on re-render, following the same pattern used for other
modifiers.

### What shipped in the accessibility improvements diff

**1. SemanticPanel + `.Semantics()` modifier — custom automation peers for
composites.** This is the single most impactful accessibility feature. Duct
components are C# records that can't override `OnCreateAutomationPeer()` on
WinUI Controls. `SemanticPanel` is a lightweight WinUI `Panel` that does override
it, providing a custom `SemanticPanelAutomationPeer` that exposes:
- Semantic role (mapped to `AutomationControlType`)
- Semantic value (exposed via `IValueProvider`)
- Range value, min, max (exposed via `IRangeValueProvider`)

```csharp
// A star rating built from Image elements can now tell screen readers
// "I am a slider with value 3 of 5 stars"
StarRating(value: 3, max: 5)
    .Semantics(role: "slider", value: "3 of 5 stars",
               rangeValue: 3, rangeMin: 0, rangeMax: 5)
```

The `.Semantics()` extension method wraps the target element in a `SemanticElement`
record, which the reconciler mounts as a `SemanticPanel` containing the child.
The panel uses single-child passthrough layout (measure/arrange delegates to the
child), so the visual appearance is unchanged.

**2. AccessibilityScanner — runtime WCAG 2.1 diagnostic scanner.** A post-
reconciliation scanner that walks the Duct virtual element tree and produces
structured diagnostics. Each diagnostic includes:
- Diagnostic ID (e.g., "A11Y_001"), severity, message
- WCAG 2.1 criterion reference
- Element type, AutomationId, component type
- Fix suggestion (which modifier to add, suggested value, code snippet)
- Rich context (parent names, nearest heading, sibling texts, child content)

The context harvesting is designed for AI-agent consumption: an agent can
generate semantically correct fixes from the JSON export alone, without
re-reading source code. The scanner is intended for DEBUG builds only, accessed
via `DuctHostControl.EnableAccessibilityDiagnostics`.

**3. Three Roslyn accessibility analyzers (DUCT_A11Y_001/002/003).**

| Analyzer | Severity | Detects | Suggests |
|---|---|---|---|
| DUCT_A11Y_001 | Warning | `Button(icon, action)` where icon is not a string literal, missing `.AutomationName()` | Add `.AutomationName()` |
| DUCT_A11Y_002 | Warning | `Image(source)` without `.AutomationName()` or `.AccessibilityHidden()` | Add alt text or mark decorative |
| DUCT_A11Y_003 | Warning | Interactive elements (CheckBox, ToggleSwitch, Slider, ComboBox) without `.AutomationName()` | Add `.AutomationName()` |

Each has matching unit tests via `CSharpAnalyzerVerifier`. The analyzers walk
the fluent modifier chain upward from the call site to check for the presence of
`.AutomationName()` or `.AccessibilityHidden()`.

**4. UseFocusTrap hook — modal focus management.**

```csharp
var trap = UseFocusTrap(isActive: isDialogOpen);
Dialog(content).Set(el => trap.SetContainer(el))
```

`FocusTrapHandle` hooks into the `LosingFocus` event on the container element.
When active, it cancels any focus change that would move focus outside the
container by checking `IsDescendantOf(newFocus, container)` via a visual tree
walk. The hook activates/deactivates reactively based on the `isActive` flag.

**5. LabeledBy deferred resolution.** Previously, `LabeledBy` was defined in
`AccessibilityModifiers` but `ApplyAccessibilityModifiers()` had no code to
apply it. Now the reconciler resolves `LabeledBy` by finding the target element
via `FindByAutomationId()`. During mount, the target may not be in the visual
tree yet (XamlRoot is null), so resolution defers to the `Loaded` event:

```csharp
// If target isn't found at mount time, defer to Loaded
fe.Loaded += OnLoaded; // resolves FindByAutomationId in Loaded handler
```

**6. ElementPool a11y cleanup.** The pool now clears all 14 UIA properties
(`Name`, `AutomationId`, `HelpText`, `FullDescription`, `LandmarkType`,
`AccessibilityView`, `IsRequiredForForm`, `LiveSetting`, `PositionInSet`,
`SizeOfSet`, `Level`, `ItemStatus`, `LabeledBy`, `HeadingLevel`) plus
`AccessKey` on pool return. This prevents stale accessibility state from
leaking across element reuse.

**7. Heading/SubHeading DSL auto-set HeadingLevel.** The `Heading()` factory
now sets `AutomationHeadingLevel.Level1` and `SubHeading()` sets `Level2`
automatically. Previously, heading structure required explicit
`.HeadingLevel()` modifiers.

**8. a11y-showcase sample app.** A dedicated accessibility sample demonstrating
all features: SemanticPanel usage, scanner output, form patterns, heading
structure, landmarks, live regions.

**9. New tests (1,450+ lines).** `AccessibilityScannerTests` (324 lines),
`AccessibilityAnalyzerTests` (249 lines), `A11yShowcaseScannerTest` (185 lines),
`AccessibilityInteractionTests` (298 lines, E2E via Appium), plus selfhost
fixtures and navigation stress tests.

### What's actually good about this (credit where due)

The tiered storage design is smart engineering. Most elements in a typical UI
need zero accessibility annotations (WinUI's built-in automation peers handle
the basics). The few that need annotations usually need only Tier 1 (a heading
level, a tab stop). The rare elements that need advanced annotations (landmarks,
live regions, position-in-set) get a lazy sub-record. This means the common
case (no a11y modifiers) pays zero cost, the typical case (one or two modifiers)
pays minimal cost, and only the advanced case allocates the sub-record. This is
better than a flat struct with 16 nullable fields on every element.

The E2E test approach is genuinely rigorous. Testing through the real UIA
pipeline (out-of-process via WinAppDriver) validates what assistive technology
actually sees, not what the framework thinks it set. This is a higher bar than
React's `eslint-plugin-jsx-a11y` (which checks markup, not runtime behavior) or
SwiftUI's accessibility inspector (which is a developer tool, not a CI test).
If these tests pass, Narrator will actually read the right values. That matters.

The WCAG criterion mapping in the tests is good practice. Each test says
*which* accessibility requirement it validates. This makes it possible to answer
"do we cover WCAG 1.3.1?" by grepping the test file rather than reading
implementation code.

**SemanticPanel solves the right problem with the right WinUI mechanism.** The
previous review called custom automation peers "architecturally blocked" because
Duct components can't override `OnCreateAutomationPeer()` on Controls. The
`SemanticPanel` solution is elegant: a real WinUI `Panel` subclass that *can*
override `OnCreateAutomationPeer()`, wrapping the composite component's content.
The panel delegates layout to its single child (passthrough measure/arrange), so
it's visually transparent. The custom peer implements `IRangeValueProvider` and
`IValueProvider`, which are the two most common patterns for composite widgets
(sliders, ratings, progress indicators, gauges). SwiftUI's
`.accessibilityRepresentation {}` and Compose's `Modifier.semantics { role =
Role.Slider }` solve the same problem — SemanticPanel is Duct's answer, and
it works within WinUI's automation peer model rather than fighting it.

**The three-layer diagnostic approach (compile-time + runtime + E2E) is now
comprehensive.** Roslyn analyzers catch missing `AutomationName` at edit time.
The `AccessibilityScanner` catches issues at runtime after reconciliation. The
Appium UIA tests validate that properties actually reach assistive technology.
This is a defense-in-depth strategy that covers the full development lifecycle.
No other C# UI framework has all three layers — WinUI has Accessibility Insights
(external tool), but nothing at compile time or framework-integrated at runtime.

**Auto-setting HeadingLevel on Heading/SubHeading is a pit-of-success design.**
Every `Heading("Title")` now automatically exposes as Level1 to screen readers.
This means developers who use the semantic DSL functions get correct heading
structure for free. The alternative — requiring `.HeadingLevel(Level1)` on every
heading — was an opt-in model that most developers would skip.

**UseFocusTrap addresses a real production need.** Modal dialogs that don't
trap focus are a WCAG 2.4.3 violation and a screen reader usability disaster.
The `LosingFocus` event approach is the correct WinUI mechanism — it fires
before focus actually moves, and `Cancel` prevents the move. This is better
than the alternatives (keyboard event interception, which misses programmatic
focus changes).

### What's still concerning (skeptic's view)

**1. SemanticPanel adds yet another invisible wrapper element.** This is the
fourth invisible wrapper in the stack (component Border, NavigationHost Grid,
CommandHost Grid, and now SemanticPanel). A composite component with semantic
annotations inside a navigation host produces: NavigationView > ... > Border
(component) > Grid (nav host) > SemanticPanel > content. Each extra layout
container costs measure/arrange time. SemanticPanel uses passthrough layout
(delegates to single child), which minimizes the cost, but it's still a real
WinUI element in the visual tree.

The alternative — having the reconciler set automation properties directly on
the child's WinUI control without a wrapper — would avoid this overhead. But
WinUI's automation peer model is per-control (only the control that overrides
`OnCreateAutomationPeer()` gets the custom peer), so the wrapper is
architecturally necessary. This is an honest trade-off, not a design mistake.

**2. SemanticPanel only supports Value and RangeValue patterns.** WinUI's
automation system has ~20 control patterns (`ISelectionProvider`,
`IToggleProvider`, `IExpandCollapseProvider`, `IScrollProvider`,
`IInvokeProvider`, etc.). SemanticPanel implements `IValueProvider` and
`IRangeValueProvider`. A custom tree control that needs to expose
`IExpandCollapseProvider`, or a custom multi-select widget that needs
`ISelectionProvider`, can't use SemanticPanel. The two patterns cover the
most common composite widgets (sliders, ratings, gauges), but the long tail
of automation patterns is unaddressed.

SwiftUI's `.accessibilityRepresentation {}` lets you provide an arbitrary
native view as the accessibility representation. Compose's `Modifier.semantics
{}` supports arbitrary role, state, and action descriptions. Both are open
systems. SemanticPanel is closed to two patterns — it's a targeted solution,
not a general one.

**3. The semantic role is a string, not an enum.** `.Semantics(role: "slider")`
uses a bare string for the role. A typo like `"slidre"` silently maps to
`AutomationControlType.Custom` (the fallback in `MapRoleToControlType`). There
are ~40 `AutomationControlType` values; SemanticPanel's string-to-enum mapping
covers a subset. An enum-based API (e.g., `SemanticRole.Slider`) would provide
compile-time safety and IntelliSense discoverability. The string approach is
more flexible but loses the type safety that Duct prizes elsewhere.

**4. AccessibilityScanner is runtime-only and DEBUG-only.** The scanner walks
the element tree after reconciliation and produces diagnostics. But it requires
actually rendering the UI — you can't scan a component's accessibility from a
unit test without a WinUI host. The Roslyn analyzers (DUCT_A11Y_001/002/003)
cover the compile-time case, but they only check for missing `AutomationName`
on three specific patterns (icon buttons, images, interactive elements). The
scanner catches more issues (heading structure, landmark coverage, form
labeling) but only at runtime. For comparison, React's `eslint-plugin-jsx-a11y`
catches a broader set of issues at edit time without running the app.

**5. UseFocusTrap traps but doesn't cycle.** The `LosingFocus` handler cancels
focus changes that would leave the container, which prevents Tab from escaping
a modal dialog. But when the user tabs past the last focusable element in the
trap, focus simply stays on that element — it doesn't wrap to the first
focusable element. The WAI-ARIA Dialog Pattern specifies that Tab should cycle
within the dialog. React Focus Lock, the de facto React modal focus library,
cycles Tab and Shift+Tab. Duct's trap prevents escape but doesn't implement
the full cycling behavior that accessibility guidelines expect.

Additionally, `UseFocusTrap` requires `.Set(el => trap.SetContainer(el))` to
wire the container — the escape hatch is needed to connect the hook to the
DOM. A first-class integration (e.g., a modifier or a `FocusTrapHost` element)
would be more consistent with the framework's declarative model.

**6. Remaining imperative accessibility hooks are still unbuilt.** UseFocusTrap
is the first imperative accessibility hook. But the accessibility design doc
specifies several more:

- `UseAnnounce()` — triggering a live-region announcement from code (e.g., "3
  items deleted") without needing a visible element
- `UseReducedMotion()` — respecting user's motion preferences
- `UseScreenReaderActive()` — adapting UI when a screen reader is running

`UseHighContrast()` is partially covered by `UseColorScheme()` (returns
`ColorScheme.HighContrast`), though that hook still reads app-level theme, not
element effective theme (see Section 6 critique). A developer who needs to
announce a toast message to screen readers today has no option except `.Set()`
on a hidden live-region element.

**7. DUCT_A11Y analyzer coverage is narrow.** DUCT_A11Y_001 only matches
`Button` by identifier name, not by type resolution — if someone writes
`var b = Button(icon, action)` and then chains modifiers on `b` in a
separate statement, the analyzer misses it. DUCT_A11Y_001 doesn't check
`AppBarButton`, `ToggleButton`, `SplitButton`, or `RepeatButton` — all of
which can be icon-only. DUCT_A11Y_003 checks `CheckBox`, `ToggleSwitch`,
`Slider`, `ComboBox` but misses `RadioButton`, `TextBox`, `PasswordBox`,
`AutoSuggestBox`. The analyzers are a start, but the false negative rate
will be high for real codebases that use the full control surface.

**8. Focus management beyond trapping is still missing.** UseFocusTrap
addresses one focus scenario (modal dialogs). But there's still no:

- Programmatic focus control (`FocusRequester` / `@FocusState` equivalent)
- `XYFocusUp/Down/Left/Right` for directional D-pad/gamepad navigation
- Focus restoration on back-navigation

SwiftUI's `@FocusState` + `.focused()` and Compose's `FocusRequester` +
`Modifier.focusable()` both provide programmatic focus management. Duct covers
Tab order and trapping but not the programmatic side.

**9. E2E interaction tests have improved but still have gaps.** The new
`AccessibilityInteractionTests` (10 methods via Appium) test keyboard
navigation, live regions, headings, and semantic panels through the UIA
pipeline. This addresses the previous critique that "the test suite validates
modifiers but not interaction patterns." However, the tests still don't cover:

- Focus restoration after dialog dismiss
- High contrast rendering (are all elements visible in HC mode?)
- Reduced motion behavior
- SemanticPanel range value interaction (can AT change the value?)

**10. The original showcase apps still don't demonstrate accessibility.** The
Outlook clone, file manager, registry editor, and word puzzle game still don't
use any accessibility modifiers. The new a11y-showcase sample exists, but it's
an isolated demo — not a proof that accessibility composes naturally in a real
app. When the framework's most complex apps don't use heading structure,
landmarks, or live regions, it signals that accessibility is still an add-on,
not a default. The Heading/SubHeading auto-HeadingLevel fix helps — apps using
those DSL functions get heading structure for free — but that's a narrow
improvement, not a systemic adoption of accessibility in the showcase apps.

### Revised accessibility verdict

**Previously: D- → C+. Now: B-.**

The improvement from C+ to B- is earned by addressing the three hardest
critiques from the previous review:

1. **Custom automation peers were "architecturally blocked."** SemanticPanel
   provides a working solution — composite components can now describe their
   role, value, and range to screen readers. The solution adds a wrapper
   element and only covers two automation patterns (Value, RangeValue), but
   it's a real answer to the hardest problem.

2. **"No accessibility diagnostics or linting."** Three Roslyn analyzers
   provide compile-time checks. AccessibilityScanner provides runtime WCAG
   diagnostics with structured JSON output. Between them, the development
   lifecycle has coverage from edit-time to runtime.

3. **"No accessibility hooks."** UseFocusTrap is the first imperative
   accessibility hook. It handles the most critical scenario (modal dialogs)
   but needs cycling behavior and better integration with the declarative model.

The grade is B-, not B, because: SemanticPanel is closed to two patterns (the
competition's semantic systems are open), the semantic role is stringly-typed,
UseFocusTrap doesn't cycle (WAI-ARIA Dialog Pattern expects wrapping), the
remaining imperative hooks (UseAnnounce, UseReducedMotion, UseScreenReaderActive)
are still unbuilt, the analyzers have narrow coverage, and the showcase apps
still don't use accessibility features.

The competition gap has narrowed meaningfully. SwiftUI's
`.accessibilityRepresentation {}` is still more flexible than SemanticPanel
(arbitrary view as accessibility proxy vs. two fixed patterns). Compose's
`Modifier.semantics {}` supports arbitrary roles, states, and actions. React
has ARIA on DOM elements plus a mature lint ecosystem. But Duct now has
something at each layer: annotations (16 modifiers), semantics (SemanticPanel),
behavior (UseFocusTrap), compile-time diagnostics (3 analyzers), and runtime
diagnostics (AccessibilityScanner). The coverage is uneven — some layers are
thin — but the shape of a complete accessibility system is visible for the first
time.

---

## 12. Input Handling and Events

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

**3. Commanding exists but doesn't cover all input surfaces.** The new
commanding system (Section 8) closes the P0 gap — `DuctCommand` bundles
execute + canExecute + metadata, and `UseCommand` handles async lifecycle. But
the commanding system only integrates with `Button`, `AppBarButton`, and
`MenuItem`. Other command-capable controls (`SplitButton`, `SwipeItem`,
`ContentDialog` actions) still use bare `Action` callbacks. And the absence of
command routing to the focused view (Section 8, critique #3) means Cut/Copy/
Paste in multi-panel apps still requires manual wiring.

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

## 13. Developer Experience

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

## 14. The .Set() Problem

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
- Drag and drop
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

## 15. Component-by-Component Scorecard

How Duct's individual feature areas compare to the mature frameworks (React,
SwiftUI, Compose).

| Feature Area | Duct | React | SwiftUI | Compose | Notes |
|---|---|---|---|---|---|
| **Component Model** | B+ | A | A | A | Context, memoization, generic hooks; reflection in memo check, no slots |
| **Local State** | A- | A | A | A | Generic hooks, post-render cleanup, persisted state; dep arrays still box |
| **Global State** | B+ | A | A | A | DuctContext + UseContext + .Provide(); boxing in scope stack, no selector |
| **Reconciler** | B- | A | A- | A | Works but monolithic, no concurrent mode |
| **Layout** | B+ | B+ | A | A | Flex is good; Grid is stringly-typed |
| **Theming** | B- | B+ | A | A | Style caching fixes XamlReader.Load perf; RequestedTheme modifier; UseColorScheme hook (reads app theme, not element effective); 3 ThemeRef props; no custom resources |
| **Navigation** | B+ | A | A | A | Type-safe routes, dev-owned stack, GPU transitions, source+destination guards, caching, serialization, enhanced deep linking (query strings, wildcards, optional params), diagnostics; ConnectedTransition is stub, no adaptive multi-pane, E2E now running (43 Appium tests) |
| **Commanding** | B+ | N/A | C+ | N/A | Define-once commands, 16 standard, async lifecycle, focus-scoped accelerators; no competitor has this. Accelerator rebuild per render, labels not localized, no command routing, no palette UI |
| **Lists/Collections** | B | B+ | A | A | Virtualization exists, no sections |
| **Animation** | B- | B | A | A | Curve DSL, enter+exit, interaction states (hover/press/focus), keyframes, stagger+enter integration, scroll-linked, WithAnimation (all 5 props); 4 prior bugs fixed; compositor-property-bound ceiling; no per-frame hooks |
| **Accessibility** | B- | B | A | A | 16 modifiers + SemanticPanel (custom peers for composites), UseFocusTrap hook, 3 Roslyn a11y analyzers, runtime WCAG scanner, 22+ E2E tests; SemanticPanel limited to 2 patterns, focus trap doesn't cycle, remaining hooks unbuilt |
| **Input/Events** | C | B | A | A | Semantic events good; commanding helps but no gesture system, no pointer enter/exit, rest is .Set() |
| **Styling** | B- | B+ | A | A | Lightweight styling is a genuine differentiator; ResourceBuilder fluent API; 3 Roslyn analyzers; stringly-typed resource keys; analyzer coverage shallow |
| **Developer Experience** | C+ | A | B+ | B+ | Hot reload works; preview is screenshot-only; no devtools |
| **Control Coverage** | A | N/A | A | A | 94% of WinUI wrapped |
| **Error Handling** | B | B+ | D | D | ErrorBoundary exists (rare feature) |
| **Localization** | B+ | B | B | B | ICU-based, full system, now using DuctContext |
| **Responsive Layout** | B | B+ | A | A | Hooks work but force full re-render |

---

## 16. Conclusion

### The sample apps are telling — and the story has shifted again

Duct now has eight sample apps: the original four (Outlook clone, file manager,
registry editor, word puzzle game) plus NavigationDemo, CommandingDemo,
StylingGallery, and the new a11y-showcase. Additionally, the WinFormsInterop
sample demonstrates bidirectional hosting. The new samples demonstrate their
respective features comprehensively: NavigationDemo covers routes, guards
(including the new destination-side guards), transitions, caching, deep linking,
and nested navigation. CommandingDemo covers standard commands, async lifecycle,
parameterized commands, focus-scoped accelerators, per-site overrides, and
context-based command sharing. StylingGallery demonstrates theme tokens,
RequestedTheme, UseColorScheme, lightweight styling, and style caching.
a11y-showcase demonstrates SemanticPanel, AccessibilityScanner, form patterns,
heading structure, landmarks, and live regions.

But the original showcase apps remain frozen in time:
- **Outlook clone** still uses string-based view switching (`currentPage switch
  { ... }`) — it hasn't adopted the navigation system that was built to solve
  exactly this problem
- **DuctFiles** still requires manual `SynchronizationContext` capture for
  off-thread state updates — no async state management
- **Samples that use hard-coded colors** are still broken in dark mode
- **None of the original samples** use the context system, memoization,
  persisted state, commanding, navigation, SemanticPanel, or the accessibility
  modifiers

The positive signal: the Heading/SubHeading DSL change auto-sets HeadingLevel,
so any showcase app that uses `Heading()` now gets correct a11y heading structure
for free — a systemic improvement that doesn't require per-app adoption.

But the fundamental pattern remains: every new feature ships with its own
isolated demo app, while the showcase apps — the ones that prove the framework
works for *real* UIs — don't adopt the new features. The Outlook clone is the
most telling case: it's the framework's most complex app, it has the navigation
problem *and* the accessibility problem *and* the commanding problem, and all
three systems were explicitly designed to solve those problems. It's still using
`UseState<string>` for navigation.

The gap between "feature works in isolation" and "feature works in a real app"
is where production confidence lives. Duct keeps building features and demo
apps without going back to prove they compose in the existing showcase apps.
This is a red flag for a framework that wants to be production-ready.

### What Duct gets right

1. **The component model foundation is now solid.** Context, memoization, generic
   hooks, persisted state, post-render effect cleanup — these are the core
   mechanics that every declarative UI framework needs, and they're now
   implemented with correct semantics.

2. **Navigation is architecturally competitive and maturing.** Type-safe routes
   via C# records, developer-owned back stack, composition-layer GPU transitions,
   source + destination lifecycle guards with cancellation, LRU caching, state
   serialization, enhanced deep linking (query strings, optional params,
   wildcards), and NavigationDiagnostics for observability. The design
   independently converged with Compose Nav3's philosophy and is arguably
   stronger on type safety and transitions.

3. **Commanding is a genuine differentiator.** No competing declarative framework
   provides define-once commands with metadata bundling, standard commands, async
   lifecycle, and focus-scoped accelerators. Duct is ahead of the field here.

4. **Control coverage is impressive.** 94% of WinUI controls wrapped with
   clean factory APIs. This is a huge amount of tedious work done well.

5. **The hooks system is faithful to React and now correctly implemented.**
   UseState, UseReducer, UseEffect, UseMemo, UseRef, UseContext, UseNavigation,
   UseCommand, UsePersisted — the hook surface area has grown meaningfully and
   the React mental model transfers cleanly.

6. **ErrorBoundary exists.** Neither SwiftUI nor Compose has this. Duct's error
   boundary is a genuine differentiator for resilient UIs.

7. **FlexPanel is ambitious and useful.** A full Flexbox implementation on WinUI
   provides layout capabilities that WinUI itself doesn't have.

8. **Type safety over XAML.** No more binding errors, DataContext confusion, or
   resource-not-found runtime failures. The C# compiler catches real mistakes.
   Navigation and commanding extend this further — routes are types, commands
   are records, everything is compiler-checked.

9. **Lightweight styling surfaces a WinUI capability no competitor wraps.**
   `ResourceBuilder` provides ergonomic access to WinUI's per-control resource
   key overrides with proper cleanup, managed-key tracking, and ThemeRef
   integration. No other C# declarative framework does this.

10. **Accessibility now has a layered system, not just annotations.** SemanticPanel
    solves the hardest problem (custom automation peers for composites). Three
    Roslyn analyzers and AccessibilityScanner provide compile-time and runtime
    diagnostics. UseFocusTrap handles modal focus. Auto-HeadingLevel on
    Heading/SubHeading is a pit-of-success design. No other C# UI framework
    has this combination of semantic, diagnostic, and behavioral accessibility
    layers.

11. **Observable interop.** UseObservable, UseObservableTree, UseObservableProperty,
    and UseCollection bridge cleanly to MVVM. Essential for incremental adoption
    in existing WinUI codebases.

12. **WinForms interop opens a brownfield migration path.** `XamlIslandControl`
    lets WinForms apps host Duct/WinUI content in specific panels — the
    incremental adoption story that enterprise apps need. E2E tests validate
    Tab navigation, rendering, and accessibility across the WinForms/WinUI
    boundary.

13. **The localization system validates the framework's own abstractions.** The
    migration of LocaleProvider to `DuctContext<IntlAccessor?>` proves the context
    system works for real cross-cutting concerns. Navigation uses DuctContext for
    sharing handles. Commanding can use DuctContext for sharing commands. When
    multiple framework features build on the same primitive, that's good
    architecture.

### What prevents Duct from being production-ready

1. **The showcase apps don't use the framework's own features.** This is the
   most damning critique I can level. Navigation, commanding, context, memoization,
   persisted state — none of these are used in the Outlook clone, file manager,
   registry editor, or word puzzle game. Each feature has its own isolated demo
   app, but nobody has proven they compose in a complex real-world UI. Until the
   Outlook clone navigates with `UseNavigation`, uses `DuctContext` for session
   state, and surfaces Cut/Copy/Paste through `StandardCommand`, the framework's
   production-readiness is theoretical.

2. **Theming has improved but the pieces don't compose.** Style caching fixes
   performance. Lightweight styling is a genuine differentiator. But
   `UseColorScheme` reads the app-level theme instead of the element's effective
   theme, and `{ThemeResource}` in dynamically-loaded Styles doesn't respect
   per-element `RequestedTheme`. The two headline features — RequestedTheme
   modifier and UseColorScheme hook — were designed for the "dark sidebar"
   scenario and don't work together correctly in that exact scenario. Custom
   branded theme resources still don't exist.

3. **Accessibility has real breadth but uneven depth.** SemanticPanel solves the
   hardest architectural problem (custom automation peers for composites), 3
   Roslyn analyzers and AccessibilityScanner provide diagnostics at compile-time
   and runtime, and UseFocusTrap is the first imperative hook. But SemanticPanel
   only covers 2 of ~20 automation patterns, UseFocusTrap doesn't cycle focus
   (WAI-ARIA Dialog Pattern expects wrapping), the remaining imperative hooks
   (UseAnnounce, UseReducedMotion) are unbuilt, and the semantic role is
   stringly-typed. The shape of a complete system is visible, but the layers are
   thin.

4. **Animation is now fully operational within its ceiling.** The four
   integration bugs from the previous review (dead exit transitions, unwired
   Focused state, WithAnimation only routing Opacity, stagger not integrating
   with enter transitions) are all fixed. Three additional runtime issues
   (async scope loss, Opacity routing for .Animate(), pool crash with
   compositor-tainted elements) are also resolved. The API now delivers what
   it promises. The remaining limitation is structural: the compositor-property
   ceiling (Opacity, Scale, Rotation, Translation, CenterPoint + 3 brush swaps)
   means Width, CornerRadius, Margin, FontSize, and arbitrary colors can't
   animate. This is a WinUI platform constraint, not a Duct design failure.

5. **.Set() still carries too much weight.** Navigation, commanding, and styling
   have all reclaimed meaningful surface area — `.RequestedTheme()` eliminates
   another `.Set()` workaround, and lightweight styling eliminates `.Set()`
   for per-control resource overrides. But gestures, pointer enter/exit,
   right-tap, double-tap, drag-and-drop, composition-layer effects, materials,
   and most windowing APIs still require `.Set()`. The abstraction is thicker
   than before but still not thick enough for a "you don't need to know WinUI"
   claim.

6. **Performance concerns accumulate (but one is fixed).** Style caching
   eliminates the XamlReader.Load-per-themed-element concern. But reflection-
   based ShouldUpdateWithProps (Section 2), boxing in context scope (Section 2),
   unbounded persisted state cache (Section 2), accelerator rebuild per render
   (Section 8), and invisible Border/Grid wrappers adding layout cost (Sections
   4, 7, 8) remain. The style caching fix shows the team responds to performance
   critiques, but no systematic profiling pass has occurred. No profiling tools
   exist to measure the remaining costs (Section 13).

7. **E2E test infrastructure has improved but revealed a process gap.** The
   discovery that 44/46 E2E tests were invisible behind a broken filter is both
   a fix and a warning. The fix: 43 Appium tests now run across 6 test classes,
   covering interactive controls, accessibility, accessibility interactions,
   events, data grid, and WinForms interop. The warning: these tests existed
   for the entire development period without anyone noticing they weren't
   running. How confident can we be in the rest of the CI pipeline? Commanding
   still has no integration tests.
8. **WinForms interop is promising but has a fundamental hosting asymmetry.**
   WinForms hosting Duct/WinUI works fully. Duct hosting WinForms is blocked
   in the Duct-primary scenario because WinUI windows use compositor-only
   rendering (`WS_EX_NOREDIRECTIONBITMAP`) — there's no GDI surface for Win32
   child HWNDs. This is a WinUI platform limitation, not a Duct design issue,
   but it limits the brownfield migration story to "WinForms-primary apps
   adopting Duct panels," not "Duct apps embedding legacy WinForms controls."

### The fundamental question (revisited)

Is Duct a *framework* or a *wrapper*?

The previous version of this review posed this question and concluded "moving
toward framework." That's still true, and the movement has accelerated. The
component model (context, hooks, memoization), navigation (type-safe routing,
developer-owned stack, transitions), and commanding (define-once commands,
standard commands, async lifecycle) are genuine framework-level abstractions.
A developer can now build a multi-page app with shared state, keyboard
shortcuts, and navigation transitions thinking entirely in Duct's model.

But the framework-vs-wrapper question has refined. Duct is no longer a wrapper
for the parts it covers. The problem is coverage: too many common scenarios
still require `.Set()`, and the features that exist haven't been stress-tested
in the framework's own showcase apps. The framework's ambition exceeds its
integration testing.

The competitive landscape has also shifted. When this review was first written,
Compose Navigation used strings and an opaque controller. Now Compose Navigation
3 (stable Nov 2025) uses developer-owned typed stacks — the same model Duct
chose. SwiftUI's NavigationStack is mature. React Router v7 has view transitions
and TanStack Router has full type inference. The window where "declarative
navigation for WinUI" was a novelty has closed; now it needs to be competitive
in quality, not just existence.

Duct's strongest position is commanding — it's genuinely ahead of the entire
industry. Its navigation is competitive and maturing (destination guards, deep
link enhancements, diagnostics). Its component model is solid. Animation has
crossed a threshold — the four integration bugs are fixed, all advertised
features now work, and the compositor-property ceiling is the only remaining
structural concern. Styling has made a significant jump: style caching fixes the
major perf concern, lightweight styling is a genuine differentiator, and Roslyn
analyzers provide static guidance — but the UseColorScheme/RequestedTheme
composition bug and missing custom theme resources keep it from being fully
competitive. Accessibility has made the largest single-iteration improvement:
SemanticPanel solves the hardest architectural problem, the three-layer
diagnostic approach (compile-time + runtime + E2E) is now more comprehensive
than any other C# UI framework, and UseFocusTrap is the first behavioral hook.
But the layers are still thin — SemanticPanel covers 2 of ~20 automation
patterns, and the remaining imperative hooks are unbuilt. WinForms interop
opens a migration path that didn't exist before. The `.Set()` surface area is
smaller but still too large.

### To become production-ready, Duct needs to:

1. **Adopt its own features in the showcase apps.** The Outlook clone should
   use UseNavigation, DuctContext, StandardCommand, SemanticPanel, and
   UsePersisted. This is the highest-leverage work — it proves composition and
   finds real bugs. The a11y-showcase is encouraging but isolated.
2. **Audit and harden the CI pipeline.** The E2E test filter bug (44 invisible
   tests) was a process failure, not a code failure. What other test
   configurations are silently broken? A CI health check that validates test
   counts, detects regressions in test discovery, and alerts on suspicious
   drops would prevent this class of issue.
3. **Do a performance pass.** Profile a real render cycle. Measure the cost of
   reflection-based ShouldUpdate, XamlReader.Load theming, accelerator rebuild,
   wrapper elements (now 4 types: Border, Grid×2, SemanticPanel), and the
   AccessibilityScanner overhead. Fix what's expensive.
4. **Localize StandardCommand labels.** The framework's own commanding system
   should use the framework's own localization system. This is embarrassing in
   its absence.
5. **Fix UseColorScheme to read element effective theme, not app theme.** This
   is a bug that undermines the RequestedTheme + UseColorScheme composition
   story. The fix is to read the mounted FrameworkElement's ActualTheme, not
   Application.Current.RequestedTheme.
6. **Finish the theming story.** Custom theme resource definitions, more than 3
   ThemeRef binding properties, type-safe resource key constants for lightweight
   styling.
7. **Add command routing to the focused view.** This is the missing piece that
   makes Cut/Copy/Paste work in multi-panel apps.
8. **Extend SemanticPanel to more automation patterns.** `IToggleProvider`,
   `IExpandCollapseProvider`, and `ISelectionProvider` are the next three most
   common patterns for composite widgets. Without them, custom toggle switches,
   collapsible sections, and multi-select widgets can't describe themselves.
9. **Fix UseFocusTrap cycling.** The current implementation traps focus but
   doesn't wrap Tab to the first/last element. The WAI-ARIA Dialog Pattern
   requires this. It's a small fix with big accessibility impact.

The trajectory is right, and the velocity is notable. In the last 24 hours
alone: SemanticPanel solves the hardest accessibility architecture problem,
AccessibilityScanner and 3 Roslyn analyzers add the diagnostic layers that were
entirely missing, UseFocusTrap is the first imperative accessibility hook,
destination-side navigation guards close a real gap, deep link enhancements
make the routing system more practical, the E2E test filter fix surfaces 43
previously invisible Appium tests, and WinForms interop opens a brownfield
adoption story.

Navigation and commanding moved Duct from "component library with hooks" to
"framework with application architecture." The accessibility diff moves the
story from "annotations on primitives" to "a system with semantic, diagnostic,
and behavioral layers." WinForms interop moves the adoption story from
"greenfield only" to "brownfield migration possible." These are real capability
expansions, not just polish.

But three themes keep recurring across every feature area:

1. **Wrapper element accumulation.** Component Borders, NavigationHost Grids,
   CommandHost Grids, and now SemanticPanels all add invisible layout containers
   to the WinUI visual tree. Each is individually justified, but together they
   compound — a well-structured Duct app can have 3-4 framework wrappers between
   a component and its content. This is a systemic tax that needs measurement.

2. **String-typed APIs at boundaries.** Routes are type-safe records, but deep
   link patterns are strings. SemanticPanel roles are strings. Resource keys in
   lightweight styling are strings. Connected animation keys are strings. The
   pattern: Duct has excellent type safety at the core and stringly-typed
   boundaries at the edges. Each individual instance is defensible (WinUI's
   APIs are strings, deep links are URIs, etc.), but the cumulative effect is
   that developers hit string-typing wherever they touch the framework's
   integration points with the platform or the outside world.

3. **The showcase apps remain the credibility gap.** Every new feature ships
   with its own isolated demo, proving the feature works in a clean context.
   But the Outlook clone — the framework's most complex and most public app —
   still doesn't use navigation, commanding, context, memoization, or the new
   accessibility features. Until it does, the claim "these features compose in
   real apps" is unproven.

Duct now has three features where it's genuinely ahead of the competition:
commanding (no competitor has define-once commands), lightweight styling (no
competitor surfaces WinUI's per-control resource overrides), and the
AccessibilityScanner (no other C# UI framework has framework-integrated
runtime WCAG diagnostics with structured AI-agent-friendly output). These
are real differentiators, not just catch-up. The gap between "features exist"
and "features compose correctly in real apps" is where the remaining work
lives. That gap is narrower than before — and shrinking with each iteration.
