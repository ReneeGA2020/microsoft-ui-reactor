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
    <TargetFramework>net10.0-windows10.0.22621.0</TargetFramework>
    <Platforms>x64;ARM64</Platforms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="2.0.0-experimental6" />
    <ProjectReference Include="..\Duct\Duct.csproj" />
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
using Duct.Flex;                   // FlexDirection, FlexJustify, etc. (if using Flex layout)
using Microsoft.UI.Xaml;           // Thickness, HorizontalAlignment, VerticalAlignment
using Microsoft.UI.Xaml.Controls;  // Orientation, InfoBarSeverity, etc. (if needed)
using static Duct.UI;  // Brings Text(), Button(), VStack() etc. into scope
```

The `using static Duct.UI;` import is essential — it makes all DSL factory methods available
as bare function calls (like JSX tags). The WinUI usings give you access to enums and structs
like `Thickness`, `HorizontalAlignment`, and `Orientation` — Patch uses WinUI types directly.

### App Entry Point

```csharp
// Component root with all options
DuctApp.Run<TRoot>(title: "Duct App", width: 1024, height: 768, fullScreen: false, configure: host => { });

// Inline render function root
DuctApp.Run("Window Title", ctx => { ... }, width: 1024, height: 768, fullScreen: false);
```

The `configure` parameter gives access to the host, which exposes `host.Window` for
Mica backdrop, titlebar customization, etc.

Example — component root (most common):
```csharp
DuctApp.Run<MyRootComponent>("Window Title", width: 1024, height: 768);
```

Example — inline root (no component class needed):
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

### Component Props (typed data from parent)

```csharp
// Define a component that receives typed props:
record UserCardProps(string Name, string Role);

class UserCard : Component<UserCardProps>
{
    public override Element Render()
    {
        return VStack(Text(Props.Name).Bold(), Text(Props.Role).Opacity(0.6));
    }
}

// Pass props from parent:
Component<UserCard, UserCardProps>(new UserCardProps("Alice", "Admin"))
```

Use **record** props for free structural equality — the framework auto-skips re-renders
when props haven't changed (see Memoization below). Class props without an `Equals`
override use reference equality and re-render every time.

### Memoized Function Components

```csharp
// Memo() skips re-renders when dependencies haven't changed.
// Empty deps = render once + own state changes only:
Memo(ctx => Text("Stable content"))

// With deps = re-render when any dep changes:
Memo(ctx => Text($"Hello, {name}"), name)
```

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

### UseObservable\<T\>(source) → T — tracks INotifyPropertyChanged, re-renders on change

### UseObservableProperty\<T, TProp\>(source, selector, propertyName) → TProp — tracks a single property

### UseCollection\<T\>(collection) → IReadOnlyList\<T\> — tracks ObservableCollection changes

### UseContext\<T\>(context) → T — read tree-scoped ambient state

Reads a value provided by an ancestor via `.Provide()`. Returns the context's
default if no provider exists. Re-renders automatically when the provided value changes.

```csharp
// Define a context (static field, typically on a static class):
public static readonly DuctContext<string> ThemeCtx = new("light");

// Provide a value to a subtree (modifier on any element):
VStack(
    Component<Header>(),
    Component<Content>()
).Provide(ThemeCtx, "dark")

