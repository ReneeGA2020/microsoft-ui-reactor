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

- [ ] Create `Reactor/Core/AsyncValue.cs`
- [ ] Define `abstract record AsyncValue<T>` with nested sealed records `Loading`, `Data(T Value)`, `Error(Exception Exception)`, `Reloading(T Previous)`
- [ ] Implement `.Match<TResult>(loading, data, error, reloading = null)` convenience method per spec §5 — `reloading ?? data` fallback
- [ ] Add XML docs linking each state to the transition rules in §6.2
- [ ] Verify record equality: `Data(5)` equals `Data(5)`, `Reloading(5)` does not equal `Data(5)`

#### Tests — unit (pure)

- [ ] Construct every state; assert type-discrimination via pattern match
- [ ] `Match` invokes the correct delegate for each state; `reloading` fallback lands in `data` when null
- [ ] Record equality for each pair (same-state-same-value, same-state-different-value, different-state-same-value)
- [ ] Equality when `T` is itself an `AsyncValue<_>` (nested) — guard against regression if callers accidentally wrap
- [ ] Equality when `T` holds a mutable reference type and the underlying value mutates (equality should still be reference-equal for the record; document this)
- [ ] Exhaustive `switch` expression compiles with no CS8509 warning and handles all four arms

### 1.2 `QueryCache` (§9)

- [ ] Create `Reactor/Core/QueryCache.cs`
- [ ] Define `sealed record CacheEntry<T>(T Value, DateTime FetchedAt, TimeSpan StaleTime, int SubscriberCount)` — note `SubscriberCount` is mutated via `with`, never in-place
- [ ] Implement `sealed class QueryCache` with: `TryGet<T>`, `Set<T>`, `Invalidate(string)`, `InvalidatePattern(string)`, `Clear()`, `event Action<string>? EntryChanged`
- [ ] Internal storage: `ConcurrentDictionary<string, object>` keyed on cache key, value boxed as `CacheEntry<T>` — cast checked at `TryGet` boundary
- [ ] Implement subscriber tracking: `Subscribe(key)` / `Unsubscribe(key)` return ref-count; `Unsubscribe` starts a `CacheTime` eviction timer when count hits zero
- [ ] Eviction timer uses a single shared `Timer` polling evictable entries every N seconds, not one timer per entry (avoid timer storm)
- [ ] `InvalidatePattern` walks keys with `StartsWith(prefix)` — document O(n) semantics in XML doc
- [ ] `EntryChanged` fires on UI dispatcher if set; otherwise inline (document the trade-off — set by hook, inline for tests)
- [ ] Install default `QueryCache` at app root via `Context<QueryCache>` in `ReactorApp.Run` / `ReactorHost` bootstrap

#### Tests — unit (pure)

- [ ] Cache miss → `TryGet` returns false
- [ ] Cache hit within `StaleTime` → `TryGet` returns entry, no `Reloading` flag required (hook-side concern)
- [ ] Cache hit past `StaleTime` → `TryGet` still returns the entry; "stale" decision is caller-side via `FetchedAt + StaleTime < Now`
- [ ] `Invalidate` removes the entry and fires `EntryChanged`
- [ ] `InvalidatePattern("user/")` removes `user/1`, `user/2`; leaves `employees/1`
- [ ] `Clear` removes all entries and fires `EntryChanged` for each (or once — pick and document)
- [ ] Entry past `CacheTime` with zero subscribers is evicted on the next eviction-timer tick
- [ ] Entry past `CacheTime` with ≥1 subscriber is **not** evicted
- [ ] Mismatched-type `TryGet<string>` on an entry stored as `CacheEntry<int>` returns false (does not throw)

#### Tests — unit (threading — race conditions)

