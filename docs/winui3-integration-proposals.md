# Duct + WinUI3 Integration Proposals

> Analysis of how Duct's declarative reconciler could integrate more deeply with the WinUI3 native framework to unlock performance, correctness, and developer experience improvements.
>
> **Duct repo:** `C:\Users\andersonch\Code\patch`
> **WinUI3 repo:** `C:\Users\andersonch\Code\microsoft-ui-xaml`

---

## 1. Programmatic DataTemplate Construction Without XamlReader (Unblock)

**Problem:** WinUI's `DataTemplate` can only be created from XAML markup — there is no public API to construct one programmatically from code. Duct works around this by calling `XamlReader.Load()` with a XAML string at runtime to create a DataTemplate wrapping a ContentControl shell:

```csharp
// Current hack in Reconciler.Mount.cs (lines 687-690, repeated for GridView at 746-749):
listView.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
    "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
    "<ContentControl HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch'/>" +
    "</DataTemplate>");
```

This is a significant hack with real costs:
1. **XML parsing at runtime** — `XamlReader.Load()` invokes the full XAML parser, allocating an `XamlTextReader`, parsing the XML, resolving namespaces, and instantiating objects through the XAML type system. This is orders of magnitude more expensive than a constructor call.
2. **Called per ListView/GridView mount** — every time Duct mounts a list, it re-parses this string. No caching is possible because DataTemplate instances can't be reliably shared across different ItemsControls.
3. **Fragile string-based API** — a typo in the XAML string is a runtime crash, not a compile error. The xmlns declaration is boilerplate noise.
4. **Incompatible with AOT/trimming** — `XamlReader.Load()` depends on runtime type resolution that may not survive aggressive trimming.

The underlying need is simple: Duct wants a DataTemplate that produces a single ContentControl, then takes over content management via `ContainerContentChanging`. The ContentControl is just a shell — Duct mounts its own element tree into `ContentControl.Content`.

**Gallery migration workarounds:** During the WinUI Gallery migration (70+ pages migrated), every page that used data-driven collections (FlipViewPage, TreeViewPage, SemanticZoomPage, SelectorBarPage) required imperative `XamlReader.Load()` workarounds to construct DataTemplates. The FlipView data-template example was the worst case — the migrated page had to create a hidden placeholder element, hook into its `Loaded` event, walk up to the parent panel, remove existing controls, then programmatically construct a FlipView with a parsed DataTemplate and inject it into the visual tree. This pattern completely defeats the declarative model Duct provides.

To partially address this on the Duct side, we built typed collection elements (`FlipView<T>`, `ListView<T>`, `GridView<T>`) that accept a `Func<T, int, Element> viewBuilder` instead of a DataTemplate. The reconciler drives mounting/updating/recycling of templated items natively, using a shared cached `DataTemplate` with a ContentControl shell. This eliminates the per-mount parsing cost and gives Gallery pages a declarative API. However, the underlying WinUI limitation remains — the cached template still requires one `XamlReader.Load()` call at startup, and any scenario that needs a DataTemplate outside of these typed wrappers (e.g., TreeView `ItemTemplate`, custom `ItemTemplateSelector`, grouped `GridView` with `GroupStyle`) still falls back to string-based XAML parsing.

**Proposal:** WinUI should expose a code-based DataTemplate construction API. The minimal version:

```csharp
// Option A: Factory-based DataTemplate constructor
var template = new DataTemplate(() => new ContentControl
{
    HorizontalContentAlignment = HorizontalAlignment.Stretch,
    VerticalContentAlignment = VerticalAlignment.Stretch,
});

// Option B: Static helper (lower API surface)
var template = DataTemplate.FromFactory(() => new ContentControl { ... });
```

Internally, this would create a `DataTemplate` whose `LoadContent()` method calls the factory delegate instead of instantiating from a parsed XAML tree. WinUI's `CDataTemplate` already has the concept of a template content factory (`IDataTemplateComponent`) — the proposal is to make this accessible from managed code without XAML markup.

**Impact:** Eliminates runtime XAML parsing for every ListView/GridView mount. Makes Duct's list virtualization compatible with NativeAOT trimming. Removes a class of potential runtime errors from string-based XAML. Any declarative framework that manages its own item rendering (not just Duct) would benefit from this API.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/DataTemplate.cpp` — `DataTemplate::LoadContent()` instantiation path
- `src/dxaml/xcp/dxaml/lib/DataTemplate_Partial.cpp` — Managed peer, `LoadContentImpl()`
- `src/dxaml/xcp/core/Parser/XamlReader.cpp` — `XamlReader::Load()` full parser invocation
- `src/dxaml/xcp/core/inc/DataTemplate.h` — Template content factory interfaces

**Duct files:**
- `Duct/Core/Reconciler.Mount.cs` — ListView mount (lines 687-690) and GridView mount (lines 746-749) both use the `XamlReader.Load()` hack
- `Duct/Core/Reconciler.Mount.cs` — `ContainerContentChanging` handler (lines 692-712) that populates the ContentControl shell with Duct-mounted content
- `Duct/Core/Element.cs` — `TemplatedListElementBase` / `TemplatedFlipViewElement<T>` / `TemplatedListViewElement<T>` / `TemplatedGridViewElement<T>` — typed collection elements that work around the DataTemplate gap
- `Duct/Elements/Dsl.cs` — `FlipView<T>()`, `ListView<T>()`, `GridView<T>()` DSL functions

