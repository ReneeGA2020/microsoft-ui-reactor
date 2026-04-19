# Theme-Aware Resources in Reactor

This document covers how to apply Windows 11 theme resources correctly in Reactor's C# projection.

## How Theme Resolution Works in Reactor

WinUI provides 200+ semantic theme resources organized by theme variant (Light, Dark, HighContrast). In XAML you'd use `{ThemeResource}` markup — in Reactor, you use `Theme.*` tokens or `Theme.Ref("ResourceKey")`.

When the reconciler mounts or updates an element with a `ThemeRef`, it resolves the resource by walking `Application.Current.Resources.ThemeDictionaries` to find the brush matching the element's effective theme. When the system theme changes, elements using `ThemeRef` automatically update.

## Theme Token Quick Reference

### Text Fill

| Reactor Token | WinUI Resource Key | Purpose |
|------------|-------------------|---------|
| `Theme.PrimaryText` | `TextFillColorPrimaryBrush` | Primary text (default) |
| `Theme.SecondaryText` | `TextFillColorSecondaryBrush` | Secondary text |
| `Theme.TertiaryText` | `TextFillColorTertiaryBrush` | Placeholder text |
| `Theme.DisabledText` | `TextFillColorDisabledBrush` | Disabled text |
| `Theme.AccentText` | `AccentTextFillColorPrimaryBrush` | Accent-colored text |

Do **not** set `Theme.PrimaryText` explicitly on `TextBlock()` — it is the default foreground.

### Text on Accent

| WinUI Resource Key | Purpose |
|-------------------|---------|
| `TextOnAccentFillColorPrimaryBrush` | Primary text on accent background |
| `TextOnAccentFillColorSecondaryBrush` | Secondary text on accent background |
| `TextOnAccentFillColorDisabledBrush` | Disabled text on accent background |

Use via `Theme.Ref("TextOnAccentFillColorPrimaryBrush")`.

### Accent Fill

| Reactor Token | WinUI Resource Key | Purpose |
|------------|-------------------|---------|
| `Theme.Accent` | `AccentFillColorDefaultBrush` | Default accent |
| `Theme.AccentSecondary` | `AccentFillColorSecondaryBrush` | Hover state |
| `Theme.AccentTertiary` | `AccentFillColorTertiaryBrush` | Pressed state |
| `Theme.AccentDisabled` | `AccentFillColorDisabledBrush` | Disabled state |

### Control Fill

| Reactor Token | WinUI Resource Key | Purpose |
|------------|-------------------|---------|
| `Theme.ControlFill` | `ControlFillColorDefaultBrush` | Control background |
| `Theme.ControlFillSecondary` | `ControlFillColorSecondaryBrush` | Hover background |
| `Theme.ControlFillTertiary` | `ControlFillColorTertiaryBrush` | Pressed background |
| `Theme.ControlFillDisabled` | `ControlFillColorDisabledBrush` | Disabled background |
| `Theme.ControlFillInputActive` | `ControlFillColorInputActiveBrush` | Focused input |

### Subtle Fill

| Reactor Token | WinUI Resource Key | Purpose |
|------------|-------------------|---------|
| `Theme.SubtleFill` | `SubtleFillColorTransparentBrush` | Transparent resting state |
| — | `SubtleFillColorSecondaryBrush` | Subtle hover |
| — | `SubtleFillColorTertiaryBrush` | Subtle pressed |

### Surface and Card

| Reactor Token | WinUI Resource Key | Purpose |
|------------|-------------------|---------|
| `Theme.CardBackground` | `CardBackgroundFillColorDefaultBrush` | Card background |
| `Theme.LayerFill` | `LayerFillColorDefaultBrush` | Layer background |
| `Theme.SolidBackground` | `SolidBackgroundFillColorBaseBrush` | Solid page background |
| `Theme.SmokeFill` | `SmokeFillColorDefaultBrush` | Smoke overlay |

### Stroke / Border

| Reactor Token | WinUI Resource Key | Purpose |
|------------|-------------------|---------|
| `Theme.CardStroke` | `CardStrokeColorDefaultBrush` | Card border |
| `Theme.SurfaceStroke` | `SurfaceStrokeColorDefaultBrush` | Surface border |
| `Theme.DividerStroke` | `DividerStrokeColorDefaultBrush` | Dividers |
| `Theme.ControlStroke` | `ControlStrokeColorDefaultBrush` | Control border |
| `Theme.ControlStrokeSecondary` | `ControlStrokeColorSecondaryBrush` | Secondary border |

### Signal / Status

| Reactor Token | WinUI Resource Key | Purpose |
|------------|-------------------|---------|
| `Theme.SystemSuccess` | `SystemFillColorSuccessBrush` | Success |
| `Theme.SystemCaution` | `SystemFillColorCautionBrush` | Warning |
| `Theme.SystemCritical` | `SystemFillColorCriticalBrush` | Error |
| `Theme.SystemAttention` | `SystemFillColorAttentionBrush` | Information |
| `Theme.SystemNeutral` | `SystemFillColorNeutralBrush` | Neutral |

Background variants: `Theme.SystemSuccessBackground`, `Theme.SystemCautionBackground`, `Theme.SystemCriticalBackground`, `Theme.SystemAttentionBackground`, `Theme.SystemNeutralBackground`.

