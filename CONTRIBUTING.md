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

```bash
# Run all tests
dotnet test Duct.Tests

# Run a specific test class
dotnet test Duct.Tests --filter "FullyQualifiedName~TreeSerializerTests"

# Run with verbose output
dotnet test Duct.Tests -v normal
```

Tests use **xUnit** and target the same `net8.0-windows10.0.22621.0` framework as the main library. The test project has `InternalsVisibleTo` access so it can test internal APIs.

### UI tests

```bash
# Run the UI integration tests (launches TestApp with --self-test)
dotnet test Duct.UITests -p:Platform=x64

# Or run the self-test harness directly
dotnet run --project Duct.TestApp -p:Platform=x64 -- --self-test
```

UI tests exercise the full Duct pipeline (Element DSL → Reconciler → WinUI control tree) by launching the TestApp with a `--self-test` flag. The app uses `VisualTreeHelper` and `ButtonAutomationPeer` to walk and interact with its own WinUI control tree in-process, then reports TAP-format results. The `Duct.UITests` MSTest project wraps these results so they integrate with `dotnet test` and Visual Studio Test Explorer.

> **Why not WinAppDriver?** WinUI 3 Desktop apps host XAML inside a `DesktopChildSiteBridge` which creates a UIA boundary that external-process automation tools (WinAppDriver, FlaUI, System.Windows.Automation) cannot traverse. In-process testing via `VisualTreeHelper` is the reliable approach.

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

## Running the test app

The interactive test app exercises every built-in control:

```bash
dotnet run --project Duct.TestApp
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
    Dsl.cs                     200+ static factory methods (Text, Button, VStack, etc.)
    ElementExtensions.cs       Fluent modifiers (.Bold(), .Margin(), .Width(), etc.)
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
Duct.Tests/                    xUnit test suite
Duct.TestApp/                  Interactive control showcase
Duct.Cli/                      CLI scaffolding tool (duct --create <Name>)
samples/apps/
  wordpuzzle/                  Word puzzle game sample
  ductfiles/                   File explorer sample
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

Add test cases in `Duct.Tests/` covering element creation, mount, and update.

## How to add a new hook

Hooks live in `Duct/Core/Component.cs` (public API) and `Duct/Core/RenderContext.cs` (implementation).

1. Add the hook method to `Component` (delegates to `RenderContext`)
2. Implement the logic in `RenderContext`, using `GetOrCreateHook<T>()` to manage state
3. Follow the convention: hooks must be called in the same order every render, no conditional calls
4. Add tests in `Duct.Tests/`

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
