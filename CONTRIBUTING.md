# Contributing to Reactor

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows App SDK 2.0 (experimental) — restored automatically from NuGet, no manual install required
- Visual Studio 2022 (17.8+) or VS Code with C# Dev Kit
- (Optional) [Rust toolchain](https://rustup.rs/) via rustup — needed only for the native differ

> **Package version:** All projects reference `Microsoft.WindowsAppSDK` version **2.0.0-experimental6**
> (public NuGet). The version is centralized in `Directory.Build.props` — update it there to change
> the version for every project at once.

## Building

### From the command line

```bash
# Restore packages (pulls experimental WinUI 3 from NuGet)
dotnet restore Reactor.sln

# Build the entire solution (framework, tests, test app, samples)
dotnet build Reactor.sln -p:Platform=x64

# Build just the framework
dotnet build src/Reactor/Reactor.csproj -p:Platform=x64
```

### From Visual Studio

1. Open `Reactor.sln` in Visual Studio 2022 (17.8+)
2. Select the **x64** or **ARM64** platform from the toolbar (not "Any CPU")
3. Build the solution (Ctrl+Shift+B)

Visual Studio will automatically restore NuGet packages on first load, pulling the
experimental Windows App SDK package.

The Reactor.csproj has an MSBuild target that automatically builds the Rust differ DLL (`viewdiffer.dll`) via Cargo if the Rust toolchain is installed. If Rust is not installed, the build succeeds and the framework falls back to the pure C# reconciliation path at runtime.

### Platforms

The solution targets `x64` and `ARM64`. MSBuild maps these to Rust target triples automatically:
- x64 → `x86_64-pc-windows-msvc`
- ARM64 → `aarch64-pc-windows-msvc`

## Running tests

Reactor has **three types of tests**. Each serves a different purpose — make sure you run the right one.

### Quick reference

```bash
# ── 1. Unit tests (xUnit, no UI window, ~3s) ──
dotnet test tests/Reactor.Tests

# ── 2. Selfhost tests (in-process WinUI window, ~10s) ──
dotnet run --project tests/Reactor.AppTests.Host -- --self-test

# ── 3. Appium / E2E tests (requires WinAppDriver) ──
dotnet test tests/Reactor.AppTests --filter "ClassName!=Reactor.AppTests.Tests.SelfTestBatch"

# ── ALL tests (unit + selfhost + E2E) ──
dotnet test tests/Reactor.Tests && dotnet test tests/Reactor.AppTests
```

> **Platform note:** Omit `-p:Platform=...` to use the default (ARM64 on ARM machines,
> x64 on Intel). Add `-p:Platform=ARM64` or `-p:Platform=x64` to force a specific platform.

### 1. Unit tests (`tests/Reactor.Tests`) — xUnit

2,200+ xUnit tests covering framework internals **without a WinUI window**:
element creation, reconciliation algorithms (LIS, keyed/positional), Yoga layout,
localization, property hashing, control pooling, hooks.

**When to run:** After any code change. Fast (~3s), no prerequisites beyond .NET SDK.

```bash
dotnet test tests/Reactor.Tests

# Run a specific test class
dotnet test tests/Reactor.Tests --filter "FullyQualifiedName~TreeSerializerTests"
```

### 2. Selfhost tests (`tests/Reactor.AppTests.Host --self-test`) — TAP

350+ in-process checks that run inside a real WinUI window at CPU speed. Each fixture
(in `tests/Reactor.AppTests.Host/SelfTest/Fixtures/`) mounts UI via `ReactorHost`, runs
assertions through `VisualTreeHelper`, and outputs TAP results to stdout.

These exercise the full reconciler pipeline (mount → update → unmount) against real
WinUI controls — the **only** way to test the diff system end-to-end.

**When to run:** After reconciler, control mount/update, or UI-related changes.

```bash
# Run directly (TAP output to stdout, ~10s)
dotnet run --project tests/Reactor.AppTests.Host -- --self-test

# Filter by fixture name prefix
dotnet run --project tests/Reactor.AppTests.Host -- --self-test --filter "Flex"
```

> **How it connects to `dotnet test`:** The `SelfTestBatch` class in
> `tests/Reactor.AppTests/Tests/SelfTestBatch.cs` launches the Host app as a subprocess
> with `--self-test`, parses the TAP output, and maps each fixture result to an MSTest
> `[TestMethod]`. This means selfhost tests also run as part of `dotnet test
> tests/Reactor.AppTests` — you don't need to run them separately unless you want the
> raw TAP output or faster iteration.

### 3. Appium / E2E tests (`tests/Reactor.AppTests`) — MSTest + WinAppDriver

End-to-end tests that use Appium/WinAppDriver to simulate real user input (button
clicks, keyboard input, tab navigation) through the cross-process UI Automation
pipeline. These verify the full input→render→output path and validate that UIA
properties are visible to assistive technology.

**When to run:** Before shipping. These are slow and require WinAppDriver.

There are **6 E2E test classes** across two host apps:

| Class | Host app | Methods | What it tests |
|-------|----------|---------|---------------|
| `InteractiveTests` | WinUI | 2 | Counter clicks, observable mutation |
| `AccessibilityTests` | WinUI | 14 | WCAG property validation via UIA |
| `AccessibilityInteractionTests` | WinUI | 10 | Keyboard nav, live regions, headings, semantic panels |
| `EventHandlerTests` | WinUI | 5 | OnTapped, OnSizeChanged, OnPointerPressed, OnKeyDown, UseReducer |
| `DataGridTests` | WinUI | 1 | Click-to-edit, keyboard commit |
| `WinFormsInteropTests` | WinForms | 14 | XAML Island rendering, tab navigation, UIA across boundaries |

```bash
# All E2E tests (excludes SelfTestBatch)
dotnet test tests/Reactor.AppTests --filter "ClassName!=Reactor.AppTests.Tests.SelfTestBatch"

# Run a specific E2E test class
dotnet test tests/Reactor.AppTests --filter "ClassName=Reactor.AppTests.Tests.AccessibilityTests"
```

> **Requires:** [WinAppDriver](https://github.com/microsoft/WinAppDriver/releases) installed
> at `C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe`. The unit tests
> and selfhost tests run without it.
>
> **WinForms tests** also require the `Reactor.WinFormsTests.Host` project to be built.
> It launches a separate WinForms app with a XAML Island.

### Code coverage

```bash
# Unit tests (via coverlet, bundled in Reactor.Tests.csproj)
dotnet test tests/Reactor.Tests --collect:"XPlat Code Coverage"

# Selfhost tests — covers Reactor.dll and ReactorCharting.dll
# (install once: dotnet tool install -g dotnet-coverage)
#
# Step 1: Rebuild with explicit Debug settings (required for instrumentation)
dotnet build tests/Reactor.AppTests.Host -c Debug -p:Optimize=false -p:DebugType=portable
#
# Step 2: Statically instrument Reactor.dll and ReactorCharting.dll (dynamic instrumentation
#          skips referenced assemblies with "optimized_or_instrumented")
dotnet-coverage instrument \
  "tests/Reactor.AppTests.Host/bin/$(RuntimeIdentifier)/Debug/net9.0-windows10.0.22621.0/Reactor.dll" \
  -s coverage.settings.xml
dotnet-coverage instrument \
  "tests/Reactor.AppTests.Host/bin/$(RuntimeIdentifier)/Debug/net9.0-windows10.0.22621.0/ReactorCharting.dll" \
  -s coverage.settings.xml
#
# Step 3: Collect coverage
dotnet-coverage collect -s coverage.settings.xml \
  --output selftest.cobertura.xml --output-format cobertura \
  -- dotnet run --project tests/Reactor.AppTests.Host --no-build -- --self-test
```

> **Platform note:** Replace `$(RuntimeIdentifier)` with `ARM64` or `x64` depending on your machine,
> or omit the platform segment if you used the default platform from `Directory.Build.props`.

The `coverage.settings.xml` file in the repo root controls which modules are included:

### Test architecture

| # | Type | Project | Runner | Count | What it tests |
|---|------|---------|--------|-------|---------------|
| 1 | **Unit** | `tests/Reactor.Tests` | xUnit | 2,200+ | Algorithms, element equality, Yoga layout, hooks — no WinUI window |
| 2 | **Selfhost** | `tests/Reactor.AppTests.Host` | TAP | 350+ | Full reconciler pipeline against real WinUI controls, in-process |
| 3 | **E2E** | `tests/Reactor.AppTests` | Appium/MSTest | 46 | Cross-process UIA validation via WinAppDriver (6 test classes) |

> **No stale binaries:** `Reactor.AppTests.csproj` has a `ProjectReference` to the Host app with `ReferenceOutputAssembly="false"`, so `dotnet test` always builds the Host before running tests.

> **`dotnet test tests/Reactor.AppTests` without a filter runs everything:** both the
> SelfTestBatch (selfhost via subprocess) and all 6 E2E test classes. This is the
> simplest way to run all non-unit tests.

### What the tests cover

- **Element creation and equality** — `ElementTests.cs`
- **Tree serialization** — `TreeSerializerTests.cs`
- **Child reconciliation** — `ChildReconcilerTests.cs`
- **Native differ integration** — `DiffTreesReconcilerTests.cs`, `NativeDifferIntegrationTests.cs`
- **Property hashing** — `PropValueRegistryTests.cs`
- **Control pooling** — `ElementPoolTests.cs`
- **Component props** — `ComponentPropsTests.cs`
- **MVVM interop hooks** — `ObservableHookTests.cs`
- **Regression cases** — `ReconcilerRegressionTests.cs`
- **Yoga layout engine** — `YogaGenerated/*.cs` (590 fixtures ported from Yoga C++ test suite)
- **Flex layout E2E** — 24 fixtures covering nesting, composition, grow/shrink, gaps, padding, margins, alignment, layout-cycle regressions
- **Reconciler E2E** — mount, update, add/remove children, keyed list reuse
- **Error boundaries** — catch and recover from render errors
- **Observable/INPC** — `UseObservable`, `UseObservableProperty`, `UseCollection` hooks
- **PropertyGrid** — reflection, categories, enums, immutable records, custom editors, target switching
- **Localization** — locale switching with ICU MessageFormat
- **Interactive input** — counter buttons, INPC mutation via WinAppDriver (`InteractiveTests.cs`)
- **Accessibility (WCAG)** — UIA property validation for Name, HelpText, HeadingLevel, Landmarks, LiveRegions, AccessKeys, PositionInSet (`AccessibilityTests.cs`)
- **Accessibility interactions** — keyboard tab order, live region announcements, heading hierarchy, semantic panels, LabeledBy resolution (`AccessibilityInteractionTests.cs`)
- **Event handlers** — OnTapped, OnSizeChanged, OnPointerPressed, OnKeyDown, UseReducer via Appium (`EventHandlerTests.cs`)
- **DataGrid editing** — click-to-edit, keyboard commit, cross-row navigation via Appium (`DataGridTests.cs`)
- **WinForms interop** — XAML Island rendering, forward/backward tab cycle across WinForms ↔ WinUI boundary, UIA properties through the island (`WinFormsInteropTests.cs`)

## Running the demo app

The interactive demo app exercises every built-in control:

```bash
dotnet run --project samples/Reactor.TestApp -p:Platform=x64
```

## Project layout

```
src/Reactor/                      Core framework library
  Core/
    Component.cs               Base Component class, hook methods
    Element.cs                 40+ virtual element record types
    RenderContext.cs            Hook state storage, effect tracking
    Reconciler.cs              Tree diff orchestration
    Reconciler.Mount.cs        Mount handlers for each element type
    Reconciler.Update.cs       Update handlers for each element type
    Reconciler.DiffTrees.cs    Native Rust differ integration
    ChildReconciler.cs         Keyed child list reconciliation
    TreeSerializer.cs          Flat tree serialization for the Rust differ
    ElementPool.cs             Control reuse pool
    PropValueRegistry.cs       Property value caching/hashing
  Elements/
    Dsl.cs                     200+ static factory methods (Text, Button, VStack, Flex, etc.)
    ElementExtensions.cs       Fluent modifiers (.Bold(), .Margin(), .Width(), etc.)
    FlexExtensions.cs          .Flex() attached property modifier for flex children
  Flex/
    FlexPanel.cs               CSS Flexbox panel backed by Yoga layout engine
  Yoga/
    YogaAlgorithm.cs           Pure C# port of Meta's Yoga layout algorithm
    YogaNode.cs                Yoga node tree structure
    YogaStyle.cs               Style properties (direction, justify, align, etc.)
    YogaEnums.cs               Yoga enum types (YogaFlexDirection, YogaJustify, etc.)
  Hosting/
    ReactorApp.cs                 Static entry point — ReactorApp.Run<T>()
    ReactorHost.cs                Render loop, state batching, dispatcher scheduling
    ReactorHostControl.cs         Embeddable host for existing WinUI apps
    HotReloadService.cs        .NET Hot Reload integration for Visual Studio
  Native/
    ViewDiffer.cs              C# P/Invoke wrapper for the Rust differ
    differ/                    Rust crate (Cargo.toml, src/)
      src/
        types.rs               Wire types (DifferNode, DifferProp, DifferPatch)
        diff.rs                Tree diff algorithm
        reconcile.rs           Keyed list reconciliation with LIS
        ffi.rs                 extern "C" FFI entry points
        arena.rs               Reusable diff context/buffer
src/Reactor.Cli/                  CLI scaffolding tool (mur --create <Name>)
tests/
  Reactor.Tests/                  1. Unit tests — xUnit (2,200+ tests, no UI window)
  Reactor.AppTests/               2+3. Test runner — MSTest (orchestrates selfhost + E2E tests)
  Reactor.AppTests.Host/          2. Selfhost test app — WinUI host with 60+ in-process fixtures
  stress_perf/                 Performance benchmarks
samples/
  Reactor.TestApp/                Interactive control showcase / demo app
  apps/                        Sample apps (wordpuzzle, ductfiles, regedit, etc.)
  FlexPanelGallery/            FlexPanel layout gallery
  TodoApp/                     Todo app sample
```

## How to add a new element type

Adding a new WinUI control to Reactor requires changes in four places:

### 1. Define the element record (`src/Reactor/Core/Element.cs`)

```csharp
public record MyControlElement(
    string Label,
    Action? OnClick = null
) : Element;
```

### 2. Add a DSL factory method (`src/Reactor/Elements/Dsl.cs`)

```csharp
public static MyControlElement MyControl(string label, Action? onClick = null)
    => new(label, onClick);
```

### 3. Add a mount handler (`src/Reactor/Core/Reconciler.Mount.cs`)

```csharp
private FrameworkElement MountMyControl(MyControlElement el)
{
    var control = new WinUI.MyControl();
    control.Label = el.Label;
    if (el.OnClick != null)
    {
        SetElementTag(control, el);
        control.Click += (s, _) => GetElementTag<MyControlElement>(s)?.OnClick?.Invoke();
    }
    return control;
}
```

Register it in the mount dispatch switch in `Mount()`.

### 4. Add an update handler (`src/Reactor/Core/Reconciler.Update.cs`)

```csharp
private void UpdateMyControl(WinUI.MyControl control, MyControlElement old, MyControlElement @new)
{
    if (old.Label != @new.Label) control.Label = @new.Label;
    SetElementTag(control, @new);
}
```

Register it in the update dispatch switch in `Update()`.

### 5. (Optional) Add modifiers (`src/Reactor/Elements/ElementExtensions.cs`)

If the control has properties that make sense as fluent modifiers, add extension methods.

### 6. Add tests

Add test cases in `tests/Reactor.Tests/` covering element creation, mount, and update.

## How to add a new hook

Hooks live in `src/Reactor/Core/Component.cs` (public API) and `src/Reactor/Core/RenderContext.cs` (implementation).

1. Add the hook method to `Component` (delegates to `RenderContext`)
2. Implement the logic in `RenderContext`, using `GetOrCreateHook<T>()` to manage state
3. Follow the convention: hooks must be called in the same order every render, no conditional calls
4. Add tests in `tests/Reactor.Tests/`

## Working on the Rust native differ

The differ lives in `src/Reactor/Native/differ/`. It's a standalone Rust crate that builds as a `cdylib`.

```bash
# Build the differ directly
cd src/Reactor/Native/differ
cargo build
cargo test

# Run clippy
cargo clippy
```

The C# interop layer is `src/Reactor/Native/ViewDiffer.cs`. If you change any struct layouts in `types.rs`, you **must** update the matching C# structs in `ViewDiffer.cs` — there are no compile-time checks across the FFI boundary (see the [code review](docs/viewdiffer-code-review.md) for details).

Key files:
- `src/types.rs` — wire types shared between Rust and C#
- `src/diff.rs` — tree diff algorithm
- `src/reconcile.rs` — keyed list reconciliation (LIS-based)
- `src/ffi.rs` — `extern "C"` entry points called from C#

## Code style

- **Elements are immutable records.** Use `with` expressions for variations.
- **Hooks follow React conventions.** Same order every render, no conditional hooks.
- **Factory methods over constructors.** `Text("hello")` not `new TextElement("hello")`.
- **Fluent modifiers for layout.** `.Margin(16).Bold()` not constructor parameters.
- **Tag-based event dispatch.** Event handlers are wired once at mount; the current element is stored in `Tag` so handlers always read the latest closure.
- **No XAML.** Everything is C#.

## Hot reload

Reactor supports .NET Hot Reload via `HotReloadService.cs`. When you edit code in Visual Studio and save, the framework re-renders with your changes while preserving hook state. No special setup needed — it hooks into the standard `MetadataUpdateHandler` mechanism.