---

## 2. XAML-Free Navigation via Custom Navigation Stack (Unblock)

**Problem:** WinUI's `Frame.Navigate()` requires page types registered in the XAML type metadata system (`MetadataAPI::GetClassInfoByFullName()` in `NavigationCache.cpp`), parameterless constructors (via `ActivationAPI::ActivateInstance()`), and typically XAML code-behind files. Duct components are render functions with props — they don't have parameterless constructors and aren't registered in the XAML type system. This means Duct apps can't use Frame-based navigation at all, leaving them with no back stack, no navigation lifecycle events, and no state serialization for page history.

**Proposal:** Build a declarative navigation system that bypasses Frame entirely. Introduce a `UseNavigation()` hook that manages a navigation stack internally, and a `NavigationHost` element that renders the current page component. The stack tracks page type + props + scroll position, supports back/forward, and provides lifecycle hooks (`OnNavigatedTo`, `OnNavigatedFrom`, `OnNavigatingFrom` with cancellation). Navigation transitions would use the Composition animation system from Proposal #15. The key insight is that Duct doesn't need Frame's type activation — it already knows how to instantiate components from their type + props.

```csharp
var nav = UseNavigation(initialPage: Component<HomePage>());

return NavigationView(
    MenuItems: [...],
    Content: NavigationHost(nav),
    OnSelectionChanged: tag => nav.Navigate(tag switch {
        "home" => Component<HomePage>(),
        "settings" => Component<SettingsPage>(new { userId = currentUser }),
        _ => Component<NotFoundPage>()
    }),
    IsBackEnabled: nav.CanGoBack,
    OnBackRequested: () => nav.GoBack()
);
```

**Impact:** Enables full navigation patterns (master-detail, wizard flows, deep linking) without XAML files. Back stack support means browser-like back/forward. State serialization enables app suspend/resume with navigation state preserved.

**WinUI3 files:**
- `src/dxaml/xcp/dxaml/lib/Frame_Partial.cpp` — Frame.Navigate() implementation (lines 310-405), shows the type resolution + cache + lifecycle pattern to replicate
- `src/dxaml/xcp/dxaml/lib/NavigationCache.cpp` — `LoadContent()` (lines 125-145) uses `ActivationAPI::ActivateInstance()` — the parameterless constructor requirement we bypass
- `src/dxaml/xcp/dxaml/lib/Page_Partial.h` — OnNavigatedTo/From/OnNavigatingFrom lifecycle (lines 16-47) — pattern to mirror in Duct hooks
- `src/controls/dev/NavigationView/NavigationView.cpp` — NavigationView is decoupled from Frame (fires SelectionChanged only), so it works naturally with a custom stack

**Duct files:**
- `Duct/Core/Element.cs` — `NavigationViewElement` (lines 528-543) already wraps NavigationView with Content + OnSelectionChanged
- `Duct/Core/Reconciler.Mount.cs` — `MountNavigationView()` (lines 576-610) sets Content to mounted subtree — NavigationHost would replace this content on navigation
- `Duct/Hosting/DuctHost.cs` — `RenderLoop()` / root content management — navigation changes trigger re-render naturally
- `Duct/Core/RenderContext.cs` — `UseState` pattern to model `UseNavigation` after

---

## 3. Deep Virtualization Hooks for ListView, GridView, and ItemsRepeater (Unblock)

**Problem:** Duct's virtualized list integration has three major gaps:

1. **Full-reset on data change:** ListView/GridView updates replace the entire `ItemsSource` (`lv.ItemsSource = Enumerable.Range(0, n.Items.Length).ToList()`), which destroys and recreates all containers. Inserting 1 item into a 10,000-item list causes a full reset — hundreds of milliseconds of work.

2. **No element lifecycle awareness:** ItemsRepeater fires `ElementPrepared`, `ElementClearing`, and `ElementIndexChanged` events that Duct ignores entirely. Components inside virtualized lists have no way to know when they become visible or get recycled, preventing deferred loading patterns (lazy image loading, analytics visibility tracking).

3. **Recycling gap:** `DuctElementFactory.RecycleElementCore()` calls `UnmountChild()` but explicitly skips element pooling because modifying the visual tree during ItemsRepeater's layout pass causes `COMException 0x800F1000`. This means every scroll creates fresh allocations instead of reusing pooled controls.

**Proposal:** Three targeted fixes:

**(a) Fine-grained collection change notifications:** Replace the index-list `ItemsSource` with a custom `ObservableCollection<int>` (or a custom `INotifyCollectionChanged` implementation) that emits Add/Remove/Replace/Move notifications. On update, diff the old and new item arrays by key, then emit granular change events. ListView/GridView handle these natively — they'll create/remove/move only the affected containers.

**(b) Expose ItemsRepeater lifecycle events:** Wire `ElementPrepared` and `ElementClearing` to new Duct component lifecycle hooks (`OnAppeared` / `OnDisappeared`). When an element enters the realization window, fire `OnAppeared` on its component context; when it leaves, fire `OnDisappeared`. This enables lazy loading, visibility-driven prefetch, and resource cleanup without framework-level changes.

