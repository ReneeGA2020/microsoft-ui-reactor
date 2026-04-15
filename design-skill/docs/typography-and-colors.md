# Typography and Colors in Duct

## Typography

### The Windows 11 Type Ramp

Windows 11 defines a type ramp of semantic text styles. In Duct, use the built-in text factories for common sizes, or apply WinUI styles for the full ramp.

#### Duct Text Factories

| Factory | Size | Weight | Use Case |
|---------|------|--------|----------|
| `Caption("text")` | 12px | Regular (400) | Small labels, timestamps, metadata |
| `Text("text")` | 14px | Regular (400) | Default body text |
| `SubHeading("text")` | 20px | SemiBold (600) | Section headers, card titles |
| `Heading("text")` | 28px | Bold (700) | Page titles |

#### Full WinUI Type Ramp

For sizes not covered by the factories, apply a WinUI style:

| WinUI Style | Size | Weight | Duct Equivalent |
|-------------|------|--------|-----------------|
| `CaptionTextBlockStyle` | 12px | Regular | `Caption()` |
| `BodyTextBlockStyle` | 14px | Regular | `Text()` (default) |
| `BodyStrongTextBlockStyle` | 14px | SemiBold | `Text().SemiBold()` |
| `BodyLargeTextBlockStyle` | 18px | Regular | Apply via `.Set()` |
| `SubtitleTextBlockStyle` | 20px | SemiBold | `SubHeading()` |
| `TitleTextBlockStyle` | 28px | SemiBold | `Heading()` |
| `TitleLargeTextBlockStyle` | 40px | SemiBold | Apply via `.Set()` |
| `DisplayTextBlockStyle` | 68px | SemiBold | Apply via `.Set()` |

**Applying WinUI styles in Duct:**

```csharp
// For styles not covered by factories
Text("Large title").Set(tb =>
    tb.Style = (Style)Application.Current.Resources["TitleLargeTextBlockStyle"])

Text("Display text").Set(tb =>
    tb.Style = (Style)Application.Current.Resources["DisplayTextBlockStyle"])

Text("Body strong").SemiBold()

// BodyLargeTextBlockStyle
Text("Prominent text").Set(tb =>
    tb.Style = (Style)Application.Current.Resources["BodyLargeTextBlockStyle"])
```

### Typography Rules

1. **Use semantic factories or styles** — do not set `FontSize` and `FontWeight` directly for standard UI text.

   ```csharp
   // Correct
   Heading("Settings")
   SubHeading("General")
   Caption("Last updated: 2 hours ago")

   // Wrong
   Text("Settings").FontSize(28).FontWeight(new FontWeight(700))
   ```

2. **SemiBold (600), not Bold (700)** — Bold is not part of the Windows 11 design language. Exception: `Heading()` intentionally uses 700 for page titles.

   ```csharp
   // Correct: SemiBold for emphasis
   Text("Important").SemiBold()

   // Wrong: Bold
   Text("Important").Bold()
   ```

3. **Minimum font size: 12px** — Anything below 12px makes complex Asian characters unreadable. `Caption()` at 12px is the smallest acceptable body text size.

4. **Icon font family** — Never hardcode `"Segoe Fluent Icons"`. Use the system resource:

   ```csharp
   Text("\uE710").Set(tb =>
       tb.FontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"])
       .Set(tb => tb.IsTextScaleFactorEnabled = false)
   ```

5. **Icon TextBlocks should not scale with text settings:**

   ```csharp
   Text("\uE710")
       .Set(tb => tb.IsTextScaleFactorEnabled = false)
       .VAlign(VerticalAlignment.Center)
   ```

6. **Tabular numerals for changing numbers** — Prevents width jitter on clocks, percentages, counters:

   ```csharp
   Text($"{batteryPercent}%").Set(tb =>
       tb.Typography.NumeralAlignment = FontNumeralAlignment.Tabular)
   ```

7. **Text trimming requires constrained width** — `HStack` gives unbounded width so trimming never fires. Use `Grid` with a `"*"` column:

   ```csharp
   Grid(
       columns: ["Auto", "*"],
       rows: ["Auto"],
       Image(avatar).Size(32, 32).Grid(column: 0),
       Text(longTitle)
           .TextTrimming(TextTrimming.CharacterEllipsis)
           .Grid(column: 1))
   ```

8. **Default foreground** — `TextFillColorPrimaryBrush` is the default TextBlock foreground. Do not set it explicitly.

## Colors

### Color Application in Duct