- [ ] **Concurrent `Set` + `TryGet`.** 4 threads `Set`ing the same key with different values; 4 threads `TryGet`ing. Assert: every `TryGet` observes a valid `CacheEntry<T>` (no torn reads, no `KeyNotFoundException`).
- [ ] **Concurrent `Subscribe` / `Unsubscribe`.** 8 threads each call `Subscribe` N times then `Unsubscribe` N times on the same key. Assert final `SubscriberCount == 0` and no negative intermediate count ever observed.
- [ ] **Subscriber-count never goes negative.** Single-threaded regression: `Unsubscribe` on a key with zero subscribers throws `InvalidOperationException` (defensive; catches hook-logic bugs).
- [ ] **Invalidate-during-fetch-landing.** Thread A has a fetch in flight; thread B calls `Invalidate(key)` during the continuation that would `Set(key, ...)`. Assert: either the `Invalidate` wins (key absent after) OR the `Set` wins (key present with new value) — never a torn state where `EntryChanged` fires but `TryGet` returns false.
- [ ] **Eviction timer vs. Subscribe race.** Thread A: last subscriber unsubscribes, eviction timer starts. Thread B: new `Subscribe` on same key before timer fires. Assert: entry is retained (timer must check ref-count before evicting, not just time).
- [ ] **`InvalidatePattern` while keys are being added.** Thread A iterating `InvalidatePattern("user/")`; thread B concurrently `Set("user/99", ...)`. Assert: the new key is either evicted or retained, no `InvalidOperationException` from concurrent modification.
- [ ] **`EntryChanged` fires exactly once per `Set`.** 100 `Set` calls → exactly 100 `EntryChanged` events; no duplicates from double-dispatch.
- [ ] **Stress: 1000 keys × 8 threads × mixed ops** for 5 seconds. Assert no exceptions; cache internal state consistent (`SubscriberCount ≥ 0` for every entry).

### 1.3 `UseResource<T>` hook (§6)

- [ ] Create `Reactor/Core/Hooks/UseResource.cs` (partial extension on `RenderContext` or static method per convention in existing hooks)
- [ ] Register a new `ResourceHookState<T>` (extends `HookState` base from spec 009) containing: `CancellationTokenSource? Cts`, `string CacheKey`, `AsyncValue<T> LastValue`, `Task<T>? InFlight`, `IDispatcherQueue CapturedDispatcher`
- [ ] Define `ResourceOptions(TimeSpan? StaleTime, TimeSpan? CacheTime, int RetryCount, bool RefetchOnMount, string? CacheKey)` record with defaults per D11 (`StaleTime = Zero`, `CacheTime = 5min`)
- [ ] Implement hook body per §6.2 six-step state machine:
  - Derive cache key = `options.CacheKey ?? $"{CallerHookId}/{DepsHash(deps)}"`
  - Cache miss: invoke `fetcher(ct)`; inspect returned `Task` status; sync-complete fast-path → `Data` this render; faulted-sync → `Error` this render; pending → `Loading` + schedule continuation
  - Cache hit fresh (age ≤ `StaleTime`) → `Data(cached)`, no fetch
  - Cache hit stale → `Reloading(cached)` + fetch
  - Deps change → cancel old `Cts`, unsubscribe from old key, recompute
  - Unmount → cancel `Cts`, unsubscribe; if last subscriber and completed → eviction timer; if in-flight → drop
- [ ] Capture `IDispatcherQueue` at hook-registration time from current `RenderContext`
- [ ] Continuation marshals result via `CapturedDispatcher.TryEnqueue` before writing to cache
- [ ] Re-render short-circuit: compare new `AsyncValue<T>` to `LastValue` by record equality; skip `ScheduleReRender` if equal (§6.4)
- [ ] Respect `ValueEqualityComparer` for `deps` (same as `UseMemo`)
- [ ] Automatic retry with exponential backoff when `RetryCount > 0`: delay = `2^attempt * 100ms`, cancellable via the same `Cts`
- [ ] **No special-case `TaskCanceledException`**: if token fires, result is dropped silently; fetcher's other exceptions still surface as `Error`

#### Tests — unit (RenderContext, deterministic fetcher)