// Consume in any descendant component:
class Header : Component
{
    public override Element Render()
    {
        var theme = UseContext(ThemeCtx);  // "dark" from ancestor
        return Text($"Theme: {theme}");
    }
}
```

Nesting: inner `.Provide()` shadows outer for the same context. Sibling subtrees
are independent — context from one subtree doesn't leak to another.

### UsePersisted\<T\>(key, initialValue) → (T Value, Action\<T\> Set) — state that survives unmount

Like `UseState`, but the value is saved to an in-memory cache on unmount and restored
on remount. Use for scroll positions, form drafts, or tab state.

```csharp
var (scrollPos, setScrollPos) = UsePersisted("tab1-scroll", 0);
```

Different components sharing the same key share the cached state. `UseState` values
are lost on unmount; `UsePersisted` values survive.

### UseWindowSize(window) → (double Width, double Height) — reactive window dimensions

### UseBreakpoint(window, minWidth) → bool — responsive breakpoint helper

---

## Memoization (Automatic Re-render Skipping)

Components skip re-rendering by default when their parent re-renders but nothing relevant changed:

| Component type | Default memo behavior |
|---|---|
| **Propless `Component`** | Skips parent-triggered re-renders (only re-renders from own state or context changes) |
| **`Component<TProps>`** | Skips when `Equals(oldProps, newProps)` — record props get structural comparison for free |
| **`Memo(ctx => ..., deps)`** | Skips when dependencies array hasn't changed. Null deps = render once only |

Self-triggered re-renders (from `UseState` / `UseReducer` setters) always bypass memo.

Override `ShouldUpdate` for custom comparison:
```csharp
class MyComponent : Component<MyProps>
{
    // Only re-render when Name changes, ignore Age
    protected internal override bool ShouldUpdate(MyProps? old, MyProps? next)
        => old?.Name != next?.Name;
}
```

**Slots and memo:** Static slot content (e.g., `Text("label")`) allows memo skip because
record equality holds. Slots with new lambda delegates (e.g., `Button("Go", () => ...)`)
defeat memo — use `UseCallback` to stabilize delegate references when memo matters.

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
| `RichEditBox(text?, onTextChanged?)` | `(string, Action<string>?) → RichEditBoxElement` |
| `ThreeStateCheckBox(checkedState?, onChanged?, label?)` | `(bool?, Action<bool?>?, string?) → CheckBoxElement` |

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
| `Flex(children...)` | `params Element?[] → FlexElement` | CSS Flexbox (row default) |
| `Flex(direction, children...)` | `(FlexDirection, params Element?[]) → FlexElement` | Flexbox with direction |
| `FlexRow(children...)` | `params Element?[] → FlexElement` | Flex row shortcut |
| `FlexColumn(children...)` | `params Element?[] → FlexElement` | Flex column shortcut |
| `WrapGrid(children...)` | `params Element?[] → WrapGridElement` | Wrapping grid |
| `RelativePanel(children...)` | `params Element?[] → RelativePanelElement` | Relative positioning |

### Grid

```csharp
Grid(
    columns: ["*", "*", "200"],      // Star, Star, 200px fixed
    rows: ["Auto", "*"],              // Auto-height, fill remaining
    Text("A").Grid(row: 0, column: 0),
    Text("B").Grid(row: 0, column: 1),
    Text("C").Grid(row: 0, column: 2),
    Text("Wide").Grid(row: 1, column: 0, columnSpan: 3)
)
```

Column/row definition syntax:
- `"*"` — 1 star (proportional)
- `"2*"` — 2 stars
- `"Auto"` — size to content
- `"200"` — fixed 200px

### Flex Layout (CSS Flexbox)

FlexPanel implements **CSS Flexbox** using a C# port of Meta's Yoga layout engine. If you know
CSS Flexbox, you already know how this works — the property names map directly:

| CSS Property | Duct Container Property | Duct Enum |
|---|---|---|
| `flex-direction` | `Direction` | `FlexDirection` { Row, RowReverse, Column, ColumnReverse } |
| `justify-content` | `JustifyContent` | `FlexJustify` { FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround, SpaceEvenly } |
| `align-items` | `AlignItems` | `FlexAlign` { FlexStart, Center, FlexEnd, Stretch, Baseline } |
| `align-content` | `AlignContent` | `FlexAlign` (same as above + SpaceBetween, SpaceAround, SpaceEvenly) |
| `flex-wrap` | `Wrap` | `FlexWrap` { NoWrap, Wrap, WrapReverse } |
| `column-gap` | `ColumnGap` | `double` |
| `row-gap` | `RowGap` | `double` |

Child properties (set via `.Flex()` modifier):

| CSS Property | `.Flex()` Parameter | Default |
|---|---|---|
| `flex-grow` | `grow` | 0 |
| `flex-shrink` | `shrink` | 1 |
| `flex-basis` | `basis` | null (auto) |
| `align-self` | `alignSelf` | null (inherit) |
| `position` | `position` | Relative |
| `left/top/right/bottom` | `left/top/right/bottom` | null |

```csharp
// Basic row (like CSS: display:flex)
Flex(
    Text("A"),
    Text("B"),
    Text("C")
)

