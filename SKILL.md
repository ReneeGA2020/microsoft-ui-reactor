---
name: duct-app
description: >
  Create WinUI 3 desktop applications using the Duct (Functional UI) framework — a React-inspired
  declarative C# projection over WinUI 3. No XAML, no data binding, no templates. Use when asked
  to "create a Duct app", "build a desktop app with Duct", "make a WinUI app", or any request
  involving the Duct framework. This skill contains the complete API reference, patterns, and
  project setup needed to generate correct Patch code in one shot.
---

# Duct App Builder — Complete Reference for Code Generation

## What is Patch?

Duct (Functional UI) is a **React-inspired functional projection for WinUI 3**. It replaces XAML
with pure C# using a declarative, component-based approach. Think of it as "React hooks + JSX,
but in C# targeting native Windows controls."

**Key mental model:** You write functions that return lightweight element descriptions (the
"virtual DOM"). Patch diffs old vs new trees and patches real WinUI controls. State changes trigger
re-renders automatically.

---

## Project Setup

### .csproj (REQUIRED — copy exactly)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.22621.0</TargetFramework>
    <Platforms>x64;ARM64</Platforms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
    <WindowsSdkPackageVersion>10.0.22621.52</WindowsSdkPackageVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="2.0.0-experimental4" />
    <ProjectReference Include="..\Patch\Duct.csproj" />
  </ItemGroup>
</Project>
```

**Critical notes:**
- `WindowsPackageType` MUST be `None` (unpackaged app — no App.xaml needed)
- `UseWinUI` MUST be `true`
- No XAML files of any kind. No App.xaml. No MainWindow.xaml. Just `.cs` files.

### Required Imports (top of every file)

```csharp
using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;           // Thickness, HorizontalAlignment, VerticalAlignment
using Microsoft.UI.Xaml.Controls;  // Orientation, InfoBarSeverity, etc. (if needed)
using static Duct.UI;  // Brings Text(), Button(), VStack() etc. into scope
```

The `using static Duct.UI;` import is essential — it makes all DSL factory methods available
as bare function calls (like JSX tags). The WinUI usings give you access to enums and structs
like `Thickness`, `HorizontalAlignment`, and `Orientation` — Patch uses WinUI types directly.

### App Entry Point

```csharp
// Top-level statement — this IS the entire Program.cs
DuctApp.Run<MyRootComponent>("Window Title", width: 1024, height: 768);
```

Alternative inline root (no component class needed):
```csharp
DuctApp.Run("Window Title", ctx =>
{
    var (msg, setMsg) = ctx.UseState("Hello!");
    return VStack(Text(msg), Button("Change", () => setMsg("Changed!")));
});
```

---

## Components

### Class Components (primary pattern)

```csharp
class MyApp : Component
{
    public override Element Render()
    {
        // Hook calls go here (must be in same order every render)
        var (count, setCount) = UseState(0);

        // Return an element tree
        return VStack(
            Text($"Count: {count}"),
            Button("+1", () => setCount(count + 1))
        );
    }
}
```

### Function Components (inline, for small reusable pieces)

```csharp
// Used with UI.Func() — gets its own RenderContext for independent state
var toggle = Func(ctx =>
{
    var (on, setOn) = ctx.UseState(false);
    return ToggleSwitch(on, setOn);
});
```

### Embedding Components

```csharp
// Class component as child element:
VStack(
    Component<MyWidget>(),
    Component<AnotherWidget>()
)
```

**IMPORTANT:** Patch components do NOT accept props through constructor parameters.
Each Component class is self-contained with its own state via hooks.
To pass data between components, use shared state patterns or restructure as
inline rendering within a parent component.

---

## Hooks (State Management)

All hooks follow React rules:
1. **Call in the same order every render** (no hooks inside if/for/switch)
2. **Only call from within Render()** or function component body

### UseState\<T\>(initialValue) → (T Value, Action\<T\> Set)

Primary state hook. Returns current value and a setter that triggers re-render.

```csharp
var (name, setName) = UseState("");
var (count, setCount) = UseState(0);
var (items, setItems) = UseState(new List<string>());
var (isOpen, setIsOpen) = UseState(false);
```

### UseReducer\<T\>(initialValue) → (T Value, Action\<Func\<T, T\>\> Update)

For state updates that depend on previous value (like React's functional setState).

```csharp
var (items, updateItems) = UseReducer(new List<TodoItem>());

// Add item (receives previous list, returns new list):
updateItems(list => [.. list, new TodoItem("New", false)]);

