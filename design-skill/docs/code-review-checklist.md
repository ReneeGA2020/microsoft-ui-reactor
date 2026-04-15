# Code Review Checklist for Duct

Use this checklist when reviewing Duct UI code for Windows 11 design compliance.

## Theming

- [ ] Uses `Theme.*` tokens or `Theme.Ref()` for colors/brushes — no hardcoded hex on themed surfaces
- [ ] Uses `Theme.Ref()` with keys ending in `Brush` (not the Color name)
- [ ] No hardcoded colors in High Contrast code paths
- [ ] No opacity on elements in HC — translucency encoded in alpha channel for Light/Dark only
- [ ] Acrylic surfaces use correct background + border pairings
- [ ] `HighContrastAdjustment` set at app level if custom theme dictionaries are used

## Typography

- [ ] Uses semantic text factories (`Heading`, `SubHeading`, `Caption`, `Text`) or WinUI style tokens
- [ ] No raw `FontSize` + `FontWeight` for standard UI text
- [ ] `FontWeight` is SemiBold (600), not Bold (700) — except `Heading()` page titles
- [ ] Minimum font size is 12px
- [ ] Icon fonts use `SymbolThemeFontFamily` — not hardcoded `"Segoe Fluent Icons"`
- [ ] Icon TextBlocks set `IsTextScaleFactorEnabled = false`
- [ ] Default foreground (`TextFillColorPrimaryBrush`) not explicitly set on `Text()`
- [ ] Changing numbers use tabular numerals

## Layout

- [ ] All margins, padding, spacing use multiples of 4
- [ ] Corner radius is 4 (controls) or 8 (overlays) — no non-standard values
- [ ] Uses `MinHeight`/`MinWidth` instead of fixed `Height`/`Width` for text containers
- [ ] No fixed heights on text containers — clips at larger text scales
- [ ] No fixed widths on buttons — clips long localized strings
- [ ] Uses `Border` for single-child containers (not `Grid`/`VStack` wrappers)
- [ ] `HStack` does not contain text that needs `TextTrimming`
- [ ] Uses spacing parameters, not spacer elements
- [ ] Mixed-control rows explicitly set `VAlign(VerticalAlignment.Center)`
- [ ] `ThemeShadow` has `Translation(0, 0, 32)` for elevation
- [ ] Shadow containers have 12px parent padding for clipping prevention

## Controls and Styling

- [ ] `.Resources()` used for button visual state overrides (not `.Background()` directly)
- [ ] All visual states covered when overriding (rest + hover + pressed + disabled)
- [ ] No explicit setting of WinUI default values (default padding, corner radius, etc.)
- [ ] Uses existing WinUI styles before creating custom overrides

## Accessibility

- [ ] `AutomationName` set on icon-only controls
- [ ] `HeadingLevel` set on heading text for screen reader navigation
- [ ] `AccessKey` set on primary actions
- [ ] `PositionInSet` / `SizeOfSet` set on list items
- [ ] `LiveRegion` set on dynamically updated status text
- [ ] Hit-test targets for light-dismiss are visible (`Background("#00000000")`)
- [ ] `DividerStrokeColorDefaultBrush` used for dividers (custom brushes break in HC)

## State and Reconciliation

- [ ] `.WithKey()` set on items in dynamic lists
- [ ] Hooks are unconditional and in consistent order
- [ ] No hooks inside `if` blocks or variable-length loops
- [ ] `UseCallback` wraps handlers passed to child components
- [ ] `UseMemo` wraps expensive computations
- [ ] `UseEffect` cleanup returns dispose logic for timers/subscriptions

## Performance

- [ ] No deep visual tree nesting — flatten where possible
- [ ] `Border` used instead of single-child `Grid`/`VStack`
- [ ] `.Set()` used only for properties not exposed as modifiers
- [ ] Large lists use `ListView` (virtualized), not `VStack` + `.Select()`

## Testing Evidence

- [ ] Tested in Light, Dark, and High Contrast (NightSky) themes
- [ ] Tested hover/pressed states on interactive elements
- [ ] Tested at 100%, 150%, 200%, 250% display scaling
- [ ] Tested with maximum text scaling
- [ ] Tested with long/localized strings
- [ ] Before/after screenshots included for visual changes
- [ ] Acrylic pairing and shadow clipping verified after layout changes
- [ ] Figma measurements validated at 100% scale factor
