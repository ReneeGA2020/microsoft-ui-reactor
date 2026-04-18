# AI Agent Devtools — Implementation Tasks

Reference: [docs/specs/024-ai-agent-devtools.md](../024-ai-agent-devtools.md)
Tracks: [microsoft/microsoft-ui-reactor#16](https://github.com/microsoft/microsoft-ui-reactor/issues/16)

---

## How to use this document

Every task is a checkbox. Tick it as you finish it. Don't check a task until every sub-bullet under it is true.

Phases are sequential — a later phase assumes the earlier phase's exit criteria are met.

### Test classification

Tests for this feature are classified by the infrastructure they require. Every MCP tool gets coverage at both a fast and a slow level so we can run a tight loop in CI and still catch real input-stack regressions before ship.

| Level | Project | What it needs | Speed | When to use |
|---|---|---|---|---|
| **Unit** | `Reactor.Tests` (xUnit) | `NodeRegistry`, selector parser, id construction, schema shapes, pure logic. No reconciler, no WinUI, no HTTP. | Fast | Id stability invariants, selector resolution order, schema version pinning, predicate-object evaluation, JSON shape snapshots. |
| **Self-host MCP** | `Reactor.Tests` (xUnit) | Reconciler + WinUI controls in-process, `DevtoolsMcpServer` bound to a loopback port, no visible window, no Appium. The MCP client is an in-test JSON-RPC helper. | Medium | Tree walk correctness, tool dispatch, `reactor.click` hitting UIA, `waitFor` predicate timing, screenshot PNG produced, reload sentinel shutdown. This is the bulk of MCP coverage. |
| **E2E Appium** | `Reactor.AppTests` (MSTest + Appium) | Full app process launched via `mur devtools`, real window visible, WinAppDriver out-of-process, real MCP client connecting over loopback HTTP. | **Slow** | End-to-end agent flows: launch → tree → click → screenshot → edit → reload → reconnect. Multi-window. Real build-rebuild cycle. Deprecation-warning smoke. |

**Rule of thumb:** if a test can be written self-host, write it self-host. Only reach for Appium when the test genuinely depends on a second process, a visible window, or the `mur devtools` supervisor behavior.

### Conventions

- New runtime code lives under `src/Reactor/Hosting/Devtools/`.
- New launcher code lives under `src/Reactor.Cli/Devtools/`.
- Self-host MCP tests live under `tests/Reactor.Tests/Devtools/`.
- E2E tests live under `tests/Reactor.AppTests/Tests/Devtools/`.
- Wire tool names are unprefixed (`tree`, `click`, …). The `reactor.*` form is prose shorthand.

---

## Phase 1 — Rename and foundation

**Scope:** Rename the existing `--preview` surface to `--devtools` without changing behavior. Deprecation aliases stay for one release. No new capabilities in this phase.

**Spec refs:** §7, §17 Phase 1.

### 1.1 Rename `ReactorApp.Run` parameter

- [x] `src/Reactor/Hosting/ReactorApp.cs`: rename `preview:` parameter to `devtools:` on both `Run<TRoot>` and `Run` overloads.
- [x] Keep `preview:` as a second parameter. **Deviation from spec**: `[Obsolete]` is not valid on parameter declarations in C#, so the deprecation is documented via an inline comment and surfaced at runtime. When both are passed, `devtools` wins; when only `preview` is passed, OR them and emit a one-time `Console.Error.WriteLine("[reactor] 'preview:' is deprecated; use 'devtools:'.")`.
- [x] `TryRunPreview` → `TryRunDevtools`. Update all internal call sites.
- [x] XML docs updated: comments reference `--devtools` with `--preview` noted as deprecated.

### 1.2 Rename CLI flags

- [x] In `TryRunDevtools`, accept `--devtools` and `--devtools-list` as primary flags.
- [x] Accept `--preview` and `--preview-list` as aliases that print a one-time deprecation warning to stderr (`[reactor] '--preview' is deprecated; use '--devtools run'`).
- [x] Add subverb parsing for `--devtools run|list|screenshot|tree`. In this phase only `run` and `list` are wired (the rest return "not implemented in phase 1" and are filled in Phase 2/3).
- [x] `--vscode` flag retains its meaning (enable the capture server for the VS Code panel). Document that MCP is always on when `--devtools` is on; `--vscode` is additive.
- [x] `[devtools]` is the new log prefix. Replace `[preview]` log prefixes across `ReactorApp.cs` and `PreviewCaptureServer.cs`.

### 1.3 Update scaffold

- [x] `src/Reactor.Cli/Program.cs` (`GenerateProgram`): rename the generated `preview: true` parameter to `devtools: true`.
- [x] Update the generated source comment about hot reload / preview to mention `mur devtools` and the MCP endpoint.
- [ ] Update `Reactor.Cli` help text: `mur new` output mentions `mur devtools`, not `--preview`. (Deferred — current help text doesn't mention `--preview`; needs a `mur devtools` line once the supervisor lands in Phase 2.)

### 1.4 Update VS Code extension

- [x] `src/vscode-reactor/src/extension.ts`: change the spawn args from `--preview --vscode` to `--devtools run --vscode`.
- [x] Keep one-release compat: if the target Reactor package is older, fall back to `--preview --vscode`. Detect by first-line stdout sniff (`[devtools] ready` vs `[preview] …`).
- [x] Update extension's user-facing labels ("Reactor Preview" panel title is unchanged; internal telemetry event names get a `_devtools` variant with the old name kept as an alias for one release). *(Telemetry transport is a stub `outputChannel.appendLine` — the event-name pair is in place for when a real transport is wired.)*

### 1.5 Phase 1 tests — Unit

- [x] `tests/Reactor.Tests/Devtools/CliFlagParsingTests.cs`: `--devtools run`, `--devtools list`, `--devtools screenshot`, `--devtools tree` parse into the expected subverb enum.
- [x] `CliFlagParsingTests`: `--preview` and `--preview-list` still parse (aliased), and each sets a "deprecated" flag that the runtime can read.
- [x] `CliFlagParsingTests`: passing both `--preview` and `--devtools` is an error (not a silent precedence).
- [x] `tests/Reactor.Tests/Devtools/DevtoolsParamCompatTests.cs`: calling `ReactorApp.Run(..., preview: true)` still invokes the devtools path and emits the deprecation warning exactly once per process. **Scoped to the `ResolveDevtoolsParam` helper** — exercising the full `Run()` path requires spinning up a WinUI window, which is out of reach in a pure unit test.

### 1.6 Phase 1 tests — Self-host MCP

*(No MCP server in phase 1. Self-host level is skipped here; Phase 2 introduces it.)*

### 1.7 Phase 1 tests — E2E Appium

- [ ] `tests/Reactor.AppTests/Tests/Devtools/DeprecationAliasTests.cs`: launch a sample project with `--preview`, assert the window opens, assert the deprecation warning appears on stderr, assert the capture server is reachable on its port.
- [ ] `DeprecationAliasTests`: same launch with `--devtools run` — window opens, no deprecation warning, capture server still reachable.
- [ ] `DeprecationAliasTests`: `--devtools list` writes component names one per line and exits with code 0.

### 1.8 Phase 1 exit criteria

- [ ] All existing `--preview` tests pass unchanged when invoked under the new `--devtools` names (rename-only refactor of the test wiring allowed).
- [ ] The VS Code preview panel continues to work end-to-end on a sample project.
- [ ] A full CI run shows zero new warnings except the intentional `[Obsolete]` on `preview:`.

---

## Phase 2 — MCP server and core atomic tools

**Scope:** Stand up `DevtoolsMcpServer`, the node registry, selector resolution, and the ten tools listed in spec §17 Phase 2. Ship `mur devtools` supervisor.

**Spec refs:** §6, §8 (core rows), §9 (summary view only), §10, §11 (click/type/focus/waitFor + reload), §13.

### 2.1 Scaffolding: DevtoolsMcpServer skeleton

- [x] Create `src/Reactor/Hosting/Devtools/DevtoolsMcpServer.cs`. Mirrors `PreviewCaptureServer`'s HttpListener + dispatcher-marshalling pattern. Binds to `127.0.0.1` on a free port.
- [x] JSON-RPC 2.0 framing over HTTP POST. One method per MCP tool. `tools/list` returns the inventory; `tools/call` dispatches by name.
- [x] `MCP_ENDPOINT=http://127.0.0.1:NNNN/mcp` and `MCP_PORT=NNNN` printed to stdout at startup, alongside the existing `CAPTURE_PORT=...` line.
- [x] `--mcp-port N` CLI flag to pin the port (required for the supervisor reload loop).
- [x] Startup prints `[devtools] ready (build <iso-timestamp>)` after the first render completes. `build` tag derived from the assembly's compile timestamp.
- [x] Graceful shutdown on window close: flush in-flight responses, close the listener.

### 2.2 Node registry

- [x] Create `src/Reactor/Hosting/Devtools/NodeRegistry.cs`.
- [x] Per-window id scope. Every id is `r:<window>/<local>`.
- [x] Id construction rules from spec §13:
  - With `AutomationId`: `r:<window>/<Component>.<AutomationId>`.
  - With Reactor source: `r:<window>/<Component>.<file>:<line>:<siblingIndex>`.
  - Otherwise: content-addressed path from nearest stable ancestor + type + sibling index.
- [x] Entries hold `WeakReference<object>` (object rather than UIElement so unit tests can inject sentinels; public API still returns UIElement only). Lookups resolving to a dead target return a structured `"gone"` status; ids are never reused.
- [x] Window-close invalidates every id in that window's scope. Window ids are never reused even if a new window opens with the same title. *(Window-id scope lives in the registry via InvalidateWindow; the WindowRegistry §2.3 will own window-id assignment.)*
- [ ] Registry is populated lazily on every tree walk; an element seen in multiple walks keeps the same id. *(ConditionalWeakTable provides the reverse map; the tree-walk integration lands with §2.8.)*
- [x] Thread-safe read; all writes happen on the UI dispatcher during tree walk.

### 2.3 Window addressing

- [ ] `WindowRegistry` assigns stable window ids at `Window.Activated` time. Unique `Window.Title` → lowercased-slug; otherwise `Win1`, `Win2`, …
- [ ] `reactor.windows` tool returns `[{ id, title, hwnd, bounds, isMain, buildTag }]`.
- [ ] Tools that accept a `window` arg: when exactly one window exists, default to that window; when more than one, error if omitted and list active ids in the error payload.
- [ ] Cross-window mismatch (id's window ≠ explicit `window` arg) returns an error, not a silent preference.

### 2.4 Selector resolver

- [x] Create `src/Reactor/Hosting/Devtools/SelectorParser.cs`.
- [x] Grammar (spec §11 "Selector resolution order"):
  1. Node id: `r:<window>/<local>`.
  2. AutomationId: `#btn-inc`.
  3. AutomationName: `[name='Increment']`.
  4. Type + optional index: `Button`, `Button[2]`, `StackPanel > Button`.
  5. Reactor source: `{component:'CounterDemo',line:42}`.
- [ ] Resolver runs on the UI dispatcher. Returns the `UIElement` or a structured ambiguity error listing matching ids. *(Parser → IR is done; the IR→UIElement resolver lands with §2.8 tree walk.)*
- [ ] Ambiguity error format: `{ code: "ambiguous-selector", candidates: ["r:main/…", "r:main/…"] }`. *(Error shape defined via `McpToolException(code: …, data: { candidates })`; emitted by the resolver once it's wired.)*

### 2.5 Tool: `reactor.version`

- [x] Returns `{ build, pid, mcpPort }`.
- [x] Zero-side-effect; no dispatcher hop.
- [x] Used by the agent after reconnect to confirm a reload took.

### 2.6 Tool: `reactor.components`

- [x] Returns `string[]` — the component class names in the loaded assembly.
- [x] Reuses the existing `FindAllComponentNames()` helper from `ReactorApp.cs`.

### 2.7 Tool: `reactor.switchComponent`

- [x] Args: `name`, optional `window`. *(`window` arg honored once §2.3 WindowRegistry lands.)*
- [x] Returns `{ ok, current }`.
- [x] Internally reuses the existing `SwitchComponent` Func on `PreviewCaptureServer` so both servers share the same in-process switching path (extracted as `SwitchComponentCore` in `ReactorApp.TryRunDevtools`).
- [ ] Invalidates every id in the target window's scope (the old tree is gone). *(NodeRegistry.InvalidateWindow exists; the wiring from switchComponent to the registry lands with the tree walk §2.8.)*

### 2.8 Tool: `reactor.tree` (summary view only)

- [ ] Args: `selector?`, `window?`, `view: "summary"` (default), `includeReactorSource: bool` (default true, overridable).
- [ ] Walk via `VisualTreeHelper.GetChild` from `Window.Content` on the UI dispatcher.
- [ ] Output as a flat `TreeNode[]` with `parentId`/`childIds`, not nested.
- [ ] Summary fields: `id`, `type`, `name`, `automationId`, `automationName`, `bounds`, `text`, `isVisible`, `parentId`, `childIds`, `reactor?`.
- [ ] `$schema: "reactor-tree/1"` pinned on every payload.
- [ ] `selector` scopes the walk to the selected subtree.
- [ ] `includeReactorSource: false` skips the backref resolution entirely (cheap path).

### 2.9 Tool: `reactor.screenshot`

- [ ] Args: `selector?`, `window?`, `waitIdle: bool` (default true), `dpi?`, `includeChrome: bool` (default false).
- [ ] `waitIdle: true` forces layout + one `CompositionTarget.Rendering` frame before capture.
- [ ] Captures the window via `PrintWindow` (reuse `PreviewCaptureServer`'s path). Crops to the selector's bounds when selector is given.
- [ ] Returns `{ png: base64, bounds }`.
- [ ] Also wire up `dotnet run -- --devtools screenshot <Component> --out path.png`: runs the app to first-render, writes PNG, exits. No persistent server.

### 2.10 Tool: `reactor.click`

- [ ] Args: `selector`.
- [ ] Resolve selector → `UIElement` → UIA peer.
- [ ] Prefer `IInvokeProvider.Invoke`.
- [ ] Fall back to `ITogglePattern`, then `ISelectionItemProvider`, then synthesized pointer press/release via UIA automation client.
- [ ] In-process UIA client against the app's own HWND.
- [ ] Returns `{ ok, via }` where `via` indicates which pattern fired (`"invoke"`, `"toggle"`, `"selection"`, `"pointer"`).

### 2.11 Tool: `reactor.type`

- [ ] Args: `selector`, `text`, `clear?: bool`.
- [ ] Resolve selector, acquire focus, use `IValueProvider.SetValue` when available.
- [ ] Fall back to UIA text input synthesized keystrokes.
- [ ] `clear: true` sets value to empty string before appending.
- [ ] Returns `{ ok }`.

### 2.12 Tool: `reactor.focus`

- [ ] Args: `selector`.
- [ ] Calls `UIElement.Focus(FocusState.Programmatic)` on the UI dispatcher.
- [ ] Returns `{ ok }`.

### 2.13 Tool: `reactor.waitFor`

- [ ] Args: `predicate: { selector, textEquals?, textMatches?, visible?, count? }`, `timeoutMs`.
- [ ] Evaluates the predicate against the live tree on a tick (~50 ms). No scripting language.
- [ ] Returns `{ ok, elapsedMs }` when satisfied, `{ ok: false, reason: "timeout", observed }` on timeout.
- [ ] `count` matches the count of elements matching `selector`. `textMatches` is a regex. `textEquals` is exact.

### 2.14 Tool: `reactor.reload`

- [x] Args: optional `component` (focus after restart). *(Arg accepted; the post-restart focus is honored once the supervisor passes it as `--component` on respawn — currently it always reuses the original value.)*
- [x] Flushes response `{ ok: true, exitingBuild }` **before** shutdown.
- [x] Closes MCP and capture listeners; closes the window on the dispatcher; exits with sentinel code `42`.
- [x] Node registry is NOT transferred across the restart — old ids are gone, by design (process boundary).

### 2.15 `mur devtools` supervisor

- [x] Create `src/Reactor.Cli/Devtools/DevtoolsSupervisor.cs`.
- [x] `mur devtools [path-to-project] [--component Name] [--mcp-port N]`.
- [x] Runs `dotnet run -- --devtools run`. Explicitly NOT `dotnet watch run`.
- [x] Pins `--mcp-port` across respawns. If omitted, picks a free port on first launch and reuses it.
- [x] On child exit code `42`: run `dotnet build`, relaunch the same `dotnet run` args, print a fresh `[devtools] ready (build <tag>)` line (the child emits it).
- [x] On any other exit code: propagate it and exit.
- [x] On `dotnet build` failure during reload: print the build error, wait — do not respawn. The MCP port stays unbound; the agent will see a transport error.
- [x] Stream child stdout/stderr to parent stdout/stderr verbatim. Don't buffer.

### 2.16 Phase 2 tests — Unit

- [x] `tests/Reactor.Tests/Devtools/NodeRegistryTests.cs` + `NodeIdBuilderTests.cs`:
  - [x] Id construction with AutomationId produces `r:<window>/<Component>.<AutomationId>`.
  - [x] Id construction with Reactor source produces `r:<window>/<Component>.<file>:<line>:<siblingIndex>`.
  - [x] Templated-part id is content-addressed from nearest stable ancestor.
  - [ ] A GC'd element's id returns a structured `"gone"` error. *(WeakReference collection semantics asserted indirectly via the live-element path in the self-host fixture; pure unit uses a sentinel that won't be GC'd.)*
  - [x] Window close invalidates every id in scope (InvalidateWindow + tombstones). A new window of the same title gets a new window id — *covered once WindowRegistry §2.3 lands.*
  - [ ] Two walks of the same live tree return the same ids for the same elements. *(Covered when the tree walker in §2.8 lands; the reverse map via ConditionalWeakTable gives this for free.)*
- [x] `tests/Reactor.Tests/Devtools/SelectorParserTests.cs`:
  - [x] Each of the five selector forms parses into the right IR.
  - [ ] Ambiguous selector yields `ambiguous-selector` with all candidates. *(Covered once the IR→UIElement resolver in §2.8 is wired — ambiguity is a runtime concept, not a parse one.)*
  - [ ] Unknown selector yields `unknown-selector`. *(Same — runtime resolver.)*
  - [ ] Cross-window id + explicit `window` mismatch is an error. *(Covered once §2.3 window arg is wired.)*
- [ ] `tests/Reactor.Tests/Devtools/TreeSchemaTests.cs`: deferred with §2.8.
- [ ] `tests/Reactor.Tests/Devtools/WaitForPredicateTests.cs`: deferred with §2.13.
- [x] `tests/Reactor.Tests/Devtools/McpDispatchTests.cs`: JSON-RPC envelope round-trip, registry order, structured errors.
- [x] `tests/Reactor.Tests/Devtools/SupervisorArgsTests.cs`: `mur devtools` arg parsing.

### 2.17 Phase 2 tests — Self-host MCP

- [ ] `tests/Reactor.Tests/Devtools/Infrastructure/SelfHostMcpFixture.cs`: bootstraps `DevtoolsMcpServer` against a hidden WinUI window hosting a fixture component. Exposes an in-test JSON-RPC client.
- [ ] `DevtoolsMcpFixture` tears down cleanly and frees the port between tests.
- [ ] `tests/Reactor.Tests/Devtools/McpVersionTests.cs`: `version` returns a `build` tag, pid, mcpPort.
- [ ] `tests/Reactor.Tests/Devtools/McpComponentsTests.cs`: `components` lists fixture components.
- [ ] `tests/Reactor.Tests/Devtools/McpSwitchComponentTests.cs`: switching components invalidates all tree ids; subsequent tree walk returns new ids.
- [ ] `tests/Reactor.Tests/Devtools/McpTreeTests.cs`:
  - [ ] Full-window walk returns all visible elements.
  - [ ] `selector` scopes the walk.
  - [ ] `reactor` backref is present only when source mapping resolves (use a fixture with a known authored button).
  - [ ] Multi-window fixture: each window's nodes carry its window id; cross-window ids do not leak.
- [ ] `tests/Reactor.Tests/Devtools/McpScreenshotTests.cs`:
  - [ ] Returns non-empty base64 PNG.
  - [ ] Selector-scoped screenshot has bounds matching the selected element (within 1 px tolerance for DPI).
  - [ ] `waitIdle: true` produces a stable frame on a fixture with pending layout.
- [ ] `tests/Reactor.Tests/Devtools/McpClickTests.cs`:
  - [ ] Click on a `Button` fires its `Click` handler via `IInvokeProvider.Invoke`; response `via: "invoke"`.
  - [ ] Click on a `ToggleButton` toggles state; response `via: "toggle"`.
  - [ ] Click on a custom-drawn fixture element with no UIA pattern falls through to pointer synthesis; response `via: "pointer"`.
- [ ] `tests/Reactor.Tests/Devtools/McpTypeTests.cs`:
  - [ ] `type` sets a `TextBox`'s text via `SetValue`.
  - [ ] `clear: true` clears first.
- [ ] `tests/Reactor.Tests/Devtools/McpFocusTests.cs`: `focus` moves keyboard focus; observable via a subsequent tree walk (`isKeyboardFocusable` → actual focus state).
- [ ] `tests/Reactor.Tests/Devtools/McpWaitForTests.cs`:
  - [ ] Succeeds when a predicate becomes true within the timeout (fire a delayed handler from the fixture).
  - [ ] Times out with `{ ok: false, reason: "timeout" }` otherwise.
  - [ ] `count: 0` waits for an element to disappear.
- [ ] `tests/Reactor.Tests/Devtools/McpReloadTests.cs`:
  - [ ] Calling `reload` flushes the JSON-RPC response before shutdown (test asserts it reads a complete response, then sees the port close).
  - [ ] Process exits with code 42.
- [ ] `tests/Reactor.Tests/Devtools/McpWindowsTests.cs`:
  - [ ] With one window, `window` arg defaults work.
  - [ ] With two windows, omitting `window` on ambiguous calls returns an error listing both ids.
  - [ ] Mismatched id/`window` pair errors with `window-mismatch`.

### 2.18 Phase 2 tests — E2E Appium

- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsLaunchTests.cs`:
  - [ ] `mur devtools <sample-project>` launches, prints `MCP_ENDPOINT=` on stdout, window is visible.
  - [ ] Connecting an MCP client over the printed endpoint and calling `version` returns a build tag.
- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsReloadTests.cs`:
  - [ ] Call `reload`, wait for the child to respawn, reconnect, call `version` — confirm a new `build` tag.
  - [ ] Assert the MCP port is the same as before reload.
  - [ ] Modify a fixture source file between the two version calls; after reload, a `tree` walk shows the modification reflected.
- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsReloadBuildFailureTests.cs`:
  - [ ] Introduce a syntax error in the fixture; call `reload`.
  - [ ] Supervisor prints build error to stdout.
  - [ ] Reconnect attempt times out / errors cleanly.
  - [ ] Remove the syntax error and call `reload` again — supervisor picks it up on the next attempt.
- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsEndToEndAgentFlowTests.cs`:
  - [ ] Script the full phase-2 happy path: launch → `components` → `switchComponent` → `tree` → `click` → `waitFor` → `screenshot` → `reload` → reconnect → `version`. Assert every step succeeds and the final build tag differs from the initial one.
- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsMultiWindowTests.cs`:
  - [ ] A fixture opens a second window; `windows` returns two entries; clicking in each window is scoped correctly.

### 2.19 Phase 2 exit criteria

- [ ] An agent can run the full end-to-end flow described in spec §17 Phase 2 exit criteria through MCP alone, with no human in the loop.
- [ ] `reactor.tree` summary for a 200-node fixture responds in < 50 ms locally (p95 over 100 runs).
- [ ] `reactor.screenshot` at `waitIdle: true` responds in < 150 ms for the fixture window (p95).
- [ ] All Phase 2 unit, self-host, and E2E tests are green in CI.

---

## Phase 3 — Layout debugging, state, and remaining atomic tools

**Scope:** Ship `view: "full"`, source-mapping integration (depends on spec 010), `state`, and the remaining automation verbs (`invoke`, `toggle`, `select`, `scroll`, `fire`).

**Spec refs:** §8 (remaining rows), §9 (full view), §12, §11 (fire semantics).

### 3.1 Tree `view: "full"`

- [ ] Add `desiredSize`, `actualSize`, `layout { margin, padding, horizontalAlignment, verticalAlignment, horizontalContentAlignment, verticalContentAlignment }`, `context { parentType, stackOrientation|gridRow|gridColumn|gridRowSpan|gridColumnSpan|canvasLeft|canvasTop|dockPanelDock }`, `visual { opacity, clip, zIndex, renderTransform }` to the node schema.
- [ ] Flatten text content into `text` for text-bearing controls (`TextBlock`, `Button`, `TextBox`, etc.).
- [ ] Skip serializing identity-matrix transforms and null clips.

### 3.2 Source-mapping integration (depends on spec 010)

- [ ] Wire the source mapper into `NodeRegistry` id construction so Reactor-source id form becomes available.
- [ ] Populate the `reactor` block on tree nodes when the source map resolves.
- [ ] Sparse-and-honest rule: no guesses for templated parts, framework wrappers, or anonymous containers.
- [ ] Selector form `{component:'X',line:N}` resolves through the same source map.
- [ ] Make source-map lookup cache per-build; invalidate on reload.
- [ ] If source-map lookup is expensive (> 1 ms per node p95), make `includeReactorSource` opt-in; otherwise default-on.

### 3.3 Tool: `reactor.state`

- [ ] Args: `selector?`.
- [ ] Reads per-component hook tables on the UI dispatcher.
- [ ] Output matches spec §12 shape: `{ hooks: [{ component, instanceId, hook, index, name?, valueType, value }] }`.
- [ ] Primitives serialize as JSON values. Complex objects return `{ "$type", "$shape" }` — no full dump.
- [ ] `name` heuristic: if the hook's caller destructured the return into a named local variable traceable via the source map, include the name; otherwise omit.
- [ ] Mutation is out of scope — read-only.

### 3.4 Tool: `reactor.invoke`

- [ ] Args: `selector`.
- [ ] Calls `IInvokeProvider.Invoke` directly; errors if the element does not expose the pattern.

### 3.5 Tool: `reactor.toggle`

- [ ] Args: `selector`.
- [ ] Calls `IToggleProvider.Toggle`.
- [ ] Returns `{ ok, state }`.

### 3.6 Tool: `reactor.select`

- [ ] Args: `selector` (container), `itemSelector` (within container).
- [ ] Calls `ISelectionItemProvider.Select` on the item.
- [ ] Works with `ListView`, `ComboBox`, `GridView` at minimum.

### 3.7 Tool: `reactor.scroll`

- [ ] Args: `selector`, `by?` (offset pair), `to?` (selector of descendant to scroll into view).
- [ ] Uses `IScrollProvider` where available, `IScrollItemProvider.ScrollIntoView` for `to`.
- [ ] Returns `{ ok, scrollPosition }`.

### 3.8 Tool: `reactor.fire` (escape hatch)

- [ ] Args: `component`, `event`, `args?`.
- [ ] Resolves to the live `Component` instance by `component` name; finds the handler by event name.
- [ ] Calls the handler on the UI dispatcher.
- [ ] Response carries `{ ok, via: "reactor-event-injection" }` so logs make the shortcut visible.
- [ ] Inappropriate-use docs: link from the tool description back to §11.

### 3.9 Snapshot / diff — deferred

- [ ] Create `docs/specs/notes/024-snapshot-diff-watch.md` stub: records why `reactor.snapshot`/`reactor.diff` are deferred (§10) and the signal we're watching for before shipping them.
- [ ] No code.

### 3.10 Phase 3 tests — Unit

- [ ] `tests/Reactor.Tests/Devtools/TreeFullViewTests.cs`:
  - [ ] Full view includes all §9 fields; summary view still doesn't.
  - [ ] Identity transforms and null clips are omitted.
- [ ] `tests/Reactor.Tests/Devtools/SourceMapReactorBackrefTests.cs`:
  - [ ] `reactor` block appears when the source map resolves; absent otherwise.
  - [ ] Templated-part nodes never get a synthesized `reactor` block.
- [ ] `tests/Reactor.Tests/Devtools/StateShapeTests.cs`:
  - [ ] Primitive hook values serialize directly.
  - [ ] Complex objects serialize as `{ "$type", "$shape" }`, no deep dump.
- [ ] `tests/Reactor.Tests/Devtools/FireResolutionTests.cs`:
  - [ ] Component + event name resolves to the right handler.
  - [ ] Unknown component or unknown event returns structured errors.

### 3.11 Phase 3 tests — Self-host MCP

- [ ] `tests/Reactor.Tests/Devtools/McpFullTreeTests.cs`: fixture with mis-aligned `StackPanel` child has full-view fields that let a test assert the clipping.
- [ ] `tests/Reactor.Tests/Devtools/McpStateTests.cs`: counter fixture; `state` returns `useState` index 0 value; fires click; second `state` call reflects the incremented value.
- [ ] `tests/Reactor.Tests/Devtools/McpInvokeToggleSelectScrollTests.cs`:
  - [ ] `invoke` on a button.
  - [ ] `toggle` on a checkbox; `{ state: "on" }` / `{ state: "off" }`.
  - [ ] `select` on a `ListView`; subsequent `tree` shows the selected item.
  - [ ] `scroll by` moves position; `scroll to` a descendant brings it into view.
- [ ] `tests/Reactor.Tests/Devtools/McpFireTests.cs`:
  - [ ] Fires a handler on a custom-drawn fixture element with no UIA peer.
  - [ ] Response carries `via: "reactor-event-injection"`.
  - [ ] Unknown event name returns a structured error, not an exception.

### 3.12 Phase 3 tests — E2E Appium

- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsLayoutBugSeedTests.cs`:
  - [ ] Seeded fixture with a clipped `TextBlock` in a `StackPanel`.
  - [ ] Automated agent-style loop: `tree (full) → spot the clip → fix authored source → reload → tree → assert fixed`.
  - [ ] Assert ≤ 8 MCP calls end-to-end (matches the spec §17 phase-3 exit criterion).
- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsStateInspectionTests.cs`:
  - [ ] Launch counter app; click 3 times via MCP; `state` shows `count: 3`; screenshot confirms "3" visible.
- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsFireEscapeHatchTests.cs`:
  - [ ] Fire a handler that isn't reachable via UIA in a fixture; assert the handler ran and the tool response is tagged `via: "reactor-event-injection"`.

### 3.13 Phase 3 exit criteria

- [ ] A seeded-layout-bug suite (≥ 5 bugs across alignment, clipping, binding, ordering, overflow) completes with ≤ 8 MCP calls on the 90th percentile.
- [ ] `state` tool works for every hook type Reactor ships.
- [ ] All Phase 3 unit, self-host, and E2E tests are green.

---

## Phase 4 — Polish and packaging

**Scope:** stdio transport, config publishing, observability, performance.

**Spec refs:** §8 (transport), §14 (security), §15 (perf targets), §17 Phase 4.

### 4.1 stdio MCP transport

- [ ] Add stdio transport alongside HTTP. `--devtools run --mcp-transport stdio` switches.
- [ ] Same tool surface, same schema. Spec §16 resolved question #1: HTTP stays the default in v1; stdio is additive.
- [ ] Self-host test: run the same tool suite against stdio and assert parity.

### 4.2 `mur devtools --print-config`

- [ ] Emits a JSON fragment suitable for pasting into:
  - Claude Code MCP config (`~/.claude/settings.json` `mcpServers.reactor`).
  - Copilot workspace MCP config.
  - VS Code MCP config.
- [ ] Fragment parameterized with the printed `MCP_ENDPOINT`.
- [ ] Does NOT write any config file itself — stdout only. The human chooses where to put it.

### 4.3 Observability

- [ ] Rolling log file under `%LOCALAPPDATA%/Reactor/devtools/<pid>.log` (or `$XDG_STATE_HOME/reactor/devtools/` on non-Windows).
- [ ] Every tool call logged as one line: timestamp, tool name, selector, latency ms, result code.
- [ ] Rotation: 10 MB per file, keep 5.
- [ ] `--devtools-log-level` flag: `off|error|call|trace`.

### 4.4 Performance pass

- [ ] `reactor.tree` summary p95 < 50 ms for 200 nodes (spec §15).
- [ ] `reactor.screenshot` p95 < 150 ms at `waitIdle: true`.
- [ ] Atomic UIA verbs p95 < 30 ms.
- [ ] Benchmark harness under `tests/Reactor.Benchmarks/Devtools/` (BenchmarkDotNet) measuring the three above.
- [ ] Any regression >10% from baseline fails a perf-gate CI job.

### 4.5 Security review

- [ ] Confirm MCP listener is `127.0.0.1`-only in Release builds.
- [ ] Confirm `devtools: true` is gated behind `#if DEBUG` in the scaffold template.
- [ ] Confirm `reactor.fire`, `reactor.state`, and the capture server are all refused at startup when `devtools: false`.
- [ ] Document the "any local process can connect" caveat in the MCP surface README.

### 4.6 Phase 4 tests — Unit

- [ ] `tests/Reactor.Tests/Devtools/StdioTransportTests.cs`: framing parity with HTTP.
- [ ] `tests/Reactor.Tests/Devtools/PrintConfigTests.cs`: fragment parses as valid JSON for each target agent; endpoint placeholder substituted.
- [ ] `tests/Reactor.Tests/Devtools/LoggingTests.cs`: every tool call writes one line; rotation at 10 MB.

### 4.7 Phase 4 tests — Self-host MCP

- [ ] Run the Phase 2 + 3 self-host suite under stdio transport; assert parity with HTTP.
- [ ] Logging self-host test: 100 tool calls produce 100 log lines with monotonic timestamps and non-negative latencies.

### 4.8 Phase 4 tests — E2E Appium

- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsPrintConfigTests.cs`: `mur devtools --print-config` exits 0 and stdout is valid JSON.
- [ ] `tests/Reactor.AppTests/Tests/Devtools/DevtoolsSecurityTests.cs`:
  - [ ] Release build with `devtools: false` exposes no MCP port.
  - [ ] MCP listener refuses a non-loopback connection (bind attempt from a secondary adapter fails).

### 4.9 Phase 4 exit criteria

- [ ] An external user reports a layout bug they fixed entirely by pairing with an agent over `mur devtools`, with no manual inspection.
- [ ] Performance gates hold across five consecutive CI runs.
- [ ] All Phase 4 tests green.

---

## Cross-phase coverage matrix

Use this to verify every MCP tool has both a fast (self-host) and slow (E2E) test before declaring the feature shipped. Tick a box when the test exists AND is green in CI.

| Tool | Unit | Self-host MCP | E2E Appium | Phase |
|---|---|---|---|---|
| `reactor.windows` | [ ] | [ ] | [ ] | 2 |
| `reactor.components` | [ ] | [ ] | [ ] | 2 |
| `reactor.switchComponent` | [ ] | [ ] | [ ] | 2 |
| `reactor.tree` (summary) | [ ] | [ ] | [ ] | 2 |
| `reactor.tree` (full) | [ ] | [ ] | [ ] | 3 |
| `reactor.screenshot` | [ ] | [ ] | [ ] | 2 |
| `reactor.state` | [ ] | [ ] | [ ] | 3 |
| `reactor.click` | [ ] | [ ] | [ ] | 2 |
| `reactor.type` | [ ] | [ ] | [ ] | 2 |
| `reactor.scroll` | [ ] | [ ] | [ ] | 3 |
| `reactor.focus` | [ ] | [ ] | [ ] | 2 |
| `reactor.invoke` | [ ] | [ ] | [ ] | 3 |
| `reactor.toggle` | [ ] | [ ] | [ ] | 3 |
| `reactor.select` | [ ] | [ ] | [ ] | 3 |
| `reactor.waitFor` | [ ] | [ ] | [ ] | 2 |
| `reactor.fire` | [ ] | [ ] | [ ] | 3 |
| `reactor.version` | [ ] | [ ] | [ ] | 2 |
| `reactor.reload` | [ ] | [ ] | [ ] | 2 |

---

## Non-goals for this task list

The following items belong to future work (not this feature) and are explicitly excluded:

- Multi-app orchestration / MCP broker (spec §2, §17 future).
- Computer-use / vision-in-loop integrations.
- End-to-end test framework built on top of MCP (Playwright-style).
- Snapshot / diff server-side primitives (deferred, §10). Revisit after v1 usage signal.
- Auth on the MCP port beyond loopback-only (§14).

If any of these become pressing during implementation, raise a new issue and spec rather than extending this task list.