// Remove by index:
updateItems(list => { var copy = new List<TodoItem>(list); copy.RemoveAt(i); return copy; });

// Toggle item:
updateItems(list =>
{
    var copy = new List<TodoItem>(list);
    copy[i] = copy[i] with { Done = !copy[i].Done };
    return copy;
});
```

### UseEffect(action, deps) — side effects

```csharp
// Run once on mount (empty deps):
UseEffect(() => { Console.WriteLine("Mounted!"); });

// Run when dependency changes:
UseEffect(() => { Console.WriteLine($"Count is {count}"); }, count);

// With cleanup (returns a cleanup Action):
UseEffect(() =>
{
    var timer = new Timer(_ => { /* tick */ }, null, 0, 1000);
    return () => timer.Dispose();  // cleanup runs before next effect or on unmount
}, /* deps */);
```

### UseMemo\<T\>(factory, deps) — memoized computation

```csharp
var sorted = UseMemo(() => items.OrderBy(x => x.Name).ToList(), items);
var filtered = UseMemo(() => items.Where(x => x.IsActive).ToArray(), items, filter);
```

### UseCallback(action, deps) — stable callback reference

```csharp
var handleClick = UseCallback(() => setCount(count + 1), count);
```

### UseRef\<T\>(initialValue) — mutable ref persisting across renders

```csharp
var prevCount = UseRef(0);
UseEffect(() => { prevCount.Current = count; }, count);
```

---

## DSL Reference — All Available Elements

### Text

| Factory | Description | Signature |
|---------|-------------|-----------|
| `Text(content)` | Basic text | `string → TextElement` |
| `Heading(content)` | 28px bold | `string → TextElement` |
| `SubHeading(content)` | 20px semi-bold | `string → TextElement` |
| `Caption(content)` | 12px | `string → TextElement` |
| `RichText(text)` | Rich text block | `string → RichTextBlockElement` |

**Implicit conversion:** `string` implicitly converts to `TextElement`, so you can write
`VStack("Hello", "World")` instead of `VStack(Text("Hello"), Text("World"))`.

### Buttons

| Factory | Signature |
|---------|-----------|
| `Button(label, onClick?)` | `(string, Action?) → ButtonElement` |
| `HyperlinkButton(content, navigateUri?, onClick?)` | `(string, Uri?, Action?) → HyperlinkButtonElement` |
| `RepeatButton(label, onClick?)` | `(string, Action?) → RepeatButtonElement` |
| `ToggleButton(label, isChecked?, onToggled?)` | `(string, bool, Action<bool>?) → ToggleButtonElement` |
| `DropDownButton(label, flyout?)` | `(string, Element?) → DropDownButtonElement` |
| `SplitButton(label, onClick?, flyout?)` | `(string, Action?, Element?) → SplitButtonElement` |
| `ToggleSplitButton(label, isChecked?, onChanged?, flyout?)` | `(string, bool, Action<bool>?, Element?) → ToggleSplitButtonElement` |

### Input Controls

| Factory | Signature |
|---------|-----------|
| `TextField(value, onChanged?, placeholder?)` | `(string, Action<string>?, string?) → TextFieldElement` |
| `PasswordBox(password, onPasswordChanged?, placeholderText?)` | `(string, Action<string>?, string?) → PasswordBoxElement` |
| `NumberBox(value, onValueChanged?, header?)` | `(double, Action<double>?, string?) → NumberBoxElement` |
| `AutoSuggestBox(text, onTextChanged?, onQuerySubmitted?)` | `(string, Action<string>?, Action<string>?) → AutoSuggestBoxElement` |
| `CheckBox(isChecked, onChanged?, label?)` | `(bool, Action<bool>?, string?) → CheckBoxElement` |
| `RadioButton(label, isChecked?, onChecked?, groupName?)` | `(string, bool, Action<bool>?, string?) → RadioButtonElement` |
| `RadioButtons(items, selectedIndex?, onSelectionChanged?)` | `(string[], int, Action<int>?) → RadioButtonsElement` |
| `ComboBox(items, selectedIndex?, onSelectionChanged?)` | `(string[], int, Action<int>?) → ComboBoxElement` |
| `Slider(value, min?, max?, onChanged?)` | `(double, double, double, Action<double>?) → SliderElement` |
| `ToggleSwitch(isOn, onChanged?, onContent?, offContent?)` | `(bool, Action<bool>?, string?, string?) → ToggleSwitchElement` |
| `RatingControl(value?, onValueChanged?)` | `(double, Action<double>?) → RatingControlElement` |
| `ColorPicker(color, onColorChanged?)` | `(Windows.UI.Color, Action<Windows.UI.Color>?) → ColorPickerElement` |

### Date & Time

| Factory | Signature |
|---------|-----------|
| `CalendarDatePicker(date?, onDateChanged?)` | `(DateTimeOffset?, Action<DateTimeOffset?>?) → CalendarDatePickerElement` |
| `DatePicker(date, onDateChanged?)` | `(DateTimeOffset, Action<DateTimeOffset>?) → DatePickerElement` |
| `TimePicker(time, onTimeChanged?)` | `(TimeSpan, Action<TimeSpan>?) → TimePickerElement` |

### Progress & Status

| Factory | Signature |
|---------|-----------|
| `Progress(value)` | `double → ProgressElement` (determinate) |
| `ProgressIndeterminate()` | `→ ProgressElement` (spinning) |
| `ProgressRing()` | `→ ProgressRingElement` (indeterminate ring) |
| `ProgressRing(value)` | `double → ProgressRingElement` (determinate ring) |
| `InfoBar(title?, message?)` | `(string?, string?) → InfoBarElement` |
| `InfoBadge()` / `InfoBadge(value)` | `→ InfoBadgeElement` |

### Layout & Containers

| Factory | Signature | Notes |
|---------|-----------|-------|
| `VStack(children...)` | `params Element?[] → StackElement` | Vertical stack, default spacing 8 |
| `VStack(spacing, children...)` | `(double, params Element?[]) → StackElement` | Custom spacing |
| `HStack(children...)` | `params Element?[] → StackElement` | Horizontal stack, default spacing 8 |
| `HStack(spacing, children...)` | `(double, params Element?[]) → StackElement` | Custom spacing |
| `ScrollView(child)` | `Element → ScrollViewElement` | Scrollable container |
| `Border(child)` | `Element → BorderElement` | Card-like container |
| `Expander(header, content, isExpanded?, onExpandedChanged?)` | See type | Collapsible section |
| `SplitView(pane?, content?)` | `(Element?, Element?) → SplitViewElement` | Side pane layout |
| `Viewbox(child)` | `Element → ViewboxElement` | Scales child to fit |
| `Canvas(children...)` | `params CanvasChild[] → CanvasElement` | Absolute positioning |
| `CanvasItem(element, left?, top?)` | `(Element, double, double) → CanvasChild` | Positioned canvas child |

### Grid

```csharp
Grid(
    columns: ["*", "*", "200"],      // Star, Star, 200px fixed
    rows: ["Auto", "*"],              // Auto-height, fill remaining
    Cell(Text("A"), row: 0, column: 0),
    Cell(Text("B"), row: 0, column: 1),
    Cell(Text("C"), row: 0, column: 2),
    Cell(Text("Wide"), row: 1, column: 0, columnSpan: 3)
)
```

Column/row definition syntax:
- `"*"` — 1 star (proportional)
- `"2*"` — 2 stars
- `"Auto"` — size to content
- `"200"` — fixed 200px

### Navigation

| Factory | Signature |
|---------|-----------|
| `NavigationView(menuItems, content?)` | `(NavigationViewItemData[], Element?) → NavigationViewElement` |
| `NavItem(content, icon?, tag?)` | `(string, string?, string?) → NavigationViewItemData` |
| `TabView(tabs...)` | `params TabViewItemData[] → TabViewElement` |
| `Tab(header, content)` | `(string, Element) → TabViewItemData` |
| `BreadcrumbBar(items, onItemClicked?)` | `(BreadcrumbBarItemData[], Action<BreadcrumbBarItemData>?) → BreadcrumbBarElement` |
| `Breadcrumb(label, tag?)` | `(string, object?) → BreadcrumbBarItemData` |
| `Pivot(items...)` | `params PivotItemData[] → PivotElement` |
| `PivotItem(header, content)` | `(string, Element) → PivotItemData` |

### Collections

| Factory | Signature |
|---------|-----------|
| `ListView(items...)` | `params Element[] → ListViewElement` |
| `GridView(items...)` | `params Element[] → GridViewElement` |
| `TreeView(nodes...)` | `params TreeViewNodeData[] → TreeViewElement` |
| `TreeNode(content, children...)` | `(string, params TreeViewNodeData[]) → TreeViewNodeData` |
| `FlipView(items...)` | `params Element[] → FlipViewElement` |

### Dialogs & Overlays

| Factory | Signature |
|---------|-----------|
| `ContentDialog(title, content, primaryButtonText?)` | `(string, Element, string) → ContentDialogElement` |
| `Flyout(target, flyoutContent)` | `(Element, Element) → FlyoutElement` |
| `TeachingTip(title, subtitle?)` | `(string, string?) → TeachingTipElement` |

### Menus

| Factory | Signature |
|---------|-----------|
| `MenuBar(items...)` | `params MenuBarItemData[] → MenuBarElement` |
| `Menu(title, items...)` | `(string, params MenuFlyoutItemBase[]) → MenuBarItemData` |
| `MenuItem(text, onClick?, icon?)` | `(string, Action?, string?) → MenuFlyoutItemData` |
| `MenuSeparator()` | `→ MenuFlyoutSeparatorData` |
| `MenuSubItem(text, items...)` | `(string, params MenuFlyoutItemBase[]) → MenuFlyoutSubItemData` |
| `MenuFlyout(target, items...)` | `(Element, params MenuFlyoutItemBase[]) → MenuFlyoutElement` |
| `CommandBar(primaryCommands?, secondaryCommands?)` | `(AppBarButtonData[]?, AppBarButtonData[]?) → CommandBarElement` |
| `AppBarButton(label, onClick?, icon?)` | `(string, Action?, string?) → AppBarButtonData` |

### Media

| Factory | Signature |
|---------|-----------|
| `Image(source)` | `string → ImageElement` |
| `PersonPicture()` | `→ PersonPictureElement` |
| `WebView2(source?)` | `Uri? → WebView2Element` |

### Conditional Helpers

| Factory | Purpose | Signature |
|---------|---------|-----------|
| `When(condition, then)` | Render only if true | `(bool, Func<Element>) → Element` |
| `If(condition, then, otherwise?)` | If/else | `(bool, Func<Element>, Func<Element>?) → Element` |
| `ForEach(items, render)` | Map list to elements | `(IEnumerable<T>, Func<T, Element>) → Element` |
| `ForEach(items, render)` | Map with index | `(IEnumerable<T>, Func<T, int, Element>) → Element` |
| `Empty()` | Renders nothing | `→ Element` |

---

## Fluent Modifiers (Extension Methods)

Modifiers wrap any element and can be chained. They return `Element` (not the
concrete type), so call type-specific sugar methods BEFORE generic modifiers.

### Layout Modifiers (work on ANY Element)

```csharp
.Margin(16)                          // uniform
.Margin(16, 8)                       // horizontal, vertical
.Margin(10, 20, 10, 20)             // left, top, right, bottom
.Padding(12)                         // uniform (NOTE: only on BorderElement)
.Padding(8, 4)                       // horizontal, vertical (on BorderElement)
.Width(300)
.Height(200)
.Size(300, 200)                      // width + height
.MinWidth(100)
.MinHeight(50)
.MaxWidth(600)
.MaxHeight(400)
.HAlign(HorizontalAlignment.Center)  // Left, Center, Right, Stretch
.VAlign(VerticalAlignment.Top)       // Top, Center, Bottom, Stretch
.Center()                            // Centers both H and V
.Visible(condition)                  // Show/hide (Collapsed when false)
.Opacity(0.6)                        // 0.0 to 1.0
.ToolTip("Helpful text")
```

### Type-Specific Sugar (call BEFORE generic modifiers)

```csharp
// TextElement:
Text("Hello").Bold()                  // FontWeights.Bold
Text("Hello").SemiBold()              // FontWeights.SemiBold
Text("Hello").FontSize(24)            // Custom font size

