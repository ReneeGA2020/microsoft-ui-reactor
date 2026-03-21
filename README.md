# Duct — Functional UI for WinUI 3

Duct is a React/SwiftUI/Compose-inspired framework for building WinUI 3 desktop apps in pure C#. Declarative, component-based, no XAML — just C#.

```csharp
using Duct;
using Duct.Core;
using static Duct.UI;

DuctApp.Run<MyApp>("Hello Duct");

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

- .NET 8 SDK
- Windows App SDK 1.8+
- (Optional) Rust toolchain — only needed if you want the native differ

### Create a new app

```bash
# Using the Duct CLI
dotnet run --project Duct.Cli -- --create MyApp
```

Or manually — create a `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.8.260101001" />
    <ProjectReference Include="..\Duct\Duct.csproj" />
  </ItemGroup>
</Project>
```

Add a single `App.cs`:

```csharp
using Duct;
using Duct.Core;
using static Duct.UI;

DuctApp.Run<App>("My App", width: 900, height: 600);

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
dotnet build Duct.sln
dotnet run --project Duct.TestApp
```

### Run tests

```bash
dotnet test Duct.Tests
```

## Project structure

```
Duct/                  Core framework
  Core/                Reconciler, components, hooks, elements
  Elements/            DSL factory methods + fluent modifiers
  Hosting/             App bootstrap, render loop, hot reload
  Native/              Experimental Rust differ (ViewDiffer)
Duct.Tests/            xUnit test suite
Duct.TestApp/          Interactive control showcase
Duct.Cli/              CLI scaffolding tool
samples/apps/
  wordpuzzle/          Word puzzle game
  ductfiles/           File explorer demo
```

## Documentation

| Doc | Description |
|-----|-------------|
| [Getting Started](Duct/Docs/GettingStarted.md) | Tutorial — elements, layout, state, components |
| [Architecture](Duct/Docs/Architecture.md) | Virtual tree, reconciler, hooks, design decisions |
| [Contributing](CONTRIBUTING.md) | Build, test, add features, code style |
| [State & Hooks](docs/reference/state-and-hooks.md) | Deep dive on the hook system and reactivity |
| [Reconciliation](docs/reference/reconciliation.md) | How tree diffing works (C# and Rust paths) |
| [Native Differ](docs/reference/native-differ.md) | Rust differ architecture, FFI, experiments |
| [WinUI Integration Proposals](docs/winui3-integration-proposals.md) | Features we need in WinUI to go further |

## Sample apps

### DuctFiles — file explorer

A full file explorer built with Duct, demonstrating TreeView, hierarchical navigation, component composition, and platform interop.

```bash
dotnet run --project samples/apps/ductfiles
```

### WordPuzzle — word search game

A word search game demonstrating grid layout, timers, and interactive state.

```bash
dotnet run --project samples/apps/wordpuzzle
```

## License

Microsoft Internal