**(c) Deferred recycling via Dispatcher:** Instead of recycling synchronously during ItemsRepeater's layout pass, queue cleanup work to `DispatcherQueue.TryEnqueue()` at low priority. After layout completes, the queued work safely unmounts Duct state and returns the control to `ElementPool`. This bridges the timing gap between ItemsRepeater's recycling cadence and Duct's cleanup requirements.

```csharp
// (a) Granular collection updates
private DuctObservableSource<T> _source;
private void UpdateLazyStack(LazyStackElement<T> n) {
    _source.DiffAndApply(n.Items, n.KeySelector); // emits Add/Remove/Move
}

// (b) Lifecycle hooks
repeater.ElementPrepared += (s, args) => {
    var ctx = GetComponentContext(args.Element);
    ctx?.RaiseOnAppeared();
};
repeater.ElementClearing += (s, args) => {
    var ctx = GetComponentContext(args.Element);
    ctx?.RaiseOnDisappeared();
};

// (c) Deferred recycling
protected override void RecycleElementCore(ElementFactoryRecycleArgs args) {
    DispatcherQueue.GetForCurrentThread().TryEnqueue(
        DispatcherQueuePriority.Low,
        () => { _reconciler.UnmountChild(args.Element); _pool.Return(args.Element); });
}
```

**Impact:** (a) Inserting/removing items in a 10k list goes from full reset (~500ms) to O(changed) (~1ms). (b) Components gain visibility awareness, enabling lazy image loading and scroll-driven analytics. (c) Element reuse during scrolling reduces GC pressure — a fast scroll through 1000 items reuses ~32 pooled controls instead of allocating 1000.

**WinUI3 files:**
- `src/controls/dev/Repeater/ViewManager.cpp` — `GetElement()` cascade (lines 22-96) and `ClearElement()` flow (lines 98-137) showing the full element lifecycle Duct must coordinate with
- `src/controls/dev/Repeater/ViewManager.h` — PinnedPool, UniqueIdResetPool, Animator ownership states
- `src/controls/dev/Repeater/VirtualizationInfo.h` — `ElementOwner` enum (lines 26-117), `AutoRecycleCandidate` / `KeepAlive` flags that control when elements are cleared
- `src/controls/dev/Repeater/ItemsRepeater.cpp` — `MeasureOverride` auto-clear logic (lines 136-150) where realized elements outside the viewport are recycled
- `src/controls/dev/Repeater/BuildTreeScheduler.h` — Frame-budget work scheduling with `QueryPerformanceCounter` timing — pattern for Duct's deferred recycling
- `src/controls/dev/Repeater/ViewportManager.h` — VisibleWindow / RealizationWindow / CacheLength concepts
- `src/dxaml/xcp/dxaml/lib/ListViewBase_Partial.cpp` — ContainerContentChanging event, container lifecycle for ListView/GridView

**Duct files:**
- `Duct/Core/DuctElementFactory.cs` — `GetElementCore()` / `RecycleElementCore()` — recycling gap fix site
- `Duct/Core/Reconciler.Mount.cs` — ListView mounting (lines 675-733), GridView mounting (lines 735-800), LazyStack mounting (lines 1046-1071)
- `Duct/Core/Reconciler.Update.cs` — ListView update (lines 457-472) full-reset pattern, LazyStack update (lines 499-511)
- `Duct/Core/ElementPool.cs` — `CleanElement()` / pool management — target for deferred return
- `Duct/Core/Element.cs` — `LazyVStackElement<T>` / `LazyHStackElement<T>` (lines 733-774) — KeySelector exists but unused

---

## 4. Bypass DependencyProperty for Duct Element↔UIElement Binding (Tactical)

**Problem:** Duct stores its `Element` reference in `FrameworkElement.Tag` (a DependencyProperty) so that generic event handlers can find the current Element at invocation time. `SetElementTag()` is called ~80 times across Mount and Update — on every control type including layout-only containers (StackPanel, Grid, Border, ScrollViewer, Canvas), not just interactive controls. Each call is a COM property set through the DP system via `CDependencyObject::SetValue` (`src/dxaml/xcp/core/core/elements/depends.cpp`). The Tag property is read on every event handler invocation (button clicks, text changes, toggle switches) via `GetElementTag()`.

**What didn't work:** Replacing Tag with a managed-side `ConditionalWeakTable<UIElement, Element>` or `Dictionary<nint, Element>` was **empirically slower**. WinUI's C# objects are CsWinRT-generated projections — thin RCW wrappers with only an `IObjectReference _inner` field. There is no CLR-side layer where fields could be added. The Tag DP is optimized internally — it's a known property index with no change notification, no layout invalidation, and no coercion, making it close to a bare field write on the native `CDependencyObject`. A `ConditionalWeakTable` adds ephemeron GC handle tracking, hash computation, and lock contention on every access, which outweighs the COM marshaling cost. A plain `Dictionary` adds hash + resize overhead and requires manual cleanup.

**Proposal:** Two solutions, deployable independently:

**(a) Stop setting Tag on non-interactive controls.** The comment on `SetElementTag` already says "Only call for interactive controls" but the code sets it on StackPanel, Grid, Border, ScrollViewer, Canvas, Expander, SplitView — none of which use the Tag-based event dispatch pattern. Removing these ~35 unnecessary `SetElementTag` calls from Mount and Update eliminates the COM cost entirely for layout-only elements, which are the majority of any tree.