// ButtonElement:
Button("Click").Disabled()            // IsEnabled = false
Button("Click").Disabled(condition)   // Conditionally disabled

// BorderElement:
Border(child).CornerRadius(8)         // Rounded corners
Border(child).Background("#f5f5f5")   // Background color
Border(child).Background("lightgray") // Named colors work too
Border(child).WithBorder("#ccc", 1)   // Border brush + thickness
Border(child).Padding(new Thickness(16))

// StackElement:
VStack(...).Spacing(16)

// ComboBoxElement:
ComboBox(items).Placeholder("Select...").Editable()

// NumberBoxElement:
NumberBox(val).Range(0, 100).SpinButtons()

// SliderElement:
Slider(val, 0, 100).StepFrequency(5)

// RatingControlElement:
RatingControl(3).MaxRating(10).ReadOnly()

// InfoBarElement:
InfoBar("Title", "Message").Severity(InfoBarSeverity.Warning).Closable(false)

// NavigationViewElement:
NavigationView(items, content).PaneDisplayMode(NavigationViewPaneDisplayMode.Left).PaneTitle("Nav")

// ExpanderElement:
Expander("Header", content).Direction(ExpandDirection.Up)

// PersonPictureElement:
PersonPicture().DisplayName("John Doe").Initials("JD")

