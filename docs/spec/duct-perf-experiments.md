# Duct Performance Experiments — Tracking Spec

## Status
**Draft** — hypotheses only. Actual measurements pending.

---

## Goal

Identify and validate performance optimizations for Duct's reconciler, element
creation, and render loop. Each experiment follows the same methodology:

1. **Baseline (vanilla WinUI3)** — Direct control manipulation or data binding
2. **Current (Duct, unoptimized)** — Today's Duct reconciler
3. **New (Duct, with optimization)** — Duct with the specific experiment applied

All measurements use the `PerfTracker` harness from `StressPerf.Shared` and
report: **Avg Update ms**, **Avg FPS**, **Peak Memory MB**.

Where experiments require new test scenarios beyond StressPerf's 4,800-cell grid,
new benchmark apps follow the same `--headless --percent P --duration D` CLI pattern.

### Machine
ARM64 builds, Release configuration. All numbers on the same hardware.

---

## Experiment Index

| # | Optimization | Target Metric | Section |
|---|---|---|---|
| 1 | Dirty Subtree Tracking | Wall clock, CPU | [EXP-1](#exp-1-dirty-subtree-tracking) |
| 2 | Property Diff Bitmasks | Wall clock (COM calls) | [EXP-2](#exp-2-property-diff-bitmasks) |
| 3 | Structural Sharing / Ref-Equality Bailout | Wall clock, memory | [EXP-3](#exp-3-structural-sharing) |
| 4 | Off-Thread Tree Building | UI thread blocking | [EXP-4](#exp-4-off-thread-tree-building) |
| 5 | Mutation Journaling | Enabler for #4, #7 | [EXP-5](#exp-5-mutation-journaling) |
| 6 | Interactive Control Pooling | Memory, mount cost | [EXP-6](#exp-6-interactive-control-pooling) |
| 7 | Time-Sliced Reconciliation | Interactivity (jank) | [EXP-7](#exp-7-time-sliced-reconciliation) |
| 8 | Prioritized State Updates (Lanes) | Perceived responsiveness | [EXP-8](#exp-8-prioritized-state-updates) |
| 9 | Lazy Off-Screen Mounting | Initial render time | [EXP-9](#exp-9-lazy-off-screen-mounting) |
| 10 | Arena Allocation for Element Trees | GC pressure, memory | [EXP-10](#exp-10-arena-allocation) |

---

## EXP-1: Dirty Subtree Tracking

### Problem
`RenderLoop` re-renders the full component tree from root on every state change.
A `SetState` in one leaf component triggers `Render()` on every component in the
tree, even those with identical props and no state changes.

### Optimization
Maintain an invalidation set. When `SetState` is called, mark that component's
`ComponentNode` (and ancestors up to root) as dirty. During reconcile, if a
component is not dirty and its element passes `ShallowEquals`, skip the entire
subtree — don't call `Render()` at all.

**Precedent:** React's `beginWork` bailout (`oldProps === newProps && !hasLanes`
skips entire subtrees). Compose's `$changed` bitmask skips composable bodies.
SwiftUI's AttributeGraph only re-evaluates dirty attributes.

### Test Scenario: `PerfBench.DirtyTracking`

Workload:
- **200 independent counter components** arranged in a 10×20 grid
- Each counter is a function component with its own `UseState<int>`
- A `DispatcherTimer` at 30 Hz increments **1 randomly chosen counter** per tick
- Measures: tree build ms + reconcile ms per frame

WinUI3 baselines:
- **Direct**: 200 TextBlocks in a Grid, update 1 TextBlock per tick
- **Bound**: 200 TextBlocks bound to 200 ViewModels, update 1 ViewModel per tick

### Hypothesis

| Variant | Avg Update (ms) | Rationale |
|---|---|---|
| WinUI3 Direct | **0.02** | Single TextBlock.Text set, trivial |
| WinUI3 Bound | **0.05** | Binding propagation for 1 property |
| Duct Current | **1.5–3.0** | Renders all 200 components, reconciles all 200 elements even though 199 are unchanged |
| Duct + Dirty Tracking | **0.05–0.15** | Renders only 1 dirty component, skips 199 subtrees via dirty check |

**Expected improvement:** 10–30× reduction in per-frame update cost. This is the
single highest-impact optimization because it converts O(total components) work
into O(dirty components) work.

### Actual Results

| Variant | Avg Update (ms) | Avg FPS | Peak Memory (MB) |
|---|---|---|---|
| WinUI3 Direct | _pending_ | _pending_ | _pending_ |
| WinUI3 Bound | _pending_ | _pending_ | _pending_ |
| Duct Current | _pending_ | _pending_ | _pending_ |
| Duct + Dirty Tracking | _pending_ | _pending_ | _pending_ |

---

## EXP-2: Property Diff Bitmasks

### Problem
When `ShallowEquals` returns false (element changed), the reconciler sets *every*
property on the WinUI control, even if only one property actually differs. Each
property set is a COM interop call across the WinUI apartment boundary.

### Optimization
Generate per-element-type `DiffProps(old, new)` methods (via source generator or
manual code) that return a bitmask of which properties changed. `ApplyChanges`
only sets the changed properties.

Example for `TextElement`:
```
Bit 0 = Content changed
Bit 1 = FontSize changed
Bit 2 = FontWeight changed
Bit 3 = Foreground changed
...
```

**Precedent:** Vue 3's compiler emits patch flags per template node indicating
which bindings are dynamic. Lit only updates changed template "parts."

### Test Scenario: `PerfBench.PropertyDiff`

Workload:
- **4,800 TextBlocks** in the StressPerf grid layout
- 50% of cells updated per tick at 30 Hz
- Each update changes **only `Text` and `Foreground`** (2 of ~8 properties)
- Measures: reconcile phase ms per frame

WinUI3 baselines:
- **Direct**: Set only `.Text` and `.Foreground` on changed cells
- **Bound**: Bindings fire only for changed properties (inherently fine-grained)

### Hypothesis

| Variant | Avg Reconcile (ms) | Rationale |
|---|---|---|
| WinUI3 Direct | **1.5** | 2,400 cells × 2 property sets = 4,800 COM calls |
| WinUI3 Bound | **2.5** | Binding overhead per property, but only 2 properties fire |
| Duct Current | **4.0–6.0** | 2,400 cells × ~8 property sets = ~19,200 COM calls (sets all props even if unchanged) |
| Duct + Bitmask Diff | **2.0–2.5** | 2,400 cells × 2 property sets = 4,800 COM calls + bitmask comparison overhead |

**Expected improvement:** 2–3× reduction in reconcile time for partial-update
scenarios. Brings Duct closer to parity with direct manipulation.

### Actual Results

| Variant | Avg Reconcile (ms) | Avg FPS | Peak Memory (MB) |
|---|---|---|---|
| WinUI3 Direct | _pending_ | _pending_ | _pending_ |
| WinUI3 Bound | _pending_ | _pending_ | _pending_ |
| Duct Current | _pending_ | _pending_ | _pending_ |
| Duct + Bitmask Diff | _pending_ | _pending_ | _pending_ |

---

## EXP-3: Structural Sharing

### Problem
Every `Render()` call allocates new `Element` record instances and new `Element[]`
arrays, even for subtrees that haven't changed. The reconciler can't bail out
early because `oldElement != newElement` (different object references) even when
the content is identical — it must walk into `ShallowEquals` and children.

### Optimization
Cache and reuse `Element` subtree references when inputs haven't changed.
- Components whose `Render()` output depends only on props and state can cache
  the previous `Element` result and return the same reference when inputs match.
- A `Memo(props, () => element)` helper returns the cached `Element` reference
  when `props` is unchanged.
- The reconciler adds a fast path: `if (ReferenceEquals(oldElement, newElement)) return;`
  — skips the entire subtree in O(1), no `ShallowEquals`, no child walk.

**Precedent:** React's primary bailout: `oldProps === newProps` (referential
equality) skips the entire subtree. React Native Fabric's shadow tree uses C++
pointer equality on `ShadowNode`. Persistent data structures share unchanged
subtrees automatically.

### Test Scenario: `PerfBench.StructuralSharing`

Workload:
- A **dashboard layout** with 5 panels, each containing 50 elements (250 total)
- A timer at 30 Hz updates **1 panel's** data (50 elements change)
- The other 4 panels (200 elements) are completely static per frame
- Measures: tree build ms + reconcile ms per frame

WinUI3 baselines:
- **Direct**: Update 50 TextBlocks in the changed panel only
- **Bound**: 250 bound TextBlocks, 50 VMs fire PropertyChanged

### Hypothesis

| Variant | Avg Update (ms) | Rationale |
|---|---|---|
| WinUI3 Direct | **0.1** | 50 property sets, trivial |
| WinUI3 Bound | **0.3** | 50 bindings fire, 200 are dormant |
| Duct Current | **1.0–2.0** | Renders all 5 panels (250 elements), reconciles all 250 with ShallowEquals checks |
| Duct + Structural Sharing | **0.3–0.5** | 4 panels return cached Element refs → O(1) ref-equality skip each. Only 1 panel (50 elements) reconciled |

**Expected improvement:** 2–4× for layouts with mostly-static regions. Combines
multiplicatively with EXP-1 (dirty tracking skips the render call; structural
sharing skips the reconcile walk).

### Actual Results

| Variant | Avg Update (ms) | Avg FPS | Peak Memory (MB) |
|---|---|---|---|
| WinUI3 Direct | _pending_ | _pending_ | _pending_ |
| WinUI3 Bound | _pending_ | _pending_ | _pending_ |
| Duct Current | _pending_ | _pending_ | _pending_ |
| Duct + Structural Sharing | _pending_ | _pending_ | _pending_ |

---

## EXP-4: Off-Thread Tree Building

### Problem
The entire render cycle — tree build, reconcile, effects — runs on the UI
thread via `DispatcherQueue.TryEnqueue`. The tree build phase
(`Component.Render()`) produces immutable `Element` records with no WinUI
affinity, yet it blocks the UI thread while running.

### Optimization
Move the tree build phase to a `ThreadPool` thread. The flow becomes:

```
UI Thread                          Thread Pool
──────────                         ───────────
RequestRender() ──dispatch──►      Build Element tree
                                   (Component.Render calls)
◄──────────────── post back ────   Element tree ready
Reconcile(old, new)
FlushEffects()
```

State reads during `Render()` need snapshot isolation (similar to Compose's
snapshot system) — take a copy of state values before dispatching to the
background thread, ensuring the tree build sees a consistent view.

**Precedent:** Compose's composition can run off-main-thread via snapshot
isolation. React Native Fabric builds ShadowNode trees in C++ on a background
thread.

### Test Scenario: `PerfBench.OffThread`

Workload:
- **1,000 elements** with moderately expensive `Render()` (each component reads
  2 state values, produces 3 child elements with computed properties)
- 30 Hz timer triggers full re-render
- **Primary metric:** UI thread blocked time per frame (excludes tree build)
- **Secondary metric:** Total wall clock per frame (tree build + reconcile)
- Also measures: input latency via a TextBox that accepts keystrokes during
  updates — measure time between keydown event and character appearing

WinUI3 baselines:
- **Direct**: 1,000 TextBlocks updated directly (all on UI thread, no tree build phase)
- **Bound**: 1,000 bindings firing (binding engine is inherently UI thread)

### Hypothesis

| Variant | UI Thread Blocked (ms) | Total Wall Clock (ms) | Input Latency (ms) |
|---|---|---|---|
| WinUI3 Direct | **2.0** | **2.0** | **< 1** |
| WinUI3 Bound | **3.0** | **3.0** | **< 1** |
| Duct Current | **5.0–8.0** | **5.0–8.0** | **5–10** |
| Duct + Off-Thread Build | **2.5–4.0** | **5.0–8.0** (same total) | **2–4** |

**Expected improvement:** 40–50% reduction in UI thread blocking. Total wall
clock stays the same (work is moved, not eliminated), but interactivity improves
because the UI thread is free to process input during tree build. Input latency
should roughly halve.

### Actual Results

| Variant | UI Thread Blocked (ms) | Total Wall Clock (ms) | Input Latency (ms) |
|---|---|---|---|
| WinUI3 Direct | _pending_ | _pending_ | _pending_ |
| WinUI3 Bound | _pending_ | _pending_ | _pending_ |
| Duct Current | _pending_ | _pending_ | _pending_ |
| Duct + Off-Thread Build | _pending_ | _pending_ | _pending_ |

---

## EXP-5: Mutation Journaling

### Problem
The reconciler directly mutates WinUI controls during the tree walk (sets
properties, adds/removes children). This couples the diff phase to the UI
thread and prevents batching or reordering of mutations.

### Optimization
The reconciler produces a flat `List<Mutation>` instead of touching controls:

```csharp
abstract record Mutation;
record Create(Type ControlType, int Id) : Mutation;
record SetProp(int Id, string Prop, object Value) : Mutation;
record AddChild(int ParentId, int ChildId, int Index) : Mutation;
record RemoveChild(int ParentId, int ChildId) : Mutation;
record Move(int ParentId, int ChildId, int NewIndex) : Mutation;
```

A `MutationApplier` replays the journal on the UI thread in a single batch.
This enables:
- The diff phase to run off-thread (since it no longer touches UIElements)
- Coalescing redundant mutations (e.g., two `SetProp` on the same property → keep last)
- Atomic commits (apply all changes in one shot)

**Precedent:** React Native Fabric's `Differentiator.cpp` produces mutation
instructions applied on the UI thread. Flutter's layer tree diffs produce a
mutation list for the raster thread.

### Test Scenario: `PerfBench.Journal`

Workload:
- **StressPerf grid** (4,800 cells) at 50% update rate, 30 Hz
- Compare: (a) current in-place reconcile vs (b) journal + batch apply
- Measure reconcile phase and apply phase separately
- Also measure: mutation coalescing effectiveness (how many mutations are
  eliminated by dedup)

WinUI3 baselines:
- **Direct**: Direct property sets (no journal overhead, optimal path)
- **Bound**: Binding engine batches internally within a single dispatcher tick

### Hypothesis

| Variant | Reconcile (ms) | Apply (ms) | Total (ms) | Mutations/frame |
|---|---|---|---|---|
| WinUI3 Direct | — | **1.5** | **1.5** | 4,800 |
| WinUI3 Bound | — | **2.5** | **2.5** | 4,800 |
| Duct Current | **4.0** (interleaved) | (included) | **4.0** | ~19,200 (all props) |
| Duct + Journal | **1.5** (no COM) | **2.5** (batch) | **4.0** | ~4,800 (after coalesce) |

**Expected improvement:** Total wall clock similar or slightly worse due to
journal overhead. The win is structural — this decouples diff from apply,
enabling EXP-4 (off-thread diff) and EXP-7 (time-slicing). Combined with
EXP-2 (bitmask diff), mutation count drops and apply phase shrinks.

### Actual Results

| Variant | Reconcile (ms) | Apply (ms) | Total (ms) |
|---|---|---|---|
| WinUI3 Direct | — | _pending_ | _pending_ |
| WinUI3 Bound | — | _pending_ | _pending_ |
| Duct Current | _pending_ | — | _pending_ |
| Duct + Journal | _pending_ | _pending_ | _pending_ |

---

## EXP-6: Interactive Control Pooling

### Problem
`ElementPool` only recycles non-interactive controls (TextBlock, StackPanel,
etc.). Interactive controls (Button, TextBox, ToggleSwitch, ComboBox) are
excluded because their event subscriptions and transient state make cleanup
complex. These are also the most expensive controls to create — Button template
instantiation involves XAML template parsing, visual state groups, and
ContentPresenter creation.

### Optimization
Add interactive controls to the pool with a `ResetInteractiveElement()` method:
- Unsubscribe all event handlers (Click, TextChanged, etc.)
- Clear transient visual state (IsPressed, Focus, selection)
- Reset Content/Text to defaults
- Call `GoToState("Normal")` to reset visual states

Since Duct uses the Tag-based event pattern (wire once at mount, read element
from Tag), the reset only needs to detach the single event subscription.

**Precedent:** Android `RecyclerView` recycles all view types including
interactive controls. `ViewHolder.onRecycled()` clears transient state. iOS
`UICollectionView.prepareForReuse()` recycles any cell.

### Test Scenario: `PerfBench.InteractivePool`

Workload:
- A **form list** with 500 items, each containing: Button + TextBox + ToggleSwitch
  (1,500 interactive controls total)
- Virtualized via `LazyVStack` — only ~30 items visible at once
- User scrolls rapidly (programmatic scroll at 2000px/sec for 5 seconds)
- Measures: mount time per newly visible item, total memory, GC collections

WinUI3 baselines:
- **Direct**: `ItemsRepeater` with `DataTemplate` containing Button + TextBox +
  ToggleSwitch (WinUI's native recycling via `ElementFactory`)
- **Bound**: Same but with `{x:Bind}` bindings for each control's properties

### Hypothesis

| Variant | Avg Mount/item (ms) | Peak Memory (MB) | GC Gen0 Collections |
|---|---|---|---|
| WinUI3 Direct (recycle) | **0.5** | **45** | **low** |
| WinUI3 Bound (recycle) | **0.8** | **50** | **low** |
| Duct Current (no pool) | **3.0–5.0** | **120** | **high** |
| Duct + Interactive Pool | **0.8–1.5** | **55** | **low** |

**Expected improvement:** 3–5× reduction in per-item mount cost during scroll.
Memory usage should drop significantly as controls are recycled instead of
allocated/GC'd. Scroll smoothness should match native `ItemsRepeater`.

### Actual Results

| Variant | Avg Mount/item (ms) | Peak Memory (MB) | GC Gen0 |
|---|---|---|---|
| WinUI3 Direct | _pending_ | _pending_ | _pending_ |
| WinUI3 Bound | _pending_ | _pending_ | _pending_ |
| Duct Current | _pending_ | _pending_ | _pending_ |
| Duct + Interactive Pool | _pending_ | _pending_ | _pending_ |

---

## EXP-7: Time-Sliced Reconciliation

### Problem
A large reconcile pass (1000+ elements) can exceed 16ms, causing a missed frame
and visible jank. The entire reconcile runs synchronously — no yield points.

### Optimization
Break reconcile into interruptible units of work. After processing N element
pairs (e.g., 50), check elapsed time. If > 5ms, yield back to the
DispatcherQueue and resume next frame. Requires EXP-5 (mutation journal) so
that partial work can be accumulated and applied atomically when complete.

Two-phase commit:
1. **Reconcile phase (interruptible):** Walk element tree, produce mutations.
   Can pause and resume across frames.
2. **Apply phase (atomic):** Replay accumulated mutations in one batch. Must
   complete within a single frame.

**Precedent:** React Fiber's core design — each fiber node is a unit of work,
checked against a 5ms deadline via `shouldYield()`. Render phase is
interruptible; commit phase is atomic and synchronous.

### Test Scenario: `PerfBench.TimeSlice`

Workload:
- **Initial mount of 2,000 elements** (simulates navigating to a complex page)
- During mount, a 60fps animation runs on a separate `Canvas` (a bouncing ball
  via `CompositionTarget.Rendering`)
- Measure: animation frame drops during mount, total mount wall clock,
  longest single-frame block

WinUI3 baselines:
- **Direct**: Create 2,000 TextBlocks in a loop (synchronous, single frame block)
- **Bound**: Same with bindings (also synchronous)

### Hypothesis

| Variant | Longest Frame Block (ms) | Animation Drops | Total Mount (ms) |
|---|---|---|---|
| WinUI3 Direct | **80–120** | **5–7** | **80–120** |
| WinUI3 Bound | **100–150** | **6–9** | **100–150** |
| Duct Current | **100–160** | **6–10** | **100–160** |
| Duct + Time-Sliced | **5–8** | **0–1** | **150–250** |

**Expected improvement:** Longest frame block drops from >100ms to <8ms. Total
mount time *increases* (overhead of yielding + resuming, plus mutations are
batched), but perceived smoothness is dramatically better — animations stay
fluid during mount. Trade-off: the UI appears incrementally over several frames
instead of all-at-once.

### Actual Results

| Variant | Longest Frame Block (ms) | Animation Drops | Total Mount (ms) |
|---|---|---|---|
| WinUI3 Direct | _pending_ | _pending_ | _pending_ |
| WinUI3 Bound | _pending_ | _pending_ | _pending_ |
| Duct Current | _pending_ | _pending_ | _pending_ |
| Duct + Time-Sliced | _pending_ | _pending_ | _pending_ |

---

## EXP-8: Prioritized State Updates

### Problem
All state updates are treated equally. A keystroke in a TextBox and a large list
filter update are processed in the same render pass. If the list filter is
expensive, it delays the keystroke feedback.

### Optimization
Implement a 2-tier priority system:

- **Sync** (default for `UseState` setters called from input event handlers):
  Processed immediately in the next `DispatcherQueue` tick.
- **Transition** (opt-in via `StartTransition(() => setFilteredItems(...))`):
  Deferred. Can be interrupted if a Sync update arrives. Shows stale UI during
  processing (the old list stays visible while the new one is computed).

Implementation: `RequestRender` tags each pending update with a priority.
`RenderLoop` processes Sync updates first. If a Transition render is in
progress and a Sync update arrives, abandon the Transition work and process
the Sync update.

**Precedent:** React 18's lanes system with `startTransition()`. Compose's
snapshot system coalesces low-priority updates.

### Test Scenario: `PerfBench.Priorities`

Workload:
- A **TextBox** (search input) + a **list of 5,000 items** filtered by the
  search text
- User types "abcdef" at ~10 chars/sec (100ms between keystrokes)
- Each keystroke triggers: (a) TextBox display update (Sync), (b) filter +
  re-render 5,000 items (Transition)
- Measure: keystroke-to-display latency, list update latency, dropped frames

WinUI3 baselines:
- **Direct**: TextBox.TextChanged filters an `ObservableCollection`, updates
  `ListView.ItemsSource` — all synchronous on UI thread
- **Bound**: TextBox bound to ViewModel, filter runs in setter, updates
  ObservableCollection — all synchronous

### Hypothesis

| Variant | Keystroke Latency (ms) | List Update Latency (ms) | Dropped Frames |
|---|---|---|---|
| WinUI3 Direct | **30–80** (blocked by filter) | **30–80** | **2–4 per keystroke** |
| WinUI3 Bound | **30–80** (blocked by filter) | **30–80** | **2–4 per keystroke** |
| Duct Current | **40–100** (blocked by filter + reconcile) | **40–100** | **3–6 per keystroke** |
| Duct + Priorities | **2–5** (immediate) | **100–200** (deferred) | **0** |

**Expected improvement:** Keystroke feedback becomes instant. List updates are
visibly delayed (stale content shown briefly), but the UI never janks. This is a
trade-off: total work is the same or slightly more, but perceived responsiveness
is dramatically better. Note that even WinUI3 Direct suffers here because the
filter is synchronous — Duct with priorities would actually *beat* vanilla WinUI3
on keystroke responsiveness.

### Actual Results

| Variant | Keystroke Latency (ms) | List Latency (ms) | Dropped Frames |
|---|---|---|---|
| WinUI3 Direct | _pending_ | _pending_ | _pending_ |
| WinUI3 Bound | _pending_ | _pending_ | _pending_ |
| Duct Current | _pending_ | _pending_ | _pending_ |
| Duct + Priorities | _pending_ | _pending_ | _pending_ |

---

## EXP-9: Lazy Off-Screen Mounting

### Problem
When a complex view loads (e.g., a tabbed settings page), all tabs' content is
mounted immediately even though only the active tab is visible. Components in
collapsed Expanders, hidden Pivots, and below-fold scroll content all pay full
mount cost upfront.

### Optimization
Introduce a `Deferred(element, estimatedSize?)` wrapper element. The reconciler
mounts a lightweight placeholder (empty Border with estimated size) instead of
the real element tree. When the placeholder becomes visible (enters viewport or
parent becomes visible), mount the real content.

Visibility detection:
- `EffectiveViewportChanged` on the placeholder for scroll-based visibility
- `Visibility` / `IsLoaded` tracking for tab/expander scenarios

**Precedent:** CSS `content-visibility: auto` defers layout/paint for off-screen
content. SwiftUI's `LazyVStack` defers view body evaluation until visible.
React's `<Suspense>` + `lazy()` defers component tree construction.

### Test Scenario: `PerfBench.DeferredMount`

Workload:
- A **Pivot** (tab control) with **5 tabs**, each containing 200 elements
  (1,000 total elements across all tabs)
- Only tab 0 is visible initially
- Measure: time from app launch to first interactive frame
- Then: measure time to switch to tab 1 (first visit, must mount 200 elements)

WinUI3 baselines:
- **Direct**: 5 panels with 200 TextBlocks each, all created at startup.
  Only the active panel is visible.
- **Bound**: Same with bindings, all created at startup.

### Hypothesis

| Variant | Initial Render (ms) | Tab Switch (ms) | Peak Memory (MB) |
|---|---|---|---|
| WinUI3 Direct | **80–100** (all 1000 created) | **< 1** (already exists) | **60** |
| WinUI3 Bound | **100–130** (all 1000 created) | **< 1** (already exists) | **65** |
| Duct Current | **100–140** (all 1000 rendered + reconciled) | **< 1** (already mounted) | **70** |
| Duct + Deferred | **25–35** (only 200 for active tab) | **25–35** (mount on demand) | **30** (grows as tabs visited) |

**Expected improvement:** 3–5× faster initial render. Memory proportional to
visible content only. Trade-off: first visit to each tab has a mount cost.
This matches the web/mobile pattern where faster initial load + lazy tab
content is preferred over slower initial load + instant tabs.

### Actual Results

| Variant | Initial Render (ms) | Tab Switch (ms) | Peak Memory (MB) |
|---|---|---|---|
| WinUI3 Direct | _pending_ | _pending_ | _pending_ |
| WinUI3 Bound | _pending_ | _pending_ | _pending_ |
| Duct Current | _pending_ | _pending_ | _pending_ |
| Duct + Deferred | _pending_ | _pending_ | _pending_ |

---

## EXP-10: Arena Allocation for Element Trees

### Problem
Every render cycle allocates a new tree of `Element` record instances and
`Element[]` arrays. These are short-lived (only needed until reconciliation
compares them with the mounted state) and create Gen0 GC pressure proportional
to tree size on every frame.

### Optimization
Use an `ArrayPool<T>`-backed arena for element arrays, and explore `struct`
element variants for leaf nodes (TextElement, ImageElement) to avoid heap
allocation entirely.

Two sub-experiments:
- **10a: Array pooling** — `Element[]` children arrays rent from `ArrayPool`
  and return after reconcile. Requires a `RenderScope` that tracks rentals.
- **10b: Struct leaf elements** — Convert frequently-used leaf element types
  to `readonly record struct`. Requires boxing when stored in `Element[]` but
  avoids heap allocation for the element itself. Alternative: use a
  discriminated-union-style `LeafElement` struct with a type tag.

**Precedent:** Compose's slot table uses flat `int[]` and `Any?[]` arrays
(gap buffer) instead of heap-allocated tree nodes. Game engines use bump/arena
allocators for per-frame scratch data. ECS frameworks use slab allocation.

### Test Scenario: `PerfBench.Allocation`

Workload:
- **StressPerf grid** (4,800 cells) at 100% update rate, 30 Hz (worst case
  allocation pressure — every element reallocated every frame)
- Measure: GC Gen0 collections over 10 seconds, GC pause time, peak working set
- Use `GC.CollectionCount(0)` and `GC.GetGCMemoryInfo()` for precise tracking

WinUI3 baselines:
- **Direct**: No per-frame allocation (mutates existing controls in place)
- **Bound**: ViewModels are long-lived, minimal per-frame allocation (just
  property change events)

### Hypothesis

| Variant | Gen0 Collections / 10s | Avg GC Pause (ms) | Peak Memory (MB) |
|---|---|---|---|
| WinUI3 Direct | **< 5** | **< 0.5** | **140** |
| WinUI3 Bound | **< 10** | **< 0.5** | **150** |
| Duct Current | **50–100** | **1.0–2.0** | **170** |
| Duct + Array Pool | **20–40** | **0.5–1.0** | **155** |
| Duct + Struct Leaves | **10–20** | **< 0.5** | **145** |

**Expected improvement:** Moderate. .NET's Gen0 GC is already very fast, so
the wall-clock impact may be small (< 1ms per frame). The real benefit is
reducing GC pauses that occasionally cause frame drops. This experiment has
the lowest expected impact and highest implementation complexity — pursue only
if GC is measured as a bottleneck in real workloads.

### Actual Results

| Variant | Gen0 / 10s | Avg GC Pause (ms) | Peak Memory (MB) |
|---|---|---|---|
| WinUI3 Direct | _pending_ | _pending_ | _pending_ |
| WinUI3 Bound | _pending_ | _pending_ | _pending_ |
| Duct Current | _pending_ | _pending_ | _pending_ |
| Duct + Array Pool | _pending_ | _pending_ | _pending_ |
| Duct + Struct Leaves | _pending_ | _pending_ | _pending_ |

---

## Implementation Priority

Recommended order based on expected impact / effort ratio:

```
Phase 1 — Low-hanging fruit (high impact, moderate effort)
  EXP-1  Dirty Subtree Tracking          ██████████  10–30× for sparse updates
  EXP-3  Structural Sharing              ████████    2–4× for stable layouts
  EXP-2  Property Diff Bitmasks          ███████     2–3× reconcile time

Phase 2 — Architectural (high impact, high effort)
  EXP-5  Mutation Journaling             ██████      Enabler for Phase 3
  EXP-6  Interactive Control Pooling     ██████      3–5× mount cost in scrolling

Phase 3 — Advanced (requires Phase 2)
  EXP-4  Off-Thread Tree Building        ████████    50% less UI thread blocking
  EXP-7  Time-Sliced Reconciliation      ██████      Eliminates jank on large mounts

Phase 4 — Specialized
  EXP-8  Prioritized State Updates       ██████      Best-in-class input responsiveness
  EXP-9  Lazy Off-Screen Mounting        █████       3–5× faster initial render
  EXP-10 Arena Allocation                ███         Marginal GC improvement
```

---

## Test Infrastructure Requirements

Each experiment produces a standalone benchmark app following the StressPerf
pattern:

```
tests/perf_bench/
├── PerfBench.Shared/           # Extended PerfTracker (GC metrics, input latency)
├── PerfBench.DirtyTracking/
│   ├── DirtyTracking.Direct/   # WinUI3 baseline
│   ├── DirtyTracking.Bound/    # WinUI3 + data binding baseline
│   └── DirtyTracking.Duct/     # Duct variant (toggle optimization on/off via flag)
├── PerfBench.PropertyDiff/
│   └── ...
└── ...
```

### Extended PerfTracker Metrics

Beyond the existing FPS / Update ms / Memory metrics, experiments need:

| Metric | How | Used By |
|---|---|---|
| UI thread blocked ms | `Stopwatch` around reconcile-only portion | EXP-4 |
| Input latency ms | Timestamp in KeyDown → timestamp when TextBlock updates | EXP-4, EXP-8 |
| GC Gen0/Gen1 collections | `GC.CollectionCount(n)` delta per interval | EXP-10 |
| GC pause time | `GC.RegisterForFullGCNotification` or ETW | EXP-10 |
| Mutation count | Counter in journal | EXP-5 |
| Longest frame block | `CompositionTarget.Rendering` delta max | EXP-7 |
| Animation frame drops | Count frames where delta > 20ms | EXP-7 |

### CLI Flag for A/B Toggle

Duct benchmark apps accept `--optimization on|off` to enable/disable the
specific experiment, allowing A/B comparison in a single binary:

```bash
# Current behavior (optimization off)
PerfBench.DirtyTracking.Duct.exe --headless --optimization off --duration 10

# With optimization
PerfBench.DirtyTracking.Duct.exe --headless --optimization on --duration 10
```
