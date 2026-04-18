# Async Resources — Implementation Tasks

Reference: [docs/specs/020-async-resources-design.md](../020-async-resources-design.md)
Tracking issue: [microsoft/microsoft-ui-reactor#14](https://github.com/microsoft/microsoft-ui-reactor/issues/14)

---

## Test classification

Async resources have three large sources of bugs: **identity** (cache keying, hook slot indexing), **lifecycle** (mount/unmount/deps-change ordering vs. in-flight tasks), and **threading** (UI dispatcher marshalling, cancellation token propagation, `QueryCache` concurrent access). Each surface has a matching test tier.

| Level | Project | What it exercises | Speed | Used for |
|---|---|---|---|---|
| **Unit — pure** | `Reactor.Tests` (xUnit) | `AsyncValue<T>`, `QueryCache`, key derivation, reducer-style state transitions. No `RenderContext`, no threads. | <1ms | ADT equality, cache algorithms, LRU eviction, key hashing |
| **Unit — RenderContext** | `Reactor.Tests` (xUnit) | Hook behaviour driven through `RenderContext` directly. Deterministic `TaskCompletionSource` fetchers. No WinUI controls, no real dispatcher. | ~1-5ms | Hook slot order, cache miss/hit, deps-change cancellation, sync-complete fast path, re-render short-circuit |
| **Unit — threading** | `Reactor.Tests` (xUnit, `[Trait("Category","Threading")]`) | Multi-threaded stress over `QueryCache`, `InfiniteResource` pull-model, and hook subscription counts. Uses real `Task.Run` + `Barrier` / `CountdownEvent`. | ~10-500ms | Race conditions: concurrent `TryGet`/`Set`, subscriber-count underflow, double-cancel, stale-task-landing-after-unmount, deps-change-mid-flight |
| **Selfhost** | `Reactor.AppTests.Host` (TAP) | Real WinUI window + `ReactorHost` + real `DispatcherQueue`. Controlled fetchers. Fixtures drive multiple frames with `DispatcherQueue.TryEnqueue`. | ~10-50ms per fixture | Dispatcher marshalling, hook state across real render frames, framerate-paced edge cases, scroll-driven paging, `Pending` subtree bubble-up |
| **Selfhost — framerate** | `Reactor.AppTests.Host` (TAP, `--self-test --filter "AsyncResource.Framerate"`) | Same as selfhost but each fixture drives 60+ frames at `CompositionTarget.Rendering` cadence, mutating deps / firing refetches / scrolling on each frame. | ~500ms-2s per fixture | Regression budget for "thing works in isolation, breaks at 60Hz" bugs: deps thrashing, scroll-driven `EnsureRange` flooding, cache-LRU churn, re-render storm detection |
| **E2E UIA** | `Reactor.AppTests` (MSTest + Appium) | Full app, cross-process UIA. | Slow | Regression only. Nothing in this spec needs a new E2E test — `Pending` fallbacks are visible to AT but reuse `Skeleton` which is already covered. |

**Parity tests for Phase 3** (`UseInfiniteResource` vs. `DataPageCache`) run in both unit and selfhost tiers — the cache semantics unit-test cleanly, but DataGrid's scroll-driven range prefetch only reproduces in the selfhost tier.

**Threading-test conventions:**

- Use `TaskCompletionSource<T>` (not `Task.Delay`) to control completion ordering deterministically.
- Use `Barrier(N)` to force N threads to the same instant of execution before releasing.
- Assert **no unobserved `TaskCanceledException`** — subscribe to `TaskScheduler.UnobservedTaskException` at fixture start and fail the test if it fires.
- Use a fake `IDispatcherQueue` that records call ordering and lets the test advance the queue synchronously. Real `DispatcherQueue.TryEnqueue` is reserved for selfhost.
- Every cancellation test asserts the fetcher's `CancellationToken.ThrowIfCancellationRequested()` actually fired — it's not enough that `deps` changed.

---

## Phase 1 — Foundation (`AsyncValue<T>`, `QueryCache`, `UseResource<T>`)

Scope: new files `Reactor/Core/AsyncValue.cs`, `Reactor/Core/QueryCache.cs`, `Reactor/Core/Hooks/UseResource.cs`, plus hook registration in `RenderContext.cs`. No DataGrid dependency. No reconciler changes.

### 1.1 `AsyncValue<T>` ADT (§5)

- [x] Create `Reactor/Core/AsyncValue.cs`
- [x] Define `abstract record AsyncValue<T>` with nested sealed records `Loading`, `Data(T Value)`, `Error(Exception Exception)`, `Reloading(T Previous)`
- [x] Implement `.Match<TResult>(loading, data, error, reloading = null)` convenience method per spec §5 — `reloading ?? data` fallback
- [x] Add XML docs linking each state to the transition rules in §6.2
- [x] Verify record equality: `Data(5)` equals `Data(5)`, `Reloading(5)` does not equal `Data(5)`

#### Tests — unit (pure)

- [x] Construct every state; assert type-discrimination via pattern match
- [x] `Match` invokes the correct delegate for each state; `reloading` fallback lands in `data` when null
- [x] Record equality for each pair (same-state-same-value, same-state-different-value, different-state-same-value)
- [x] Equality when `T` is itself an `AsyncValue<_>` (nested) — guard against regression if callers accidentally wrap
- [x] Equality when `T` holds a mutable reference type and the underlying value mutates (equality should still be reference-equal for the record; document this)
- [x] Exhaustive `switch` expression handles all four arms (C# 12 still emits CS8509 for private-protected-sealed hierarchies; documented in the test)

### 1.2 `QueryCache` (§9)

- [x] Create `Reactor/Core/QueryCache.cs`
- [x] Define `sealed record CacheEntry<T>(T Value, DateTime FetchedAt, TimeSpan StaleTime, int SubscriberCount)` — note `SubscriberCount` is mutated via `with`, never in-place
- [x] Implement `sealed class QueryCache` with: `TryGet<T>`, `Set<T>`, `Invalidate(string)`, `InvalidatePattern(string)`, `Clear()`, `event Action<string>? EntryChanged`
- [x] Internal storage: `ConcurrentDictionary<string, Slot>` with typed cast checked at `TryGet` boundary
- [x] Implement subscriber tracking: `Subscribe(key)` / `Unsubscribe(key)` return ref-count; eviction runs via single shared timer
- [x] Eviction timer uses a single shared `Timer` polling evictable entries every N seconds, not one timer per entry (avoid timer storm)
- [x] `InvalidatePattern` walks keys with `StartsWith(prefix)` — document O(n) semantics in XML doc
- [x] `EntryChanged` fires on UI dispatcher if `DispatcherPost` set; otherwise inline
- [x] Install default `QueryCache` at app root via `Context<QueryCache>` in `ReactorApp.Run` / `ReactorHost` bootstrap

#### Tests — unit (pure)

- [x] Cache miss → `TryGet` returns false
- [x] Cache hit within `StaleTime` → `TryGet` returns entry, no `Reloading` flag required (hook-side concern)
- [x] Cache hit past `StaleTime` → `TryGet` still returns the entry; "stale" decision is caller-side via `FetchedAt + StaleTime < Now`
- [x] `Invalidate` removes the entry and fires `EntryChanged`
- [x] `InvalidatePattern("user/")` removes `user/1`, `user/2`; leaves `employees/1`
- [x] `Clear` removes all entries and fires `EntryChanged` for each
- [x] Entry past `CacheTime` with zero subscribers is evicted on the next eviction-timer tick
- [x] Entry past `CacheTime` with ≥1 subscriber is **not** evicted
- [x] Mismatched-type `TryGet<string>` on an entry stored as `CacheEntry<int>` returns false (does not throw)

#### Tests — unit (threading — race conditions)

- [x] **Concurrent `Set` + `TryGet`.** 4 threads `Set`ing the same key with different values; 4 threads `TryGet`ing. No torn reads, no exceptions.
- [x] **Concurrent `Subscribe` / `Unsubscribe`.** 8 threads each call `Subscribe` N times then `Unsubscribe` N times on the same key. Final count converges; no negative count observed.
- [x] **Subscriber-count never goes negative.** `Unsubscribe` on a key with zero subscribers throws `InvalidOperationException` (defensive).
- [x] **Invalidate-during-Set.** Races between `Set` and `Invalidate` never expose a value other than the one that was set.
- [x] **Eviction timer vs. Subscribe race.** `IsEvicted` flag on slots forces Subscribe to retry with a fresh slot if eviction races it.
- [x] **`InvalidatePattern` while keys are being added.** Snapshot-iterate pattern; no `InvalidOperationException` from concurrent modification.
- [x] **`EntryChanged` fires exactly once per `Set`.** 100 `Set` calls → exactly 100 `EntryChanged` events.
- [x] **Stress: 1000 keys × 8 threads × mixed ops** — 500ms random-op stress terminates without exceptions.

### 1.3 `UseResource<T>` hook (§6)

- [x] Create `Reactor/Hooks/UseResource.cs` (extension on `RenderContext`, following the UseAnnounce convention)
- [x] Register a `ResourceHookState<T>` stored in `UseRef`; fields: `CancellationTokenSource? Cts`, `string CacheKey`, `AsyncValue<T> LastValue`, `bool InFlight`, `IHookDispatcher? Dispatcher`
- [x] Define `ResourceOptions(TimeSpan? StaleTime, TimeSpan? CacheTime, int RetryCount, bool RefetchOnMount, string? CacheKey)` record with defaults per D11 (`StaleTime = Zero`, `CacheTime = 5min`)
- [x] Implement hook body per §6.2 six-step state machine (cache-miss/hit-fresh/stale, deps-change, unmount)
- [x] Capture dispatcher at hook-registration time (COMException-safe for unit-test hosts)
- [x] Continuation marshals result via `IHookDispatcher.Post` before writing to cache
- [x] Re-render short-circuit: compare new `AsyncValue<T>` to `LastValue` by record equality
- [x] Deps compared via default equality (matches existing UseEffect/UseMemo convention)
- [x] Automatic retry with exponential backoff when `RetryCount > 0`: delay = `2^attempt * 100ms`, cancellable via the hook-owned token
- [x] **No special-case `TaskCanceledException`**: if token fires, result is dropped silently

#### Tests — unit (RenderContext, deterministic fetcher)

- [x] First render with pending `Task<T>` → returns `Loading`; after `TaskCompletionSource.SetResult`, next render returns `Data(value)`
- [x] First render with `Task.FromResult(value)` sync-complete → returns `Data(value)` in the **same** render (no `Loading` flash)
- [x] First render with `Task.FromException<T>(...)` sync-faulted → returns `Error(ex)` in the same render
- [x] Cache hit fresh (within `StaleTime`) → second `UseResource` with same deps returns `Data` immediately, fetcher **not** invoked
- [x] Cache hit stale (past `StaleTime`) → returns `Reloading(previous)` and fetcher is invoked
- [x] Deps change → cancellation token of previous fetch is cancelled; new fetch starts with new cache key
- [x] Unmount while in-flight → token cancelled; late result dropped; cache not updated
- [x] Unmount while completed → cache retains entry (`Unmount_After_Completion_Retains_Cache_Entry`); subscriber count decrement is asserted end-to-end in the QueryCache eviction tests
- [x] `RefetchOnMount = false` → cache-only; cache miss returns `Loading`; no fetch issued
- [x] Two siblings with the same auto-derived key **do not** share by default (D6); explicit `CacheKey = "shared"` makes them share
- [x] `RetryCount = N` → fetcher fails then succeeds; exact invocation count asserted
- [x] Retry exhausted → surfaces final `Error`
- [x] Re-render short-circuit verification (`Identical_Data_Across_Refetches_Skips_Rerender`)
- [~] Non-idempotent fetcher behaviour (stale result drop via cancellation) — implicit in deps-change-mid-flight test; no dedicated test

#### Tests — unit (threading — race conditions)

- [x] **Deps-change mid-flight.** A's result dropped; B's result lands.
- [x] **Sibling cache sharing.** 10 sibling renders with same CacheKey → 1 fetch.
- [x] **Unmount during pending fetch.** Marshalled callback is a no-op; cache not updated.
- [x] **Cancel during fetcher body (`Task.Delay(ct)`)** → silent, no Error, no unobserved exception.
- [ ] **Concurrent invalidation refetch storm** — not explicitly tested yet
- [x] **Dispatcher-missing path** — inline continuation verified in `No_Dispatcher_Still_Updates_State_Via_Inline_Path`.
- [x] **`UnobservedTaskException` never fires** — each threading test subscribes and asserts zero.

#### Tests — selfhost (real dispatcher, framerate)

Location: `tests/Reactor.AppTests.Host/SelfTest/Fixtures/AsyncResourceFixtures.cs` (new) with filter prefix `AsyncResource.*`.

- [ ] `AsyncResource.BasicResolve` — single fetch, `Task.Delay(16ms)` fetcher, asserts visible `Loading → Data` transition over 2 frames
- [ ] `AsyncResource.SyncCompleteNoFlash` — `Task.FromResult` fetcher; asserts `Loading` was **never observed** across 10 rendered frames (captures per-frame snapshot, asserts only `Data`)
- [ ] `AsyncResource.StaleWhileRevalidate` — cache hit past `StaleTime`; asserts the stale subtree remained on screen during refetch (opacity / content unchanged for N frames until new `Data`)
- [ ] `AsyncResource.DepsChangeCancel` — deps change every frame for 60 frames, each `fetcher` takes 50ms; asserts only the **last** deps value's result is visible and no stale value flashes at any frame
- [ ] `AsyncResource.UnmountDuringFetch` — 100 remount-with-fetch cycles; asserts after the run: zero unobserved task exceptions, cache subscriber count == 0, memory has not grown unboundedly (`GC.GetTotalMemory` delta under a threshold)

#### Tests — selfhost (framerate edge cases)

Location: same file, fixture names `AsyncResource.Framerate.*`. These drive 60 frames via `CompositionTarget.Rendering` (or the TestApp-equivalent frame tick used by animation fixtures) and assert per-frame invariants.

- [ ] `AsyncResource.Framerate.DepsThrashing` — `deps` hashcode changes every frame (text input simulation); asserts no more than one in-flight fetch at any frame and no un-cancelled task ever completes
- [ ] `AsyncResource.Framerate.RenderShortCircuit` — invalidate and resolve to **the same** `Data(value)` on every frame for 60 frames; asserts number of component `Render()` invocations == 1 (short-circuit held)
- [ ] `AsyncResource.Framerate.CacheChurn` — 16 siblings rotating cache keys every frame; asserts LRU eviction keeps the working set bounded and `QueryCache` entry count ≤ configured cap
- [ ] `AsyncResource.Framerate.FastRemount` — component mounted and unmounted every frame for 60 frames, each mount kicking off a 200ms fetch; asserts no runaway task list, zero unobserved exceptions, zero leaked `CancellationTokenSource` instances (use a weak-reference probe)
- [ ] `AsyncResource.Framerate.DispatcherPressure` — 1000 `TryEnqueue` callbacks queued per frame across 60 frames; asserts the hook's marshalling does not starve the dispatcher (fixture completes within budget, no frame skipped)

### 1.4 Dogfood: `AsyncValueSamples` page (§16 Phase 1)

- [x] Add `samples/Reactor.TestApp/Demos/AsyncValueSamplesDemo.cs` (wired into App.cs tab list)
- [x] **1a. Deterministic fake fetcher** — succeed / fail / slow buttons
- [x] **1b. Sync-complete fetcher** — `Task.FromResult`
- [x] **1c. Deps-change cancellation** — text input drives deps
- [x] **1d. Two siblings, one cache key** — explicit `CacheKey: "demo/shared"`
- [x] **1e. Cache hit across remount** — Hide/Show toggle within `StaleTime`
- [ ] Wire each scenario to the TestApp snapshot suite (selfhost fixtures — see §1.3 selfhost tests)

**Phase 1 exit criteria:** Every phase-1 unit, threading, and selfhost test green; all five `AsyncValueSamples` scenarios pass in the TestApp snapshot suite; no unobserved task exceptions across the full suite.

---

## Phase 2 — `UseInfiniteResource` (cursor paging on top of phase-1 cache)

Scope: new files `Reactor/Core/Hooks/UseInfiniteResource.cs`, `Reactor/Data/DataSourceResourceExtensions.cs`. Parity with existing `Reactor/Data/DataPageCache.cs` — do **not** delete it yet (phase 3).

### 2.1 `Page<TItem, TCursor>` + `LoadState` ADT (§7.1)

- [x] Add `Page<TItem, TCursor>(IReadOnlyList<TItem> Items, TCursor? NextCursor, int? TotalCount = null)` record
- [x] Define `abstract record LoadState` with sealed `Loading`, `Idle`, `EndOfList`, `Error(Exception Exception)`
- [x] Exhaustive pattern-match coverage test (`AsyncValueTests.LoadState_*`)

### 2.2 `InfiniteResource<TItem>` class (§7.1)

- [x] Create `sealed class InfiniteResource<TItem>` with full public surface
- [x] Internal storage: page table + flattened `IReadOnlyList<TItem?>` facade with null placeholder slots
- [x] `ItemAt(i)` semantics match spec (loaded / in-flight / not-requested / past-end)
- [x] `EnsureRange(first, last)` computes covering pages, dedups, fires in order
- [x] `Refresh()` invalidates cache, clears page table, restarts
- [x] `FetchNext()` no-ops outside `Idle`+`HasMore`
- [ ] UI-thread affinity assertion (deferred — currently the resource is internally thread-safe)
- [x] `LoadState` transitions drive re-render via the hook subscription

### 2.3 `UseInfiniteResource<TItem, TCursor>` hook (§7.2)

- [x] Create `Reactor/Hooks/UseInfiniteResource.cs`
- [x] `InfiniteHookState<TItem, TCursor>` with an owned `InfiniteResource<TItem>` + per-page CTS map
- [x] First render: Loading, empty Items, schedule first page with cursor=null
- [x] Page completion: append items, update NextCursor, flip LoadState to Idle/EndOfList
- [x] Deps change: cancel in-flight, clear page table, restart (prior pages remain in QueryCache under old key)
- [x] Unmount: cancel in-flight, unsubscribe all page keys
- [x] Per-page cache keys of shape `{prefix}/{depsHash}/page:{pageIndex}`
- [x] QueryCache subscriber tracking (each UseInfiniteResource subscribes to N page keys)

### 2.4 `DataSourceResource.UseDataSource` adapter (§7.3)

- [x] Create `Reactor/Data/DataSourceResourceExtensions.cs`
- [x] `UseDataSource<T>` delegates to `UseInfiniteResource<T, string>`
- [x] Deps: `[source, request.Sort, request.Filters, request.SearchQuery]`
- [ ] Parity sweep with existing data sources (spot-checked via InMemorySource in tests; GraphQLDataSource / ListDataSource not yet exercised against the new hook)

### 2.5 Phase-2 tests

#### Tests — unit (RenderContext)

- [x] First render → page-1 fetch scheduled with `cursor = null`; sync-complete fast-path collapses Loading → Idle when fetcher is synchronous
- [x] Page completes with NextCursor → Items populated, LoadState = Idle, HasMore=true
- [x] Page completes with NextCursor=null → LoadState = EndOfList, HasMore=false
- [x] `ItemAt(i)` on loaded page returns the item
- [x] `ItemAt(i)` on unloaded page → triggers fetch
- [x] `ItemAt(i)` past known end (via TotalCount) returns null, no fetch
- [x] `EnsureRange` covers overlapping pages, dedups
- [x] `Refresh()` length preservation (`Refresh_With_Same_TotalCount_Preserves_Items_Length`)
- [x] `Retry()` on Error refetches the failed page
- [x] Deps change cancels in-flight and restarts
- [x] LRU eviction over `MaxLoadedPages` cap
- [ ] Placeholder count exact-match with DataPageCache (parity sweep deferred)

#### Tests — unit (threading — race conditions)

- [x] **Concurrent `ItemAt` across pages.** 8 threads call `ItemAt(random index)`; assert every page requested ≤ 2× under contention (coalescing). This surfaced and fixed a claim-before-callback gap in `InfiniteResource.ItemAt`/`EnsureRange`.
- [x] **`EnsureRange` + completing page race.** `EnsureRange` never re-requests a page already loaded or in-flight.
- [x] **Deps change mid-page-fetch.** Late completion of an old-deps page is dropped.
- [x] **`Refresh()` during paging.** Refresh cancels in-flight, late completion silently ignored, restart proceeds cleanly.
- [x] **Unmount during in-flight fetch.** Pending completion on background thread drops silently, cache never written.
- [x] **Scroll-driven `EnsureRange` flood.** 1000× overlapping ranges coalesce to ≤ distinct page count.

#### Tests — unit (parity with `DataPageCache<T>`)

These tests instantiate **both** `DataPageCache<T>` and `UseInfiniteResource`-backed equivalents and assert identical observable behaviour. Live in `tests/Reactor.Tests/Data/DataPageCacheParityTests.cs`.

- [x] Same LRU eviction order for the same access pattern
- [x] Same placeholder (`null`) positions in the flat item list for a given scroll window
- [x] Hook's **stricter** cancellation-on-deps-change semantics (late completion dropped; legacy writes stale)
- [x] Same "block loaded" observer semantics (DataGrid's `BlockLoaded` event ↔ hook's re-render subscription)
- [x] Same behaviour for fetcher that throws mid-page
- [~] Fetcher returns empty page before `EndOfList` — legacy-only (hook treats empty-cursor pages as undefined per design; documented in test)

#### Tests — selfhost

- [ ] `AsyncResource.InfiniteBasic` — scroll through 5 pages, assert `Items` fills in order
- [ ] `AsyncResource.InfinitePlaceholder` — scroll to page 3 before page 2 resolves; assert placeholder rows visible on-screen and resolve in-order
- [ ] `AsyncResource.InfiniteRefresh` — `Refresh()` while scrolled to row 150; assert scroll preserved (consumer-side contract from D17)
- [ ] `AsyncResource.InfiniteDepsChange` — search-as-you-type over a paginated list; assert stale pages cancel cleanly mid-scroll

#### Tests — selfhost (framerate edge cases)

- [ ] `AsyncResource.Framerate.ScrollFlood` — simulate 60Hz scroll across 1000 rows for 60 frames; assert `ItemAt` calls are coalesced and total in-flight fetches stays bounded (≤ prefetch window)
- [ ] `AsyncResource.Framerate.RapidEnsureRange` — `EnsureRange` called 4× per frame with jittered ranges for 60 frames; assert no duplicate page fetches and no torn `Items` length observed at any frame
- [ ] `AsyncResource.Framerate.RefreshMidScroll` — `Refresh()` invoked on every 10th frame while scrolling; assert no `NullReferenceException` from `ItemAt` reading a torn page table and no visible flicker beyond one frame
- [ ] `AsyncResource.Framerate.LruChurn` — working set larger than LRU capacity, scroll forward and backward every frame for 60 frames; assert LRU correctness holds (no loaded-page-resurrection-as-placeholder at any frame)
- [ ] `AsyncResource.Framerate.ParallelPages` — 20 pages requested simultaneously; assert completion order does not corrupt `Items` (each page lands in its own slot regardless of arrival order)

### 2.6 Phase-2 dogfood (§16 Phase 2)

- [x] Infinite-list demo on the `AsyncValueSamples` page — pull-model via `ItemAt`, null-placeholder rows (`InfiniteScrollScenario`, uses `VirtualListDsl.VirtualList`)
- [x] Search-as-you-type over an infinite list — deps + pull model together (`SearchInfiniteScenario`)
- [x] `Refresh()` demo — button-triggered, epoch-stamped values prove the swap (`InfiniteRefreshScenario`)
- [x] `UseDataSource` adapter demo — `IDataSource<T>` wired through the hook (`DataSourceAdapterScenario`)
- [ ] Port `samples/apps/regedit/Components/ValueList.cs` to `UseInfiniteResource` (deferred — separate PR)

**Phase 2 exit criteria:** Parity tests green against `DataPageCache`; regedit `ValueList` runs on `UseInfiniteResource`; all framerate fixtures pass on both x64 and ARM64 in CI.

---

## Phase 3 — `UseMutation` + DataGrid migration

Scope: new `Reactor/Core/Hooks/UseMutation.cs`; modifications to `Reactor/Controls/DataGrid/DataGridComponent.cs` and `DataGridState.cs`; deletion of `Reactor/Data/DataPageCache.cs` gated behind a production-soak window.

### 3.1 `UseMutation<TInput, TResult>` hook (§8)

- [x] Create `Reactor/Hooks/UseMutation.cs`
- [x] Define `sealed class Mutation<TInput, TResult>` with `IsPending`, `Error`, `LastResult`, `Task<TResult> RunAsync(TInput)`, `Reset()`
- [x] Define `sealed record MutationOptions<TInput, TResult>(Action<TInput>? OnOptimistic, Action<TResult, TInput>? OnSuccess, Action<Exception, TInput>? OnError, string[]? InvalidateKeys)`
- [x] `RunAsync`:
  - Fire `OnOptimistic(input)` synchronously (no dispatcher hop)
  - Kick off `mutator(input, ct)` on thread pool
  - On success: marshal back to UI dispatcher, fire `OnSuccess`, `cache.Invalidate(key)` for each `InvalidateKeys`, update `LastResult`
  - On failure: marshal back, fire `OnError`, store `Error`
  - Overlapping calls: concurrent `RunAsync` calls each get their own token; latest `LastResult` wins (document: callers who want serialization wrap `RunAsync` with their own gate)
- [x] `Reset()` clears `Error`, `LastResult`, `IsPending` (does not cancel in-flight — explicit choice; document)
- [x] Hook-owned `CancellationToken` cancels on unmount

### 3.2 `UseMutation` tests

#### Unit (RenderContext)

- [x] `IsPending` true during fetch, false after completion
- [x] `OnOptimistic` fires before the task starts (synchronous)
- [x] `OnSuccess` fires on UI thread, after `OnOptimistic`, before `IsPending` drops
- [x] `OnError` fires on UI thread; `Error` populated; `LastResult` unchanged
- [x] `InvalidateKeys` trigger `cache.Invalidate` on each key on success (not on error)
- [x] `Reset()` clears fields
- [x] Unmount during pending → token cancelled; `OnSuccess` / `OnError` do **not** fire after unmount

#### Unit (threading — race conditions)

- [x] **Overlapping `RunAsync`.** Call `RunAsync(a)` then `RunAsync(b)` before `a` resolves. Both complete; `LastResult` is the later-completing one; both callbacks fire in completion order. No torn `IsPending` (should be true until both resolve).
- [x] **`OnOptimistic` crashes.** If `OnOptimistic` throws, mutation aborts: `RunAsync` returns a faulted task; `mutator` never invoked. Document this — prevents half-applied state.
- [x] **`InvalidateKeys` while mutation pending.** Manual `cache.Invalidate` call for the same key during pending; assert the post-success invalidate still fires (idempotent).
- [x] **Cancellation of pending mutation via unmount.** Assert `OnError` **does not** fire on `OperationCanceledException` from unmount (consistent with `UseResource` — cancellation is silent).

#### Selfhost

- [ ] `AsyncResource.MutationOptimistic` — button press triggers optimistic update; assert UI reflects optimistic value before the real response lands
- [ ] `AsyncResource.MutationRollback` — same but fetcher fails; assert UI reverts via `OnError`
- [ ] `AsyncResource.MutationInvalidates` — mutation with `InvalidateKeys = ["employees.list"]`; assert a sibling `UseResource` of that key re-renders with fresh data

### 3.3 DataGrid migration (§11)

- [ ] Add `ReactorFeatureFlags.UseHookBasedPaging` (default **off**)
- [ ] Refactor `DataGridComponent.Render()` to call `this.UseDataSource(Props.Source, Props.Request)` when flag is on, else fall back to legacy `DataPageCache` path
- [ ] Keep the 32ms settle timer in `DataGridComponent` (rendering concern, per §11 table)
- [ ] Replace `state.EnsureRangeLoaded(first, last)` call site with `resource.EnsureRange(first, last)` inside the flag gate
- [ ] Replace `CacheBlock<T>.LoadingBlock(index)` placeholder check with `InfiniteResource<T>.Items[i] == null` check
- [ ] Remove private `BlockLoaded` event wiring under the flag — hook re-render subscription replaces it
- [ ] Port `DataGridState.BeginAsyncCommit` / `CompleteAsyncCommit` / `FailAsyncCommit` to `UseMutation` (`OnOptimistic` / `OnSuccess` / `OnError`)
- [ ] CI matrix: run the full DataGrid selfhost suite with both `UseHookBasedPaging = false` **and** `= true`. Fail the build on any divergence in reconciler output.

#### Tests — parity (both paths in CI)

- [ ] Every existing `DataGridPagingFixtures` test passes on both paths
- [ ] Every existing `DataGridScrollFixtures` test passes on both paths
- [ ] Every existing `DataGridEditFixtures` test passes on both paths (covers the `UseMutation` port)
- [ ] Add `DataGridParityFixtures.cs` that asserts rendered tree equality (via `TreeSerializer`) frame-by-frame during scroll across 60 frames, on both paths

#### Tests — selfhost (framerate regression for DataGrid)

- [ ] `AsyncResource.Framerate.DataGridScroll` — scroll 10 000 rows at 60Hz for 120 frames on the new path; assert no placeholder flicker, no dropped frames, LRU working-set stable
- [ ] `AsyncResource.Framerate.DataGridEditMutation` — rapid cell edits (one per frame for 60 frames), each firing a `UseMutation`; assert optimistic updates render immediately, server responses settle without visual regression, `IsPending` never gets stuck true

### 3.4 HeadTrax `EmployeeGrid` port

- [ ] Enable `UseHookBasedPaging` by default on the HeadTrax sample
- [ ] Keep a reversion path for one release cycle
- [ ] Dogfood in production for at least one release cycle before deletion below

### 3.5 Delete `DataPageCache`

- [ ] Verify `UseHookBasedPaging = true` is the default and no consumer still reads `DataPageCache`
- [ ] Delete `Reactor/Data/DataPageCache.cs`
- [ ] Delete `DataGridState` fields and methods that were only used by the legacy path
- [ ] Delete `ReactorFeatureFlags.UseHookBasedPaging`
- [ ] Update docs / CLAUDE.md references

**Phase 3 exit criteria:** DataGrid public API unchanged; `DataPageCache.cs` deleted; all selfhost DataGrid tests green on the hook path; HeadTrax `EmployeeGrid` soaked through one release.

---

## Phase 4 — Polish (`Pending`, focus revalidation, analyzer diagnostics)

### 4.1 `Pending` element (§10)

- [ ] Create `Reactor/Elements/Pending.cs`
- [ ] Define `Pending(Element fallback, Element child)` element record
- [ ] Define `PendingScope` context carrying a registry of `AsyncValue.Loading` resources in the subtree
- [ ] Every `UseResource` / `UseInfiniteResource` registers with the nearest ancestor `PendingScope` on mount, unregisters on unmount / deps change
- [ ] `Pending` renders `fallback` iff any registered resource is in `Loading` state (not `Reloading` — explicit per §10.1)
- [ ] Subtree still renders fully in the background so it's ready to swap in when all resources resolve

#### Tests

- [ ] Unit: `PendingScope` ref-count transitions (one resource Loading → 1; resolves → 0; scope renders child)
- [ ] Unit: `Reloading` does **not** trigger the fallback (only `Loading`)
- [ ] Selfhost: `AsyncResource.PendingBubbleUp` — three nested components each fetching; fallback visible until all three resolve
- [ ] Selfhost: `AsyncResource.PendingWithOverride` — a child matches `AsyncValue` locally and renders its own placeholder; outer `Pending` fallback still waits for that child
- [ ] Framerate: `AsyncResource.Framerate.PendingChurn` — 16 resources alternately loading/resolving every frame; assert `Pending` toggles correctly and never flashes fallback when all are resolved

### 4.2 Focus revalidation (§15 Q1)

- [ ] Add `ResourceOptions.RefetchOnWindowFocus = false` default (D11 equivalent)
- [ ] Hook `CoreWindow.Activated` + `CoreApplication.Resuming` in a central service
- [ ] On activation: iterate cache entries past `StaleTime`, fire refetch for each, dedup concurrent
- [ ] Throttle: default 30s between activation-triggered refetches (avoid Alt-Tab thrash)
- [ ] Feature-flag `ReactorFeatureFlags.FocusRevalidation` (default off); document rollout plan

#### Tests

- [ ] Unit: activation triggers refetch only for entries past `StaleTime`
- [ ] Unit: throttle blocks revalidation within 30s of previous
- [ ] Unit: per-query `RefetchOnWindowFocus: false` opts out
- [ ] Selfhost: `AsyncResource.FocusRevalidate` — simulate window-activated event; assert refetch fires and `Reloading` is observed

### 4.3 Analyzer diagnostics (§16 Phase 4)

Note: the first three rules below are pre-existing gaps for **all** hooks; file them under `Reactor.Analyzers` with new `REACTOR_HOOKS_*` IDs. They should ship alongside this phase, not be blocked on it.

- [ ] `REACTOR_HOOKS_001` — conditional hook call (inside `if`/`for`/`while`/`try`/early return)
- [ ] `REACTOR_HOOKS_002` — out-of-order hook calls across render paths
- [ ] `REACTOR_HOOKS_003` — missing deps (value captured in `fetcher` / `UseEffect` / `UseMemo` lambda not in `deps`)
- [ ] `REACTOR_HOOKS_004` — non-stable deps (new object/array/lambda each render)
- [ ] `REACTOR_HOOKS_005` — hook called outside `Render()` or a custom-hook method
- [ ] `REACTOR_HOOKS_006` — heuristic: `UseResource` with fetcher name matching `Create`/`Post`/`GenerateRandom`/… (non-idempotent)
- [ ] Unit tests per rule: positive cases (diagnostic fires), negative cases (diagnostic quiet)
- [ ] Suppression attribute `[SuppressResourceDiagnostic(...)]` for intentional violations

### 4.4 Docs

- [ ] Porting guide: `UseEffect + UseState` → `UseResource` (cookbook entry in `docs/cookbook/`)
- [ ] Infinite scroll cookbook entry: `UseInfiniteResource` + `LazyVStack`
- [ ] Migration guide for consumers with their own `DataPageCache` analogues

### 4.5 `UseStream<T>` scoping decision

- [ ] Survey real Reactor apps for `IAsyncEnumerable` / SignalR / SSE use cases
- [ ] File a separate spec if warranted; otherwise close out
- [ ] Document the **reason** for deferral in the spec appendix if we don't ship it

**Phase 4 exit criteria:** `Pending` works in nested scenarios; focus revalidation opt-in; all `REACTOR_HOOKS_*` diagnostics green in the analyzer test suite; porting guide published.

---

## Cross-phase testing checklist

Run before merging any phase:

- [ ] **All unit tests** (`dotnet test tests/Reactor.Tests`) — 2,200+ existing + phase-specific additions
- [ ] **All threading tests** with `-m:1` (single process) and `-m:4` (parallel) to expose contention that only appears under load
- [ ] **All selfhost fixtures** (`dotnet run --project tests/Reactor.AppTests.Host -- --self-test`) including the `AsyncResource.*` prefix
- [ ] **Framerate fixtures** (`--filter "AsyncResource.Framerate"`) — these are the regression canary for "works once, breaks at 60Hz"
- [ ] **E2E regression** (`dotnet test tests/Reactor.AppTests`) — no new E2E tests required; verify no existing AT/a11y regression
- [ ] **DataGrid parity CI job** (phase 3 only) — both `UseHookBasedPaging` values green
- [ ] **Unobserved-task assertion** — a test-collection-level subscription to `TaskScheduler.UnobservedTaskException` fails the run on any fire
- [ ] **Memory leak probe** — before/after `GC.GetTotalMemory(true)` delta on the rapid-remount fixtures; budget enforced in CI

---

## Files affected summary (from §Appendix C, expanded)

### New

- `Reactor/Core/AsyncValue.cs`
- `Reactor/Core/QueryCache.cs`
- `Reactor/Core/Hooks/UseResource.cs`
- `Reactor/Core/Hooks/UseInfiniteResource.cs`
- `Reactor/Core/Hooks/UseMutation.cs`
- `Reactor/Elements/Pending.cs`
- `Reactor/Data/DataSourceResourceExtensions.cs`
- `tests/Reactor.Tests/Core/AsyncValueTests.cs`
- `tests/Reactor.Tests/Core/QueryCacheTests.cs`
- `tests/Reactor.Tests/Core/QueryCacheThreadingTests.cs`
- `tests/Reactor.Tests/Core/UseResourceTests.cs`
- `tests/Reactor.Tests/Core/UseResourceThreadingTests.cs`
- `tests/Reactor.Tests/Core/UseInfiniteResourceTests.cs`
- `tests/Reactor.Tests/Core/UseInfiniteResourceThreadingTests.cs`
- `tests/Reactor.Tests/Core/UseMutationTests.cs`
- `tests/Reactor.Tests/Data/DataPageCacheParityTests.cs`
- `tests/Reactor.AppTests.Host/SelfTest/Fixtures/AsyncResourceFixtures.cs`
- `tests/Reactor.AppTests.Host/SelfTest/Fixtures/AsyncResourceFramerateFixtures.cs`
- `tests/Reactor.AppTests.Host/SelfTest/Fixtures/DataGridParityFixtures.cs` (phase 3)
- `samples/Reactor.TestApp/Pages/AsyncValueSamples.cs`

### Modified

- `Reactor/Core/RenderContext.cs` — register new hook states alongside existing
- `Reactor/Core/Hosting/ReactorApp.cs` / `ReactorHost.cs` — install default `QueryCache` via context
- `Reactor/Controls/DataGrid/DataGridComponent.cs` — consume `UseInfiniteResource` under flag (phase 3), default-on then flag removed
- `Reactor/Controls/DataGrid/DataGridState.cs` — port `BeginAsyncCommit` family to `UseMutation`
- `Reactor.Analyzers/*` — new `REACTOR_HOOKS_*` rules (phase 4)
- `samples/apps/regedit/Components/ValueList.cs` — phase-2 port dogfood

### Deleted (phase 3, after soak)

- `Reactor/Data/DataPageCache.cs`
- Related `DataGridState` private members that only backed the legacy path