// ListViewElement / GridViewElement:
ListView(items).SelectionMode(ListViewSelectionMode.Multiple)

// TabViewElement:
TabView(tabs).ShowAddButton()

// Element Key (for stable identity in lists):
element.WithKey("unique-id")
```

### Set() — Escape Hatch to Native WinUI Properties

When Patch doesn't expose a property, use `.Set()` for direct WinUI control access:

```csharp
Button("Go", onClick)
    .Set(b => b.FlowDirection = Microsoft.UI.Xaml.FlowDirection.RightToLeft)

Text("Hello")
    .Set(tb => tb.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap)
    .Set(tb => tb.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 100, 100, 100)))

TextField(value, setValue)
    .Set(tb => tb.AcceptsReturn = true)
    .Set(tb => tb.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap)
```

The lambda parameter is the **real WinUI control type** — you get full IntelliSense.
Every element type has a corresponding `.Set()` overload.

---

## Available Colors (for Background, BorderBrush)

Named colors: `"red"`, `"green"`, `"blue"`, `"white"`, `"black"`, `"gray"` / `"grey"`,
`"lightgray"` / `"lightgrey"`, `"transparent"`

Hex colors: `"#RRGGBB"` or `"#AARRGGBB"` (e.g., `"#f5f5f5"`, `"#80000000"`)

---

## Available Enums (all from WinUI — no Patch shadow types)

Patch uses WinUI types directly. Add `using Microsoft.UI.Xaml;` and
`using Microsoft.UI.Xaml.Controls;` to access them.

```csharp
// Microsoft.UI.Xaml namespace:
Orientation              { Vertical, Horizontal }
HorizontalAlignment      { Left, Center, Right, Stretch }
VerticalAlignment        { Top, Center, Bottom, Stretch }
Thickness                // struct: new Thickness(uniform) or new Thickness(l, t, r, b)