// Column with spacing (like flex-direction:column; row-gap:12px)
FlexColumn(
    Text("First"),
    Text("Second"),
    Text("Third")
) with { RowGap = 12 }

// Grow children to fill space (like flex-grow)
FlexRow(
    Text("Sidebar").Flex(grow: 0, basis: 200),       // fixed 200px
    Text("Main content").Flex(grow: 1),                // fills remaining
    Text("Aside").Flex(grow: 0, basis: 150)            // fixed 150px
)

// Justify and align (like justify-content + align-items)
Flex(
    Text("Centered")
) with { JustifyContent = FlexJustify.Center, AlignItems = FlexAlign.Center }

// Wrapping (like flex-wrap:wrap with gap)
Flex(
    tags.Select(t => Text(t).Margin(4)).ToArray()
) with { Wrap = FlexWrap.Wrap, ColumnGap = 8, RowGap = 8 }

// Absolute positioning within flex (like position:absolute)
Flex(
    Text("Normal flow"),
    Text("Badge").Flex(position: FlexPositionType.Absolute, top: 0, right: 0)
)

// Nested flex for complex layouts (holy grail)
FlexColumn(
    Flex(Text("Header")).Flex(shrink: 0),
    FlexRow(
        Text("Nav").Flex(basis: 200, shrink: 0),
        Text("Content").Flex(grow: 1),
        Text("Aside").Flex(basis: 150, shrink: 0)
    ).Flex(grow: 1),
    Flex(Text("Footer")).Flex(shrink: 0)
)
```

**When to use Flex vs other layout containers:**
- Use **VStack/HStack** for simple stacking with uniform spacing — simpler API, good default
- Use **Grid** for 2D row/column grid layouts with defined tracks
- Use **Flex** when you need CSS Flexbox behavior: grow/shrink proportions, wrapping,
  space distribution (justify/align), or absolute positioning within a flex container

**Required imports for Flex:**
```csharp
using Duct.Flex;  // FlexDirection, FlexJustify, FlexAlign, FlexWrap, FlexPositionType
```

### Navigation

| Factory | Signature |
|---------|-----------|
| `NavigationView(menuItems, content?)` | `(NavigationViewItemData[], Element?) → NavigationViewElement` |
| `NavItem(content, icon?, tag?)` | `(string, string?, string?) → NavigationViewItemData` |
| `TitleBar(title)` | `(string) → TitleBarElement` |
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
| `ListBox(items, selectedIndex?, onSelectionChanged?)` | `(string[], int, Action<int>?) → ListBoxElement` |

### Templated Collections (data-driven, virtualized)

| Factory | Signature |
|---------|-----------|
| `ListView<T>(items, keySelector, viewBuilder)` | `(IReadOnlyList<T>, Func<T, string>, Func<T, int, Element>) → TemplatedListViewElement<T>` |
| `GridView<T>(items, keySelector, viewBuilder)` | `(IReadOnlyList<T>, Func<T, string>, Func<T, int, Element>) → TemplatedGridViewElement<T>` |
| `FlipView<T>(items, keySelector, viewBuilder)` | `(IReadOnlyList<T>, Func<T, string>, Func<T, int, Element>) → TemplatedFlipViewElement<T>` |
| `LazyVStack<T>(items, keySelector, viewBuilder)` | `(IReadOnlyList<T>, Func<T, string>, Func<T, int, Element>) → LazyVStackElement<T>` — virtualized via ItemsRepeater |
| `LazyHStack<T>(items, keySelector, viewBuilder)` | `(IReadOnlyList<T>, Func<T, string>, Func<T, int, Element>) → LazyHStackElement<T>` — virtualized via ItemsRepeater |
| `ItemsView<T>(items, keySelector, viewBuilder)` | `(IReadOnlyList<T>, Func<T, string>, Func<T, int, Element>) → ItemsViewElement<T>` |

### Dialogs & Overlays

| Factory | Signature |
|---------|-----------|
| `ContentDialog(title, content, primaryButtonText?)` | `(string, Element, string) → ContentDialogElement` |
| `Flyout(target, flyoutContent)` | `(Element, Element) → FlyoutElement` |
| `TeachingTip(title, subtitle?)` | `(string, string?) → TeachingTipElement` |
| `ContentFlyout(content, placement?)` | `(Element, FlyoutPlacementMode) → ContentFlyoutElement` |
| `Popup(child, isOpen?, onClosed?)` | `(Element, bool, Action?) → PopupElement` |
| `RefreshContainer(content, onRefreshRequested?)` | `(Element, Action?) → RefreshContainerElement` |
| `CommandBarFlyout(target, primaryCommands?, secondaryCommands?)` | `(Element, AppBarItemBase[]?, AppBarItemBase[]?) → CommandBarFlyoutElement` |

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
| `AppBarToggleButton(label, isChecked?, onToggled?, icon?)` | `(string, bool, Action<bool>?, string?) → AppBarToggleButtonData` |
| `AppBarSeparator()` | `→ AppBarSeparatorData` |
| `ToggleMenuItem(text, isChecked?, onToggled?, icon?)` | `(string, bool, Action<bool>?, string?) → ToggleMenuFlyoutItemData` |
| `RadioMenuItem(text, groupName, isChecked?, onClick?, icon?)` | `(string, string, bool, Action?, string?) → RadioMenuFlyoutItemData` |
| `MenuItems(items...)` | `params MenuFlyoutItemBase[] → MenuFlyoutContentElement` |

### Media

| Factory | Signature |
|---------|-----------|
| `Image(source)` | `string → ImageElement` |
| `PersonPicture()` | `→ PersonPictureElement` |
| `WebView2(source?)` | `Uri? → WebView2Element` |
| `MediaPlayerElement(source?)` | `string? → MediaPlayerElementElement` |
| `AnimatedVisualPlayer()` | `→ AnimatedVisualPlayerElement` |
| `MonacoEditor(text?, onTextChanged?, language?, theme?)` | `(string, Action<string>?, string, string) → MonacoEditorElement` |

### Additional Controls

| Factory | Signature |
|---------|-----------|
| `SelectorBar(items, selectedIndex?, onSelectionChanged?)` | `(SelectorBarItemData[], int, Action<int>?) → SelectorBarElement` |
| `SelectorBarItem(text, icon?)` | `(string, string?) → SelectorBarItemData` |
| `PipsPager(numberOfPages, selectedPageIndex?, onSelectedIndexChanged?)` | `(int, int, Action<int>?) → PipsPagerElement` |
| `AnnotatedScrollBar()` | `→ AnnotatedScrollBarElement` |
| `CalendarView()` | `→ CalendarViewElement` |
| `SemanticZoom(zoomedInView, zoomedOutView)` | `(Element, Element) → SemanticZoomElement` |
| `SwipeControl(content, leftItems?, rightItems?)` | `(Element, SwipeItemData[]?, SwipeItemData[]?) → SwipeControlElement` |
| `AnimatedIcon(source?, fallbackIconSource?)` | `(object?, IconSource?) → AnimatedIconElement` |
| `ParallaxView(child, verticalShift?, horizontalShift?)` | `(Element, double, double) → ParallaxViewElement` |
| `MapControl(mapServiceToken?, zoomLevel?)` | `(string?, double) → MapControlElement` |
| `Frame(sourcePageType?, navigationParameter?)` | `(Type?, object?) → FrameElement` |

### Shapes

| Factory | Signature |
|---------|-----------|
| `Rectangle()` | `→ RectangleElement` |
| `Ellipse()` | `→ EllipseElement` |

### Rich Text

| Factory | Signature |
|---------|-----------|
| `RichText(paragraphs)` | `RichTextParagraph[] → RichTextBlockElement` |
| `Paragraph(inlines...)` | `params RichTextInline[] → RichTextParagraph` |
| `Run(text)` | `string → RichTextRun` |
| `Hyperlink(text, navigateUri)` | `(string, Uri) → RichTextHyperlink` |
| `Markdown(markdown)` | `string → Element` |
| `Markdown(markdown, options)` | `(string, MarkdownOptions) → Element` |

### Icons

| Factory | Signature |
|---------|-----------|
| `SymbolIcon(symbol)` | `string → SymbolIconData` |
| `FontIcon(glyph, fontFamily?, fontSize?)` | `(string, string?, double?) → FontIconData` |
| `BitmapIcon(source, showAsMonochrome?)` | `(Uri, bool) → BitmapIconData` |
| `PathIcon(data)` | `string → PathIconData` |
| `ImageIcon(source)` | `Uri → ImageIconData` |

### Utilities

| Factory | Signature |
|---------|-----------|
| `Thick(uniform)` | `double → Thickness` |
| `Thick(horizontal, vertical)` | `(double, double) → Thickness` |
| `Thick(left, top, right, bottom)` | `(double, double, double, double) → Thickness` |
| `Accelerator(key, modifiers?)` | `(VirtualKey, VirtualKeyModifiers) → KeyboardAcceleratorData` |
| `AcrylicBrush(tintColor, tintOpacity?, fallbackColor?, tintLuminosityOpacity?)` | `→ AcrylicBrush` |

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
.Padding(12)                         // uniform
.Padding(8, 4)                       // horizontal, vertical
.Padding(10, 20, 10, 20)            // left, top, right, bottom
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
.WithToolTip(element)                // rich tooltip with element content
.WithFlyout(flyout)                  // attaches a flyout
.WithContextFlyout(contextFlyout)    // attaches a context flyout
.ApplyStyle(styleName)               // apply a named style
.AutomationName(name)                // accessibility
.SoundMode(mode)                     // control sound feedback
.OnMount(action)                     // callback when the real WinUI control is created
.Translation(x, y, z)               // composition translation
```

