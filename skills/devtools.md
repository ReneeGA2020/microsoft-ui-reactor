---
name: reactor-devtools
description: >
  Drive a running Reactor app from code via the devtools MCP server —
  inspect the visual tree, capture screenshots, click/type/scroll, read
  component state, and fire named handlers. Load this when diagnosing a
  visible bug (layout, contrast, wrong text), verifying a change landed,
  or doing any UI automation against a live build.
---

# Reactor Devtools — MCP-driven UI automation

The Reactor devtools are a **JSON-RPC over HTTP loopback** surface that a
debug-build app exposes when launched with `--devtools run`. You use it to
look at the app the way a user does (screenshots, rendered text, layout
bounds) and drive it the way a user does (click, type, toggle, scroll).

Loopback-only, no auth — **DEBUG builds only**, never ship it.

## Launching the app with devtools

The app author enables devtools in their `Program.cs`:

```csharp
ReactorApp.Run<MyApp>("Title", width: 1200, height: 800
#if DEBUG
    , devtools: true
#endif
);
```

Then launch with the `--devtools run` CLI flag:

```bash
dotnet run --project path/to/App.csproj -- --devtools run \
  > /tmp/app-stdout.log 2>&1 &
```

Parse the stdout for the machine-readable ready line (one JSON object on
its own line after first render):

```
{"event":"devtools-ready","endpoint":"http://127.0.0.1:54610/mcp",...}
```

Everything after that is keyed off that `endpoint`. **Always wait for
`devtools-ready` before calling tools** — the port is chosen at random
per-run.

## Discovering the tool surface

`GET /mcp` returns a self-describing document: protocol version, selector
grammar, tree schema version, and the full tool list with input schemas.
Read this once per session instead of guessing:

```bash
curl -s http://127.0.0.1:PORT/mcp | jq '.tools[].name'
```

Every other call is JSON-RPC `tools/call`:

```bash
curl -s http://127.0.0.1:PORT/mcp -H 'Content-Type: application/json' \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/call",
       "params":{"name":"click","arguments":{"selector":"[name=\"Save\"]"}}}'
```

## Tool catalog

| Tool | What it does |
|---|---|
| `version` | Build tag + pid + port — confirm the app is the one you expect. |
| `components` | Class names of every `Component` subclass; `current` is what's mounted. `isNested:true` marks helper components. |
| `switchComponent` | Swap the root component by class name. Invalidates all node ids. |
| `reload` | Exits with sentinel code 42 so `mur devtools` rebuilds. Old ids dead. |
| `windows` | Active window ids, titles, bounds, currently-mounted component. Use `window:"<id>"` when >1. |
| `tree` | Flat array of visual-tree nodes. `view:"full"` adds layout/automation/visual fields. Optional `selector` scopes the walk. |
| `screenshot` | Base64 PNG of the window. `selector` crops to that element's bounds; `includeChrome:true` includes the titlebar. |
| `click` | Prefers Invoke → Toggle → SelectionItem. Returns `via` so you know which pattern fired. |
| `invoke` / `toggle` / `select` | Direct UIA pattern access when you want one specifically. `select` auto-expands ComboBoxes. |
| `type` | Sets TextBox / IValueProvider text. `clear:true` replaces, else appends. |
| `focus` | Programmatic focus on a Control. |
| `scroll` | `by:{vertical:N,horizontal:N}` as **percentage deltas** 0–100 (not pixels), or `to:"<itemSelector>"` for `ScrollIntoView`. Returns both percent and pixel offsets. |
| `expand` / `collapse` | ExpandCollapse pattern (ComboBox popup, TreeViewItem, Expander). |
| `waitFor` | Polls a predicate (`selector`, `textEquals`, `textMatches`, `visible`, `count`) until satisfied or `timeoutMs`. |
| `state` | Dumps every hook value (useState/useReducer/etc.) across mounted components. Great for "why is the UI showing X?". |
| `fire` | Calls a NAMED METHOD on a live component by reflection. Escape hatch for handlers that aren't reachable via UIA. **Inline lambdas (`() => setCount(...)`) are NOT reachable** — only declared methods on the Component class. |

## Selector grammar (5 forms)

1. **Node id** — `r:main/CounterDemo.SubmitButton` — copy from `tree`. Stable within a window's lifetime; invalidated by `switchComponent` / `reload`.
2. **AutomationId** — `#btn-inc`. Matches `AutomationProperties.AutomationId` exactly.
3. **AutomationName** — `[name='Increment']` or `[name="+ 1"]`. Matches `AutomationProperties.Name` OR the visible caption of Buttons / TextBlocks / TextBoxes / ContentControls.
4. **TypePath** — `Button`, `Button[2]`, `StackPanel > Button`. Type name is `GetType().Name`. Index disambiguates.
5. **Reactor source** — `{component:'CounterDemo',line:42}`. Reserved (Phase 3).

