# Flex Layout Spec (Yoga-backed FlexPanel)

This document specifies a CSS Flexbox layout panel for WinUI3, powered by a pure C# port of Meta's [Yoga](https://github.com/facebook/yoga) layout engine. The implementation is split into three layers: a standalone Yoga port, a standalone WinUI3 FlexPanel, and Duct integration.

---

## Goals

1. **Pure C# Yoga port** — isolated, no WinUI or Duct dependencies, updatable independently.
2. **Standalone FlexPanel** — usable in any WinUI3 app via XAML markup with no Duct dependency.
3. **Duct integration** — first-class `Flex()` element and attached property extensions on par with `Grid()`, `VStack()`, etc.

## Non-Goals

- CSS Grid support (Yoga has experimental Grid, but it's not stable enough to ship).
- Virtualization (see analysis below).
- Replacing existing Duct layout primitives — Flex is additive.

---

## Analysis: Virtualizing vs. Non-Virtualizing Layout

### Recommendation: **NonVirtualizingLayout**

Yoga's algorithm **requires the complete node tree in memory** to compute layout. It performs a recursive top-down traversal resolving flex basis, distributing space along flex lines, and computing absolute positions for every node. There is no API to compute layout for a subset of children or to defer realization.

Specific technical reasons:

| Yoga Requirement | Virtualizing Compatibility |
|---|---|
| Full child list needed for flex-grow/shrink distribution | Cannot skip unrealized children — their flex basis affects all siblings |
| Wrap mode groups children into lines; line breaks depend on measuring every child | Cannot determine which children are "visible" without measuring all of them first |
| `align-content` distributes space across lines — needs all line sizes | Must know total line count and sizes before positioning any line |
| Absolute-positioned children resolved relative to containing block | Parent dimensions depend on all flex children being measured |
| Dirty-tree optimization caches per-node, but still walks the full tree | No mechanism to skip subtrees that are "off screen" |

A `VirtualizingLayout` is designed for `ItemsRepeater` where hundreds/thousands of homogeneous items scroll through a viewport. Flexbox is a **container layout** — it arranges a handful of heterogeneous children with complex sizing relationships. These are fundamentally different use cases.

Using `NonVirtualizingLayout` is correct and carries no performance penalty for the target scenario (layout containers with 1–50 children).

---

## Analysis: Layout approach — `NonVirtualizingLayout` on `LayoutPanel` vs. Custom Panel

### Option A: `NonVirtualizingLayout` used via `LayoutPanel`

The WinUI3 `LayoutPanel` accepts a `Layout` property and delegates `MeasureOverride`/`ArrangeOverride` to it. We'd write a `FlexLayout : NonVirtualizingLayout` class.

**XAML usage (Option A):**
```xml
<muxc:LayoutPanel>
    <muxc:LayoutPanel.Layout>
        <local:FlexLayout Direction="Row" JustifyContent="SpaceBetween" AlignItems="Center" />
    </muxc:LayoutPanel.Layout>
    <TextBlock Text="Left" local:Flex.Grow="1" />
    <TextBlock Text="Right" />
</muxc:LayoutPanel>
```

| Pros | Cons |
|---|---|
| Follows the WinUI3 Layout extensibility model exactly | Requires `LayoutPanel` + `FlexLayout` — two objects, more verbose XAML |
| Can swap layout algorithms at runtime | `LayoutPanel` is not widely used; some teams may not be familiar with it |
| `NonVirtualizingLayoutContext.Children` gives direct access to child list | Extra indirection layer between panel and layout |
| Future: could add a virtualizing variant later if needed | Attached properties must be read from children manually (no panel-level helper) |

### Option B: Custom `FlexPanel : Panel`

We write `FlexPanel` as a direct `Panel` subclass, overriding `MeasureOverride` and `ArrangeOverride`.

**XAML usage (Option B):**
```xml
<local:FlexPanel Direction="Row" JustifyContent="SpaceBetween" AlignItems="Center">
    <TextBlock Text="Left" local:FlexPanel.Grow="1" />
    <TextBlock Text="Right" />
</local:FlexPanel>
```

| Pros | Cons |
|---|---|
| Clean, familiar API — looks like StackPanel/Grid | Must maintain Panel subclass boilerplate (Background, BorderBrush, etc.) |
| Single element in XAML, less nesting | Cannot swap layout at runtime (minor — nobody does this) |
| Attached properties scoped naturally to `FlexPanel.Grow` | Slightly more code in the panel itself |
| Standard pattern for WinUI3 custom controls | N/A |

### Recommendation: **Option B — Custom `FlexPanel : Panel`**

The ergonomic advantage is significant. Every WinUI3 developer understands panels. The XAML is cleaner, the attached property syntax is more natural (`FlexPanel.Grow` vs `Flex.Grow` on an unrelated `LayoutPanel`), and we avoid requiring developers to know about the `LayoutPanel` control. The "standalone, no Duct dependency" goal also benefits — a single `FlexPanel` class is much easier to extract and distribute than a `FlexLayout` + instructions to use `LayoutPanel`.

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                   Duct Integration                   │
│  FlexElement, FlexAttached, FlexExtensions, DSL      │
│  (depends on Duct.Core + Duct.Flex)                  │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│              Duct.Flex (Standalone)                   │
│  FlexPanel : Panel                                   │
│  FlexPanel attached DPs (Grow, Shrink, Basis, etc.)  │
│  (depends on WinUI3 + Duct.Layout only)              │
└──────────────────────┬──────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────┐
│              Duct.Layout (Standalone)                  │
│  Pure C# port of Yoga layout engine                  │
│  YogaNode, YogaConfig, enums, algorithm              │
│  (zero external dependencies)                        │
└─────────────────────────────────────────────────────┘
```

Each layer is a separate namespace (potentially separate project/assembly if extraction is needed later). Dependency flows strictly downward.

---

## Layer 1: Duct.Layout — Pure C# Yoga Port

### Namespace: `Duct.Layout`

### What to port

The Yoga C++ source at `yoga/` is ~11,700 lines. The C# port targets:

| C++ Source | C# Target | Approx Lines |
|---|---|---|
| `yoga/algorithm/CalculateLayout.cpp` | `YogaAlgorithm.cs` | ~2,500 |
| `yoga/algorithm/AbsoluteLayout.cpp` | (inline in algorithm) | ~600 |
| `yoga/algorithm/FlexLine.h/cpp` | `FlexLine.cs` | ~200 |
| `yoga/algorithm/Baseline.h/cpp` | (inline in algorithm) | ~100 |
| `yoga/algorithm/PixelGrid.cpp` | `PixelGrid.cs` | ~135 |
| `yoga/algorithm/Cache.h/cpp` | (inline in node) | ~50 |
| `yoga/node/Node.h/cpp` | `YogaNode.cs` | ~400 |
| `yoga/node/LayoutResults.h/cpp` | `LayoutResults.cs` | ~150 |
| `yoga/style/Style.h` | `YogaStyle.cs` | ~500 |
| `yoga/config/Config.h/cpp` | `YogaConfig.cs` | ~150 |
| `yoga/enums/*.h` | `YogaEnums.cs` | ~200 |
| Numeric/value utilities | `YogaValue.cs`, helpers | ~200 |

**Estimated total: ~5,000–6,000 lines of C#.**

### Key types

```csharp
namespace Duct.Layout;

// ── Enums ──────────────────────────────────────────────
public enum FlexLayoutDirection { Inherit, LTR, RTL }
public enum FlexDirection { Column, ColumnReverse, Row, RowReverse }
public enum FlexJustify { FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround, SpaceEvenly }
public enum FlexAlign { Auto, FlexStart, Center, FlexEnd, Stretch, Baseline, SpaceBetween, SpaceAround, SpaceEvenly }
public enum FlexWrap { NoWrap, Wrap, WrapReverse }
public enum FlexPositionType { Static, Relative, Absolute }
public enum YogaOverflow { Visible, Hidden, Scroll }
public enum YogaDisplay { Flex, None }
public enum YogaUnit { Undefined, Point, Percent, Auto }
public enum YogaEdge { Left, Top, Right, Bottom, Start, End, Horizontal, Vertical, All }
public enum YogaMeasureMode { Undefined, Exactly, AtMost }
public enum YogaBoxSizing { BorderBox, ContentBox }

// ── Value type ─────────────────────────────────────────
public readonly record struct YogaValue(float Value, YogaUnit Unit)
{
    public static YogaValue Auto => new(0, YogaUnit.Auto);
    public static YogaValue Undefined => new(float.NaN, YogaUnit.Undefined);
    public static YogaValue Point(float v) => new(v, YogaUnit.Point);
    public static YogaValue Percent(float v) => new(v, YogaUnit.Percent);
}

// ── Callbacks ──────────────────────────────────────────
public delegate YogaSize MeasureFunc(YogaNode node, float width, YogaMeasureMode widthMode, float height, YogaMeasureMode heightMode);
public delegate float BaselineFunc(YogaNode node, float width, float height);

// ── Core node ──────────────────────────────────────────
public sealed class YogaNode
{
    // Tree structure
    public void InsertChild(YogaNode child, int index);
    public void RemoveChild(YogaNode child);
    public int ChildCount { get; }
    public YogaNode? Owner { get; }

    // Style properties (all setters mark node dirty)
    public FlexDirection FlexDirection { get; set; }
    public FlexJustify JustifyContent { get; set; }
    public FlexAlign AlignItems { get; set; }
    public FlexAlign AlignSelf { get; set; }
    public FlexAlign AlignContent { get; set; }
    public FlexWrap FlexWrap { get; set; }
    public FlexPositionType PositionType { get; set; }
    public YogaDisplay Display { get; set; }
    public float FlexGrow { get; set; }
    public float FlexShrink { get; set; }
    public YogaValue FlexBasis { get; set; }
    public YogaValue Width { get; set; }
    public YogaValue Height { get; set; }
    public YogaValue MinWidth { get; set; }
    public YogaValue MinHeight { get; set; }
    public YogaValue MaxWidth { get; set; }
    public YogaValue MaxHeight { get; set; }
    public float AspectRatio { get; set; }
    // Margin, Padding, Border, Position — per-edge setters
    public void SetMargin(YogaEdge edge, YogaValue value);
    public void SetPadding(YogaEdge edge, YogaValue value);
    public void SetBorder(YogaEdge edge, float value);
    public void SetPosition(YogaEdge edge, YogaValue value);
    public void SetGap(YogaGutter gutter, float value);

    // Custom measurement (for text/leaf nodes)
    public MeasureFunc? MeasureFunction { get; set; }
    public BaselineFunc? BaselineFunction { get; set; }

    // Layout computation
    public void CalculateLayout(float availableWidth, float availableHeight, FlexLayoutDirection direction);

    // Layout results (read after CalculateLayout)
    public float LayoutX { get; }
    public float LayoutY { get; }
    public float LayoutWidth { get; }
    public float LayoutHeight { get; }
    // LayoutMargin, LayoutPadding, LayoutBorder per edge
    public bool HasNewLayout { get; set; }
}

// ── Config ─────────────────────────────────────────────
public sealed class YogaConfig
{
    public float PointScaleFactor { get; set; }
    public bool UseWebDefaults { get; set; }
}
```

### Porting approach

1. **Direct line-by-line translation** from the C++ source, preserving algorithm structure.
2. Replace C++ pointers with object references; `std::vector<Node*>` → `List<YogaNode>`.
3. Replace `FloatOptional` with `float` + `float.IsNaN()` checks (C# handles NaN natively).
4. Replace the `StyleValuePool` compaction with simple struct fields (memory savings less important in managed code than code clarity).
5. Keep the 8-entry measurement cache per node.
6. Keep the generation-count dirty tracking system.
7. Unit test against Yoga's own fixture tests (`gentest/` contains expected layout results for hundreds of test cases).

---

## Layer 2: Duct.Flex — Standalone FlexPanel

### Namespace: `Duct.Flex`

### FlexPanel

```csharp
namespace Duct.Flex;

public class FlexPanel : Panel
{
    // ── Container properties (DependencyProperty) ─────────
    public FlexDirection Direction { get; set; }          // default: Row
    public FlexJustify JustifyContent { get; set; }           // default: FlexStart
    public FlexAlign AlignItems { get; set; }                 // default: Stretch
    public FlexAlign AlignContent { get; set; }               // default: FlexStart
    public FlexWrap Wrap { get; set; }                        // default: NoWrap
    public FlexLayoutDirection LayoutDirection { get; set; }        // default: LTR
    public double ColumnGap { get; set; }                     // default: 0
    public double RowGap { get; set; }                        // default: 0
    public Thickness FlexPadding { get; set; }                // default: 0 (Yoga-computed padding)

    // ── Attached properties (for children) ────────────────
    public static readonly DependencyProperty GrowProperty;
    public static readonly DependencyProperty ShrinkProperty;
    public static readonly DependencyProperty BasisProperty;
    public static readonly DependencyProperty AlignSelfProperty;
    public static readonly DependencyProperty PositionProperty;      // Relative | Absolute
    public static readonly DependencyProperty LeftProperty;
    public static readonly DependencyProperty TopProperty;
    public static readonly DependencyProperty RightProperty;
    public static readonly DependencyProperty BottomProperty;

    // Static Get/Set helpers for each attached property
    public static void SetGrow(UIElement el, double value);
    public static double GetGrow(UIElement el);
    // ... etc for each property

    // ── Layout ────────────────────────────────────────────
    protected override Size MeasureOverride(Size availableSize);
    protected override Size ArrangeOverride(Size finalSize);
}
```

### Layout algorithm integration

`MeasureOverride` and `ArrangeOverride` bridge between WinUI3 and Yoga:

```
MeasureOverride(availableSize):
  1. Create/update YogaNode tree:
     - Root node: set width/height constraints from availableSize
     - One child YogaNode per Children[i]
     - Read attached DPs (Grow, Shrink, Basis, etc.) → set on child YogaNode
     - Set MeasureFunction on each child node → calls child.Measure(constraint) → returns DesiredSize
  2. Call root.CalculateLayout(availableWidth, availableHeight, direction)
  3. Return Size(root.LayoutWidth, root.LayoutHeight)

ArrangeOverride(finalSize):
  1. If dirty, re-run CalculateLayout with final constraints
  2. For each child:
     child.Arrange(new Rect(childNode.LayoutX, childNode.LayoutY, childNode.LayoutWidth, childNode.LayoutHeight))
  3. Return finalSize
```

**Key detail: child measurement.** Each child's YogaNode gets a `MeasureFunction` that calls `child.Measure(new Size(width, height))` and returns `child.DesiredSize`. This is how Yoga learns the intrinsic size of non-flex children (e.g., a TextBlock that wraps text). Yoga calls this function during `CalculateLayout` with appropriate constraints based on the flex algorithm's current sizing mode.

### Node caching

The FlexPanel maintains a `Dictionary<UIElement, YogaNode>` to avoid recreating nodes every layout pass. Nodes are added/removed in response to `Children` collection changes. Style properties are updated from attached DPs each measure pass (cheap — Yoga's dirty system skips unchanged nodes).

### XAML usage example

```xml
<flex:FlexPanel Direction="Row" JustifyContent="SpaceBetween" AlignItems="Center" FlexPadding="16">

    <!-- Logo, no flex, intrinsic size -->
    <Image Source="logo.png" Width="48" Height="48" />

    <!-- Nav links, grow to fill -->
    <StackPanel Orientation="Horizontal" flex:FlexPanel.Grow="1" Margin="0,0,16,0">
        <TextBlock Text="Home" />
        <TextBlock Text="About" />
    </StackPanel>

    <!-- Action button, no flex -->
    <Button Content="Sign In" flex:FlexPanel.Shrink="0" />

</flex:FlexPanel>

<!-- Wrapping layout -->
<flex:FlexPanel Direction="Row" Wrap="Wrap" ColumnGap="8" RowGap="8">
    <Border Width="100" Height="100" Background="Red" flex:FlexPanel.Grow="1" />
    <Border Width="100" Height="100" Background="Green" flex:FlexPanel.Grow="1" />
    <Border Width="100" Height="100" Background="Blue" flex:FlexPanel.Grow="1" />
    <Border Width="100" Height="100" Background="Yellow" flex:FlexPanel.Grow="1" />
</flex:FlexPanel>
```

---

## Layer 3: Duct Integration

### Namespace: `Duct` (elements) + `Duct.Core` (records)

### Element record

```csharp
// In Element.cs
public record FlexElement(Element[] Children) : Element
{
    public FlexDirection Direction { get; init; } = FlexDirection.Row;
    public FlexJustify JustifyContent { get; init; } = FlexJustify.FlexStart;
    public FlexAlign AlignItems { get; init; } = FlexAlign.Stretch;
    public FlexAlign AlignContent { get; init; } = FlexAlign.FlexStart;
    public FlexWrap Wrap { get; init; } = FlexWrap.NoWrap;
    public double ColumnGap { get; init; }
    public double RowGap { get; init; }
    internal Action<FlexPanel>[] Setters { get; init; } = [];
}
```

### Attached property record

```csharp
// In Element.cs alongside GridAttached, CanvasAttached
public record FlexAttached(
    double Grow = 0,
    double Shrink = 1,
    double? Basis = null,           // null = auto
    FlexAlign? AlignSelf = null,    // null = inherit from container
    FlexPositionType Position = FlexPositionType.Relative,
    double? Left = null,
    double? Top = null,
    double? Right = null,
    double? Bottom = null
);
```

### Extension methods

```csharp
// FlexExtensions.cs
public static class FlexExtensions
{
    public static T Flex<T>(this T el,
        double grow = 0,
        double shrink = 1,
        double? basis = null,
        FlexAlign? alignSelf = null,
        FlexPositionType position = FlexPositionType.Relative,
        double? left = null,
        double? top = null,
        double? right = null,
        double? bottom = null
    ) where T : Element =>
        (T)el.SetAttached(new FlexAttached(grow, shrink, basis, alignSelf, position, left, top, right, bottom));
}
```

### DSL factory

```csharp
// In Dsl.cs
public static FlexElement Flex(
    params Element?[] children) =>
    new(children.Where(c => c is not null).ToArray()!);

public static FlexElement Flex(
    FlexDirection direction,
    params Element?[] children) =>
    new(children.Where(c => c is not null).ToArray()!) { Direction = direction };

// Overloads for common combos
public static FlexElement FlexRow(params Element?[] children) =>
    Flex(FlexDirection.Row, children);

public static FlexElement FlexColumn(params Element?[] children) =>
    Flex(FlexDirection.Column, children);
```

### Duct usage example

```csharp
// Horizontal nav bar
FlexRow(
    Image("logo.png").Width(48).Height(48),
    HStack(Text("Home"), Text("About")).Flex(grow: 1),
    Button("Sign In", onClick).Flex(shrink: 0)
) { JustifyContent = FlexJustify.SpaceBetween, AlignItems = FlexAlign.Center }

// Wrapping tag cloud
Flex(
    tags.Select(t => Text(t).Flex(grow: 1)).ToArray()
) { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, ColumnGap = 8, RowGap = 8 }

// Absolute overlay
Flex(
    Text("Background content").Flex(grow: 1),
    Text("Overlay badge").Flex(position: FlexPositionType.Absolute, top: 8, right: 8)
)
```

### Reconciler integration

Mount and update follow the same pattern as `StackElement`/`GridElement`:

```
MountFlex(FlexElement el):
  1. Rent or create FlexPanel
  2. Set container properties (Direction, JustifyContent, etc.)
  3. For each child:
     a. Mount child → UIElement
     b. Read child.GetAttached<FlexAttached>()
     c. Apply attached DPs: FlexPanel.SetGrow(control, attached.Grow), etc.
     d. Add to FlexPanel.Children
  4. Return FlexPanel

UpdateFlex(FlexElement old, FlexElement new, FlexPanel panel):
  1. Update container properties if changed
  2. Reconcile children (same as StackElement/GridElement)
  3. Re-apply attached DPs on changed children
```

---

## File structure

```
Duct/
├── Yoga/                          # Layer 1 — zero dependencies
│   ├── YogaNode.cs
│   ├── YogaConfig.cs
│   ├── YogaStyle.cs
│   ├── YogaValue.cs
│   ├── YogaEnums.cs
│   ├── LayoutResults.cs
│   ├── FlexLine.cs
│   ├── PixelGrid.cs
│   └── YogaAlgorithm.cs          # Main layout algorithm
│
├── Flex/                          # Layer 2 — depends on Yoga + WinUI3 only
│   └── FlexPanel.cs              # Panel subclass + attached DPs
│
├── Core/
│   ├── Element.cs                 # + FlexElement record, FlexAttached record
│   ├── Reconciler.Mount.cs        # + MountFlex()
│   └── Reconciler.Update.cs       # + UpdateFlex()
│
├── Elements/
│   ├── Dsl.cs                     # + Flex(), FlexRow(), FlexColumn()
│   └── FlexExtensions.cs          # .Flex(grow:, shrink:, ...) extension

FlexPanelGallery/                   # Standalone XAML test app (repo root, no Duct dependency)
├── FlexPanelGallery.csproj
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
└── Pages/
    ├── OverviewPage.xaml           # Hero demo + property summary table
    ├── DirectionPage.xaml          # Row, RowReverse, Column, ColumnReverse
    ├── WrapPage.xaml               # NoWrap, Wrap, WrapReverse
    ├── JustifyContentPage.xaml     # FlexStart, Center, FlexEnd, SpaceBetween, SpaceAround, SpaceEvenly
    ├── AlignItemsPage.xaml         # Stretch, FlexStart, Center, FlexEnd, Baseline
    ├── FlexGrowShrinkPage.xaml     # Grow/shrink ratios, basis
    ├── GapPage.xaml                # RowGap, ColumnGap
    ├── AbsolutePositionPage.xaml   # PositionType.Absolute overlays
    └── NestedFlexPage.xaml         # Nested FlexPanels, real-world layouts (holy grail, sidebar, etc.)
```

---

## FlexPanelGallery — Standalone XAML Test App

A WinUI3 app that exercises FlexPanel entirely through XAML markup, with no Duct dependency. Modeled after the WinUI Gallery pattern: a NavigationView shell with one page per feature area.

### Purpose

1. **Validate the standalone story** — prove FlexPanel works in a pure XAML app with zero Duct code.
2. **Interactive testing** — each page has live property controls (ComboBoxes, Sliders) that change FlexPanel properties and show the effect immediately.
3. **Visual regression baseline** — screenshots of each page serve as the reference for correctness.
4. **Documentation by example** — each page doubles as a usage example developers can copy.

### Project setup

```xml
<!-- FlexPanelGallery.csproj -->
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
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="$(WindowsAppSDKVersion)" />
    <!-- Project reference to Duct only for the Yoga + Flex namespaces. No Duct.Core usage. -->
    <ProjectReference Include="..\Duct\Duct.csproj" />
  </ItemGroup>
</Project>
```

The project references Duct's assembly to get `FlexPanel` and the Yoga types, but **imports only `Duct.Yoga` and `Duct.Flex` namespaces** — no `using Duct;`, no `using Duct.Core;`. This enforces the isolation contract at the code level.

### Shell

`MainWindow.xaml` contains a `NavigationView` with one `NavigationViewItem` per page. Selecting an item navigates a `Frame` to the corresponding page. This is the standard WinUI Gallery shell pattern.

### Page pattern

Each page follows a consistent layout:

```xml
<Page>
  <ScrollViewer>
    <StackPanel Spacing="16" Padding="24">

      <!-- Title + description -->
      <TextBlock Text="Justify Content" Style="{StaticResource TitleTextBlockStyle}" />
      <TextBlock Text="Controls how items are distributed along the main axis." TextWrapping="Wrap" />

      <!-- Interactive demo -->
      <Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
              CornerRadius="8" Padding="16">
        <StackPanel Spacing="12">

          <!-- Property controls -->
          <ComboBox Header="JustifyContent" SelectedItem="{x:Bind ViewModel.JustifyContent, Mode=TwoWay}" />

          <!-- Live FlexPanel preview -->
          <Border BorderBrush="{ThemeResource DividerStrokeColorDefaultBrush}" BorderThickness="1"
                  MinHeight="200">
            <flex:FlexPanel Direction="Row"
                           JustifyContent="{x:Bind ViewModel.JustifyContent, Mode=OneWay}">
              <Border Width="60" Height="60" Background="CornflowerBlue" CornerRadius="4" />
              <Border Width="80" Height="60" Background="MediumSeaGreen" CornerRadius="4" />
              <Border Width="50" Height="60" Background="Coral" CornerRadius="4" />
            </flex:FlexPanel>
          </Border>

        </StackPanel>
      </Border>

      <!-- XAML source snippet (optional, for documentation value) -->
      <TextBlock FontFamily="Consolas" Text="..." IsTextSelectionEnabled="True" />

    </StackPanel>
  </ScrollViewer>
</Page>
```

Each page has a lightweight code-behind or ViewModel with the bindable properties. No MVVM framework required — just `INotifyPropertyChanged` or `x:Bind` to dependency properties.

### Pages

| Page | What it tests | Key interactive controls |
|---|---|---|
| **OverviewPage** | Hero demo combining multiple flex features; property summary table | None (static showcase) |
| **DirectionPage** | `Direction` = Row, RowReverse, Column, ColumnReverse | ComboBox for Direction |
| **WrapPage** | `Wrap` = NoWrap, Wrap, WrapReverse with many items | ComboBox for Wrap, Slider for child count |
| **JustifyContentPage** | All 6 JustifyContent values | ComboBox for JustifyContent |
| **AlignItemsPage** | All AlignItems values, including Baseline with mixed font sizes | ComboBox for AlignItems |
| **FlexGrowShrinkPage** | Grow/Shrink ratios, Basis, interaction between them | Sliders for Grow/Shrink/Basis on each child |
| **GapPage** | RowGap and ColumnGap with wrapped items | Sliders for RowGap, ColumnGap |
| **AbsolutePositionPage** | `PositionType.Absolute` overlays, inset edges | Sliders for Left/Top/Right/Bottom |
| **NestedFlexPage** | Real-world layouts: holy grail, sidebar+content, header/footer | ComboBox to switch between layout presets |

---

## Implementation order

1. **Yoga port** — port the algorithm, validate against Yoga's fixture tests.
2. **FlexPanel** — build the Panel subclass, test with XAML-only scenarios (no Duct).
3. **FlexPanelGallery** — build the standalone XAML test app, validate all properties visually.
4. **Duct integration** — add the element/attached/DSL, wire up mount/update.
5. **Polish** — perf profiling, edge cases (RTL, zero-size, overflow).

---

## Open questions

1. **Assembly split**: Should `Duct.Layout` and `Duct.Flex` be separate assemblies/NuGet packages, or just separate namespaces within the Duct project? Separate assemblies maximize extractability but add build complexity.
   - **Decision**: Single assembly for now.

2. **Yoga version**: The port should target a specific Yoga commit/tag for reproducibility. Current Yoga main is post-0.72 with Grid support — we should target a stable release tag.
   - **Decision**: Go with main for now, easier to roll forward if we start on the most recent.

3. **Grid support**: Yoga's CSS Grid implementation is behind an experimental feature flag. Should we port it as disabled-by-default, or omit entirely for v1?
   - **Decision**: Disabled-by-default.

4. **FlexPanel.Padding vs Panel Padding**: WinUI `Panel` doesn't have a `Padding` property natively. We can either use Yoga's padding (computed as part of the flex layout) or wrap content in a `Border`. Recommendation: use Yoga's padding — it's more correct and avoids an extra element.
   - **Decision**: Name the property `FlexPadding` — while verbose, it makes it clear this is different from `Border.Padding`.