### Transition Modifiers (work on ANY Element)

```csharp
.OpacityTransition(duration?)        // animate opacity changes
.RotationTransition(duration?)       // animate rotation
.ScaleTransition(transition?)        // animate scale
.TranslationTransition(transition?)  // animate position
.BackgroundTransition(duration?)     // animate background color
.WithTransitions(transitions...)     // theme transitions for children
.ItemContainerTransitions(transitions...) // item container transitions
```

### Type-Specific Sugar (call BEFORE generic modifiers)

```csharp
// TextElement:
Text("Hello").Bold()                  // FontWeights.Bold
Text("Hello").SemiBold()              // FontWeights.SemiBold
Text("Hello").FontSize(24)            // Custom font size
Text("Hello").FontStyle(style)        // FontStyle

// ButtonElement:
Button("Click").Disabled()            // IsEnabled = false
Button("Click").Disabled(condition)   // Conditionally disabled

// BorderElement:
Border(child).CornerRadius(8)         // Rounded corners
Border(child).Background("#f5f5f5")   // Background color
Border(child).Background("lightgray") // Named colors work too
Border(child).WithBorder("#ccc", 1)   // Border brush + thickness
Border(child).Padding(16)

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

// TitleBarElement:
TitleBar("My App").Subtitle("Home")

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

// ProgressRingElement:
ProgressRing().Active(active)         // control active state

// RepeatButtonElement:
RepeatButton("Go", onClick).Delay(delay).Interval(interval)

// RectangleElement / EllipseElement:
Rectangle().Fill(brush)
Ellipse().Fill(brush)

// PopupElement:
Popup(child, isOpen).LightDismiss(enabled).Offset(horizontal, vertical)

// ScrollViewElement:
ScrollView(child).ZoomMode(mode).HorizontalScrollMode(mode).VerticalScrollMode(mode)

// TextFieldElement / SliderElement / ToggleSwitchElement:
TextField(val, setVal).Header(header)
Slider(val, 0, 100).Header(header)
ToggleSwitch(isOn, setOn).Header(header)

// LazyVStack / LazyHStack:
LazyVStack<T>(items, keySelector, viewBuilder).SetRepeater(configure)

// MonacoEditorElement:
MonacoEditor(text, onChanged).ReadOnly().EditorFontSize(size).EditorWordWrap(wrap).Minimap(enabled)
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

// Duct.Flex namespace (for Flex layout):
FlexDirection            { Column, ColumnReverse, Row, RowReverse }
FlexJustify              { FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround, SpaceEvenly }
FlexAlign                { FlexStart, Center, FlexEnd, Stretch, Baseline, SpaceBetween, SpaceAround, SpaceEvenly }
FlexWrap                 { NoWrap, Wrap, WrapReverse }
FlexPositionType         { Static, Relative, Absolute }
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
    ).CornerRadius(8).Background("#f5f5f5").Padding(16);
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

### TitleBar with NavigationView Pattern
```csharp
// TitleBar integrates with NavigationView via back button and pane toggle.
// Window.ExtendsContentIntoTitleBar and Window.SetTitleBar() are called automatically on mount.
class AppShell : Component
{
    public override Element Render()
    {
        var (selectedTag, setSelectedTag) = UseState("home");
        var (isPaneOpen, setIsPaneOpen) = UseState(true);

        return Grid(
            rows: "Auto,*",

            TitleBar("My App") with
            {
                Subtitle = selectedTag,
                IsBackButtonVisible = true,
                IsBackButtonEnabled = selectedTag != "home",
                OnBackRequested = () => setSelectedTag("home"),
                IsPaneToggleButtonVisible = true,
                OnPaneToggleRequested = () => setIsPaneOpen(!isPaneOpen),
            }.Grid(row: 0),

            NavigationView(
                new[]
                {
                    NavItem("Home", icon: "Home", tag: "home"),
                    NavItem("Settings", icon: "Setting", tag: "settings"),
                },
                content: selectedTag switch
                {
                    "home" => Component<HomePage>(),
                    "settings" => Component<SettingsPage>(),
                    _ => Text("Not found")
                }
            ) with
            {
                SelectedTag = selectedTag,
                OnSelectionChanged = tag => { if (tag != null) setSelectedTag(tag); },
                IsPaneOpen = isPaneOpen,
                IsSettingsVisible = false,
            }
            .Grid(row: 1)
        );
    }
}
```

### TitleBar with Search Content
```csharp
// Embed controls (like AutoSuggestBox) in the TitleBar content area.
TitleBar("My App") with
{
    Subtitle = "Documents",
    Content = AutoSuggestBox(query, setQuery, placeholder: "Search..."),
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
            ).CornerRadius(4).Background("#f0f0f0").Padding(12, 8, 12, 8)
        ).ToArray()
    )
)
```

### Grid Layout Pattern
```csharp
Grid(
    columns: ["200", "*"],
    rows: ["Auto", "*"],
    Text("Sidebar Header").Bold().Grid(row: 0, column: 0),
    Text("Main Header").Bold().Grid(row: 0, column: 1),
    Component<SideNav>().Grid(row: 1, column: 0),
    Component<MainContent>().Grid(row: 1, column: 1)
)
```

### Flex Layout Pattern (Responsive Sidebar)
```csharp
// Familiar to web devs — same mental model as CSS Flexbox
FlexRow(
    Border(
        FlexColumn(
            Heading("Navigation"),
            ForEach(navItems, item => Button(item.Label, () => setPage(item.Tag)))
        ) with { RowGap = 4 }
    ).Flex(basis: 220, shrink: 0).Background("#f0f0f0").Padding(12),

    Border(
        page switch
        {
            "home" => Component<HomePage>(),
            "settings" => Component<SettingsPage>(),
            _ => Text("Select a page")
        }
    ).Flex(grow: 1).Padding(16)
)
```

---

## Testing Your Changes

Duct has **three test suites**. Run the right one for what you changed.

### 1. Unit tests — fast, no UI window (~3s)
```bash
dotnet test tests/Duct.Tests
```
Run after **any** code change. Covers reconciliation, elements, hooks, Yoga layout (2,200+ tests).

### 2. Selfhost tests — real WinUI controls, in-process (~15s)
```bash
dotnet test tests/Duct.AppTests --filter "ClassName=Duct.AppTests.Tests.SelfTestBatch"
```
Run after **reconciler, control mount/update, or UI changes**. 60+ fixtures that mount real controls and assert via `VisualTreeHelper`. This is the only way to test the diff engine end-to-end.

### 3. Appium / E2E tests — cross-process UI Automation (~30s)
```bash
dotnet test tests/Duct.AppTests --filter "ClassName=Duct.AppTests.Tests.InteractiveTests"
```
Run before **shipping**. Requires [WinAppDriver](https://github.com/microsoft/WinAppDriver/releases).

### Run everything
```bash
dotnet test Duct.sln
```

> **Note:** `samples/Duct.TestApp` is the interactive demo app, not a test runner. All tests live in `tests/`.

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
TitleBar("My App") with { Subtitle = "Home", IsBackButtonVisible = true, OnBackRequested = () => goBack() }
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
            ).Background("#f0f0f0").Padding(24, 12, 24, 12),

            // Content area
            Border(
                page switch
                {
                    "Home" => Component<HomePage>(),
                    "Dashboard" => Component<DashboardPage>(),
                    "Settings" => Component<SettingsPage>(),
                    _ => Text("Not found")
                }
            ).Padding(24).Margin(0, 0, 0, 0)
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
| `<div>` | `VStack()` / `HStack()` / `Border()` / `Flex()` |
| `<span>text</span>` | `Text("text")` |
| `<button onClick={fn}>` | `Button("label", fn)` |
| `<input value={v} onChange={fn}>` | `TextField(v, fn)` |
| `{condition && <X/>}` | `condition ? X() : null` |
| `{items.map(i => <X/>)}` | `items.Select(i => X()).ToArray()` or `ForEach(items, i => X())` |
| `<Component />` | `Component<MyComponent>()` |
| `props` | `Component<TProps>` — typed props via `Props` property |
| `createContext` + `useContext` | `DuctContext<T>` + `.Provide()` + `UseContext()` |
| `React.memo()` | Auto — propless components skip by default; `Memo()` for function components |
| `className="..."` | `.Set(el => ...)` for native property access |
| `display: flex` | `Flex()` / `FlexRow()` / `FlexColumn()` |
| `flex-grow: 1` | `.Flex(grow: 1)` |
| `style={{margin: 10}}` | `.Margin(10)` |
| JSX | C# method calls + `using static Duct.UI` |

## Commands

### When to Use Commands vs Bare Actions

Use **DuctCommand** when:
- An action appears in multiple surfaces (toolbar, menu, context menu)
- The action needs a keyboard shortcut
- The action needs CanExecute disabling (e.g., "Copy" disabled when no selection)
- It's a standard operation (Cut/Copy/Paste/Undo/Redo/Save/etc.)

Use **bare `Action`** when:
- Simple one-off button click with no reuse
- No need for metadata, keyboard shortcuts, or enabled/disabled state

### DuctCommand Record

Immutable command descriptor that bundles an action with metadata:

```csharp
var save = new DuctCommand
{
    Label = "Save",                           // required
    Execute = () => Save(),                   // sync action (mutually exclusive with ExecuteAsync)
    ExecuteAsync = async () => await SaveAsync(), // async action (use UseCommand hook)
    CanExecute = hasChanges,                  // default: true
    IsExecuting = false,                      // managed by UseCommand hook
    Icon = SymbolIcon("Save"),                // optional icon
    Description = "Save the document",        // tooltip + accessibility
    Accelerator = Accelerator(VirtualKey.S, VirtualKeyModifiers.Control), // keyboard shortcut
    AccessKey = "S",                          // Alt+key
};
// Computed: IsEnabled = CanExecute && !IsExecuting
```

### DuctCommand\<T\> — Parameterized Commands

Same as DuctCommand but Execute/ExecuteAsync receive a parameter:

```csharp
var delete = new DuctCommand<Item>
{
    Label = "Delete",
    Execute = item => Remove(item),
    Icon = SymbolIcon("Delete"),
};
MenuItem(delete, selectedItem)  // binds the parameter
```

### StandardCommand Factory

Pre-built commands with correct labels, icons, and keyboard accelerators:

```csharp
// Sync overloads
var cut   = StandardCommand.Cut(() => CutSelection());
var copy  = StandardCommand.Copy(() => CopySelection());
var paste = StandardCommand.Paste(() => PasteFromClipboard());
var undo  = StandardCommand.Undo(() => UndoLastAction());
var redo  = StandardCommand.Redo(() => RedoAction());
var del   = StandardCommand.Delete(() => DeleteSelected());
var selAll = StandardCommand.SelectAll(() => SelectAll());
var save  = StandardCommand.Save(() => SaveFile());
var open  = StandardCommand.Open(() => OpenFile());
var close = StandardCommand.Close(() => CloseTab());
var share = StandardCommand.Share(() => ShareContent());
var play  = StandardCommand.Play(() => PlayMedia());
var pause = StandardCommand.Pause(() => PauseMedia());
var stop  = StandardCommand.Stop(() => StopMedia());
var fwd   = StandardCommand.Forward(() => GoForward());
var back  = StandardCommand.Backward(() => GoBack());