`[name=…]` cannot be indexed — if it matches multiple, error is `ambiguous-selector` with all candidate ids listed; pick one by node id or prefix with a TypePath step.

## Typical workflows

### "Does the app look right?" — visual diagnosis

```
screenshot {}            → PNG of full client area
screenshot {selector:"[name='Submit']"}   → cropped to one control
tree {selector:"#login-form", view:"full"}  → layout numbers for that region
```

Full-view `tree` nodes carry `bounds`, `actualSize`, `desiredSize`,
`layout.margin`, `layout.padding`, `isVisible`, `isEnabled`,
`automationControlType`. That's usually enough to spot a margin collapse
or a zero-size child without running the app in a debugger.

### Diagnosing a layout issue

1. `screenshot {}` — confirm what's actually on screen vs. what you expect.
2. `tree {selector:"<suspect container>", view:"full"}` — read `bounds` and `actualSize` down the subtree. A child with `actualSize:{width:0,height:0}` under a parent that's sized means the child isn't getting space (missing `Flex(grow:1)` on a ScrollView inside a FlexColumn is a classic).
3. Edit the Reactor code, rebuild, then `reload` (or relaunch) and re-screenshot.
4. `waitFor {predicate:{selector:"…",visible:true}}` if the state is async — don't screenshot-before-mount.

### Diagnosing a contrast / color issue

1. `screenshot {selector:"<element>"}` — pull the cropped PNG.
2. Decode the PNG client-side and sample foreground/background pixels. Compute WCAG 2.1 ratio = (L1 + 0.05) / (L2 + 0.05) where L is relative luminance. Target ≥ 4.5:1 for body text, ≥ 3:1 for large text / UI chrome.
3. If low, check whether the color came from a Theme token (correct — rebind to the right token for the surface) or a hardcoded hex (wrong — replace with a `Theme.*` token; see `skills/design.md`). Hardcoded colors are the usual culprit and also break High Contrast.
4. Edit, rebuild, screenshot, re-measure.

### Verifying a state-driven change

```
click {selector:"[name='+ 1']"}
waitFor {predicate:{selector:"[name='Current count: 1']"},timeoutMs:1000}
state {}                      → confirm the underlying UseState value
```

`state` is particularly useful when the UI text doesn't obviously encode
the value (e.g. a slider position or a theme toggle).

## Gotchas

1. **Both the source AND the CLI flag are required.** The MCP server only starts if the app was compiled with `devtools: true` passed to `ReactorApp.Run(...)` (usually wrapped in `#if DEBUG`) **and** launched with `--devtools run` on the command line. Miss either and the app boots normally with no MCP port, no banner, no log file. If `curl http://127.0.0.1:PORT/mcp` hangs or you never see `devtools-ready` on stdout, this is almost always why — check `Program.cs` first, then the launch args.
2. **`switchComponent` and `reload` invalidate every node id.** Re-walk the tree after them; do not cache `r:…` ids across swaps.
3. **Popups aren't in the main visual tree.** `tree` walks `window.Content` — ComboBox dropdown items, flyouts, and context menus live in separate popup roots and won't show up. `select` auto-expands the container but item resolution through the main tree will still miss them. Prefer `ISelectionItemProvider` via a node-id that `tree` emitted while the popup was open, or switch to a selector that targets the SelectorItem ancestor directly.
4. **`fire` only sees declared methods.** Inline lambdas (`Button("+1", () => setCount(...))`) are unreachable. Hoist the handler to a method on the Component class when you need `fire` access.
5. **Scroll percent read-back can lag one call.** Right after `scroll {by:{vertical:50}}`, the next tool may report `scrollPercent:0` before the engine settles. If the offset matters, call `scroll {by:{vertical:0}}` once more to read the settled value, or use `to:"<itemSelector>"` (ScrollIntoView) which is deterministic.
6. **Use `waitFor` before asserting on async UI.** Many demos render on a dispatcher hop; a `screenshot` immediately after `click` may capture the pre-state.
7. **Devtools log is authoritative.** Every call lands in `%LOCALAPPDATA%\Reactor\devtools\{pid}.log` (tab-separated: ts, tool, selector, latency, ok/err, rpc code). Tail it to reconstruct a failed run.

## Spec + source pointers

- Spec: `docs/specs/024-ai-agent-devtools.md`
- Server: `src/Reactor/Hosting/Devtools/DevtoolsMcpServer.cs`
- Tool registration: `DevtoolsTools.cs`, `DevtoolsUiaTools.cs`, `DevtoolsFireTool.cs`, `DevtoolsStateTool.cs`
- Selector grammar / parser: `SelectorParser.cs`