- [ ] First render with pending `Task<T>` → returns `Loading`; after `TaskCompletionSource.SetResult`, next render returns `Data(value)`
- [ ] First render with `Task.FromResult(value)` sync-complete → returns `Data(value)` in the **same** render (no `Loading` flash; §6.2 step 2)
- [ ] First render with `Task.FromException<T>(...)` sync-faulted → returns `Error(ex)` in the same render
- [ ] Cache hit fresh (within `StaleTime`) → second `UseResource` with same deps returns `Data` immediately, fetcher **not** invoked
- [ ] Cache hit stale (past `StaleTime`) → returns `Reloading(previous)` and fetcher is invoked; on completion renders `Data(new)`
- [ ] Deps change → cancellation token of previous fetch is cancelled; new fetch starts with new cache key
- [ ] Unmount while in-flight → token cancelled; if the late result lands, it's dropped (cache not updated, no re-render scheduled)
- [ ] Unmount while completed → cache retains entry; subscriber count decrements; eviction timer armed
- [ ] `RefetchOnMount = false` → cache-only; cache miss returns `Loading` **forever** (documented behaviour); no fetch issued
- [ ] Two siblings with the same auto-derived key (same component type, same hook index, same deps) **do not** share a cache key by default (per D6); with explicit `CacheKey = "shared"` they do share
- [ ] `RetryCount = 3` → fetcher throws twice, succeeds third time → observes `Data(value)`; exactly three invocations; exponential backoff delays verifiable via injected clock
- [ ] Retry observes the hook-owned `CancellationToken` — cancelling between retries aborts the remaining attempts
- [ ] Re-render short-circuit: if `LastValue == newValue` (same `Data(5)` back-to-back after invalidation), `ScheduleReRender` is not called
- [ ] Non-idempotent fetcher called twice (cache miss + invalidate + cache miss again) — hook does not prevent this, but only the latest result is applied (token cancellation drops the stale one)

#### Tests — unit (threading — race conditions)

- [ ] **Deps-change mid-flight.** Fetcher A starts with `deps=[1]`; before it completes, render with `deps=[2]` begins fetcher B; A eventually completes. Assert: A's result never writes to cache, never triggers re-render; B's result lands normally.
- [ ] **Two siblings + same explicit `CacheKey` + one in-flight.** Sibling 1 renders, kicks off fetch. Sibling 2 renders 1ms later with identical key. Assert: fetcher invoked **once**; both siblings receive `Data` from the same continuation.
- [ ] **Unmount during continuation marshalling.** Fetcher completes on thread pool; continuation is posted to dispatcher; component unmounts before dispatcher runs it. Assert: marshalled callback is a no-op (checks `IsUnmounted` / disposed `Cts`); cache is not updated; no `ObjectDisposedException`.
- [ ] **Cancel during fetcher body, result Task already scheduled.** Fetcher awaits a `Task.Delay(ct)` that throws `OperationCanceledException`. Assert: hook observes the cancellation; does not surface as `Error`; `UnobservedTaskException` never fires.
- [ ] **Concurrent invalidation + refetch storm.** `cache.Invalidate(key)` called 100× from a background thread while the hook is mid-render. Assert: at most N+1 fetches issued (where N = number of invalidations observed before the coalescing window), never a race where two fetchers land and both `Set` the cache (last-write-wins semantics are acceptable but must be observable).
- [ ] **Dispatcher missing at hook creation.** If `RenderContext.Dispatcher` is null (test harness), hook uses inline continuation; explicitly test this path so the hook is usable outside a WinUI window (e.g. unit tests).
- [ ] **`TaskScheduler.UnobservedTaskException` never fires** across the whole phase-1 test suite. Enforce via `[CollectionDefinition]` fixture that throws if any handler fires during a test run.

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