**(b) For interactive controls, use a `StrongBox<T>` closure capture instead of Tag lookup.** At mount time, allocate a `StrongBox<ButtonElement>` and capture it in the event handler closure. During Update, mutate `box.Value` to point at the new Element. The event handler reads from the box directly — no Tag get, no sender cast, no dictionary lookup. This is a single pointer dereference at event dispatch time.

```csharp
// Mount:
private (WinUI.Button, StrongBox<ButtonElement>) MountButton(ButtonElement btn)
{
    var button = new WinUI.Button { Content = btn.Label, IsEnabled = btn.IsEnabled };
    var elementRef = new StrongBox<ButtonElement>(btn);
    button.Click += (s, _) => elementRef.Value?.OnClick?.Invoke();
    return (button, elementRef);
}

// Update (no Tag set needed — just mutate the box):
private void UpdateButton(WinUI.Button b, ButtonElement n, StrongBox<ButtonElement> elementRef)
{
    b.Content = n.Label; b.IsEnabled = n.IsEnabled;
    elementRef.Value = n; // Single managed pointer write, no COM
}
```

The reconciler would store the `StrongBox` alongside the control in its tracking structures (e.g., a parallel array or tuple in the element-to-control mapping). `ElementPool.CleanElement()` would null out the box value instead of clearing Tag.

**Impact:** (a) Removes ~35 COM `SetValue` calls per reconciliation pass for layout-only controls — these are the most numerous elements in any tree. (b) Reduces interactive control update cost from 1 COM SetValue (Tag write) to 1 managed field write (StrongBox mutation). Event dispatch goes from COM GetValue + cast to a direct pointer read. On a 200-element screen with 50 interactive controls, this eliminates ~150 COM calls on the update path and makes event dispatch essentially free.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/depends.cpp` — `SetValue` path (~line 477). Tag goes through `SetValueByKnownIndex` which is optimized but still involves COM marshaling, effective value computation, and thread affinity checks. The key finding: this is *fast enough* that a managed hash table can't beat it, but *not free* — eliminating it entirely via closure capture is still a win.
- `src/dxaml/xcp/core/inc/CDependencyObject.h` — `SetValueByKnownIndex` overloads

**Duct files:**
- `Duct/Core/Reconciler.cs` — `SetElementTag()` / `GetElementTag()` (lines 44-50) — would be replaced by StrongBox pattern for interactive controls
- `Duct/Core/Reconciler.Mount.cs` — ~80 `SetElementTag` call sites. ~35 are on layout-only controls (StackPanel line 464, Grid line 488, ScrollViewer line 499, Border line 513, Canvas line 571, etc.) that should simply be removed. ~45 are on interactive controls that should migrate to StrongBox.
- `Duct/Core/Reconciler.Update.cs` — ~35 `SetElementTag` calls on Update path, all replaceable with `elementRef.Value = n`
- `Duct/Core/ElementPool.cs` — `CleanElement()` clears `fe.Tag = null` — would null the StrongBox instead

---

## 5. Native Layout Coalescing: Hook Into LayoutManager's Batch Queue (Tactical)

**Problem:** Duct's `DuctHost.RenderLoop()` reconciles the entire tree in one shot, which can trigger hundreds of `InvalidateMeasure()` / `InvalidateArrange()` calls as individual properties are patched. Each invalidation propagates up the ancestor chain via `PropagateOnMeasureDirtyPath()`.

**Proposal:** Bracket Duct's reconciliation pass with WinUI's layout suppression. Call `LayoutManager::EnterMeasure()` before patching and `ExitMeasure()` after, so all layout invalidations are batched into a single pass. Alternatively, expose a `BeginDeferUpdates()` / `EndDeferUpdates()` API on the WinUI side that suppresses layout until the batch completes.

**Impact:** A reconciliation that patches 200 properties currently triggers 200 individual invalidation propagations. Batching would reduce this to a single layout pass.

**WinUI3 files:**
- `src/dxaml/xcp/core/layout/LayoutManager.cpp` — Enter/ExitMeasure (line ~83-87 in header)
- `src/dxaml/xcp/core/core/elements/uielement.cpp` — `InvalidateMeasure()` (lines 3551-3586), `InvalidateArrange()` (lines 3611-3646)

**Duct files:**
- `Duct/Hosting/DuctHost.cs` — `RenderLoop()` / `Render()`
- `Duct/Core/Reconciler.Update.cs` — property patching triggers layout invalidation

---

## 6. Pool Interactive Controls by Resetting Event State (Tactical)

**Problem:** `ElementPool.cs` only pools 12 non-interactive control types (TextBlock, Grid, Border, etc.). Interactive controls like Button, TextBox, CheckBox, ToggleSwitch are created fresh on every mount and discarded on unmount — never reused.

**Proposal:** Extend pooling to interactive controls by adding a `ResetEventState()` step. Since Duct's Tag-based event pattern means handlers are generic (they read from Tag at invocation time), the actual event subscriptions don't need to change. The only work needed is: (1) clear the Tag, (2) reset visual state (IsPressed, IsChecked, etc.), (3) return to pool. This is safe because Duct never stores per-instance closures in event handlers.

**Impact:** In a virtualized list of 1000 buttons, scrolling currently creates and destroys Button instances. Pooling would amortize allocation to ~32 instances.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/Control.cpp` — Control state reset
- `src/controls/dev/Repeater/ViewManager.h` — WinUI's own element reuse patterns