// Async overloads
var save = StandardCommand.Save(async () => await SaveAsync());

// CanExecute parameter
var cut = StandardCommand.Cut(() => CutSelection(), canExecute: hasSelection);
```

### Command-Aware DSL Overloads

Define once, use in any surface:

```csharp
var save = StandardCommand.Save(() => SaveFile());

Button(save)           // label → content, execute → click, isEnabled → isEnabled
AppBarButton(save)     // + icon, accelerator, accessKey, description
MenuItem(save)         // + icon, accelerator, accessKey, description
MenuItem(deleteCmd, item)  // parameterized: binds item as argument
```

### Per-Site Overrides with `with`

```csharp
var delete = StandardCommand.Delete(() => DeleteSelected());
MenuItem(delete)                                           // "Delete" with Delete icon
MenuItem(delete with { Label = "Remove permanently" })     // custom label
AppBarButton(delete with { Icon = SymbolIcon("Clear") })   // custom icon
```

### UseCommand Hook — Async Lifecycle

**When needed:** Only for commands with `ExecuteAsync`. Sync-only commands pass through unchanged.

```csharp
class MyComponent : Component
{
    public override Element Render()
    {
        var saveCmd = UseCommand(StandardCommand.Save(async () =>
        {
            await SaveAsync();
        }));

        // saveCmd.Execute is now sync (wraps the async)
        // saveCmd.ExecuteAsync is null
        // saveCmd.IsExecuting is true while the async operation is in-flight
        // saveCmd.IsEnabled is false while executing (auto-disables)

        return HStack(
            Button(saveCmd),
            saveCmd.IsExecuting ? ProgressRing() : Empty()
        );
    }
}
```

**How it works:**
- Consumes 2 hook slots (UseState for isExecuting, UseMemo for wrapped action)
- Re-entrance guard: ignores clicks while already executing
- Error handling: IsExecuting resets to false even if ExecuteAsync throws

### CommandHost — Keyboard Accelerator Scoping

Scopes keyboard accelerators to a subtree:

```csharp
var save = StandardCommand.Save(() => SaveFile());
var undo = StandardCommand.Undo(() => UndoAction());

