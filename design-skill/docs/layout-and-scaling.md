# Layout and Scaling in Duct

## The 4px Grid

All margins, padding, and sizing values should be multiples of 4: 4, 8, 12, 16, 20, 24, 32, 40, 48, etc.

Odd values (3, 5, 7, 11, 15) cause blurry rendering at fractional DPI scales (125%, 150%, 175%).

```csharp
// Correct: multiples of 4
VStack(8,
    Text("Item 1"),
    Text("Item 2")
).Padding(16).Margin(12)

// Wrong: odd values
VStack(5,
    Text("Item 1"),
    Text("Item 2")
).Padding(15).Margin(7)
```

## Corner Radius

Windows 11 defines two standard corner radii:

| Token | Value | Use Case |
|-------|-------|----------|
| `ControlCornerRadius` | 4px | Controls, buttons, cards |
| `OverlayCornerRadius` | 8px | Flyouts, dialogs, popups |

```csharp
// Control corner radius (4px)
Border(child).CornerRadius(4)
Button("Action").CornerRadius(4)

// Overlay corner radius (8px)
Border(flyoutContent).CornerRadius(8)

// Selective rounding (top corners only for panels)
Border(panelContent).CornerRadius(8, 8, 0, 0)

// Wrong: non-standard radius
Border(child).CornerRadius(3)   // Not in the design system
Border(child).CornerRadius(6)   // Not in the design system
```

**Figma caveat:** Design tools sometimes show non-standard values. Always map to 4 or 8 in code.

## Layout Containers

### VStack / HStack (Stack-Based)

Linear vertical or horizontal layout with optional spacing.

```csharp
VStack(8,
    Heading("Title"),
    Text("Description"),
    Button("Action", onClick)
)

HStack(12,
    Image(icon).Size(24, 24),
    Text("Label"),
    Text("Value").Foreground(Theme.SecondaryText)
)
```

### Grid (Row/Column)

Explicit row/column definitions with child positioning.

```csharp
Grid(
    columns: ["Auto", "*", "Auto"],
    rows: ["Auto", "*"],
    
    Text("Label").Grid(row: 0, column: 0),
    TextField(value, setValue).Grid(row: 0, column: 1),
    Button("Go", onClick).Grid(row: 0, column: 2),
    
    ScrollView(
        VStack(8, content)
    ).Grid(row: 1, column: 0, columnSpan: 3))
```

**Column/row definitions:**
- `"*"` — flexible, takes remaining space
- `"2*"` — flexible, twice the weight
- `"Auto"` — sizes to content
- `"200"` — fixed 200px

### Flex (CSS Flexbox)

Modern flexible layout via Yoga engine. Best for proportional, wrapping, and complex responsive layouts.

```csharp
// Responsive row with grow
Flex(
    Text("Fixed").Flex(shrink: 0, basis: 100),
    Border(child).Flex(grow: 1),
    Button("Action").Flex(shrink: 0)
) with
{
    JustifyContent = FlexJustify.SpaceBetween,
    AlignItems = FlexAlign.Center
}

// Wrapping grid of cards
Flex(
    cards.Select(card =>
        CardComponent(card)
            .Flex(basis: 300, grow: 1)
            .WithKey(card.Id)
    ).ToArray()
) with
{
    Wrap = FlexWrap.Wrap,
    ColumnGap = 16,
    RowGap = 16
}
```

### Border (Single-Child Container)

For a single child with background, border, padding, or corner radius.

```csharp
// Correct: Border for styled single-child container
Border(
    Text("Card content")
).Padding(16).CornerRadius(4).Background(Theme.CardBackground)

// Wrong: Grid or VStack for single child
Grid(["*"], ["*"],
    Text("Card content")
).Background(Theme.CardBackground)
```

### ScrollView

Wrap content that may exceed available space.

```csharp
ScrollView(
    VStack(8, longContent)
)
```

Headers and action bars should remain outside the ScrollView — only the content area scrolls.

## Sizing

### Prefer Min/Max Over Fixed

