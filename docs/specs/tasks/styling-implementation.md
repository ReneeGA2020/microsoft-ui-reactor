# Reactor Styling Enhancements — Implementation Plan

Execution plan for the 5 styling features defined in
[`docs/spec/duct-styling-design.md`](../spec/duct-styling-design.md).

Phases follow the spec's priority order (P0 items first). Each task is
independently checkable so work can pause and resume at any point.

---

## Phase 1: Style Caching (Proposal 5 — P0)

Zero API changes. Pure internal performance improvement — eliminate redundant
`XamlReader.Load()` calls by caching compiled `Style` objects keyed by their
target type + ThemeRef binding signature.

### 1.1 — Cache infrastructure
- [x] Add `private static readonly ConcurrentDictionary<string, Style> _styleCache`
      to `Reactor\Core\Reconciler.cs` (near existing `ApplyThemeBindings`)
- [x] Add `BuildCacheKey(string targetType, IReadOnlyDictionary<string, ThemeRef> bindings)`
      private method — format: `"TargetType|Prop1=Key1|Prop2=Key2"` with keys
      sorted by `StringComparer.Ordinal` for deterministic ordering
- [x] Unit test: `BuildCacheKey` produces identical keys regardless of input
      dictionary enumeration order

### 1.2 — Integrate cache into ApplyThemeBindings
- [x] Modify `ApplyThemeBindings()` (~line 1751–1810 in Reconciler.cs) to call
      `_styleCache.TryGetValue(cacheKey, ...)` before building XAML / calling
      `XamlReader.Load()`
- [x] On cache miss: build XAML, parse via `XamlReader.Load()`, store result
      with `_styleCache.TryAdd(cacheKey, style)`
- [x] On cache hit: skip XAML generation entirely and reuse the cached `Style`

### 1.3 — BasedOn wrapper for elements with existing styles
- [x] When `fe.Style` already exists and the cached style has the same
      `TargetType`, create a thin wrapper `Style` that chains `BasedOn` to the
      existing style and copies the cached setters — **do not mutate** the cached
      style's `BasedOn` (it is shared)
- [x] When no existing style, assign the cached `Style` directly to `fe.Style`
- [x] Unit test: verify BasedOn chain is preserved when element already has a
      style (e.g., implicit style from XAML)

### 1.4 — Cache invalidation on theme change
- [x] In `ReactorHost.AttachThemeListener` (~line 285–295 in ReactorHost.cs), call
      `Reconciler.ClearStyleCache()` (or equivalent) when `ActualThemeChanged`
      fires — this is conservative memory cleanup, not a correctness requirement
      since `{ThemeResource}` setters are live-resolved by WinUI
- [x] Expose a `static void ClearStyleCache()` method (or `internal`) on
      Reconciler for testability

### 1.5 — Validation
- [x] Benchmark: render a list of 100+ elements each with
      `Background(Theme.CardBackground)` — confirm `XamlReader.Load()` is called
      once, not 100 times (use a counter or `Debug.WriteLine` instrumentation)
- [x] Verify theme toggle (Light↔Dark) still works correctly after caching
- [x] Verify elements with and without pre-existing styles both render correctly

---

## Phase 2: RequestedTheme Modifier (Proposal 8A — P0)

Small API addition: fluent `.RequestedTheme()` modifier that maps directly to
`FrameworkElement.RequestedTheme`, applied **before** `ApplyThemeBindings()` so
ThemeRef setters resolve using the correct theme variant.

### 2.1 — ElementModifiers addition
- [x] Add `public ElementTheme? RequestedTheme { get; init; }` to the
      `ElementModifiers` record in `Reactor\Core\Element.cs` (~line 472–530)
- [x] Add merge logic in `ElementModifiers.Merge()` — `RequestedTheme` should
      use the last-set value (right-hand side wins)

### 2.2 — Extension method
- [x] Add `public static T RequestedTheme<T>(this T el, ElementTheme theme)`
      extension method to `Reactor\Elements\ElementExtensions.cs` that calls
      `Modify(el, new ElementModifiers { RequestedTheme = theme })`