- [ ] Add `samples/Reactor.TestApp/Pages/AsyncValueSamples.cs`
- [ ] **1a. Deterministic fake fetcher** — `Task.Delay(ms)` + succeed / fail / cancel buttons
- [ ] **1b. Sync-complete fetcher** — `Task.FromResult`
- [ ] **1c. Deps-change cancellation** — text input drives deps
- [ ] **1d. Two siblings, one cache key** — explicit `CacheKey`
- [ ] **1e. Cache hit across remount** — toggle between two routes within `CacheTime`
- [ ] Wire each scenario to the TestApp snapshot suite so they run in CI

**Phase 1 exit criteria:** Every phase-1 unit, threading, and selfhost test green; all five `AsyncValueSamples` scenarios pass in the TestApp snapshot suite; no unobserved task exceptions across the full suite.

---

## Phase 2 — `UseInfiniteResource` (cursor paging on top of phase-1 cache)

Scope: new files `Reactor/Core/Hooks/UseInfiniteResource.cs`, `Reactor/Data/DataSourceResourceExtensions.cs`. Parity with existing `Reactor/Data/DataPageCache.cs` — do **not** delete it yet (phase 3).

### 2.1 `Page<TItem, TCursor>` + `LoadState` ADT (§7.1)

- [ ] Add `Page<TItem, TCursor>(IReadOnlyList<TItem> Items, TCursor? NextCursor, int? TotalCount = null)` record
- [ ] Define `abstract record LoadState` with sealed `Loading`, `Idle`, `EndOfList`, `Error(Exception Exception)`
- [ ] Exhaustive pattern-match coverage test (same shape as `AsyncValue<T>` tests)

### 2.2 `InfiniteResource<TItem>` class (§7.1)

- [ ] Create `sealed class InfiniteResource<TItem>` with public surface: `Items`, `TotalCount`, `EstimatedRemaining`, `LoadState`, `HasMore`, `ItemAt(int)`, `EnsureRange(int, int)`, `FetchNext()`, `Retry()`, `Refresh()`
- [ ] Internal storage: page table `Dictionary<int pageIndex, PageEntry>` (loaded or in-flight) + flattened `IReadOnlyList<TItem?>` facade with `null` placeholder slots
- [ ] `ItemAt(i)` — if page loaded → item; if page in-flight → `null`; if page not requested → schedule fetch (coalesced) → `null`; if past known end → `null` (no fetch)
- [ ] `EnsureRange(first, last)` — computes the set of pages covering `[first, last]`, dedups already-in-flight, fires fetches in page order
- [ ] `Refresh()` — invalidates cache entry, clears page table, restarts from page 1; preserves `Items` length during refetch when `TotalCount` is stable (per D17)
- [ ] `FetchNext()` — no-op if `LoadState != Idle` or `!HasMore`; otherwise fetches with last-known cursor
- [ ] All mutating methods are **UI-thread-affined**; assert via `RenderContext.AssertUIThread` at entry (document this constraint)
- [ ] `LoadState` transitions are discrete events consumers can observe via the hook's re-render (D17 contract)

### 2.3 `UseInfiniteResource<TItem, TCursor>` hook (§7.2)

- [ ] Create `Reactor/Core/Hooks/UseInfiniteResource.cs`
- [ ] Register `InfiniteResourceHookState<TItem, TCursor>` with an owned `InfiniteResource<TItem>` instance + per-page `CancellationTokenSource` map
- [ ] On first render: `Loading`, empty `Items`, schedule first page fetch with `cursor = null`
- [ ] On page completion: append items, update `NextCursor`, set `LoadState` to `Idle` or `EndOfList`
- [ ] On deps change: cancel **all** in-flight page fetches; clear page table; restart. Previous pages remain in `QueryCache` under the old key.
- [ ] On unmount: cancel all in-flight page fetches; unsubscribe from all page cache keys
- [ ] Per-page cache keys: `$"{CallerHookId}/{DepsHash}/page:{cursor}"` — individual pages can be invalidated / LRU-evicted
- [ ] Integrate with `QueryCache` subscriber tracking (each `UseInfiniteResource` subscribes to N page keys)

### 2.4 `DataSourceResource.UseDataSource` adapter (§7.3)

