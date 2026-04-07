# Contributing to Duct

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
dotnet restore Duct.sln

# Build the entire solution (framework, tests, test app, samples)
dotnet build Duct.sln -p:Platform=x64

# Build just the framework
dotnet build Duct/Duct.csproj -p:Platform=x64
```

### From Visual Studio

1. Open `Duct.sln` in Visual Studio 2022 (17.8+)
2. Select the **x64** or **ARM64** platform from the toolbar (not "Any CPU")
3. Build the solution (Ctrl+Shift+B)

Visual Studio will automatically restore NuGet packages on first load, pulling the
experimental Windows App SDK package.

The Duct.csproj has an MSBuild target that automatically builds the Rust differ DLL (`viewdiffer.dll`) via Cargo if the Rust toolchain is installed. If Rust is not installed, the build succeeds and the framework falls back to the pure C# reconciliation path at runtime.

### Platforms

The solution targets `x64` and `ARM64`. MSBuild maps these to Rust target triples automatically:
- x64 → `x86_64-pc-windows-msvc`
- ARM64 → `aarch64-pc-windows-msvc`

## Running tests

### Quick reference

```bash
# ── Unit tests (xUnit, no UI window, ~3s) ──
dotnet test tests/Duct.Tests

# ── Self-tests (in-process WinUI, ~15s) ──
dotnet test tests/Duct.AppTests --filter "ClassName=Duct.AppTests.Tests.SelfTestBatch"

# ── UIA / interactive tests (Appium + WinAppDriver, ~30s) ──
dotnet test tests/Duct.AppTests --filter "ClassName=Duct.AppTests.Tests.InteractiveTests"

# ── ALL tests (unit + self-test + UIA) ──
dotnet test Duct.sln
```

> **Platform note:** Omit `-p:Platform=...` to use the default (ARM64 on ARM machines,
> x64 on Intel). Add `-p:Platform=ARM64` or `-p:Platform=x64` to force a specific platform.

### Unit tests (`Duct.Tests`)

2,200+ xUnit tests covering framework internals without a WinUI window:
element creation, reconciliation algorithms (LIS, keyed/positional), Yoga layout,
localization, property hashing, control pooling, hooks.

```bash
dotnet test tests/Duct.Tests

# Run a specific test class
dotnet test tests/Duct.Tests --filter "FullyQualifiedName~TreeSerializerTests"
```

### Self-tests (`Duct.AppTests` → `Duct.AppTests.Host --self-test`)

60+ in-process fixtures that run inside a real WinUI window at CPU speed. Each fixture
mounts UI via `DuctHost`, runs assertions through `VisualTreeHelper`, and outputs TAP
results. The MSTest runner parses TAP output and maps each check to an individual test.

These exercise the full reconciler pipeline (mount → update → unmount) against real
WinUI controls — the only way to test the diff system end-to-end.

```bash
# Via MSTest runner (reports to VS Test Explorer)
dotnet test tests/Duct.AppTests --filter "ClassName=Duct.AppTests.Tests.SelfTestBatch"

# Or run the host directly (raw TAP output)
dotnet run --project tests/Duct.AppTests.Host -- --self-test
```

### UIA / interactive tests (`Duct.AppTests` → Appium)

2 end-to-end tests that use Appium/WinAppDriver to simulate real user input (button
clicks, INPC mutation) through the cross-process UI Automation pipeline. These verify
the full input→render→output path.

```bash
dotnet test tests/Duct.AppTests --filter "ClassName=Duct.AppTests.Tests.InteractiveTests"
```

> **Requires:** [WinAppDriver](https://github.com/microsoft/WinAppDriver/releases) installed
> at `C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe`. The unit tests
> and self-tests run without it.

### Code coverage

```bash
# Unit tests (via coverlet, bundled in Duct.Tests.csproj)
dotnet test tests/Duct.Tests --collect:"XPlat Code Coverage"

# Self-tests (via dotnet-coverage profiler — install once with: dotnet tool install -g dotnet-coverage)
dotnet-coverage collect --output selftest.cobertura.xml --output-format cobertura \
  -- dotnet run --project tests/Duct.AppTests.Host -- --self-test