**Duct files:**
- `Duct/Core/ElementPool.cs` — `PoolableTypes` set, `CleanElement()` method
- `Duct/Core/Reconciler.Mount.cs` — event handler wiring (generic Tag-based pattern)

---

## 7. Replace Remount-On-Update Controls with Incremental Patching (Tactical)

**Problem:** Several complex controls in `Reconciler.Update.cs` use a `RemountOnUpdate` pattern — they unmount and remount entirely instead of patching properties. This includes: `RadioButtonsElement`, `ComboBoxElement`, `SplitViewElement`, `TabViewElement`, `TreeViewElement`, `MenuBarElement`, `CommandBarElement`.

**Proposal:** Implement incremental update paths for these controls. The reason they remount is that their WinUI counterparts use `Items` collections that don't support efficient diffing. For ComboBox and RadioButtons, use `ChildReconciler` on their Items collection. For TabView, patch individual `TabViewItem` properties. For TreeView, use hierarchical reconciliation matching WinUI's `TreeViewNode` structure.

**Impact:** TabView with 10 tabs currently destroys and recreates all 10 TabViewItems when any tab label changes. Incremental patching would update only the changed label — a 10x improvement for tab-heavy UIs.

**WinUI3 files:**
- `src/controls/dev/TabView/TabView.h` — TabViewItem collection management
- `src/controls/dev/RadioButtons/RadioButtons.h` — Items collection
- `src/dxaml/xcp/core/core/elements/ItemsControl.cpp` — Items collection patterns

**Duct files:**
- `Duct/Core/Reconciler.Update.cs` — RemountOnUpdate controls
- `Duct/Core/ChildReconciler.cs` — keyed reconciliation (could be reused)

---

## 8. Frame-Budget-Aware Reconciliation (Tactical)

**Problem:** `DuctHost.RenderLoop()` reconciles the entire tree synchronously. If reconciliation takes longer than 16ms (one frame at 60fps), the UI stutters. There's no mechanism to yield mid-reconciliation and continue on the next frame.

**Proposal:** Implement time-sliced reconciliation inspired by React's Fiber architecture. Break reconciliation into units of work (one component = one unit). After each unit, check elapsed time. If approaching the frame deadline, yield to the dispatcher and resume on the next frame. Priority levels: user input > animations > data updates > off-screen content.

**Impact:** Prevents jank during large tree updates. A 2000-node tree update that takes 40ms would be split across 3 frames instead of causing a single 40ms stutter.

**WinUI3 files:**
- `src/dxaml/xcp/core/layout/LayoutManager.cpp` — MaxLayoutIterations=250, layout cycle management
- WinUI's own layout manager already has iteration limits; Duct could mirror this

**Duct files:**
- `Duct/Hosting/DuctHost.cs` — `RenderLoop()` / `Render()` — currently synchronous
- `Duct/Core/Reconciler.cs` — `ReconcileComponent()` — natural unit-of-work boundary

---

## 9. Leverage WinUI's Built-In Implicit Style Resolution for Theming (Tactical)

**Problem:** Duct elements specify visual properties (FontSize, Foreground, FontWeight) explicitly via modifiers. There's no participation in WinUI's implicit style system — a Duct TextBlock doesn't pick up the app's implicit TextBlock style. This means Duct apps can't inherit theme customizations.

**Proposal:** After mounting a control, allow WinUI's implicit style resolution to run before applying Duct's explicit properties. Duct properties would override implicit styles (specificity: explicit > implicit), but unset properties would inherit from the theme. This is already how WinUI works — the fix is to *not* reset properties that Duct hasn't explicitly set, rather than clearing everything in `CleanElement()`.