### Accent Colors

| WinUI Resource Key | Purpose |
|-------------------|---------|
| `SystemAccentColor` | User-chosen accent color |
| `SystemAccentColorLight1` through `Light3` | Progressively lighter accent |
| `SystemAccentColorDark1` through `Dark3` | Progressively darker accent |

Use via `Theme.Ref("SystemAccentColor")`. These are `Color` resources (not `Brush`); wrap in a brush when needed.

### Acrylic

| WinUI Resource Key | Purpose |
|-------------------|---------|
| `AcrylicBackgroundFillColorDefaultBrush` | Flyout/tooltip acrylic |
| `AcrylicBackgroundFillColorBaseBrush` | UI surface acrylic |
| `SurfaceStrokeColorFlyoutBrush` | Flyout border on acrylic |
| `SurfaceStrokeColorDefaultBrush` | Surface border on acrylic |
| `LayerOnAcrylicFillColorDefaultBrush` | Overlay on acrylic |

Use via `Theme.Ref("AcrylicBackgroundFillColorDefaultBrush")`.

## Acrylic Surface Pairings

Acrylic backgrounds have **specific** border pairings. Mixing them produces incorrect visuals.

| Surface Type | Background | Border |
|--------------|------------|--------|
| Flyouts, tooltips | `AcrylicBackgroundFillColorDefaultBrush` | `SurfaceStrokeColorFlyoutBrush` |
| UI surfaces (panels, sidebars) | `AcrylicBackgroundFillColorBaseBrush` | `SurfaceStrokeColorDefaultBrush` |

```csharp
// Flyout acrylic pairing
Border(content)
    .Background(Theme.Ref("AcrylicBackgroundFillColorDefaultBrush"))
    .WithBorder(Theme.Ref("SurfaceStrokeColorFlyoutBrush"), 1)
    .CornerRadius(8)
    .Translation(0, 0, 32)
    .Set(b =>
    {
        b.BackgroundSizing = BackgroundSizing.InnerBorderEdge;
        b.Shadow = new ThemeShadow();
    })

// UI surface acrylic pairing
Border(content)
    .Background(Theme.Ref("AcrylicBackgroundFillColorBaseBrush"))
    .WithBorder(Theme.Ref("SurfaceStrokeColorDefaultBrush"), 1)
    .CornerRadius(8)
    .Set(b => b.BackgroundSizing = BackgroundSizing.InnerBorderEdge)
```

## High Contrast

In High Contrast mode, WinUI maps theme resources to one of 8 system color brushes. When you use `Theme.*` tokens, HC resolution happens automatically.

**Allowed HC brushes:**

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

**HC color pairings — never mix incompatible pairs:**

| Background | Foreground | Use Case |
|------------|------------|----------|
| `SystemColorWindowColor` | `SystemColorWindowTextColor` | General content |
| `SystemColorHighlightColor` | `SystemColorHighlightTextColor` | Selected/hover states |
| `SystemColorButtonFaceColor` | `SystemColorButtonTextColor` | Buttons |
| `SystemColorWindowColor` | `SystemColorGrayTextColor` | Disabled content |
| `SystemColorWindowColor` | `SystemColorHotlightColor` | Hyperlinks |

**Rules:**
- Never use opacity on elements in HC.
- Never use accent colors or regular WinUI brushes in HC-specific code paths.
- Never use gradient animations in HC.
- Use 2px border thickness in HC for flyouts, dialogs, and cards.
- **No partial theme updates** — when changing Light/Dark resource overrides, include matching HC-safe values in the same change.
- **Empty HC is valid** — when `.Resources()` overrides target only Light/Dark and WinUI defaults already satisfy HC accessibility, you do not need HC-specific overrides.

**Interactive containers in HC** — clickable cards and list items must show a visible border in HC to indicate interactivity. Use `SystemColorHighlightColor` for the border:

```csharp
// Interactive card — highlight border visible in HC
Border(cardContent)
    .Background(Theme.CardBackground)
    .WithBorder(Theme.CardStroke, 1)
    .CornerRadius(4)
    // In HC, WinUI maps CardStroke appropriately.
    // For custom interactive surfaces, test that the border is visible in NightSky.
```

**HC setup at app level:**

```csharp
Application.Current.HighContrastAdjustment = ApplicationHighContrastAdjustment.None;
```

This prevents the system from applying automatic HC overrides on top of your theme dictionaries.

## ARGB Encoding for Opacity

When a surface needs translucency in Light/Dark (but cannot use opacity in HC), encode the opacity in the alpha channel:

```csharp
// 25% opacity via alpha channel (0x40 = 64 decimal = 25%)
Border(child).Background("#40000000")
```

## Per-Subtree Theme Override

Force a subtree to Light or Dark regardless of system theme:

```csharp
// Always dark sidebar
VStack(sidebarContent)
    .RequestedTheme(ElementTheme.Dark)

// Always light content area
VStack(mainContent)
    .RequestedTheme(ElementTheme.Light)
```

## Detecting Theme Changes

Use `UseColorScheme()` to reactively respond to system theme changes:

```csharp
var scheme = UseColorScheme();

return VStack(
    TextBlock(scheme.IsDarkTheme ? "Dark Mode" : "Light Mode"),
    // Adjust layout or content based on theme
);
```
