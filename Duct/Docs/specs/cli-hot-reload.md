# CLI Hot Reload for Duct Apps

**Status:** Proposal
**Author:** andersonch
**Date:** 2026-03-23

## Problem

Hot reload works in Visual Studio because VS computes Roslyn deltas and pushes them into the running process. But there's no way to launch a Duct app from the CLI and get hot reload when editing in VS Code (or any other editor).

`dotnet watch` doesn't work for WinUI 3 desktop apps — Microsoft has acknowledged this gap but hasn't fixed it. The no-debugger hot reload path is blocked for desktop frameworks.

## Goal

A `duct dev` CLI command that:
1. Builds and launches a Duct app
2. Watches `.cs` files for changes
3. Computes Roslyn deltas and pushes them to the running process
4. Triggers `MetadataUpdateHandler` → `DuctApp.ActiveHost.RequestRender()` (already implemented)
5. Preserves hook state (UseState, UseRef, etc.) across reloads — same as VS hot reload

## Architecture

```
  ┌─────────────┐         named pipe          ┌──────────────────────┐
  │  duct dev    │ ──── delta bytes ────────►  │  DuctHotReloadAgent  │
  │  (CLI host)  │                             │  (in-process hook)   │
  │              │                             │                      │
  │  FileWatcher │                             │  ApplyUpdate()       │
  │  Roslyn      │                             │  MetadataUpdateHandler│
  │  DeltaGen    │                             │  → RequestRender()   │
  └─────────────┘                              └──────────────────────┘
       watches                                    loaded via
       .cs files                                  DOTNET_STARTUP_HOOKS
```

Two new components:

### Component 1: `Duct.DevHost` (CLI-side delta generator)

A new project (or addition to `Duct.Cli`) that:

1. Runs `dotnet build` on the target project
2. Creates a Roslyn `Workspace` from the built project, establishing the baseline compilation
3. Launches the app process with environment variables:
   - `DOTNET_MODIFIABLE_ASSEMBLIES=debug`
   - `DOTNET_STARTUP_HOOKS=<path-to-agent-dll>`
   - `DOTNET_HOTRELOAD_NAMEDPIPE_NAME=<generated-pipe-name>`
4. Watches `.cs` files with `FileSystemWatcher` (debounced, same pattern as DuctFiles sample)
5. On change:
   a. Re-parses changed files into updated `SyntaxTree`s
   b. Creates a new `Compilation` with the updated trees
   c. Computes deltas via Roslyn's `EmitDifference` API
   d. Sends delta bytes (metadata + IL + PDB) over the named pipe
   e. Reports success/failure and any "rude edit" diagnostics to the console
6. On "rude edit" (unsupported change): offers to restart the process with a fresh build

### Component 2: `Duct.HotReloadAgent` (in-process startup hook)

A small assembly (~100-150 lines) loaded via `DOTNET_STARTUP_HOOKS`:

1. `StartupHook.Initialize()` reads `DOTNET_HOTRELOAD_NAMEDPIPE_NAME` from env
2. Connects to the named pipe as a client
3. Sends capabilities string (`MetadataUpdater.GetCapabilities()`)
4. Spawns a background thread that loops reading delta payloads from the pipe
5. For each payload: calls `MetadataUpdater.ApplyUpdate(assembly, meta, il, pdb)`
6. Discovers and invokes `[MetadataUpdateHandler]` callbacks (ClearCache then UpdateApplication)

The existing `HotReloadService.cs` already handles the `UpdateApplication` callback — no changes needed to the Duct framework itself.

## Delta Computation Strategy

There are two approaches for computing deltas. We should evaluate both.

### Option A: Use `hotreload-delta-gen -live` as a subprocess

The `dotnet/hotreload-utils` repo provides a standalone tool that watches files and generates delta files on disk.

**Workflow:**
1. `duct dev` builds the project
2. Launches `hotreload-delta-gen -msbuild:Project.csproj -live` as a child process
3. Monitors the output directory for new `.dmeta`/`.dil`/`.dpdb` files
4. Reads delta bytes and sends them over the named pipe to the agent

**Pros:**
- Delegates the hard Roslyn work (SemanticEdit computation) to Microsoft's tool
- Less code to write and maintain
- Automatically handles the baseline/compilation state management