CommandHost([save, undo],
    VStack(
        Text("Ctrl+S and Ctrl+Z only work in this region"),
        TextField(value, onChange)
    )
)
```

Only commands with an `Accelerator` register keyboard accelerators. Commands without accelerators are ignored by CommandHost.

### Context-Based Command Sharing via DuctContext

Editor-provides / toolbar-consumes pattern:

```csharp
// 1. Define a command set record and context
record EditorCommands(DuctCommand Save, DuctCommand Undo, DuctCommand Redo);
static readonly DuctContext<EditorCommands?> EditorCtx = new(null);

// 2. Editor provides commands
class Editor : Component
{
    public override Element Render()
    {
        var save = UseCommand(StandardCommand.Save(async () => await SaveAsync()));
        var undo = StandardCommand.Undo(() => Undo());
        var redo = StandardCommand.Redo(() => Redo(), canExecute: false);

        return TextField(text, onChange)
            .Provide(EditorCtx, new EditorCommands(save, undo, redo));
    }
}

// 3. Toolbar consumes commands
class Toolbar : Component
{
    public override Element Render()
    {
        var cmds = UseContext(EditorCtx);
        if (cmds is null) return Empty();

        return CommandBar(primaryCommands: [
            AppBarButton(cmds.Save),
            AppBarButton(cmds.Undo),
            AppBarButton(cmds.Redo),
        ]);
    }
}
```

### CommandInterop.FromCommand — ICommand Migration

Bridge existing ICommand (MVVM/CommunityToolkit) to DuctCommand:

```csharp
var ductCmd = CommandInterop.FromCommand(
    viewModel.SaveCommand,  // ICommand
    "Save",
    icon: SymbolIcon("Save"),
    description: "Save the document",
    accelerator: Accelerator(VirtualKey.S, VirtualKeyModifiers.Control)
);
```

### Common Anti-Patterns

- **Don't** create commands inside loops — define once, bind per-item with `MenuItem(cmd, item)`
- **Don't** use `UseCommand` for sync-only commands — it wastes hook slots
- **Don't** call `UseCommand` conditionally — hooks must be called in the same order every render
- **Don't** mix `Execute` and `ExecuteAsync` on the same command — pick one