// Microsoft.UI.Xaml.Controls namespace:
InfoBarSeverity          { Informational, Success, Warning, Error }
ExpandDirection          { Down, Up }
ListViewSelectionMode    { None, Single, Multiple, Extended }
NavigationViewPaneDisplayMode  { Auto, Left, LeftCompact, LeftMinimal, Top }
SplitViewDisplayMode     { Overlay, Inline, CompactOverlay, CompactInline }
NumberBoxSpinButtonPlacementMode   { Hidden, Compact, Inline }
ScrollBarVisibility      { Auto, Visible, Hidden, Disabled }
ContentDialogButton      { None, Primary, Secondary, Close }
ContentDialogResult      { None, Primary, Secondary }
TreeViewSelectionMode    { None, Single, Multiple }
CommandBarDefaultLabelPosition  { Bottom, Right, Collapsed }

// Microsoft.UI.Xaml.Controls.Primitives namespace:
FlyoutPlacementMode      { Top, Bottom, Left, Right, Full, Auto }

// Windows.UI.Text / Microsoft.UI.Text:
FontWeight               // struct; use FontWeights helpers:
FontWeights.Bold
FontWeights.SemiBold
FontWeights.Normal
```

---

## Conditional Rendering Patterns

Patch uses **plain C# control flow** — no special template syntax.

### Pattern 1: Ternary (most common)
```csharp
isLoggedIn ? Text($"Welcome, {name}!") : Button("Log in", onLogin)
```

### Pattern 2: Null children (filtered automatically)
```csharp
VStack(
    Text("Always visible"),
    showExtra ? Text("Sometimes visible") : null,  // null → skipped
    showWarning ? InfoBar("Warning!", "Be careful") : null
)
```

### Pattern 3: When() helper
```csharp
When(items.Any(), () => Text($"{items.Count} items found"))
```

### Pattern 4: If/else helper
```csharp
If(isError,
    () => InfoBar("Error", errorMessage).Severity(InfoBarSeverity.Error),
    () => Text("All systems operational"))
```

### Pattern 5: Switch expression (multi-branch)
```csharp
status switch
{
    Status.Loading => ProgressIndeterminate(),
    Status.Error => Text("Something went wrong"),
    Status.Success => Component<SuccessView>(),
    _ => Empty()
}
```

### Pattern 6: ForEach (list rendering)
```csharp
ForEach(items, item => Text(item.Name))
ForEach(items, (item, i) => Text($"{i + 1}. {item.Name}"))