```csharp
// Correct: flexible sizing
Button("Submit").MinHeight(40)
VStack(content).MinWidth(200).MaxWidth(600)
TextField(value, setValue).MinHeight(32)

// Wrong: fixed sizing clips at larger text scales
Button("Submit").Height(32)
TextField(value, setValue).Height(30).Width(200)
```

### No Fixed Widths on Buttons

Let content determine width, or set `MinWidth`:

```csharp
// Correct
Button("Save", onSave)
Button("Save", onSave).MinWidth(120)

// Wrong
Button("Save", onSave).Width(100)  // Clips long localized strings
```

### No Fixed Heights on Text Containers

Fixed height clips text at larger text scaling settings:

```csharp
// Correct
Border(Text(message)).MinHeight(40).Padding(8)

// Wrong
Border(Text(message)).Height(40)  // Clips at 200% text scale
```

## Alignment

```csharp
// Horizontal alignment
Text("Centered").HAlign(HorizontalAlignment.Center)
Button("Right").HAlign(HorizontalAlignment.Right)
Text("Stretch").HAlign(HorizontalAlignment.Stretch)

// Vertical alignment
Text("Middle").VAlign(VerticalAlignment.Center)

// Center both axes
Text("Centered").Center()
```

### Mixed-Control Rows

When placing different controls side by side (e.g., toggle + text), vertically center all participants:

```csharp
HStack(8,
    ToggleSwitch(isOn, setOn).VAlign(VerticalAlignment.Center),
    Text("Enable feature").VAlign(VerticalAlignment.Center))
```

## Spacing

Use container spacing, not spacer elements:

```csharp
// Correct: spacing parameter
VStack(8, Text("A"), Text("B"), Text("C"))

// Wrong: spacer element
VStack(
    Text("A"),
    Border(null).Height(8).Opacity(0),
    Text("B"))
```

For Grid, use row/column spacing via `.Set()`:

```csharp
Grid(columns, rows, children).Set(g =>
{
    g.RowSpacing = 8;
    g.ColumnSpacing = 12;
})
```

## Shadows

### ThemeShadow Pattern

```csharp
Border(content)
    .CornerRadius(8)
    .Translation(0, 0, 32)  // Required: elevation makes shadow visible
    .Set(b =>
    {
        b.Shadow = new ThemeShadow();
    })
```

**Rules:**
- `Translation(0, 0, 32)` is required — without it, the shadow is invisible.
- Add 12px padding on the parent to prevent shadow clipping.
- Shadow receivers must be at a lower z-order than the elevated element.
- Use `ThemeShadow` instead of composition drop shadows.

## Text Trimming

`HStack` (StackPanel) gives children unbounded width, so `TextTrimming` never activates. Use a `Grid` with a `"*"` column:

```csharp
// Correct: Grid constrains width
Grid(
    columns: ["Auto", "*"],
    rows: ["Auto"],
    Image(avatar).Size(32, 32).Grid(column: 0),
    Text(title)
        .TextTrimming(TextTrimming.CharacterEllipsis)
        .Grid(column: 1))

// Wrong: TextTrimming never fires in HStack
HStack(8,
    Image(avatar).Size(32, 32),
    Text(title).TextTrimming(TextTrimming.CharacterEllipsis))
```

Note: `Grid` column `"Auto"` also sizes to content and prevents trimming — use `"*"` for the text column.

## BiDi / RTL Support

Use logical margin and padding for bidirectional layouts:

```csharp
Text("Label")
    .MarginInlineStart(16)   // Left in LTR, right in RTL
    .PaddingInlineEnd(8)     // Right in LTR, left in RTL
```

## Display Scaling

Test at 100%, 150%, 200%, and 250% scaling. Common issues:
- Blurry edges from odd-numbered sizes (use 4px grid).
- Clipped text from fixed heights.
- Misaligned controls from hardcoded positioning.
- Shadow clipping from insufficient parent padding.

## Design Handoff

When implementing from Figma or design comps:
- Measure at 100% scale factor.
- Map corner radius to system values (4 or 8).
- Map spacing to 4px grid multiples.
- Validate against the running build on the same scale factor.