**Impact:** Duct apps would automatically respect system themes, accessibility settings (high contrast), and app-level style overrides without any Duct-side changes.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/Style.cpp` — Implicit style lookup, BasedOn chain (lines 28-100)
- `src/dxaml/xcp/core/core/elements/Control.cpp` — `OnApplyTemplate()` and style application

**Duct files:**
- `Duct/Core/ElementPool.cs` — `CleanElement()` resets all properties
- `Duct/Core/Reconciler.Mount.cs` — Property application during mount

---

## 10. Children.Move() Instead of Remove+Insert (Tactical)

**Problem:** `ChildReconciler.cs` reorders children by removing and re-inserting them. Each `Panel.Children.Remove()` and `Panel.Children.Insert()` triggers visual tree updates, DComp visual reparenting, and layout invalidation independently.

**Proposal:** Use `UIElementCollection.Move()` (or the equivalent native `MoveInternal()`) which reorders the child in-place without remove+insert overhead. This preserves the visual's composition state and avoids the double layout invalidation. If the public API doesn't expose Move, add it to WinUI.

**Impact:** A list reorder of 10 items currently does 10 removes + 10 inserts = 20 visual tree operations. With Move, it's 10 operations with no reparenting overhead.

**WinUI3 files:**
- `src/dxaml/xcp/core/inc/panel.h` — Children collection management
- `src/dxaml/xcp/core/core/elements/panel.cpp` — SetValue for children transitions

**Duct files:**
- `Duct/Core/ChildReconciler.cs` — `ReconcileKeyed()` uses Remove+Insert for moves
- `Duct/Core/ChildCollection.cs` — Abstraction over Panel.Children

---

## 11. Pre-Warm Element Pools During Idle Time (Tactical)

**Problem:** Element pools start empty. The first render of a complex component creates all controls from scratch — the "cold start" penalty. Subsequent renders benefit from pooling, but the initial render is the most performance-critical (it's what the user sees first).

**Proposal:** During `DispatcherQueue` idle time, pre-allocate common control types into the pool. Use heuristics from the component tree structure: if the root component contains a `LazyVStack<Item>` with a view builder that produces `HStack(Image, VStack(Text, Text))`, pre-warm the pool with 32 StackPanels, 32 Images, and 64 TextBlocks before the first scroll event.

**Impact:** Eliminates cold-start allocation stutter for the first screenful of virtualized items.

**Duct files:**
- `Duct/Core/ElementPool.cs` — Pool management, max 32 per type
- `Duct/Hosting/DuctHost.cs` — Could schedule pre-warming on idle
- `Duct/Core/DuctElementFactory.cs` — Could analyze view builder output types

---

## 12. Incremental Tree Serialization with Dirty Tracking (Medium)

**Problem:** `TreeSerializer.SerializeWithMapping()` does a full BFS traversal and serializes the entire Element tree on every reconciliation pass. For a 1000-node tree where only 3 nodes changed, this wastes ~99.7% of serialization work.

**Proposal:** Add dirty tracking to the Element tree. When `UseState` produces a new value, mark the owning component and its subtree as dirty. `TreeSerializer` would then only re-serialize dirty subtrees, reusing cached ViewNode/ViewProp arrays for clean subtrees. The Rust differ already handles subtree replacement via its `Replace` patch — it just needs the serializer to provide stable subtree references.

**Impact:** Reduces serialization cost from O(n) to O(changed) per render. For typical UI updates (user types in a text field, counter increments), this could be 100x faster serialization.

**Duct files:**
- `Duct/Core/TreeSerializer.cs` — `Serialize()` / `SerializeWithMapping()` BFS traversal
- `Duct/Core/RenderContext.cs` — `UseState` / state change triggers
- `Duct/Hosting/DuctHost.cs` — `RequestRender()` could carry dirty component info

---

## 13. Unified Element Recycling with ItemsRepeater's ViewManager (Medium)

**Problem:** Duct has its own `ElementPool` and ItemsRepeater has its own `ViewManager` with separate recycling pools (PinnedPool, UniqueIdResetPool). These two systems don't know about each other. When Duct unmounts a virtualized item, it explicitly does NOT pool (comment in `DuctElementFactory.RecycleElementCore` explains why), losing the recycling opportunity.

**Proposal:** Integrate Duct's recycling with ItemsRepeater's ViewManager lifecycle. Register Duct's element pool as a custom recycling backend for ViewManager. When ItemsRepeater recycles an element, instead of clearing it to the factory, transition it to Duct's pool with element state preserved. When ItemsRepeater requests a new element, check Duct's pool first. This creates a single unified recycling pipeline.

**Impact:** Eliminates the "recycling gap" where ItemsRepeater recycles a control but Duct can't reuse it, forcing fresh allocation.

**WinUI3 files:**
- `src/controls/dev/Repeater/ViewManager.h` — `GetElement()` cascade, `ClearElement()` methods
- `src/controls/dev/Repeater/ViewManager.cpp` — Element lifecycle (lines 22-137)
- `src/controls/dev/Repeater/VirtualizationInfo.h` — Per-element state machine

**Duct files:**
- `Duct/Core/DuctElementFactory.cs` — `GetElementCore()` / `RecycleElementCore()`
- `Duct/Core/ElementPool.cs` — Current standalone pool

---

## 14. Fine-Grained Component Boundaries via WinUI's ContentPresenter (Medium)

**Problem:** Duct components are opaque to the Rust differ — they appear as "gap nodes" that require imperative C# reconciliation. This means the native diff path can't optimize across component boundaries, falling back to the slower C# path for every component in the tree.

**Proposal:** Map Duct components to WinUI `ContentPresenter` instances. ContentPresenter already manages content lifecycle and template instantiation natively. Each Duct component would own a ContentPresenter, and its rendered subtree would be the presenter's content. This gives WinUI native awareness of component boundaries — the presenter's content can be diffed independently, and WinUI's own content transition system provides free animation support.

**Impact:** Components would no longer be opaque to the differ. A tree with 20 components would go from 20 imperative reconciliation fallbacks to 20 independently diffable subtrees.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/ContentPresenter.cpp` — Content lifecycle (lines 76-145)
- `src/dxaml/xcp/core/core/elements/ContentControl.cpp` — Content hosting

**Duct files:**
- `Duct/Core/Reconciler.cs` — `ReconcileComponent()`, gap node handling
- `Duct/Core/TreeSerializer.cs` — Component serialization (treated as leaf/gap)

---

## 15. Expose WinUI's Composition Animations for Duct Transitions (Medium)

