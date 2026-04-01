# Duct Framework - Code Review Feedback

**Reviewer:** Senior Platform Engineer
**Date:** 2026-03-18
**Scope:** Full codebase review - all source, tests, configuration, build scripts

---

## Status Field Instructions

Each feedback item has a **Status** field. The workflow is:

1. **`draft`** - Initial feedback from the reviewer. All items start here.
2. **`approved`** - The engineer's manager marks items they believe should be completed.
3. **`optional`** - The manager marks items that are borderline / nice-to-have.
4. **`skip`** - The manager marks items they believe should not be acted on.
5. **`done`** - The engineer marks items complete after addressing them.

Engineers: check the box `[x]` when you've addressed the item, then change status to `done`.

---

## Overall Impressions

This is genuinely impressive work for a new C# engineer. The architecture is well-thought-out: the React-inspired reconciler pattern is sound, the use of C# records for the virtual element tree is excellent, and the DSL is ergonomic. The code is readable and well-structured. The feedback below is aimed at taking solid code and making it production-ready for a platform that will be maintained for years.

---

## Table of Contents

1. [Architecture & Design](#1-architecture--design)
2. [Logging](#2-logging)
3. [Core Library - Reconciler](#3-core-library---reconciler)
4. [Core Library - RenderContext / Hooks](#4-core-library---rendercontext--hooks)
5. [Core Library - Element Types](#5-core-library---element-types)
6. [Core Library - Supporting Classes](#6-core-library---supporting-classes)
7. [Hosting Layer](#7-hosting-layer)
8. [Elements / DSL Layer](#8-elements--dsl-layer)
9. [Native Rust Differ](#9-native-rust-differ)
10. [CLI Tool](#10-cli-tool)
11. [Build Configuration](#11-build-configuration)
12. [Tests](#12-tests)
13. [Sample Applications](#13-sample-applications)

---

## 1. Architecture & Design

### 1.1 Rust Differ Is Built But Unused By the C# Reconciler

- [x] **Status:** `done`
- **Files:** `Duct/Core/Reconciler.cs`, `Duct/Core/ChildReconciler.cs`, `Duct/Native/ViewDiffer.cs`, `Duct/Native/differ/`
- **Issue:** The Rust native differ is built, packaged, and has a full C# interop layer (`ViewDiffer.cs`), but the actual reconciler (`Reconciler.cs`, `ChildReconciler.cs`) does its own diffing entirely in C#. The `TreeSerializer` serializes elements to the Rust wire format, but nothing consumes those serialized trees. This means:
  - The Rust crate is dead code in production (cargo builds add build time, binary size, and a native dependency for no benefit)
  - There's duplicated reconciliation logic: `ChildReconciler.ComputeLIS` in C# and `reconcile::compute_lis` in Rust
  - The `ViewDiffer`, `ViewNode`, `ViewProp`, `ViewPatch`, `ViewPatchOp` types, `TreeSerializer`, and `PropValueRegistry` are all unused at runtime
- **Question for engineer:** What is the roadmap here? Is the plan to eventually switch the reconciler to use the Rust differ for performance? If so, this should be documented clearly (e.g., a design doc explaining the migration path). If not, consider removing the dead code to reduce maintenance burden. Having two implementations of the same algorithm that must stay in sync is a significant risk.

### 1.2 Static Mutable State in DuctApp

- [x] **Status:** `done`
- **File:** `Duct/Hosting/DuctApp.cs`, lines 12-16
- **Issue:** `DuctApp` uses `internal static` fields to communicate between the `Run()` call and the `DuctApplication.OnLaunched()` callback:
  ```csharp
  internal static Type? RootComponentType;
  internal static Func<RenderContext, Element>? RootRenderFunc;
  internal static string WindowTitle = "Duct App";
  internal static int WindowWidth = 1024;
  internal static int WindowHeight = 768;
  ```
  This is a global mutable singleton pattern. While `Application.Start` blocks and only one Application can exist per process, this pattern:
  - Makes the code untestable in isolation
  - Creates implicit coupling between `DuctApp.Run` and `DuctApplication`
  - Would break if someone called `Run` from a test runner or tried to host multiple Duct windows
- **Recommendation:** Pass the configuration through the `DuctApplication` constructor. You could create a `DuctAppOptions` record and pass it via a closure or store it in a way that's scoped to the application instance rather than a static.

---

## 2. Logging

### 2.1 No Logging Abstraction Exists

- [x] **Status:** `done`
- **Files:** All files in `Duct/Hosting/`, `Duct/Core/`
- **Issue:** The entire framework has exactly three logging calls, all using `System.Diagnostics.Debug.WriteLine`:
  - `DuctApp.cs:83` - UnhandledException handler
  - `DuctHost.cs:122` - Render failure
  - `DuctHostControl.cs:175` - Render failure

  These only output in debug builds and are invisible in production. A platform framework needs structured logging for:
  - Mount/unmount lifecycle events
  - Reconciliation performance (how long a render cycle took, how many elements were diffed)
  - Hook violations (e.g., hooks called in different order across renders)
  - Element pool hit/miss rates
  - Type registry lookups
  - Error conditions in user components

- **Recommendation:** Introduce an `IDuctLogger` interface (or use `Microsoft.Extensions.Logging.ILogger<T>`) with a default no-op implementation. Since this project needs to work as both OSS and internal:

  ```csharp
  public interface IDuctLogger
  {
      void Log(DuctLogLevel level, string message);
      void Log(DuctLogLevel level, string message, Exception? exception);
  }

  public enum DuctLogLevel { Trace, Debug, Info, Warning, Error }
  ```

  - Ship a `DebugDuctLogger` that writes to `Debug.WriteLine` for OSS
  - Internally, implement one that bridges to the Microsoft internal logging pipeline
  - Accept the logger via constructor injection in `Reconciler`, `DuctHost`, `DuctHostControl`
  - Alternatively, use `Microsoft.Extensions.Logging` directly - it's the standard .NET logging abstraction and already supports pluggable providers. This would give you `ILogger<Reconciler>`, structured logging, log levels, etc. out of the box. Internal teams can register their own `ILoggerProvider`.

### 2.2 UnhandledException Handler Silently Swallows Errors

- [x] **Status:** `done`
- **File:** `Duct/Hosting/DuctApp.cs`, lines 81-85
- **Issue:**
  ```csharp
  UnhandledException += (_, e) =>
  {
      System.Diagnostics.Debug.WriteLine($"[Duct] UnhandledException: ...");
      e.Handled = true;
  };
  ```
  Setting `e.Handled = true` for **all** unhandled exceptions is dangerous. This means:
  - Application state may be corrupt but the app keeps running
  - Null reference exceptions, invalid casts, out-of-memory conditions are all silently swallowed
  - Users have no way to know their app is malfunctioning
- **Recommendation:** Only set `Handled = true` for known, recoverable exceptions (e.g., render failures in user components). For everything else, let the app crash with a useful message. Consider adding a callback so apps can opt into their own error handling:
  ```csharp
  public static Action<Exception>? OnUnhandledException { get; set; }
  ```

---

## 3. Core Library - Reconciler

### 3.1 Event Handler Leak Pattern in Mount/Update

- [x] **Status:** `done`
- **File:** `Duct/Core/Reconciler.Mount.cs` (throughout), `Duct/Core/Reconciler.Update.cs` (throughout)
- **Issue:** Event handlers are attached during Mount using patterns like:
  ```csharp
  b.Click += (_, _) =>
  {
      if (b.Tag is ButtonElement el) el.OnClick?.Invoke();
  };
  ```
  These handlers are **never detached** during Update. Since the pattern uses `b.Tag` for indirection (storing the current element on the control's Tag, and the handler reads from Tag), this works _functionally_ - the handler reads the latest element from Tag each time. However:
  - **Memory:** Every call to `Mount` for interactive controls adds a new event handler. If a control is repeatedly unmounted and remounted (e.g., when `CanUpdate` returns false and the fallback is `Mount(newEl, ...)`), handlers accumulate.
  - **Several Update methods fall through to full remount:** `RadioButtonsElement`, `ComboBoxElement`, `SplitViewElement`, `TabViewElement`, `PivotElement`, `TreeViewElement`, `MenuBarElement`, `CommandBarElement` all use `=> Mount(newEl, requestRerender)` as their update strategy. Each remount adds new handlers without removing old ones.
- **Recommendation:** For controls that do full remount on Update, call `Unmount` on the old control first (which happens for some but should be verified for all). Also consider whether the "Tag indirection" pattern is the right long-term approach. It works, but it's fragile - if someone sets `Tag` for another purpose, the entire event handling breaks silently. Consider using an `AttachedProperty` or `ConditionalWeakTable` instead.

### 3.2 Reconciler.Mount.cs Is ~900 Lines of Switch Dispatching

- [ ] **Status:** `skip`
- **File:** `Duct/Core/Reconciler.Mount.cs`
- **Issue:** The `Mount` method is a single massive switch expression mapping ~40 element types to mount methods. `Reconciler.Update.cs` has a similar ~120-line tuple-switch. While partial classes help with file organization, this is still a single method with 40+ branches.
- **Question:** As you add more element types (and external teams register custom types), does this scale? The `RegisterType` API already exists for external types. Would it make sense to use the same dispatch pattern internally, so built-in types are just pre-registered entries in `_typeRegistry`? This would also make it possible for consumers to override built-in type handling.

--> this will be addressed with a large refactoring

### 3.3 Reconciler.Reconcile Has Unused `parent` and `childIndex` Parameters

- [x] **Status:** `done`
- **File:** `Duct/Core/Reconciler.cs`, lines 80-86
- **Issue:** The `Reconcile` method signature is:
  ```csharp
  public UIElement? Reconcile(Element? oldElement, Element? newElement,
      UIElement? existingControl, WinUI.Panel? parent, int childIndex, Action requestRerender)
  ```
  The `parent` and `childIndex` parameters are always passed as `null` and `0` respectively by the only callers (`DuctHost.Render()` and `DuctHostControl.Render()`). Dead parameters in a public API create confusion about intended usage.
- **Recommendation:** Remove unused parameters or document why they exist (e.g., future use). If they were part of an older design, clean them up.

### 3.4 ComponentNode Uses Mutable Class Fields

- [x] **Status:** `done`
- **File:** `Duct/Core/Reconciler.cs`, lines 309-315
- **Issue:**
  ```csharp
  internal class ComponentNode
  {
      public Component? Component;
      public RenderContext? Context;
      public Element? RenderedElement;
      public Element? Element;
  }
  ```
  Public mutable fields on a class. In C#, fields should be private, with properties or methods controlling access. More importantly, having both `Element` and `RenderedElement` with no doc comments explaining the distinction is confusing. Which is the "input" element and which is the "output" of rendering?
- **Recommendation:** Add doc comments. Consider using properties with `{ get; set; }` (this is the C# convention). Even though it's internal, future maintainers (including you) will benefit from clarity here.

### 3.5 CanUpdate Doesn't Account for Keys

- [x] **Status:** `done`
- **File:** `Duct/Core/Reconciler.cs`, lines 245-251
- **Issue:**
  ```csharp
  internal bool CanUpdate(Element oldEl, Element newEl)
  {
      if (oldEl.GetType() != newEl.GetType()) return false;
      if (oldEl is ComponentElement oldComp && newEl is ComponentElement newComp)
          return oldComp.ComponentType == newComp.ComponentType;
      return true;
  }
  ```
  This doesn't consider `Key`. Two elements of the same type but different keys should NOT be updateable (they represent different logical items). `ChildReconciler.ReconcileKeyed` handles keys separately, but if `CanUpdate` is called in other contexts (registered type handlers, Update dispatch), mismatched keys could lead to incorrect updates.
- **Recommendation:** Add `&& oldEl.Key == newEl.Key` to the key-equality check, or document why keys are intentionally excluded from `CanUpdate`.

---

## 4. Core Library - RenderContext / Hooks

### 4.1 Unsafe Casts in Hook State Retrieval

- [x] **Status:** `done`
- **File:** `Duct/Core/RenderContext.cs`, lines 47, 82, 106, 128, 150, 182
- **Issue:** All hook retrieval uses bare casts like `(T)hook.Value` and `(EffectHookState)_hooks[_hookIndex]`. If a developer accidentally calls hooks in a different order between renders (a known React anti-pattern), these will throw `InvalidCastException` or `NullReferenceException` with no helpful message.
- **Recommendation:** Add a guard that detects hook type mismatches and provides a clear error:
  ```csharp
  if (_hooks[_hookIndex] is not EffectHookState hook)
      throw new InvalidOperationException(
          $"Hook at index {_hookIndex} is {_hooks[_hookIndex].GetType().Name}, expected EffectHookState. " +
          "Hooks must be called in the same order every render.");
  ```
  This is one of the most common developer mistakes in hook-based systems and a clear error message saves hours of debugging.

### 4.2 UseReducer Integer Overflow for Force-Render Pattern

- [x] **Status:** `done`
- **File:** `Duct/Core/RenderContext.cs`, lines 195-202
- **Issue:** `UseObservable`, `UseObservableProperty`, and `UseCollection` all use `UseReducer(0)` with `forceRender(v => v + 1)` as a force-render mechanism. The counter is `int`, so after ~2.1 billion property changes, it wraps to `int.MinValue`. At that point `EqualityComparer<int>.Default.Equals(prev, next)` would return false (it still works), but the semantics are surprising. More importantly, once it wraps past `int.MaxValue` to `int.MinValue` and keeps incrementing, it will eventually reach the _same_ value it started from, causing `Equals` to return `true` and a render to be skipped.
- **Recommendation:** Use `forceRender(v => unchecked(v + 1))` to make the intent explicit, or better, use a boolean toggle: `forceRender(v => !v)` with `UseReducer(false)`.

### 4.3 UseWindowSize Creates a New Event Handler Every Render

- [x] **Status:** `done`
- **File:** `Duct/Core/RenderContext.cs`, lines 251-268
- **Issue:** `UseWindowSize` passes `window` as the dependency to `UseEffect`. Since `window` is a reference type and the same object across renders, `DepsEqual` will return `true` (reference equality) after the first render, so the effect won't re-run. This is actually fine for subscribing once. However, the initial `setSize(...)` call on line 263 is inside the effect, which only runs on mount. If the window has already been resized before the hook runs, the initial value from `UseState` (line 253) might be stale. This is a minor race condition.
- **Recommendation:** The `setSize` call inside the effect (line 263) is redundant with the initial value on line 253. Consider removing it. If you're concerned about staleness, move the initial value read closer to the subscription.

### 4.4 FlushEffects Iterates All Hooks Every Render

- [x] **Status:** `done`
- **File:** `Duct/Core/RenderContext.cs`, lines 280-298
- **Issue:** `FlushEffects` uses `_hooks.OfType<EffectHookState>()` which iterates all hooks on every render. For components with many hooks (state + memos + effects), this linear scan is unnecessary overhead. Additionally, `OfType<T>` allocates an iterator.
- **Recommendation:** Maintain a separate list of pending effects, or track which indices are effect hooks. This matters for perf-sensitive components with many hooks. Low priority, but worth keeping in mind as the framework scales.

---

## 5. Core Library - Element Types

### 5.1 `Setters` Array Breaks Record Equality

- [x] **Status:** `done`
- **File:** `Duct/Core/Element.cs` (every element type with `Setters` property)
- **Issue:** Every element type has:
  ```csharp
  internal Action<WinUI.TextBlock>[] Setters { get; init; } = [];
  ```
  C# record equality for arrays compares by reference, not by content. This means two elements that are logically identical but have setters added via `.Set()` will always be non-equal, even if the setters are the same delegates. While this doesn't break the reconciler (it always updates), it defeats potential optimizations like skipping updates for unchanged elements.
- **Question:** Is this intentional? If setters are always re-created each render (lambda captures), then they'll always be different anyway. If so, document this trade-off. If you ever want to optimize by skipping no-op updates, you'll need to address this.

### 5.2 FuncElement Equality Is Misleading

- [ ] **Status:** `skip`
- **File:** `Duct/Core/Element.cs`, line 58
- **Issue:**
  ```csharp
  public record FuncElement(Func<RenderContext, Element> RenderFunc) : Element;
  ```
  Record equality for `FuncElement` compares the delegate by reference. A method group reference (`static Element Render(RenderContext ctx)`) produces the same delegate across calls, making two `FuncElement`s appear equal even though the function may produce different output (because it reads from external state). The `ReconcilerRegressionTests` explicitly tests and documents this. However, this means the reconciler could potentially skip re-rendering a `FuncElement` if equality-based optimization is ever added.
- **Recommendation:** Already well-handled by the test suite (good). Just ensure this invariant is documented near the `FuncElement` definition.

---

## 6. Core Library - Supporting Classes

### 6.1 PropValueRegistry Grows Unboundedly

- [x] **Status:** `done`
- **File:** `Duct/Core/PropValueRegistry.cs`
- **Issue:** `Register()` adds values to a list and returns a 1-based index. `Clear()` empties the list. But if `TreeSerializer.Serialize()` is called repeatedly without `Clear()` (or if serialization is triggered per-render), the list grows without bound. Each serialization pass adds entries for every string, delegate, and brush in the tree.
- **Recommendation:** The `Serialize()` method does call `_registry.Clear()` at the top, which is good. But this relies on `TreeSerializer` being the sole consumer and always calling `Clear()`. Consider making this a scoped operation (e.g., `using var session = registry.BeginPass()`) so the contract is enforced by the type system. Also, since this is currently unused at runtime (see 1.1), this is lower priority.

### 6.2 ElementPool Has a Hardcoded Magic Number

- [ ] **Status:** `skip`
- **File:** `Duct/Core/ElementPool.cs`, line 13
- **Issue:** `private const int MaxPerType = 32;` - Why 32? Is this based on profiling? Too low and you get cache misses; too high and you hold too many controls in memory.
- **Recommendation:** Add a comment explaining the rationale. Consider making it configurable (e.g., via `DuctHostControl` or `DuctHost`) so app developers can tune it for their scenarios. Low priority but good engineering hygiene.

### 6.3 ChildCollection.Move Semantics Are Subtle

- [x] **Status:** `done`
- **File:** `Duct/Core/ChildCollection.cs`, lines 38-47
- **Issue:** The `Move` implementation does remove-then-insert, which means the `newIndex` parameter represents the position _after_ removal. The comment on line 44 says "no adjustment needed" but this is only correct if callers consistently pass the _final desired index_. This is a common source of off-by-one bugs in list manipulation.
- **Recommendation:** The existing comment is good but could be strengthened. Consider adding an `[MethodImpl(MethodImplOptions.AggressiveInlining)]` since this is a hot path in keyed reconciliation, or validate the index with a debug assert.

### 6.4 ItemsControlChildCollection.Get Has Unsafe Cast

- [x] **Status:** `done`
- **File:** `Duct/Core/ChildCollection.cs`, line 69
- **Issue:** `public UIElement Get(int index) => (UIElement)_items[index];` - ItemsControl.Items can contain non-UIElement objects. If a non-UIElement gets into the Items collection (from external code or a bug), this throws InvalidCastException with no context.
- **Recommendation:** Add a guard: `_items[index] as UIElement ?? throw new InvalidOperationException(...)` with a descriptive message.

---

## 7. Hosting Layer

### 7.1 DuctHost.Render Has No Exception Boundary Per Component

- [x] **Status:** `done`
- **File:** `Duct/Hosting/DuctHost.cs`, lines 81-129
- **Issue:** The entire render pass is wrapped in a single try/catch. If a user component's `Render()` method throws, the entire render fails and the catch block re-throws, which could crash the application. React has "error boundaries" that catch errors in subtrees and render fallback UI.
- **Recommendation:** For V1, at minimum catch exceptions from `_rootComponent.Render()` and show a fallback UI (e.g., a red border with the error message). This prevents user code bugs from crashing the entire application. Long term, consider an error boundary pattern where components can opt into error handling.

### 7.2 DuctHostControl.Dispose Doesn't Follow IDisposable Pattern

- [x] **Status:** `done`
- **File:** `Duct/Hosting/DuctHostControl.cs`, lines 184-196
- **Issue:** `DuctHostControl` implements `IDisposable` but:
  - Has no `Dispose(bool disposing)` pattern
  - Does not call `GC.SuppressFinalize(this)`
  - Does not unsubscribe from `Loaded`/`Unloaded` events
  - Sets fields to null but doesn't clear `Content` (the visual tree remains)
- **Recommendation:** While the full `Dispose(bool)` pattern is technically only needed when you have unmanaged resources (which you don't directly), the event subscription leak is real. Add:
  ```csharp
  Loaded -= OnLoaded;
  Unloaded -= OnUnloaded;
  Content = null;
  ```

### 7.3 DuctHostControl.OnLoaded Uses Activator.CreateInstance Without Validation

- [x] **Status:** `done`
- **File:** `Duct/Hosting/DuctHostControl.cs`, line 80
- **Issue:** `var component = (Component)Activator.CreateInstance(ComponentType)!;` - If `ComponentType` is set to a type that isn't a `Component` subclass or doesn't have a parameterless constructor, this throws with a confusing error.
- **Recommendation:** Add validation:
  ```csharp
  if (!typeof(Component).IsAssignableFrom(ComponentType))
      throw new InvalidOperationException($"ComponentType must derive from Component, got {ComponentType.FullName}");
  ```
  Same issue exists in `DuctApp.cs` line 100.

### 7.4 DuctPage Props Setting Uses Reflection

- [x] **Status:** `done`
- **File:** `Duct/Hosting/DuctPage.cs`, lines 32-35 and `DuctHostControl.cs`, lines 83-87
- **Issue:** Both `DuctPage<TComponent>` and `DuctHostControl` set props via reflection:
  ```csharp
  var propsProperty = component.GetType().GetProperty("Props");
  propsProperty?.SetValue(component, Props);
  ```
  The generic `DuctPage<TComponent, TProps>` does it properly via the typed `component.Props = props;`. The non-generic version should use an interface or base class method instead of reflection.
- **Recommendation:** Add a method to `Component` like `internal virtual void SetProps(object? props) { }` with an override in `Component<TProps>` that does the typed assignment. This avoids reflection and provides type safety.

### 7.5 DuctHost Render Loop Could Spin Infinitely

- [x] **Status:** `done`
- **File:** `Duct/Hosting/DuctHost.cs`, lines 73-79
- **Issue:**
  ```csharp
  do
  {
      _needsRerender = false;
      Render();
  }
  while (_needsRerender);
  ```
  If `Render()` always triggers a re-render (e.g., a component calls `setState` during render, or an effect that sets state), this loop never terminates, freezing the UI thread.
- **Recommendation:** Add a maximum iteration count (React uses a limit of ~50):
  ```csharp
  const int MaxRenderIterations = 50;
  int iteration = 0;
  do
  {
      _needsRerender = false;
      Render();
      if (++iteration >= MaxRenderIterations)
      {
          // Log warning: "Maximum re-render limit exceeded"
          break;
      }
  }
  while (_needsRerender);
  ```
  Same issue in `DuctHostControl.cs` lines 126-132.

---

## 8. Elements / DSL Layer

### 8.1 BrushHelper.ParseHex Throws On Malformed Input

- [x] **Status:** `done`
- **File:** `Duct/Elements/BrushHelper.cs`, lines 34-48
- **Issue:** `byte.Parse(hex[0..2], NumberStyles.HexNumber)` throws `FormatException` if the hex string contains non-hex characters (e.g., `#GGHHII`). The fallback for unknown named colors returns gray, but malformed hex codes crash.
- **Recommendation:** Use `byte.TryParse` and return the default gray color on failure, consistent with the named color fallback behavior. Or throw a descriptive `ArgumentException` if you prefer fail-fast. The current behavior (exception from deep inside `byte.Parse`) is unhelpful.

### 8.2 BrushHelper Creates New SolidColorBrush Every Call

- [x] **Status:** `done`
- **File:** `Duct/Elements/BrushHelper.cs`, line 31
- **Issue:** `BrushHelper.Parse` creates a new `SolidColorBrush` on every call. If called during render (e.g., `Background("#ff0000")` in a component), this creates a new brush object every render cycle. WinUI brushes are DependencyObjects and relatively heavyweight.
- **Recommendation:** Consider caching brushes for common colors or providing a `FrozenBrush` cache. Low priority unless profiling shows it as an issue. Alternatively, document that for performance-sensitive scenarios, users should cache brushes themselves.

### 8.3 ThemeResource Methods Throw On Missing Keys

- [x] **Status:** `done`
- **File:** `Duct/Elements/ThemeResource.cs`, lines 17-27
- **Issue:** `Brush`, `Double`, `CornerRadius`, and `Thickness` all cast directly from `Application.Current.Resources[key]`. If the key doesn't exist, this throws `KeyNotFoundException`. If the value is the wrong type, this throws `InvalidCastException`. Only the generic `Get<T>` method has a safe fallback.
- **Recommendation:** Either use `TryGetValue` with descriptive exceptions, or document that these methods throw on missing keys. Consider making the strongly-typed methods delegate to `Get<T>` with a sensible default:
  ```csharp
  public static Brush Brush(string key) => Get<Brush>(key)
      ?? throw new KeyNotFoundException($"Theme resource '{key}' not found or is not a Brush");
  ```

### 8.4 FilterChildren Allocates On Every VStack/HStack Call

- [x] **Status:** `done`
- **File:** `Duct/Elements/Dsl.cs`, lines 333-334
- **Issue:**
  ```csharp
  private static Element[] FilterChildren(Element?[] children) =>
      children.Where(c => c is not null).Select(c => c!).ToArray();
  ```
  This allocates a LINQ iterator, a selector, and a new array every time `VStack` or `HStack` is called. These are the most frequently used factory methods.
- **Recommendation:** Fast-path when no nulls exist (like `ChildReconciler.Filter` already does):
  ```csharp
  private static Element[] FilterChildren(Element?[] children)
  {
      foreach (var c in children)
          if (c is null) goto hasNulls;
      return children!;
      hasNulls:
      return children.Where(c => c is not null).Select(c => c!).ToArray();
  }
  ```
  Or use the same pattern as `ChildReconciler.Filter` for consistency.

---

## 9. Native Rust Differ

### 9.1 diff_props Is O(n*m) Quadratic

- [x] **Status:** `done`
- **File:** `Duct/Native/differ/src/diff.rs`, lines 11-63
- **Issue:** `diff_props` compares every new property against every old property (nested loop), making it O(old_count * new_count). For elements with many properties (e.g., a `NavigationViewElement` with ~10 props), this is fine. But the function doesn't document the expected count range, and if a custom element had 100+ properties, this would become a bottleneck.
- **Recommendation:** Add a comment documenting expected property counts. If counts could grow, consider using a hash map for old props. For now, the quadratic approach is fine for small counts.

### 9.2 Rust FFI Functions Don't Use `catch_unwind`

- [x] **Status:** `done`
- **File:** `Duct/Native/differ/src/ffi.rs`, lines 28-81
- **Issue:** If any Rust code panics (e.g., index out of bounds in `diff_subtree`), the panic will unwind across the FFI boundary, which is undefined behavior. Rust panics across `extern "C"` boundaries can corrupt the C# process.
- **Recommendation:** Wrap the FFI function bodies in `std::panic::catch_unwind`:
  ```rust
  let result = std::panic::catch_unwind(std::panic::AssertUnwindSafe(|| {
      // ... existing logic ...
  }));
  match result {
      Ok(()) => 0,
      Err(_) => {
          (*ctx).error = b"panic in differ\0".to_vec();
          -2
      }
  }
  ```
  The `error` field on `DiffContext` already exists for this purpose.

### 9.3 Cargo.toml Missing `[profile.release]` Optimization Settings

- [x] **Status:** `done`
- **File:** `Duct/Native/differ/Cargo.toml`
- **Issue:** No `[profile.release]` section. Rust defaults to `opt-level = 3` for release, which is fine, but for a hot-path library consider enabling LTO (Link-Time Optimization) for smaller binaries and better inlining:
  ```toml
  [profile.release]
  lto = true
  codegen-units = 1
  ```
- **Recommendation:** Add release profile optimization if binary size or performance matters for the native differ.

### 9.4 DiffContext.error Is a Vec<u8> But Always Set to Static Bytes

- [x] **Status:** `done`
- **File:** `Duct/Native/differ/src/arena.rs`, `Duct/Native/differ/src/ffi.rs`
- **Issue:** The `error` field is `Vec<u8>` but is only ever set to static byte strings. The `differ_get_error` FFI function returns a pointer to the vec's data (or a static string), which is fine as long as the context outlives the caller. However, the C# side (`ViewDiffer.cs`) never calls `differ_get_error` - errors are detected by the return code only.
- **Recommendation:** Either wire up error retrieval in the C# interop layer, or remove the error field to simplify the Rust code.

---

## 10. CLI Tool

### 10.1 CLI Help Text Says "patch" But Tool Name Is "duct"

- [x] **Status:** `done`
- **File:** `Duct.Cli/Program.cs`, lines 37-38, 54-63
- **Issue:** The help text says:
  ```
  Usage: patch [option]
  ...
  Usage: patch --create <ProjectName>
  ```
  But the tool is called "duct" (line 54: `duct {version} — Duct (Functional UI) CLI`). The `--create` error message also says "patch".
- **Recommendation:** Replace "patch" with "duct" in all user-facing strings, or decide on a consistent name.

### 10.2 Generated .sln References Relative Path That Assumes Directory Layout

- [x] **Status:** `done`
- **File:** `Duct.Cli/Program.cs`, line 172
- **Issue:** The generated solution includes:
  ```csharp
  $"Project(\"{csharpGuid}\") = \"Duct\", \"..\\Duct\\Duct.csproj\", \"{pg}\""
  ```
  This assumes the new project is created as a sibling of the `Duct` directory. If the user runs `duct --create MyApp` from a different location, the relative path breaks.
- **Recommendation:** Either document the expected directory layout, or use a NuGet package reference in the generated .csproj instead of a project reference. For a scaffolding tool, NuGet is the standard approach.

### 10.3 No Input Sanitization on Project Name

- [x] **Status:** `done`
- **File:** `Duct.Cli/Program.cs`, line 78-109
- **Issue:** `CreateProject(args[1])` uses the user-provided name directly in:
  - Directory creation (`Path.Combine(cwd, name)`)
  - File names (`{name}.csproj`, `{name}.sln`)
  - C# class names (in the generated `Program.cs`)
  - .sln project names

  If the name contains spaces, special characters, or path separators, this could create invalid files or path traversal issues.
- **Recommendation:** Validate the project name: `Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_\.]*$")`.

---

## 11. Build Configuration

### 11.1 Experimental SDK Version

- [ ] **Status:** `skip`
- **File:** `Duct/Duct.csproj`, line 19; `tests/Duct.Tests/Duct.Tests.csproj`, line 16; all .csproj files
- **Issue:** All projects reference `Microsoft.WindowsAppSDK Version="2.0.0-experimental4"`. Experimental packages have no stability guarantees and can break between releases.
- **Question:** Is this intentional because you need a specific API from the experimental release? If so, document which API and track when it moves to stable. If not, consider using the latest stable release. When this project ships to external developers, they should not depend on experimental packages.

--> experimental is good for now

### 11.2 Directory.Build.targets Copies Local WinUI Binaries

- [x] **Status:** `done`
- **File:** `Directory.Build.targets`
- **Issue:** This file copies ARM64 CHK (debug) binaries from a local clone of `microsoft-ui-xaml`. This is fine for local development but:
  - Hardcodes paths relative to the repo
  - References `arm64chk` specifically
  - Would fail for any contributor who doesn't have a local WinUI build
  - The `Condition` guards on `Exists()` protect against failure, which is good
- **Recommendation:** Add a comment at the top of the file explaining this is for internal WinUI team development only and not needed for normal builds. Consider wrapping the entire target in a condition on an environment variable (e.g., `Condition="'$(DUCT_USE_LOCAL_WINUI)' == 'true'"`) so it's opt-in.

--> remove the local copy feature altogether

### 11.3 No CI/CD Pipeline Defined

- [ ] **Status:** `skip`
- **Files:** No pipeline files found (no `azure-pipelines.yml`, `.github/workflows/`, etc.)
- **Issue:** There's no automated build or test pipeline. For a platform library, CI is essential to catch regressions.
- **Recommendation:** Add at minimum a build verification pipeline that:
  - Builds on x64 and ARM64
  - Runs the xunit test suite
  - Builds the Rust native differ
  - Lints with `dotnet format` and `cargo clippy`

---

## 12. Tests

### 12.1 No Tests for Reconciler Mount/Update Logic

- [x] **Status:** `done`
- **Files:** `tests/Duct.Tests/` (all test files)
- **Issue:** The reconciler is the heart of the framework - it creates WinUI controls, applies properties, and reconciles changes. There are **zero tests** that verify:
  - `Reconciler.Mount` creates the correct WinUI control type for a given element
  - `Reconciler.Update` correctly applies property changes to existing controls
  - Event handlers are correctly wired up and fire
  - The Tag indirection pattern works correctly
  - Modifiers are applied correctly via `ApplyModifiers`
  - Grid definition parsing (`ParseColumnDef`, `ParseRowDef`)

  The existing `ReconcilerRegressionTests` only test record equality and `CanUpdate` at the element level - they never instantiate a Reconciler and call Mount/Update.
- **Recommendation:** This is the highest-priority test gap. Write tests that:
  ```csharp
  [Fact]
  public void Mount_TextElement_Creates_TextBlock_With_Correct_Content()
  {
      var reconciler = new Reconciler();
      var element = new TextElement("Hello");
      var control = reconciler.Mount(element, () => {});
      var textBlock = Assert.IsType<TextBlock>(control);
      Assert.Equal("Hello", textBlock.Text);
  }
  ```
  These require WinUI runtime initialization (which `TestSetup.cs` already handles).

### 12.2 No Tests for DuctHost or DuctHostControl Render Loop

- [x] **Status:** `done`
- **Files:** `tests/Duct.Tests/DuctHostControlTests.cs`
- **Issue:** The `DuctHostControlTests` only verify API surface (that properties and methods exist on the type). They don't test:
  - Mounting a component and verifying it renders
  - State changes triggering re-renders
  - The render loop batching behavior
  - Dispose cleaning up subscriptions
  - Error handling during render
- **Recommendation:** At minimum, test that `Mount` + state change results in the correct control tree. These would be integration tests and may need a DispatcherQueue, but they're critical for confidence in the render pipeline.

### 12.3 No Tests for BrushHelper

- [x] **Status:** `done`
- **File:** `Duct/Elements/BrushHelper.cs`
- **Issue:** `BrushHelper.Parse` handles named colors, hex codes, and fallback. None of this is tested. Missing test scenarios:
  - Each named color returns the correct ARGB values
  - `#RRGGBB` format parsing
  - `#AARRGGBB` format parsing
  - Invalid hex strings (what happens with `#GG0000`?)
  - Empty string
  - null input
  - Case insensitivity ("RED" vs "red" vs "Red")
- **Recommendation:** Write a `BrushHelperTests` class covering these cases.

### 12.4 ViewDifferTests Don't Test Actual Diffing

- [x] **Status:** `done`
- **File:** `tests/Duct.Tests/ViewDifferTests.cs`
- **Issue:** The `ViewDifferTests` only test:
  - FNV-1a hash function (4 tests)
  - `ViewNode` default values
  - `ViewPatch` default values
  - `ViewPatchOp` enum values

  They don't test the actual `ViewDiffer.DiffTrees` or `ViewDiffer.ReconcileKeys` methods. Since these involve native interop with the Rust DLL, testing them would verify the entire FFI pipeline works correctly.
- **Recommendation:** Add tests that call `DiffTrees` and `ReconcileKeys` with known inputs and verify the output patches. These tests require the Rust DLL to be built and present, so they should be guarded with a `[Trait]` or conditional skip if the DLL isn't available.

### 12.5 ChildReconcilerTests Only Test ComputeLIS

- [x] **Status:** `done`
- **File:** `tests/Duct.Tests/ChildReconcilerTests.cs`
- **Issue:** The `ChildReconcilerTests` test the LIS algorithm (6 tests), key utilities (3 tests), and element equality (3 tests). They don't test the actual `Reconcile` method, which is the core of the child reconciliation algorithm. Missing scenarios:
  - Positional reconciliation: add, remove, replace children
  - Keyed reconciliation: reorder, insert, remove keyed children
  - Mixed keyed/unkeyed children
  - Empty → non-empty and non-empty → empty transitions
  - Large list performance characteristics
- **Recommendation:** Create a mock `IChildCollection` implementation that records operations, then verify the operations emitted by `ChildReconciler.Reconcile` for known old/new child arrays.

### 12.6 Thickness Tests Are Testing Framework Code, Not Your Code

- [x] **Status:** `done`
- **File:** `tests/Duct.Tests/ElementTests.cs`, lines 673-701
- **Issue:** Three tests verify that `new Thickness(10)` sets all four sides to 10, that `Thickness(5,10,5,10)` sets left/right to 5 and top/bottom to 10, etc. These are testing `Microsoft.UI.Xaml.Thickness` constructor behavior, not your code. The compiler guarantees the struct is constructed correctly.
- **Recommendation:** Remove these tests. They add maintenance burden without validating any Duct code.

### 12.7 ReconcilerRegressionTests.Move_* Tests Don't Test ChildCollection

- [x] **Status:** `done`
- **File:** `tests/Duct.Tests/ReconcilerRegressionTests.cs`, lines 206-257
- **Issue:** The Move tests operate on `List<string>`, not on the actual `PanelChildCollection` or `ItemsControlChildCollection`. They verify that remove-then-insert on a `List<string>` produces the right result, but they don't verify that the `Move` method on `IChildCollection` behaves the same way.
- **Recommendation:** These tests should create a real `StackPanel`, populate its `Children`, and call `PanelChildCollection.Move` to verify the actual WinUI collection behavior matches expectations.

### 12.8 ObservableHookTests Need Multi-Property Change Scenarios

- [x] **Status:** `done`
- **File:** `tests/Duct.Tests/ObservableHookTests.cs`
- **Issue:** The tests cover basic subscribe/unsubscribe but don't test:
  - Rapid successive property changes (should only trigger one re-render if batched)
  - Multiple hooks watching the same source
  - Hook resubscription when source object changes between renders
  - Memory leak: does cleanup actually remove the handler (verify via WeakReference)
- **Recommendation:** Add at least the "source object changes between renders" scenario, as that's a common real-world pattern.

### 12.9 TypeRegistryTests Don't Verify Unmount Dispatch

- [x] **Status:** `done`
- **File:** `tests/Duct.Tests/TypeRegistryTests.cs`
- **Issue:** Tests verify mount and update dispatch, but there's no test that verifies the unmount handler is called when a registered type control is removed from the tree.
- **Recommendation:** Add a test that registers a type with an unmount handler, mounts a control, then calls `UnmountChild` and verifies the unmount handler was invoked.

### 12.10 No Test Coverage Tool Configuration

- [x] **Status:** `done`
- **File:** `tests/Duct.Tests/Duct.Tests.csproj`
- **Issue:** No code coverage tool (e.g., `coverlet.collector`) is configured. Without coverage data, you can't measure how much of the framework is actually tested.
- **Recommendation:** Add coverlet:
  ```xml
  <PackageReference Include="coverlet.collector" Version="6.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
  ```
  Then run with `dotnet test --collect:"XPlat Code Coverage"`.

---

## 13. Sample Applications

### 13.1 WordPuzzle App Has Game Logic Mixed With UI

- [ ] **Status:** `skip`
- **File:** `samples/apps/wordpuzzle/App.cs`
- **Issue:** The puzzle game logic (board state, move validation, shuffle, win detection) is mixed into the `Render()` method via closures and local functions. For a sample app this is fine, but if this is intended to demonstrate best practices for Duct, it should show separation of concerns.
- **Recommendation:** If samples are meant to be exemplary, consider extracting game state into a separate class. If they're just functional demos, this is fine as-is.

### 13.2 TestApp Shows Good Patterns But Has No Error Handling Examples

- [ ] **Status:** `skip`
- **File:** `tests/Duct.TestApp/App.cs`
- **Issue:** The TestApp demonstrates tabs, forms, lists, conditional UI, etc. but doesn't demonstrate error handling patterns (try/catch in effects, error boundaries, loading states, etc.).
- **Recommendation:** Add one tab showing resilient patterns: loading spinners, error messages, retry logic. This helps users write production-quality apps.

---

## Summary of Priority Items

**High Priority (should address before shipping):**
- 2.1: Add a logging abstraction
- 2.2: Fix the blanket exception swallowing
- 3.1: Audit event handler lifecycle in mount/update
- 7.5: Add render loop iteration limit
- 12.1: Write reconciler mount/update tests
- 12.3: Write BrushHelper tests
- 12.5: Write ChildReconciler integration tests

**Medium Priority (should address soon):**
- 1.1: Resolve the Rust differ status (use it or remove it)
- 1.2: Remove static mutable state in DuctApp
- 3.3: Clean up unused Reconcile parameters
- 3.5: Consider keys in CanUpdate
- 4.1: Add hook type mismatch error messages
- 7.1: Add component-level error boundary
- 7.2: Fix IDisposable pattern
- 7.3: Add ComponentType validation
- 7.4: Replace reflection with virtual method
- 8.1: Fix BrushHelper error handling
- 8.3: Fix ThemeResource error handling
- 10.1: Fix CLI name inconsistency
- 10.3: Validate project name input
- 11.1: Document/address experimental SDK dependency
- 11.3: Add CI/CD pipeline
- 12.10: Add coverage tooling

**Lower Priority (good engineering, not blocking):**
- 3.2: Consider dispatch pattern for mount/update
- 3.4: Clean up ComponentNode
- 4.2: Fix integer overflow in force-render
- 4.4: Optimize FlushEffects
- 6.2: Document pool size rationale
- 8.4: Optimize FilterChildren allocation
- 9.1: Document expected property count range
- 9.2: Add catch_unwind to Rust FFI
- 9.3: Add Cargo release profile optimization