Colors are applied via three mechanisms:

1. **Theme tokens** (preferred): `Theme.PrimaryText`, `Theme.Accent`, etc.
2. **Theme references**: `Theme.Ref("AnyWinUIResourceKey")`
3. **Hex strings** (escape hatch): `"#AARRGGBB"` or `"#RRGGBB"`

```csharp
// Theme token — auto-updates with theme
Text("Hello").Foreground(Theme.PrimaryText)

// Theme reference — any WinUI resource
Border(child).Background(Theme.Ref("CardBackgroundFillColorDefaultBrush"))

// Hex string — fixed color, does NOT change with theme
Border(child).Background("#FF0000")  // Only for non-themed decorative elements
```

### Approved Color Resources

When building themed surfaces, use only approved WinUI brush resources. The full approved list:

#### Text Fill Colors
```
TextFillColorPrimaryBrush
TextFillColorSecondaryBrush
TextFillColorTertiaryBrush
TextFillColorDisabledBrush
TextFillColorInverseBrush
AccentTextFillColorPrimaryBrush
AccentTextFillColorSecondaryBrush
AccentTextFillColorTertiaryBrush
AccentTextFillColorDisabledBrush
TextOnAccentFillColorPrimaryBrush
TextOnAccentFillColorSecondaryBrush
TextOnAccentFillColorDisabledBrush
```

#### Control Fill Colors
```
ControlFillColorDefaultBrush
ControlFillColorSecondaryBrush
ControlFillColorTertiaryBrush
ControlFillColorQuarternaryBrush
ControlFillColorDisabledBrush
ControlFillColorTransparentBrush
ControlFillColorInputActiveBrush
ControlStrongFillColorDefaultBrush
ControlStrongFillColorDisabledBrush
ControlSolidFillColorDefaultBrush
```

#### Subtle Fill Colors
```
SubtleFillColorTransparentBrush
SubtleFillColorSecondaryBrush
SubtleFillColorTertiaryBrush
SubtleFillColorDisabledBrush
```

#### Accent Fill Colors
```
AccentFillColorDefaultBrush
AccentFillColorSecondaryBrush
AccentFillColorTertiaryBrush
AccentFillColorDisabledBrush
AccentFillColorSelectedTextBackgroundBrush
```

#### Stroke / Border Colors
```
ControlStrokeColorDefaultBrush
ControlStrokeColorSecondaryBrush
ControlStrokeColorOnAccentDefaultBrush
ControlStrokeColorOnAccentSecondaryBrush
CardStrokeColorDefaultBrush
CardStrokeColorDefaultSolidBrush
ControlStrongStrokeColorDefaultBrush
ControlStrongStrokeColorDisabledBrush
SurfaceStrokeColorDefaultBrush
SurfaceStrokeColorFlyoutBrush
SurfaceStrokeColorInverseBrush
DividerStrokeColorDefaultBrush
FocusStrokeColorOuterBrush
FocusStrokeColorInnerBrush
```

#### Surface / Card / Layer Colors
```
CardBackgroundFillColorDefaultBrush
CardBackgroundFillColorSecondaryBrush
SmokeFillColorDefaultBrush
LayerFillColorDefaultBrush
LayerFillColorAltBrush
LayerOnAcrylicFillColorDefaultBrush
LayerOnAccentAcrylicFillColorDefaultBrush
LayerOnMicaBaseAltFillColorDefaultBrush
SolidBackgroundFillColorBaseBrush
SolidBackgroundFillColorSecondaryBrush
SolidBackgroundFillColorTertiaryBrush
SolidBackgroundFillColorQuarternaryBrush
```

#### System / Signal Colors
```
SystemFillColorSuccessBrush
SystemFillColorCautionBrush
SystemFillColorCriticalBrush
SystemFillColorNeutralBrush
SystemFillColorSolidNeutralBrush
SystemFillColorAttentionBackgroundBrush
SystemFillColorSuccessBackgroundBrush
SystemFillColorCautionBackgroundBrush
SystemFillColorCriticalBackgroundBrush
SystemFillColorNeutralBackgroundBrush
```

### High Contrast System Colors

In High Contrast mode, only the 8 system color brushes are allowed:

```
SystemColorWindowTextColorBrush
SystemColorWindowColorBrush
SystemColorHighlightTextColorBrush
SystemColorHighlightColorBrush
SystemColorButtonTextColorBrush
SystemColorButtonFaceColorBrush
SystemColorGrayTextColorBrush
SystemColorHotlightColorBrush
```