- [x] Add XML doc comment with usage examples (dark sidebar, media controls)

### 2.3 — Reconciler integration
- [x] In `Reconciler.cs` `ApplyModifiers()` (or equivalent), add:
      `if (m.RequestedTheme.HasValue) fe.RequestedTheme = m.RequestedTheme.Value;`
- [x] Ensure this runs **before** `ApplyThemeBindings()` so ThemeRef bindings
      resolve against the correct theme
- [x] Verify ordering: add a comment in code explaining the ordering requirement

### 2.4 — Validation
- [x] Gallery sample: dark sidebar + light main content using `.RequestedTheme()`
- [x] Update any existing gallery code using `.Set(b => b.RequestedTheme = ...)`
      to use the new modifier
- [x] Verify that ThemeRef bindings inside a `RequestedTheme(Dark)` subtree
      resolve to the Dark theme variants, not the system theme
- [x] Verify that `ElementTheme.Default` restores system-theme inheritance

---

## Phase 3: UseColorScheme Hook (Proposal 6 — P0)

New reactive hook allowing components to observe the effective color scheme
(Light/Dark/HighContrast) at their position in the element tree.

### 3.1 — ColorScheme enum
- [x] Create `ColorScheme` enum in `Reactor\Core\` (or `Reactor\Hooks\`) with values:
      `Light`, `Dark`, `HighContrast`
- [x] Add XML doc comments explaining each value

### 3.2 — ColorSchemeContext
- [x] Create `internal class ColorSchemeContext` with:
  - [x] `public ColorScheme CurrentScheme { get; private set; }`
  - [x] `public void Update(ElementTheme actualTheme)` — maps
        `ElementTheme.Dark` → `Dark`, `ElementTheme.Light` → `Light`,
        `Default` → check `AccessibilitySettings.HighContrast` then fall back
        to `Light`
  - [x] `private static bool DetectHighContrast()` using
        `Windows.UI.ViewManagement.AccessibilitySettings`

### 3.3 — Hook extension methods
- [x] Add `public static ColorScheme UseColorScheme(this RenderContext ctx)`
      on `RenderContext` — reads the effective theme from the component's
      mounted `FrameworkElement.ActualTheme` (Option A from spec)
- [x] Add `public static bool UseIsDarkTheme(this RenderContext ctx)` —
      convenience wrapper returning `UseColorScheme() == ColorScheme.Dark`
- [x] Ensure the hook triggers re-render when the theme changes (leverages
      existing `AttachThemeListener` re-render mechanism)

### 3.4 — ReactorHost integration
- [x] In `ReactorHost.AttachThemeListener` (~line 285–295), update
      `ColorSchemeContext` when `ActualThemeChanged` fires
- [x] Verify that `RequestRender()` is called after the context update so
      components using `UseColorScheme()` see the new value

### 3.5 — RequestedTheme awareness
- [x] Verify that `UseColorScheme()` returns the **effective** theme at the
      component's mount point — if a parent has `RequestedTheme = Dark`, the
      hook should return `Dark` even when the system is in Light mode
- [x] This should work automatically via `FrameworkElement.ActualTheme` read,
      but add an explicit integration test to confirm

### 3.6 — Validation
- [x] Gallery sample: component that shows different icons for Light vs Dark
- [x] Gallery sample: component that adjusts opacity based on color scheme
- [x] Test: component inside `RequestedTheme(Dark)` subtree reports `Dark`
      even when system is Light
- [x] Test: High Contrast mode returns `ColorScheme.HighContrast`

---

## Phase 4: Lightweight Styling (Proposal 2 — P0)

New API surface exposing WinUI's lightweight styling (per-control resource
overrides) through an ergonomic fluent builder. This is Reactor's unique
competitive advantage — no other C# declarative framework surfaces this.

### 4.1 — ResourceBuilder type
- [x] Create `Reactor\Elements\ResourceBuilder.cs` with fluent API:
  - [x] `Set(string key, string color)` — parses via `BrushHelper.Parse()`
  - [x] `Set(string key, Brush brush)` — literal brush
  - [x] `Set(string key, ThemeRef themeRef)` — stores for reactive resolution
  - [x] `Set(string key, double value)` — for numeric resource keys
  - [x] `Set(string key, CornerRadius value)` — for corner radius keys
  - [x] `internal ResourceOverrides Build()` — returns immutable snapshot

### 4.2 — ResourceOverrides record
- [x] Create `ResourceOverrides` record:
      `record ResourceOverrides(IReadOnlyDictionary<string, object> Literals,
       IReadOnlyDictionary<string, ThemeRef> ThemeRefs)`
- [x] Ensure the record is immutable (uses `IReadOnlyDictionary`)

### 4.3 — Element base record addition
- [x] Add `public ResourceOverrides? ResourceOverrides { get; init; }` to the
      `Element` abstract record in `Reactor\Core\Element.cs`

### 4.4 — Extension method
- [x] Add `public static T Resources<T>(this T el, Action<ResourceBuilder> configure)`
      extension method to `ElementExtensions.cs`:
  - [x] Creates a new `ResourceBuilder`, calls `configure`, builds overrides
  - [x] Returns `el with { ResourceOverrides = builder.Build() }`
- [x] Add XML doc comments explaining lightweight styling, cascading behavior,
      and VisualStateManager integration

### 4.5 — Reconciler: ApplyResourceOverrides
- [x] Add `private static void ApplyResourceOverrides(FrameworkElement fe, ResourceOverrides overrides)`
      to `Reconciler.cs`:
  - [x] Ensure `fe.Resources ??= new ResourceDictionary()`
  - [x] Apply literal resources: `foreach (key, value) → fe.Resources[key] = value`
  - [x] Apply ThemeRef resources: resolve from `Application.Current.Resources`
        using `themeRef.ResourceKey`, then set `fe.Resources[key] = resolved`

### 4.6 — Reconciler: call site integration
- [x] In `Mount()`: after element creation, call `ApplyResourceOverrides()` if
      `element.ResourceOverrides is not null`
- [x] In `Update()`: call `ApplyResourceOverrides()` if overrides changed
- [x] Handle cleanup on update: when overrides change between renders, remove
      old keys from `fe.Resources` that are no longer present in the new overrides

### 4.7 — Resource cleanup on update
- [x] Track which keys were set by Reactor (vs. keys set by XAML or other sources)
      so that cleanup only removes Reactor-managed keys
- [x] Strategy: store a `HashSet<string>` of Reactor-managed keys on the element
      (e.g., via `Tag` or an attached property / side dictionary) and diff on update
- [x] When overrides are removed entirely (`ResourceOverrides` goes from non-null
      to null), remove all Reactor-managed keys from `fe.Resources`

### 4.8 — Theme change re-resolution
- [x] On theme change, ThemeRef-based resources must be re-resolved from
      `Application.Current.Resources` — verify this happens automatically via
      the existing re-render pipeline (which calls `Update()` → `ApplyResourceOverrides()`)
- [x] If not automatic, add explicit re-resolution in theme change handler

### 4.9 — Validation
- [x] Gallery sample: brand-colored button with hover/pressed state overrides
      (`ButtonBackground`, `ButtonBackgroundPointerOver`, `ButtonBackgroundPressed`)
- [x] Verify hover/pressed visual states respect the overrides (no custom
      template required)
- [x] Gallery sample: scoped overrides cascading from parent to child buttons
- [x] Test: theme change re-resolves ThemeRef overrides in `Resources()`
- [x] Test: literal color overrides persist across theme change
- [x] Test: removing `.Resources()` on update correctly cleans up old keys

---

## Phase 5: Roslyn Analyzers (Proposal 8B)

Separate analyzer project providing static analysis to guide developers toward
theme-reactive styling and the new fluent APIs.

### 5.1 — Project setup
- [x] Create `Reactor.Analyzers` project (Roslyn analyzer + code fix provider)
- [x] Add references to `Microsoft.CodeAnalysis.CSharp` and
      `Microsoft.CodeAnalysis.CSharp.Workspaces`
- [x] Set up NuGet packaging for the analyzer assembly
- [x] Add Roslyn test infrastructure (`Microsoft.CodeAnalysis.CSharp.Analyzer.Testing`)

### 5.2 — DUCT003: RequestedTheme Set → modifier
- [x] Implement `RequestedThemeSetAnalyzer` (DiagnosticAnalyzer):
  - [x] Detect `.Set(fe => fe.RequestedTheme = ...)` or
        `.Set(b => b.RequestedTheme = ElementTheme.Dark)` patterns
  - [x] Report `DUCT003` (Info severity): "RequestedTheme modifier available"
- [x] Implement `RequestedThemeSetCodeFix` (CodeFixProvider):
  - [x] Replace `.Set(fe => fe.RequestedTheme = ElementTheme.X)` with
        `.RequestedTheme(ElementTheme.X)`
- [x] Unit tests: verify detection and fix for various `.Set()` patterns

### 5.3 — DUCT001: hard-coded color → ThemeRef
- [x] Implement `UseThemeRefAnalyzer` (DiagnosticAnalyzer):
  - [x] Detect `.Background("...")`, `.Foreground("...")`, `.WithBorder("...")`
        calls where a string literal is used and a ThemeRef overload exists
  - [x] Report `DUCT001` (Warning severity): "Use ThemeRef instead of hard-coded
        color"
- [x] Implement `UseThemeRefCodeFix` (CodeFixProvider):
  - [x] Map common colors to semantic tokens:
    - `"#FFFFFF"` / `"white"` → `Theme.PrimaryBackground`
    - `"#000000"` / `"black"` → `Theme.PrimaryText`
    - `"#0078D4"` → `Theme.Accent`
  - [x] For unmapped colors: suggest generic message about ThemeRef usage
- [x] Unit tests: verify detection for known and unknown color strings
- [x] Unit tests: verify code fix replaces string with correct `Theme.X` token

### 5.4 — DUCT002: Set brush → lightweight styling
- [x] Implement `UseLightweightStylingAnalyzer` (DiagnosticAnalyzer):
  - [x] Detect `.Set()` callbacks that assign a brush to a property that has
        a known lightweight styling key equivalent (e.g., setting
        `button.Background` when `ButtonBackground` resource key exists)
  - [x] Report `DUCT002` (Info severity): "Consider lightweight styling for
        visual-state overrides"
- [x] Unit tests: verify detection for common control/property combos

### 5.5 — Validation
- [x] All analyzer unit tests pass via Roslyn test infrastructure
- [x] Manually verify analyzers work in Visual Studio on a sample Reactor project
- [x] Verify analyzer NuGet package installs and activates correctly

---

## Deferred / Out-of-Scope Items

These items are mentioned in the spec but explicitly deferred:

- [ ] **Proposal 1: Style Bundles** — document the `Func<T, T>` pattern in
      developer docs; no new types needed (plain C# extension methods suffice)
- [ ] **Proposal 3: Expanded ThemeRef Coverage** — extend `GetDependencyPropertyName()`
      and `ModifyTheme<T>()` for `Fill`, `Stroke`, `PlaceholderForeground`,
      `CaretBrush`, `SelectionHighlightColor` (P1, separate work item)
- [ ] **Proposal 4: Custom Theme Definitions** — deferred to theme system revamp
      (another engineer)
- [ ] **Proposal 7: Control Style Protocols** — highest effort/risk, deferred
      pending demand (`ButtonStyles.Accent`, `ToggleStyles.Compact`, etc.)
- [ ] **Future: `ButtonResources.Background` constants** — type-safe constants
      for lightweight styling keys to replace stringly-typed keys
