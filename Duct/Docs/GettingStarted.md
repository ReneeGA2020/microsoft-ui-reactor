# Getting Started with Duct

## Minimal App

A complete Duct application in a single file:

```csharp
using Duct;
using Duct.Core;
using static Duct.UI;

DuctApp.Run<HelloWorld>("My App");

class HelloWorld : Component
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

## Key Imports

```csharp
using Duct;                   // DuctApp, UI (DSL), element extensions
using Duct.Core;              // Component, Element, RenderContext
using Microsoft.UI.Xaml;     // WinUI types (Layout, HorizontalAlignment, etc.)
using static Duct.UI;         // Brings Text(), Button(), VStack() etc into scope
```

## Creating Elements

Patch provides static factory methods for every built-in control:

```csharp
// Text
Text("Hello")
Heading("Title")           // 28px bold
SubHeading("Subtitle")     // 20px semi-bold
Caption("Fine print")      // 12px

// Buttons
Button("Click me", () => DoSomething())
Button("Disabled").Disabled()

// Input
TextField(value, setValue, placeholder: "Type here")
CheckBox(isChecked, setChecked, label: "Agree")
Slider(value, min: 0, max: 100, onChanged: setValue)
ToggleSwitch(isOn, setOn, onContent: "Yes", offContent: "No")

// Progress
Progress(0.75)             // Determinate (75%)
ProgressIndeterminate()     // Spinning
```

## Layout

```csharp
// Vertical stack (most common)
VStack(
    Text("First"),
    Text("Second"),
    Text("Third")
)

// With custom spacing
VStack(16,
    Text("More"),
    Text("Space")
)

// Horizontal
HStack(8,
    Button("A"),
    Button("B"),
    Button("C")
)

// Scroll
ScrollView(
    VStack(items.Select(i => Text(i)).ToArray())
)

// Border (card-like container)
Border(
    VStack(Text("Inside a card"))
).CornerRadius(8).Background("#f5f5f5").Padding(new Thickness(16, 16, 16, 16))

// Grid
Grid(
    columns: ["*", "*"],
    rows: ["Auto", "*"],
    Cell(Text("Top Left"), row: 0, column: 0),
    Cell(Text("Top Right"), row: 0, column: 1),
    Cell(Text("Bottom"), row: 1, column: 0, columnSpan: 2)
)
```

## Modifiers (Fluent Style)

Every element supports layout modifiers via extension methods:

```csharp
Text("Hello")
    .Bold()
    .FontSize(24)
    .Margin(16)
    .HAlign(HorizontalAlignment.Center)
    .Width(200)
    .Opacity(0.8)
    .ToolTip("A greeting")
    .Visible(showGreeting)
```

## State Management

### UseState

The primary hook — declares a piece of reactive state:

```csharp
var (count, setCount) = UseState(0);
// count is the current value
// setCount(newValue) updates it and triggers a re-render
```

### UseReducer

For complex state updates that depend on the previous value:

```csharp
var (items, updateItems) = UseReducer(new List<string>());
// updateItems takes a Func<T, T>:
updateItems(list => [.. list, "new item"]);
```

### UseEffect

Run side effects (timers, subscriptions, etc.):

```csharp
// Run once on mount
UseEffect(() => {
    Console.WriteLine("Mounted!");
});

// Run when `count` changes
UseEffect(() => {
    Console.WriteLine($"Count is now {count}");
}, count);

// With cleanup
UseEffect(() => {
    var timer = new Timer(_ => tick(), null, 0, 1000);
    return () => timer.Dispose();  // cleanup
}, /* deps */);
```

### UseMemo

Memoize expensive computations:

```csharp
var sorted = UseMemo(() => items.OrderBy(x => x.Name).ToList(), items);
```

## Conditional Rendering

Patch supports multiple patterns for conditional UI:

```csharp
// C# ternary (most natural)
isLoggedIn ? Text($"Welcome, {name}") : Button("Log in", onLogin)

// Null coalescing (element is skipped when null)
VStack(
    Text("Always shown"),
    showExtra ? Text("Conditional") : null  // null children are filtered out
)

// When() helper
When(items.Any(), () => Text($"{items.Count} items"))

// If/else helper
If(isError,
    () => Text("Something went wrong!"),
    () => Text("All good"))

// Pattern matching (powerful for multi-case)
status switch
{
    Status.Loading => ProgressIndeterminate(),
    Status.Error => Text("Error!"),
    Status.Success => Text("Done!"),
    _ => Empty()
}
```

## List Rendering

```csharp
// Using LINQ + params
VStack(
    items.Select(item => Text(item.Name)).ToArray()
)

// Using ForEach helper
ForEach(items, item => Text(item.Name))

// With index
ForEach(items, (item, i) => Text($"{i + 1}. {item.Name}"))
```

## Composing Components

### Class components

```csharp
class UserCard : Component
{
    public override Element Render()
    {
        var (expanded, setExpanded) = UseState(false);
        return Border(
            VStack(
                Text("User").Bold(),
                When(expanded, () => Text("Details here...")),
                Button(expanded ? "Less" : "More", () => setExpanded(!expanded))
            )
        ).CornerRadius(8);
    }
}

// Use it:
VStack(
    Component<UserCard>(),
    Component<UserCard>()
)
```

### Inline function components

```csharp
var toggle = Func(ctx =>
{
    var (on, setOn) = ctx.UseState(false);
    return CheckBox(on, setOn, label: "Toggle me");
});
```

## Running the App

```csharp
// Simple
DuctApp.Run<MyComponent>("Window Title");

// With size
DuctApp.Run<MyComponent>("Window Title", width: 800, height: 600);

// Inline root
DuctApp.Run("My App", ctx =>
{
    var (msg, setMsg) = ctx.UseState("Hello!");
    return Text(msg);
});
```

## Project Setup

Your `.csproj` needs:
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
    <ProjectReference Include="..\Patch\Duct.csproj" />
  </ItemGroup>
</Project>
```

No XAML files needed. No `App.xaml`. No `MainWindow.xaml`. Just C#.
