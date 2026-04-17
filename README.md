# Reactor — Functional UI for WinUI 3

Reactor is a React/SwiftUI/Compose-inspired framework for building WinUI 3 desktop apps in pure C#. Declarative, component-based, no XAML — just C#.

```csharp
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

ReactorApp.Run<MyApp>("Hello Reactor");

class MyApp : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);

        return VStack(
            Heading($"Count: {count}"),
            HStack(8,
                Button("-", () => setCount(count - 1)),
                Button("+", () => setCount(count + 1))
            )
        );
    }
}
```

**No XAML files. No data binding. No code-behind. No ViewModels.** Just C# with full IntelliSense, refactoring, and type safety.

## Key ideas

- **Virtual element tree** — lightweight immutable records describe UI; a reconciler diffs old vs. new and patches only what changed (like React's virtual DOM).
- **Hooks for state** — `UseState`, `UseReducer`, `UseEffect`, `UseMemo`, `UseRef`, and more. State lives next to the render logic, not in a separate ViewModel.
- **Fluent DSL** — `Text("hello").Bold().Margin(16)` instead of XAML markup. 200+ factory methods cover every built-in WinUI control.
- **Experimental native differ** — an optional Rust-based tree differ for faster reconciliation, integrated via P/Invoke.

## Quick start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows App SDK 2.0 (experimental) — installed automatically via NuGet on `dotnet restore`
- Visual Studio 2022 (17.8+) for IDE builds, or just the .NET 8 SDK for CLI builds
- (Optional) [Rust toolchain](https://rustup.rs/) — only needed if you want the native differ

> **Note:** This project uses the **experimental** Windows App SDK 2.0 (`Microsoft.WindowsAppSDK` 2.0.0-experimental6).
> The package is pulled from [NuGet](https://www.nuget.org/packages/Microsoft.WindowsAppSDK) automatically —
> no manual SDK installer is needed. If you want the WinUI 3 runtime for unpackaged apps, install
> the [Windows App Runtime](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)
> or set `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>` (already set in this repo).

### Create a new app

```bash
# Using the Reactor CLI
dotnet run --project Reactor.Cli -- --create MyApp
```

Or manually — create a `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
    <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="2.0.0-experimental6" />
    <ProjectReference Include="..\Reactor\Reactor.csproj" />
  </ItemGroup>
</Project>
```

Add a single `App.cs`:

```csharp
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

ReactorApp.Run<App>("My App", width: 900, height: 600);

class App : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("World");

        return VStack(
            Heading($"Hello, {name}!"),
            TextField(name, setName, placeholder: "Your name")
        );
    }
}
```

No `App.xaml`. No `MainWindow.xaml`. Just run it.

### Build and run

```bash
# Restore NuGet packages (pulls the experimental WinUI 3 SDK automatically)
dotnet restore Reactor.sln

# Build the entire solution — from CLI
dotnet build Reactor.sln -p:Platform=x64

# Or open Reactor.sln in Visual Studio 2022, select x64 or ARM64, and build (Ctrl+Shift+B)

# Run the interactive demo app
dotnet run --project samples/Reactor.TestApp -p:Platform=x64
```

### Run tests

There are three types of tests — pick the right one for your scenario:

```bash
# ── 1. Unit tests (xUnit, no UI window, ~3s) ──
# Fast, headless tests for framework internals: reconciliation, elements, hooks, Yoga layout.
dotnet test tests/Reactor.Tests

# ── 2. Selfhost tests (in-process WinUI window, ~15s) ──
# 60+ fixtures that mount real WinUI controls and assert via VisualTreeHelper.
# This is the only way to test the reconciler end-to-end against real controls.
dotnet test tests/Reactor.AppTests --filter "ClassName=Reactor.AppTests.Tests.SelfTestBatch"

# ── 3. Appium / E2E tests (requires WinAppDriver, ~30s) ──
# Cross-process UI Automation tests that simulate real user input.
# Requires WinAppDriver installed at C:\Program Files (x86)\Windows Application Driver\
dotnet test tests/Reactor.AppTests --filter "ClassName=Reactor.AppTests.Tests.InteractiveTests"

# ── Run everything ──
dotnet test Reactor.sln
```

## Live preview

Reactor includes a built-in preview mode that lets you see a single component in isolation with hot reload. There are two ways to use it: from the CLI, or via the VS Code extension.

### CLI preview with hot reload

Any Reactor app that passes `preview: true` to `ReactorApp.Run` supports the `--preview` flag:

```csharp
ReactorApp.Run<App>("My App", preview: true);
```

Then use `dotnet watch` for live hot reload:

```bash
# Preview a specific component — rebuilds and refreshes on every file save
dotnet watch run --project MyApp -- --preview CounterDemo

# Preview the first component found (no name needed)
dotnet watch run --project MyApp -- --preview
```

`dotnet watch` monitors your source files and incrementally rebuilds on save. The preview window updates automatically — no manual restart needed.

You can also list all available components:

```bash
dotnet run --project MyApp -- --preview-list
```

### VS Code extension (recommended)

The **Reactor Preview** extension adds a live preview panel directly in VS Code. It launches a single preview process and switches between components instantly via HTTP — no process restart when you change components.

#### Install

```bash
cd vscode-reactor
npm install
npm run compile
```

Then install the extension in VS Code:

1. Open VS Code
2. Run **Extensions: Install from VSIX...** from the command palette, or:
   ```bash
   code --install-extension vscode-reactor
   ```
   (If you're developing the extension locally, press **F5** in the `vscode-reactor` folder to launch an Extension Development Host instead.)

#### Usage

1. Open a C# file containing a Reactor `Component`
2. Run **Reactor: Preview Component** from the command palette (`Ctrl+Shift+P`)
3. A preview panel opens beside your editor showing a live capture of the component

The extension:
- **Auto-detects components** in the current file and populates a dropdown if there are multiple
- **Switches components instantly** via the dropdown — no relaunch, just an HTTP call to swap what's mounted
- **Hot reloads on save** — powered by `dotnet watch` under the hood
- **Follows your editor** — when you switch to a different C# file with components, the preview updates automatically
- **Only relaunches** if you navigate to a file in a different `.csproj`

#### Commands

| Command | Description |
|---------|-------------|
| **Reactor: Preview Component** | Start preview for the current file |
| **Reactor: Connect to Preview** | Connect to an already-running preview by port |
| **Reactor: Stop Preview** | Stop the preview process |
| **Reactor: Focus Preview Window** | Bring the native preview window to front |

## Project structure

```
src/Reactor/              Core framework
  Core/                Reconciler, components, hooks, elements
  Elements/            DSL factory methods + fluent modifiers
  Flex/                FlexPanel — CSS Flexbox layout via Yoga engine
  Yoga/                Pure C# port of Meta's Yoga layout engine
  Hosting/             App bootstrap, render loop, hot reload, preview capture server
  Native/              Experimental Rust differ (ViewDiffer)
src/Reactor.Cli/          CLI scaffolding tool
src/vscode-reactor/       VS Code extension — live preview panel
tests/
  Reactor.Tests/          Unit tests — xUnit, 2,200+ tests incl. 590 Yoga layout fixtures
  Reactor.AppTests/       Test runner — MSTest, orchestrates selfhost + Appium tests
  Reactor.AppTests.Host/  Selfhost test app — WinUI host with 60+ in-process fixtures
  stress_perf/         Performance benchmarks
samples/
  apps/                Sample apps (wordpuzzle, ductfiles, regedit, etc.)
  FlexPanelGallery/    FlexPanel layout gallery
  TodoApp/             Todo app sample
```

## Documentation

| Doc | Description |
|-----|-------------|
| [Getting Started](src/Reactor/Docs/GettingStarted.md) | Tutorial — elements, layout, state, components |
| [Architecture](src/Reactor/Docs/Architecture.md) | Virtual tree, reconciler, hooks, design decisions |
| [Flex Layout Spec](src/Reactor/Docs/specs/flex-layout.md) | CSS Flexbox via Yoga — FlexPanel design and API |
| [Contributing](CONTRIBUTING.md) | Build, test, add features, code style |
| [State & Hooks](docs/reference/state-and-hooks.md) | Deep dive on the hook system and reactivity |
| [Reconciliation](docs/reference/reconciliation.md) | How tree diffing works (C# and Rust paths) |
| [Native Differ](docs/reference/native-differ.md) | Rust differ architecture, FFI, experiments |
| [WinUI Integration Proposals](docs/winui3-integration-proposals.md) | Features we need in WinUI to go further |

## Sample apps

### ReactorFiles — file explorer

A full file explorer built with Reactor, demonstrating TreeView, hierarchical navigation, component composition, and platform interop.

```bash
dotnet run --project samples/apps/reactorfiles
```

### WordPuzzle — word search game

A word search game demonstrating grid layout, timers, and interactive state.

```bash
dotnet run --project samples/apps/wordpuzzle
```

## License

Microsoft Internal