```

### Test architecture

| Layer | Project | Runner | Count | What it tests |
|-------|---------|--------|-------|---------------|
| **Unit** | `Duct.Tests` | xUnit | 2,200+ | Algorithms, element equality, Yoga layout, hooks — no WinUI window |
| **Self-test** | `Duct.AppTests.Host` | TAP → MSTest | 60+ | Full reconciler pipeline against real WinUI controls, in-process |
| **UIA** | `Duct.AppTests` | Appium/MSTest | 2 | Cross-process input injection via WinAppDriver |

> **No stale binaries:** `Duct.AppTests.csproj` has a `ProjectReference` to the Host app with `ReferenceOutputAssembly="false"`, so `dotnet test` always builds the Host before running tests.

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
- **Interactive input** — counter buttons, INPC mutation via WinAppDriver

## Running the demo app

The interactive demo app exercises every built-in control:

```bash
dotnet run --project samples/Duct.TestApp -p:Platform=x64
```

## Project layout

```
Duct/                          Core framework library
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
    DuctApp.cs                 Static entry point — DuctApp.Run<T>()
    DuctHost.cs                Render loop, state batching, dispatcher scheduling
    DuctHostControl.cs         Embeddable host for existing WinUI apps
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
Duct.Cli/                      CLI scaffolding tool (duct --create <Name>)
tests/
  Duct.Tests/                  xUnit unit test suite (2,100+ tests)
  Duct.AppTests/               MSTest UI tests (Appium + in-process self-test)
  Duct.AppTests.Host/          WinUI test host app (fixture navigator + self-test mode)
  stress_perf/                 Performance benchmarks
samples/
  Duct.TestApp/                Interactive control showcase / demo app
  apps/                        Sample apps (wordpuzzle, ductfiles, regedit, etc.)
  FlexPanelGallery/            FlexPanel layout gallery
  TodoApp/                     Todo app sample
```

## How to add a new element type

Adding a new WinUI control to Duct requires changes in four places:

### 1. Define the element record (`Duct/Core/Element.cs`)

```csharp
public record MyControlElement(
    string Label,
    Action? OnClick = null
) : Element;
```

### 2. Add a DSL factory method (`Duct/Elements/Dsl.cs`)

```csharp
public static MyControlElement MyControl(string label, Action? onClick = null)
    => new(label, onClick);
```

### 3. Add a mount handler (`Duct/Core/Reconciler.Mount.cs`)

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

### 4. Add an update handler (`Duct/Core/Reconciler.Update.cs`)

```csharp
private void UpdateMyControl(WinUI.MyControl control, MyControlElement old, MyControlElement @new)
{
    if (old.Label != @new.Label) control.Label = @new.Label;
    SetElementTag(control, @new);
}
```

Register it in the update dispatch switch in `Update()`.

### 5. (Optional) Add modifiers (`Duct/Elements/ElementExtensions.cs`)

If the control has properties that make sense as fluent modifiers, add extension methods.

### 6. Add tests

Add test cases in `tests/Duct.Tests/` covering element creation, mount, and update.

## How to add a new hook

Hooks live in `Duct/Core/Component.cs` (public API) and `Duct/Core/RenderContext.cs` (implementation).

1. Add the hook method to `Component` (delegates to `RenderContext`)
2. Implement the logic in `RenderContext`, using `GetOrCreateHook<T>()` to manage state
3. Follow the convention: hooks must be called in the same order every render, no conditional calls
4. Add tests in `tests/Duct.Tests/`

## Working on the Rust native differ

The differ lives in `Duct/Native/differ/`. It's a standalone Rust crate that builds as a `cdylib`.

```bash
# Build the differ directly
cd Duct/Native/differ
cargo build
cargo test

# Run clippy
cargo clippy
```

The C# interop layer is `Duct/Native/ViewDiffer.cs`. If you change any struct layouts in `types.rs`, you **must** update the matching C# structs in `ViewDiffer.cs` — there are no compile-time checks across the FFI boundary (see the [code review](docs/viewdiffer-code-review.md) for details).

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

Duct supports .NET Hot Reload via `HotReloadService.cs`. When you edit code in Visual Studio and save, the framework re-renders with your changes while preserving hook state. No special setup needed — it hooks into the standard `MetadataUpdateHandler` mechanism.