// Or via LINQ (equivalent, more flexible):
VStack(items.Select(item => Text(item.Name)).ToArray())
```

---

## Common Patterns & Recipes

### Card Pattern
```csharp
static Element Card(string title, Element content) =>
    Border(
        VStack(8,
            Text(title).Bold().FontSize(16),
            content
        )
    ).CornerRadius(8).Background("#f5f5f5").Padding(new Thickness(16));
```

### Tab Navigation Pattern
```csharp
class App : Component
{
    public override Element Render()
    {
        var (tab, setTab) = UseState("Home");

        return VStack(12,
            HStack(8,
                TabBtn("Home", tab, setTab),
                TabBtn("Settings", tab, setTab),
                TabBtn("About", tab, setTab)
            ),
            tab switch
            {
                "Home" => Component<HomePage>(),
                "Settings" => Component<SettingsPage>(),
                "About" => Text("About this app"),
                _ => Empty()
            }
        );
    }

    static Element TabBtn(string label, string current, Action<string> set) =>
        Button(label, () => set(label)).Disabled(label == current);
}
```

### Form Pattern
```csharp
class MyForm : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("");
        var (email, setEmail) = UseState("");
        var (agreed, setAgreed) = UseState(false);
        var (submitted, setSubmitted) = UseState(false);

        var isValid = !string.IsNullOrWhiteSpace(name) && agreed;

        if (submitted)
            return VStack(Heading("Submitted!"), Text($"Thanks, {name}!"),
                Button("Back", () => setSubmitted(false)));

        return VStack(16,
            Heading("Sign Up"),
            VStack(4, Text("Name"), TextField(name, setName, placeholder: "Your name").Width(300)),
            VStack(4, Text("Email"), TextField(email, setEmail, placeholder: "you@example.com").Width(300)),
            CheckBox(agreed, setAgreed, label: "I agree to the terms"),
            When(!isValid, () => Text("Complete all fields to continue").Opacity(0.6)),
            Button("Submit", () => setSubmitted(true)).Disabled(!isValid)
        );
    }
}
```

### List with Add/Remove Pattern
```csharp
class TodoList : Component
{
    record TodoItem(string Text, bool Done);

    public override Element Render()
    {
        var (items, updateItems) = UseReducer(new List<TodoItem>());
        var (newText, setNewText) = UseState("");

        return VStack(12,
            Heading("Todos"),
            HStack(8,
                TextField(newText, setNewText, placeholder: "New item...").Width(300),
                Button("Add", () =>
                {
                    if (!string.IsNullOrWhiteSpace(newText))
                    {
                        updateItems(list => [.. list, new TodoItem(newText.Trim(), false)]);
                        setNewText("");
                    }
                }).Disabled(string.IsNullOrWhiteSpace(newText))
            ),
            VStack(4, items.Select((item, i) =>
                HStack(8,
                    CheckBox(item.Done, done =>
                        updateItems(list =>
                        {
                            var copy = new List<TodoItem>(list);
                            copy[i] = item with { Done = done };
                            return copy;
                        }),
                        label: item.Text),
                    Button("×", () =>
                        updateItems(list =>
                        {
                            var copy = new List<TodoItem>(list);
                            copy.RemoveAt(i);
                            return copy;
                        }))
                ).WithKey($"todo-{i}")
            ).ToArray()),
            When(items.Count > 0 && items.All(i => i.Done),
                () => Text("All done! 🎉").Bold())
        );
    }
}
```

### NavigationView App Shell Pattern
```csharp
class AppShell : Component
{
    public override Element Render()
    {
        var (selectedTag, setSelectedTag) = UseState("home");

        return NavigationView(
            new[]
            {
                NavItem("Home", icon: "Home", tag: "home"),
                NavItem("Settings", icon: "Setting", tag: "settings"),
            },
            content: selectedTag switch
            {
                "home" => Component<HomePage>(),
                "settings" => Component<SettingsPage>(),
                _ => Text("Page not found")
            }
        ) with
        {
            SelectedTag = selectedTag,
            OnSelectionChanged = tag => { if (tag != null) setSelectedTag(tag); }
        };
    }
}
```

### TabView Pattern
```csharp
TabView(
    Tab("Dashboard", Component<DashboardPage>()),
    Tab("Reports", Component<ReportsPage>()),
    Tab("Settings", VStack(Text("Settings content")))
) with { SelectedIndex = 0, OnSelectionChanged = i => setTab(i) }
```

### MenuBar Pattern
```csharp
MenuBar(
    Menu("File",
        MenuItem("New", () => handleNew(), icon: "Add"),
        MenuItem("Open", () => handleOpen(), icon: "OpenFile"),
        MenuSeparator(),
        MenuItem("Exit", () => handleExit())),
    Menu("Edit",
        MenuItem("Cut", () => handleCut()),
        MenuItem("Copy", () => handleCopy()),
        MenuItem("Paste", () => handlePaste()),
        MenuSeparator(),
        MenuSubItem("Advanced",
            MenuItem("Find & Replace"),
            MenuItem("Go to Line")))
)
```

### Content Dialog Pattern
```csharp
var (showDialog, setShowDialog) = UseState(false);