**Cons:**
- External tool dependency — must be installed via `dotnet tool install` from a transport feed
- Tool may not be well-maintained (it's in the dotnet org but not a first-class product)
- File-based delta output adds latency vs. in-memory
- Less control over error reporting and UX

### Option B: Use `WatchHotReloadService` from Roslyn (same API as `dotnet watch`)

`Microsoft.CodeAnalysis.ExternalAccess.Watch` exposes `WatchHotReloadService` which wraps the internal EnC analyzer. This is what `dotnet watch` itself uses.

**Workflow:**
1. `duct dev` opens the project via `MSBuildWorkspace`
2. Creates a `WatchHotReloadService` from the workspace solution
3. On file change: updates the workspace document, calls `EmitSolutionUpdateAsync`
4. Receives delta bytes directly in memory
5. Sends them over the named pipe

**Pros:**
- In-memory pipeline — faster, no temp files
- Same code path as `dotnet watch` — well-tested
- Full control over diagnostics and UX
- No external tool dependency beyond NuGet packages

**Cons:**
- Heavier dependency: `Microsoft.CodeAnalysis.CSharp`, `Microsoft.CodeAnalysis.Workspaces.MSBuild`, etc.
- `WatchHotReloadService` is in an "ExternalAccess" namespace — semi-public, may change
- More code to write for workspace management
- MSBuildWorkspace can be finicky (needs MSBuild to be locatable)

### Recommendation

**Start with Option A** — it's faster to ship and validates the end-to-end pipeline. If `hotreload-delta-gen` proves unreliable, migrate to Option B. The agent and named pipe protocol are the same either way, so the in-process component doesn't change.

## Named Pipe Protocol

Use the same protocol as `dotnet watch` for potential future compatibility:

```
Client → Server:  [byte version=0] [string capabilities]
Server → Client:  [byte messageType] [payload...]

Message types:
  0x01 = ManagedCodeUpdate
    [int numDeltas]
    for each delta:
      [guid moduleId]
      [int metaLen] [byte[] metadata]
      [int ilLen]   [byte[] il]
      [int pdbLen]  [byte[] pdb]
      [int updatedTypesLen] [int[] updatedTypeTokens]

  0x00 = Shutdown
```

## New Projects

```
Duct.HotReloadAgent/
  Duct.HotReloadAgent.csproj    # net8.0, no WinUI dependency
  StartupHook.cs                # Entry point (no namespace, internal class)
  HotReloadAgent.cs             # Pipe client, delta applier, handler invoker
  Protocol.cs                   # Named pipe message serialization
```

The `duct dev` command can be added to `Duct.Cli` or as a separate `Duct.DevHost` project. Adding to `Duct.Cli` is simpler since the CLI already exists, but the Roslyn/MSBuild dependencies are heavy. A separate project keeps the CLI lean.

```
Duct.DevHost/
  Duct.DevHost.csproj           # net8.0, references Roslyn + MSBuild workspace
  Program.cs                    # CLI entry: build, launch, watch, push deltas
  DeltaGenerator.cs             # Roslyn compilation + EmitDifference (Option B)
                                # OR hotreload-delta-gen subprocess (Option A)
  FileWatcher.cs                # Debounced FileSystemWatcher
  Protocol.cs                   # Named pipe message serialization (shared types)
```

## CLI UX

```
$ duct dev MyApp/MyApp.csproj
  Building MyApp...
  Build succeeded (2.1s)
  Launching MyApp with hot reload enabled...
  Watching for changes in MyApp/**/*.cs

  [12:34:56] Changed: Components/Counter.cs
  [12:34:56] Applying hot reload delta... OK (0.12s)

  [12:35:10] Changed: Components/TodoList.cs
  [12:35:10] Applying hot reload delta... OK (0.08s)

  [12:36:01] Changed: App.cs
  [12:36:01] Rude edit detected: cannot add new base type
             Press Enter to rebuild and restart, or keep editing...
```

## Implementation Plan

### Phase 1: Agent + named pipe (days 1-2)

1. Create `Duct.HotReloadAgent` project
2. Implement `StartupHook` — connect to named pipe, read capabilities
3. Implement delta receive loop — deserialize payloads, call `ApplyUpdate`
4. Implement `MetadataUpdateHandler` discovery and invocation
5. Test manually: build agent, set env vars, launch TestApp, use `dotnet watch` or VS to push deltas and verify the agent receives and applies them

### Phase 2: DevHost CLI + delta generation (days 2-4)

1. Create `Duct.DevHost` project
2. Implement build + process launch with correct env vars
3. Implement file watcher (adapt DuctFiles pattern)
4. Integrate `hotreload-delta-gen -live` as subprocess (Option A)
   - OR implement `WatchHotReloadService` workspace (Option B)
5. Wire up: file change → delta gen → pipe write → agent applies → UI re-renders
6. Add error handling: rude edits, build failures, process crash detection

### Phase 3: Polish (day 5)

1. Add `duct dev` command to `Duct.Cli` that delegates to `Duct.DevHost`
2. Console output formatting (timestamps, colors, clear error messages)
3. Process lifecycle management (Ctrl+C cleanup, restart on crash)
4. Test with TestApp and sample apps end-to-end

## Open Questions

1. **Option A vs B for delta generation?** Option A (hotreload-delta-gen) is faster to ship but adds an external tool dependency. Option B (WatchHotReloadService) is self-contained but more complex. Should we start with A and migrate, or go straight to B?

2. **Separate project vs. extend Duct.Cli?** The Roslyn/MSBuild dependencies are ~50MB of NuGet packages. Adding them to `Duct.Cli` would bloat the tool. A separate `Duct.DevHost` exe keeps things clean but means two binaries.

3. **Should we also support a "warm restart" fallback?** For rude edits, we could auto-rebuild and relaunch instead of requiring manual intervention. This loses state but keeps the developer in flow.

4. **Named pipe vs simpler IPC?** We could use a simpler protocol (e.g., just write raw delta files to a known directory and signal via a named event). The named pipe approach matches `dotnet watch` conventions and is more robust, but adds protocol complexity.

## References

- [dotnet/hotreload-utils](https://github.com/dotnet/hotreload-utils) — delta generation tool
- [dotnet/sdk DotNetDeltaApplier](https://github.com/dotnet/sdk/tree/main/src/BuiltInTools/DotNetDeltaApplier) — reference agent implementation
- [DOTNET_STARTUP_HOOKS design](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md)
- [MetadataUpdater.ApplyUpdate API](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.metadata.metadataupdater.applyupdate)
- [How Rider Hot Reload Works](https://blog.jetbrains.com/dotnet/2021/12/02/how-rider-hot-reload-works-under-the-hood/)
- [Roslyn EmitDifference API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.compilation.emitdifference)
- [WinUI 3 hot reload gap (issue #7043)](https://github.com/microsoft/microsoft-ui-xaml/issues/7043)