- [ ] Create `Reactor/Data/DataSourceResourceExtensions.cs`
- [ ] `UseDataSource<T>(this RenderContext ctx, IDataSource<T> source, DataRequest request, InfiniteResourceOptions? options = null)` delegates to `UseInfiniteResource<T, string>`
- [ ] Deps: `[source, request.Sort, request.Filter, request.Search]` (identity-based on source, value-based on descriptors)
- [ ] Verify existing `GraphQLDataSource` and `ListDataSource` plug in unchanged (no source modifications)

### 2.5 Phase-2 tests

#### Tests — unit (RenderContext)

- [ ] First render → `LoadState = Loading`, `Items = []`, page-1 fetch scheduled with `cursor = null`
- [ ] Page-1 completes with `NextCursor = "cursor-2"` → `Items = [items]`, `LoadState = Idle`, `HasMore = true`
- [ ] Page-1 completes with `NextCursor = null` → `LoadState = EndOfList`, `HasMore = false`
- [ ] `ItemAt(i)` where `i` is in loaded page → returns item
- [ ] `ItemAt(i)` where `i` is in unloaded page → returns `null`, schedules fetch for containing page
- [ ] `ItemAt(i)` where `i` is beyond known end → returns `null`, **no** fetch scheduled
- [ ] `EnsureRange(50, 120)` with page size 20 → schedules fetches for pages 2, 3, 4, 5, 6; dedups against any in-flight page
- [ ] `Refresh()` → invalidates, clears page table, restarts from page 1; `Items` length retained if `TotalCount` stable per D17
- [ ] `Retry()` on `LoadState = Error` → refetches the **failed** page; success transitions back to `Idle`
- [ ] Deps change → all in-flight page `CancellationTokenSource` instances cancelled; page table cleared; page-1 refetched with new key
- [ ] LRU eviction: configured max N pages; requesting page N+1 evicts the least-recently-accessed page
- [ ] Placeholder semantics: `Items.Count == LoadedItemCount + PlaceholderCount` matching `DataPageCache` behaviour in `DataGridComponent.cs:490`

#### Tests — unit (threading — race conditions)

- [ ] **Concurrent `ItemAt` across pages.** 8 threads call `ItemAt(random index)` on an `InfiniteResource` with 20 pages; 3 pages loaded, rest unloaded. Assert: every unloaded-page access schedules at most one fetch per page (coalescing works under contention).
- [ ] **`EnsureRange` + completing page race.** Thread A fetches page 3 (completing now). Thread B calls `EnsureRange(40, 80)` covering page 3. Assert: `EnsureRange` observes page 3 as loaded OR in-flight, never schedules a second fetch.
- [ ] **Deps change mid-page-fetch.** Page 2 in flight. Deps change. Assert: page-2 token cancelled; its late completion does not write to the new page table; new page-1 fetch proceeds cleanly.
- [ ] **`Refresh()` during paging.** Pages 1-3 loaded, page 4 in flight. `Refresh()` fires. Assert: page 4 cancelled; old entries cache-retained under old key; new page-1 fetch starts.
- [ ] **Unmount during multi-page in-flight.** Five pages in-flight at unmount. Assert: all five cancelled; no unobserved exceptions; subscriber counts on all five cache keys decrement to zero.
- [ ] **Scroll-driven `EnsureRange` flood.** `EnsureRange` called 1000× with overlapping ranges in rapid succession. Assert: total fetches ≤ distinct page count in the covered range.

#### Tests — unit (parity with `DataPageCache<T>`)

These tests instantiate **both** `DataPageCache<T>` and `UseInfiniteResource`-backed equivalents and assert identical observable behaviour. Live in `tests/Reactor.Tests/Data/DataPageCacheParityTests.cs`.

- [ ] Same LRU eviction order for the same access pattern
- [ ] Same placeholder (`null`) positions in the flat item list for a given scroll window
- [ ] Same cancellation-on-deps-change semantics
- [ ] Same "block loaded" observer semantics (DataGrid's `BlockLoaded` event ↔ hook's re-render subscription)
- [ ] Same behaviour for fetcher that throws mid-page
- [ ] Same behaviour for fetcher that returns an empty page before `EndOfList`

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