VStack(
    Button("Delete Item", () => setShowDialog(true)),
    ContentDialog("Confirm Delete",
        Text("Are you sure you want to delete this item?"),
        primaryButtonText: "Delete"
    ) with
    {
        IsOpen = showDialog,
        SecondaryButtonText = "Cancel",
        OnClosed = result =>
        {
            setShowDialog(false);
            if (result == ContentDialogResult.Primary) { /* delete */ }
        }
    }
)
```

### Scrollable List Pattern
```csharp
ScrollView(
    VStack(4,
        items.Select(item =>
            Border(
                HStack(8, Text(item.Name).SemiBold(), Text(item.Description).Opacity(0.7))
            ).CornerRadius(4).Background("#f0f0f0").Padding(new Thickness(12, 8, 12, 8))
        ).ToArray()
    )
)
```

### Grid Layout Pattern
```csharp
Grid(
    columns: ["200", "*"],
    rows: ["Auto", "*"],
    Cell(Text("Sidebar Header").Bold(), row: 0, column: 0),
    Cell(Text("Main Header").Bold(), row: 0, column: 1),
    Cell(Component<SideNav>(), row: 1, column: 0),
    Cell(Component<MainContent>(), row: 1, column: 1)
)
```

---

## Critical Rules & Gotchas

### 1. Hook Order Must Be Constant
```csharp
// ❌ WRONG — hook inside conditional
if (showAdvanced)
{
    var (setting, setSetting) = UseState("");  // BUG: hook count changes
}

// ✅ CORRECT — always call hooks, conditionally USE the result
var (setting, setSetting) = UseState("");
// then conditionally render:
showAdvanced ? TextField(setting, setSetting) : null
```

### 2. Type-Specific Extensions Before Generic Modifiers
```csharp
// ❌ WRONG — .Margin() returns Element, .Bold() needs TextElement
Text("Hello").Margin(10).Bold()  // compile error

// ✅ CORRECT — type-specific first
Text("Hello").Bold().Margin(10)
```

### 3. UseReducer for List Mutations
```csharp
// ❌ WRONG — mutating the same list reference won't trigger re-render
var (items, setItems) = UseState(new List<string>());
items.Add("new");       // mutates in place — no re-render!
setItems(items);        // same reference — equality check skips update!

// ✅ CORRECT — UseReducer with new list
var (items, updateItems) = UseReducer(new List<string>());
updateItems(prev => [.. prev, "new"]);  // creates new list → re-render
```

### 4. Null Children Are Safe
```csharp
// VStack, HStack automatically filter null children
VStack(
    Text("Always"),
    condition ? Text("Maybe") : null,  // safely ignored when false
    anotherCondition ? Component<Widget>() : null
)
```

### 5. Records with `with` for Init-Only Properties
```csharp
// Many element records have init-only properties not exposed as constructor params.
// Use `with` expressions or fluent extensions:
NavigationView(menuItems, content) with { SelectedTag = "home", IsPaneOpen = true }
ComboBox(items) with { Header = "Choose one", PlaceholderText = "Select..." }
InfoBar("Title", "Msg") with { Severity = InfoBarSeverity.Warning, IsClosable = false }
```

### 6. No XAML, No App.xaml, No Resources
Patch apps have zero XAML files. The `DuctApplication` class loads `XamlControlsResources`
programmatically. WinUI theme resources (Fluent design) are automatically available.

### 7. Icons Use Symbol Enum Names
For NavigationView items, menu items, and command bar buttons, the `icon` parameter
takes a string that maps to the `Symbol` enum (e.g., `"Home"`, `"Setting"`, `"Add"`,
`"Delete"`, `"Save"`, `"Edit"`, `"Find"`, `"Mail"`, `"Refresh"`, `"Download"`,
`"Upload"`, `"Favorite"`, `"Camera"`, `"People"`, `"Phone"`, `"Pin"`,
`"OpenFile"`, `"Placeholder"`). Case-insensitive.

### 8. Thickness for Padding/Margin
```csharp
new Thickness(16)                    // uniform 16 on all sides
Thick(16, 8)                         // 16 horizontal, 8 vertical (Patch helper)
new Thickness(10, 20, 10, 20)       // left, top, right, bottom
```

---

## Complete Starter App Template

```csharp
// App.cs — A complete Duct application in a single file
using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using static Duct.UI;