**Problem:** Duct has no transition/animation system. When elements are inserted, removed, or reordered, changes are instant. WinUI has a full composition animation system (implicit animations, connected animations, layout transitions) that Duct can't access because it bypasses the template/style system.

**Proposal:** Add a `.Transition()` modifier to Duct elements that maps to WinUI's `UIElement.TransitionCollection`. For layout changes, use `RepositionThemeTransition`. For inserts/removes, use `AddDeleteThemeTransition`. For connected animations, expose `ConnectedAnimationService` via a `UseConnectedAnimation()` hook. The reconciler would set these during Mount and they'd animate automatically.

**Impact:** Duct apps get polished, native-feeling animations with zero custom code. List reorders would animate smoothly instead of snapping.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/panel.cpp` — Panel_ChildrenTransitions property
- `src/dxaml/xcp/core/hw/` — Composition animation infrastructure

**Duct files:**
- `Duct/Elements/ElementExtensions.cs` — Would add `.Transition()` modifier
- `Duct/Core/Element.cs` — ElementModifiers would gain transition fields
- `Duct/Core/Reconciler.Mount.cs` — Would apply TransitionCollection during mount

---

## 16. Native Property Diffing in Rust (Medium)

**Problem:** `TreeSerializer` serializes properties as `(dp_id, value_hash)` pairs, and the Rust differ compares hashes to detect changes. But the actual property application still happens in C# via per-control switch statements in `Reconciler.Update.cs`. The Rust differ knows *which* properties changed but can't *apply* them.

**Proposal:** Extend the Rust differ to emit typed property patches with actual values (not just hashes). For simple properties (strings, doubles, booleans, enums), the patch would carry the new value directly. A thin C interop layer would call WinUI's `SetValue` with the right `KnownPropertyIndex` and value, bypassing the C# switch dispatch entirely.

**Impact:** Eliminates the C# property dispatch overhead for simple properties. For a TextBlock content update, the path becomes: Rust diff → emit `UpdateProp(TextBlock, Content, "new text")` → native SetValue. No C# involved.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/depends.cpp` — `SetValueByKnownIndex()` for direct property access
- `src/dxaml/xcp/core/inc/KnownPropertyIndex.h` — Property index enum

**Duct files:**
- `Duct/Native/differ/src/types.rs` — DifferProp, DifferPatch definitions
- `Duct/Core/Reconciler.Update.cs` — Per-control property switch statements
- `Duct/Core/PropValueRegistry.cs` — Complex value storage

---

## 17. Direct Composition Visuals for Layout-Only Elements (Wild)

**Problem:** Duct creates full WinUI FrameworkElement instances for layout-only elements like Border, StackPanel, and Grid. Each one carries the full UIElement allocation overhead: DComp render data (`PrimitiveCompositionPropertyData`), layout storage, automation peer infrastructure, managed peer linking, and property system participation.

**Proposal:** For elements that are purely structural (Border with just margin/padding, StackPanel with orientation/spacing), bypass UIElement creation entirely and create lightweight `Visual` objects directly via the Windows.UI.Composition API. These would participate in the DComp visual tree but skip the entire XAML framework overhead — no DependencyProperty storage, no layout manager participation, no event routing. Duct's reconciler would calculate layout positions itself (it already knows the constraints) and set `Visual.Offset` and `Visual.Size` directly.