- [ ] `LazyVStack` demo on `AsyncValueSamples` page — pull-model via `ItemAt`, placeholder on `null`
- [ ] Search-as-you-type over an infinite list — deps + pull model together
- [ ] `Refresh()` with consumer-side scroll preservation (on `LazyVStack`)
- [ ] Port `samples/apps/regedit/Components/ValueList.cs` to `UseInfiniteResource` (low-risk, in-memory source)

**Phase 2 exit criteria:** Parity tests green against `DataPageCache`; regedit `ValueList` runs on `UseInfiniteResource`; all framerate fixtures pass on both x64 and ARM64 in CI.

---

## Phase 3 — `UseMutation` + DataGrid migration

Scope: new `Reactor/Core/Hooks/UseMutation.cs`; modifications to `Reactor/Controls/DataGrid/DataGridComponent.cs` and `DataGridState.cs`; deletion of `Reactor/Data/DataPageCache.cs` gated behind a production-soak window.

### 3.1 `UseMutation<TInput, TResult>` hook (§8)

- [ ] Create `Reactor/Core/Hooks/UseMutation.cs`
- [ ] Define `sealed class Mutation<TInput, TResult>` with `IsPending`, `Error`, `LastResult`, `Task<TResult> RunAsync(TInput)`, `Reset()`
- [ ] Define `sealed record MutationOptions<TInput, TResult>(Action<TInput>? OnOptimistic, Action<TResult, TInput>? OnSuccess, Action<Exception, TInput>? OnError, string[]? InvalidateKeys)`
- [ ] `RunAsync`:
  - Fire `OnOptimistic(input)` synchronously (no dispatcher hop)
  - Kick off `mutator(input, ct)` on thread pool
  - On success: marshal back to UI dispatcher, fire `OnSuccess`, `cache.Invalidate(key)` for each `InvalidateKeys`, update `LastResult`
  - On failure: marshal back, fire `OnError`, store `Error`
  - Overlapping calls: concurrent `RunAsync` calls each get their own token; latest `LastResult` wins (document: callers who want serialization wrap `RunAsync` with their own gate)
- [ ] `Reset()` clears `Error`, `LastResult`, `IsPending` (does not cancel in-flight — explicit choice; document)
- [ ] Hook-owned `CancellationToken` cancels on unmount

### 3.2 `UseMutation` tests

#### Unit (RenderContext)

- [ ] `IsPending` true during fetch, false after completion
- [ ] `OnOptimistic` fires before the task starts (synchronous)
- [ ] `OnSuccess` fires on UI thread, after `OnOptimistic`, before `IsPending` drops
- [ ] `OnError` fires on UI thread; `Error` populated; `LastResult` unchanged
- [ ] `InvalidateKeys` trigger `cache.Invalidate` on each key on success (not on error)
- [ ] `Reset()` clears fields
- [ ] Unmount during pending → token cancelled; `OnSuccess` / `OnError` do **not** fire after unmount

#### Unit (threading — race conditions)

- [ ] **Overlapping `RunAsync`.** Call `RunAsync(a)` then `RunAsync(b)` before `a` resolves. Both complete; `LastResult` is the later-completing one; both callbacks fire in completion order. No torn `IsPending` (should be true until both resolve).
- [ ] **`OnOptimistic` crashes.** If `OnOptimistic` throws, mutation aborts: `RunAsync` returns a faulted task; `mutator` never invoked. Document this — prevents half-applied state.
- [ ] **`InvalidateKeys` while mutation pending.** Manual `cache.Invalidate` call for the same key during pending; assert the post-success invalidate still fires (idempotent).
- [ ] **Cancellation of pending mutation via unmount.** Assert `OnError` **does not** fire on `OperationCanceledException` from unmount (consistent with `UseResource` — cancellation is silent).

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