DuctApp.Run<App>("My Duct App", width: 1200, height: 800);

class App : Component
{
    public override Element Render()
    {
        var (page, setPage) = UseState("Home");

        return VStack(
            // Header bar
            Border(
                HStack(12,
                    Heading("My App").VAlign(VerticalAlignment.Center),
                    HStack(8,
                        NavBtn("Home", page, setPage),
                        NavBtn("Dashboard", page, setPage),
                        NavBtn("Settings", page, setPage)
                    ).VAlign(VerticalAlignment.Center)
                )
            ).Background("#f0f0f0").Padding(new Thickness(24, 12, 24, 12)),

            // Content area
            Border(
                page switch
                {
                    "Home" => Component<HomePage>(),
                    "Dashboard" => Component<DashboardPage>(),
                    "Settings" => Component<SettingsPage>(),
                    _ => Text("Not found")
                }
            ).Padding(new Thickness(24)).Margin(0, 0, 0, 0)
        );
    }

    static Element NavBtn(string label, string current, Action<string> set) =>
        Button(label, () => set(label)).Disabled(label == current);
}

class HomePage : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("");

        return VStack(16,
            Heading("Welcome Home"),
            HStack(8,
                TextField(name, setName, placeholder: "Enter your name").Width(300),
                When(!string.IsNullOrEmpty(name), () => Text($"Hello, {name}!").SemiBold())
            ),
            Text("This is a Duct application — no XAML, just C#.").Opacity(0.6)
        );
    }
}

class DashboardPage : Component
{
    public override Element Render()
    {
        var (progress, setProgress) = UseState(42.0);

        return VStack(16,
            Heading("Dashboard"),
            HStack(8,
                Text("Progress:"),
                Slider(progress, 0, 100, setProgress).Width(300),
                Text($"{progress:F0}%")
            ),
            Progress(progress),
            When(progress >= 100, () => Text("Complete! 🎉").Bold().FontSize(20))
        );
    }
}

class SettingsPage : Component
{
    public override Element Render()
    {
        var (darkMode, setDarkMode) = UseState(false);
        var (notifications, setNotifications) = UseState(true);
        var (fontSize, setFontSize) = UseState(14.0);

        return VStack(16,
            Heading("Settings"),
            ToggleSwitch(darkMode, setDarkMode, onContent: "Dark", offContent: "Light")
                with { Header = "Theme" },
            ToggleSwitch(notifications, setNotifications, onContent: "On", offContent: "Off")
                with { Header = "Notifications" },
            VStack(4,
                Text("Font Size"),
                HStack(8,
                    Slider(fontSize, 10, 30, setFontSize).Width(200),
                    Text($"{fontSize:F0}px")
                )
            ),
            Button("Save Settings", () => { /* save logic */ })
        );
    }
}
```

---

## Comparison to React (for AI context)

| React | Patch |
|-------|-----|
| `function App() {}` | `class App : Component { Render() {} }` |
| `useState(0)` | `UseState(0)` |
| `useReducer` | `UseReducer(initial)` — updater receives `Func<T,T>` |
| `useEffect(() => {}, [dep])` | `UseEffect(() => {}, dep)` |
| `useMemo(() => val, [dep])` | `UseMemo(() => val, dep)` |
| `useRef(null)` | `UseRef<T>(default)` |
| `<div>` | `VStack()` / `HStack()` / `Border()` |
| `<span>text</span>` | `Text("text")` |
| `<button onClick={fn}>` | `Button("label", fn)` |
| `<input value={v} onChange={fn}>` | `TextField(v, fn)` |
| `{condition && <X/>}` | `condition ? X() : null` |
| `{items.map(i => <X/>)}` | `items.Select(i => X()).ToArray()` or `ForEach(items, i => X())` |
| `<Component />` | `Component<MyComponent>()` |
| `props` | N/A — components use hooks for all state |
| `className="..."` | `.Set(el => ...)` for native property access |
| `style={{margin: 10}}` | `.Margin(10)` |
| JSX | C# method calls + `using static Duct.UI` |