**Impact:** Could reduce element creation cost by 10-50x for layout-only containers. A deeply nested component tree with 5 levels of VStack/HStack nesting would go from 5 FrameworkElement allocations to 5 lightweight Visual allocations.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/uielement.cpp` — UIElement construction overhead (lines 1-150)
- `src/dxaml/xcp/core/hw/hwwalk.cpp` — DComp visual creation
- `src/dxaml/xcp/core/hw/CompositorTreeHost.cpp` — Composition tree management

**Duct files:**
- `Duct/Core/Reconciler.Mount.cs` — MountStack, MountBorder, MountGrid create full FrameworkElements
- `Duct/Core/Element.cs` — StackElement, BorderElement, GridElement definitions

---

## 18. Rust Differ at the Composition Layer (Wild)

**Problem:** The Rust differ currently operates on serialized Element trees (ViewNode/ViewProp arrays) and produces patches that the C# reconciler applies to the WinUI control tree. This means: serialize → diff in Rust → deserialize patches → apply via COM interop → trigger layout → render. Three language boundaries per update.

**Proposal:** Move the Rust differ to operate directly on the DComp visual tree. The differ would read the current visual tree state from shared memory and emit DComp operations (visual inserts, property changes, offset updates) directly, bypassing the XAML layer entirely for layout-only subtrees. Interactive controls would still go through XAML, but pure layout subtrees could be updated in a single Rust→DComp pass.

**Impact:** Eliminates the C# ↔ Rust ↔ C# round-trip for structural updates. For a 500-node tree where 90% are layout-only, this could reduce update latency by 40-60%.

**WinUI3 files:**
- `src/dxaml/xcp/core/hw/CompositorTreeHost.cpp` — DComp tree access
- `src/dxaml/xcp/core/hw/hwcompnode.cpp` — Composition node management

**Duct files:**
- `Duct/Native/differ/src/diff.rs` — `diff_subtree()` algorithm
- `Duct/Native/differ/src/ffi.rs` — FFI boundary
- `Duct/Core/TreeSerializer.cs` — Serialization for Rust differ

---

## 19. Shared Memory Ring Buffer for Rust ↔ C# Communication (Wild)

**Problem:** Every Rust differ invocation involves marshaling flat arrays across the FFI boundary: `ViewNode[]`, `ViewProp[]` in, `ViewPatch[]` out. While the current zero-copy design (patches point into Rust heap) is efficient for reads, the serialization of the input tree still copies data.

**Proposal:** Use a shared memory ring buffer for bidirectional communication. C# writes serialized tree nodes directly into a memory-mapped region. Rust reads from the same region without copying. Patches are written to a separate output region. The ring buffer supports pipelining: C# can begin serializing the next frame while Rust is still diffing the current one.

**Impact:** Eliminates all FFI marshaling overhead. For a 1000-node tree, this removes ~40KB of array copying per reconciliation pass. The pipelining benefit is larger — it overlaps serialization and diffing.

**Duct files:**
- `Duct/Native/differ/src/ffi.rs` — Current FFI boundary
- `Duct/Core/ViewDiffer.cs` — P/Invoke wrappers, pointer management
- `Duct/Core/TreeSerializer.cs` — Could write directly to shared memory

---

## 20. Duct as WinUI's Official Declarative Layer (Wild)

**Problem:** WinUI's declarative story is XAML + data binding + MVVM. This requires: .xaml files, code-behind, INotifyPropertyChanged boilerplate, DataTemplate definitions, converter classes, and style resources. The cognitive overhead is enormous compared to Duct's `Text("hello").Bold()`.

**Proposal:** Ship Duct as a first-party WinUI package (`Microsoft.UI.Xaml.Declarative`). This would involve:
1. Adding Duct-aware APIs to WinUI controls (e.g., `IReconcilable` interface for incremental updates)
2. Exposing internal WinUI APIs to Duct (layout suppression, direct property access, composition visuals)
3. Making Duct's Element types part of the WinUI SDK
4. Providing migration tooling (XAML → Duct converter)

**Impact:** Every WinUI developer gets a modern, React-like development experience. Eliminates the XAML/MVVM learning curve. Microsoft ships a competitive declarative UI framework alongside SwiftUI and Jetpack Compose.

**WinUI3 files:**
- `src/controls/dev/` — Every control would get an `IReconcilable` implementation
- `src/dxaml/xcp/core/layout/LayoutManager.cpp` — Would expose batch APIs
- `src/dxaml/xcp/core/core/elements/depends.cpp` — Would expose fast property paths

---

## 21. Parallel Subtree Reconciliation (Wild)

**Problem:** Reconciliation is single-threaded. For a tree with independent subtrees (e.g., a split-pane view with left navigation and right content), both sides are reconciled sequentially even though they have no data dependencies.

**Proposal:** Identify independent subtrees (components with no shared state) and reconcile them in parallel on separate threads. Each thread produces a list of patches. Patches are applied on the UI thread in a single batch. The Rust differ already supports this — `DiffContext` is per-thread, and patches are returned as arrays that can be concatenated.

**Impact:** A complex app with 4 independent panels could reconcile 4x faster on multi-core machines. The constraint is that WinUI controls can only be touched on the UI thread, so only the diff phase parallelizes — patch application remains single-threaded.

**Duct files:**
- `Duct/Core/Reconciler.cs` — `ReconcileComponent()` as parallelization boundary
- `Duct/Native/differ/src/arena.rs` — `DiffContext` is already per-instance (not global)
- `Duct/Hosting/DuctHost.cs` — Would coordinate parallel diff + sequential apply

---

## Summary Table

| # | Proposal | Ambition | Effort | Impact |
|---|----------|----------|--------|--------|
| 1 | Programmatic DataTemplate (no XamlReader) | Unblock | S | High |
| 2 | XAML-free navigation stack | Unblock | M | High |
| 3 | Deep virtualization hooks (ListView/Repeater) | Unblock | L | High |
| 4 | Bypass DependencyProperty for Tag | Tactical | S | Medium |
| 5 | Layout coalescing via LayoutManager | Tactical | M | High |
| 6 | Pool interactive controls | Tactical | S | Medium |
| 7 | Replace remount-on-update controls | Tactical | M | Medium |
| 8 | Frame-budget-aware reconciliation | Tactical | M | High |
| 9 | Implicit style participation | Tactical | S | Medium |
| 10 | Children.Move() for reorders | Tactical | S | Medium |
| 11 | Pre-warm element pools on idle | Tactical | S | Low-Medium |
| 12 | Incremental tree serialization | Medium | L | High |
| 13 | Unified recycling with ViewManager | Medium | L | Medium |
| 14 | Component → ContentPresenter mapping | Medium | L | High |
| 15 | Composition animations for transitions | Medium | M | High |
| 16 | Native property diffing in Rust | Medium | L | Medium |
| 17 | Direct Composition visuals for layout | Wild | XL | Very High |
| 18 | Rust differ at Composition layer | Wild | XL | Very High |
| 19 | Shared memory ring buffer | Wild | XL | Medium |
| 20 | Duct as WinUI's declarative layer | Wild | XXL | Transformative |
| 21 | Parallel subtree reconciliation | Wild | XL | High |
