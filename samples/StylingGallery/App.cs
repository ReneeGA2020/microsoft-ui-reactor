using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Elements;
using static Microsoft.UI.Reactor.Factories;
using static StylingHelpers;
using Microsoft.UI.Xaml;

ReactorApp.Run<StylingGalleryApp>("Styling Gallery", width: 900, height: 750);

// ════════════════════════════════════════════════════════════════════════
//  Root app — tab-based navigation between styling feature demos
// ════════════════════════════════════════════════════════════════════════

class StylingGalleryApp : Component
{
    public override Element Render()
    {
        var (tab, setTab) = UseState(0);

        return VStack(0,
            // Tab bar
            HStack(0,
                TabButton("Theme Tokens", 0, tab, setTab),
                TabButton("RequestedTheme", 1, tab, setTab),
                TabButton("UseColorScheme", 2, tab, setTab),
                TabButton("Lightweight Styling", 3, tab, setTab),
                TabButton("Style Caching", 4, tab, setTab)
            ).Background(Theme.SolidBackground)
             .Padding(8, 8, 8, 0),

            // Content area
            ScrollView(
                tab switch
                {
                    0 => Component<ThemeTokensDemo>(),
                    1 => Component<RequestedThemeDemo>(),
                    2 => Component<ColorSchemeDemo>(),
                    3 => Component<LightweightStylingDemo>(),
                    4 => Component<StyleCachingDemo>(),
                    _ => TextBlock("Unknown tab"),
                }
            )
        );
    }

    static Element TabButton(string label, int index, int current, Action<int> setTab) =>
        Button(label, () => setTab(index))
            .Resources(r =>
            {
                if (index == current)
                {
                    r.Set("ButtonBackground", Theme.Accent)
                     .Set("ButtonBackgroundPointerOver", Theme.AccentSecondary)
                     .Set("ButtonForeground", "#FFFFFF")
                     .Set("ButtonForegroundPointerOver", "#FFFFFF");
                }
            })
            .Margin(0, 0, 4, 0);
}

// ════════════════════════════════════════════════════════════════════════
//  Tab 1: Theme Tokens
// ════════════════════════════════════════════════════════════════════════

class ThemeTokensDemo : Component
{
    public override Element Render()
    {
        return VStack(20,
            SectionHeader("Theme Tokens", "All colors resolve from WinUI's resource system and adapt automatically on theme change."),

            SubHeading("Text Colors"),
            VStack(8,
                TextBlock("PrimaryText").Foreground(Theme.PrimaryText),
                TextBlock("SecondaryText").Foreground(Theme.SecondaryText),
                TextBlock("TertiaryText").Foreground(Theme.TertiaryText),
                TextBlock("DisabledText").Foreground(Theme.DisabledText),
                TextBlock("AccentText").Foreground(Theme.AccentText).SemiBold()
            ),

            SubHeading("Accent Colors"),
            HStack(8,
                ColorSwatch("Accent", Theme.Accent),
                ColorSwatch("AccentSecondary", Theme.AccentSecondary),
                ColorSwatch("AccentTertiary", Theme.AccentTertiary),
                ColorSwatch("AccentDisabled", Theme.AccentDisabled)
            ),

            SubHeading("Surface Colors"),
            HStack(8,
                ColorSwatch("SolidBackground", Theme.SolidBackground),
                ColorSwatch("CardBackground", Theme.CardBackground),
                ColorSwatch("SubtleFill", Theme.SubtleFill),
                ColorSwatch("LayerFill", Theme.LayerFill)
            ),

            SubHeading("Signal Colors"),
            HStack(8,
                ColorSwatch("Attention", Theme.SystemAttention),
                ColorSwatch("Success", Theme.SystemSuccess),
                ColorSwatch("Caution", Theme.SystemCaution),
                ColorSwatch("Critical", Theme.SystemCritical)
            ),

            SubHeading("Stroke Colors"),
            HStack(8,
                ColorSwatch("CardStroke", Theme.CardStroke),
                ColorSwatch("SurfaceStroke", Theme.SurfaceStroke),
                ColorSwatch("DividerStroke", Theme.DividerStroke),
                ColorSwatch("ControlStroke", Theme.ControlStroke)
            )
        ).Padding(24);
    }

    static Element ColorSwatch(string label, ThemeRef color) =>
        VStack(4,
            Border(TextBlock(""))
                .Background(color)
                .Width(80).Height(40)
                .CornerRadius(6)
                .WithBorder(Theme.ControlStroke),
            Caption(label).Foreground(Theme.SecondaryText)
                .HAlign(HorizontalAlignment.Center)
        );
}

// ════════════════════════════════════════════════════════════════════════
//  Tab 2: RequestedTheme Modifier
// ════════════════════════════════════════════════════════════════════════

class RequestedThemeDemo : Component
{
    public override Element Render()
    {
        var (isDark, setIsDark) = UseState(false);

        return VStack(20,
            SectionHeader("RequestedTheme Modifier",
                "Force a subtree to render in a specific theme variant using .RequestedTheme(). " +
                "Native WinUI controls (Button, TextBlock, etc.) automatically adopt the overridden " +
                "theme's styling — no explicit color overrides needed."),

            SubHeading("Dark Sidebar + Light Content"),
            HStack(0,
                // Dark sidebar — native controls get dark styling automatically.
                // Use a static dark background; text/buttons adopt dark theme natively.
                VStack(12,
                    TextBlock("Sidebar").Bold(),
                    TextBlock("Navigation").Opacity(0.7),
                    TextBlock("Settings").Opacity(0.7),
                    TextBlock("Profile").Opacity(0.7)
                ).Padding(16)
                 .Background("#1E1E1E")
                 .RequestedTheme(ElementTheme.Dark)
                 .Width(180),

                // Light content (system theme)
                VStack(12,
                    TextBlock("Main Content").Bold(),
                    TextBlock("This area uses the system theme."),
                    Button("Action", () => { })
                ).Padding(16)
                 .Background(Theme.CardBackground)
                 .HAlign(HorizontalAlignment.Stretch)
            ).CornerRadius(8)
             .WithBorder(Theme.CardStroke)
             .Height(200),

            SubHeading("Toggle Theme Override"),
            ToggleSwitch(isDark, setIsDark, onContent: "Dark", offContent: "Light"),
            // RequestedTheme makes native controls adopt the theme automatically.
            // Background uses a static color matching the theme variant.
            Border(
                VStack(12,
                    TextBlock("This panel follows the toggle."),
                    TextBlock("Native controls adapt automatically."),
                    HStack(8,
                        Button("Primary", () => { }),
                        Button("Secondary", () => { }),
                        ToggleSwitch(false, _ => { }, onContent: "On", offContent: "Off")
                    )
                ).Padding(16)
            ).Background(isDark ? "#2D2D2D" : "#F3F3F3")
             .CornerRadius(8)
             .RequestedTheme(isDark ? ElementTheme.Dark : ElementTheme.Light),

            SubHeading("ElementTheme.Default (System Inheritance)"),
            TextBlock("Setting RequestedTheme to Default restores the system theme:"),
            HStack(12,
                ThemeBox("Dark", ElementTheme.Dark, "#2D2D2D"),
                ThemeBox("Default", ElementTheme.Default, null),
                ThemeBox("Light", ElementTheme.Light, "#F3F3F3")
            )
        ).Padding(24);
    }

    static Element ThemeBox(string label, ElementTheme theme, string? bg) =>
        Border(
            VStack(4,
                TextBlock(label).Bold(),
                TextBlock("Sample text"),
                Button("Button", () => { })
            ).Padding(12)
        ).Background(bg ?? "#F9F9F9")
         .CornerRadius(6)
         .RequestedTheme(theme)
         .Width(150);
}

// ════════════════════════════════════════════════════════════════════════
//  Tab 3: UseColorScheme Hook
// ════════════════════════════════════════════════════════════════════════

class ColorSchemeDemo : Component
{
    public override Element Render()
    {
        var scheme = UseColorScheme();
        var isDark = UseIsDarkTheme();

        return VStack(20,
            SectionHeader("UseColorScheme Hook",
                "Reactive hook that observes the effective color scheme at this component's position in the tree."),

            SubHeading("Current Color Scheme"),
            HStack(12,
                InfoCard("ColorScheme", scheme.ToString()),
                InfoCard("IsDarkTheme", isDark.ToString())
            ),

            SubHeading("Adaptive Component"),
            TextBlock("This component adjusts its presentation based on the detected color scheme:")
                .Foreground(Theme.SecondaryText),

            Border(
                HStack(16,
                    TextBlock(isDark ? "\U0001F319" : "\u2600\uFE0F").FontSize(48),
                    VStack(4,
                        TextBlock(isDark ? "Dark Mode Active" : "Light Mode Active")
                            .Bold().Foreground(Theme.PrimaryText),
                        TextBlock(isDark
                                ? "UI is optimized for low-light viewing"
                                : "UI is optimized for well-lit environments")
                            .Foreground(Theme.SecondaryText)
                    )
                ).Padding(20)
            ).Background(Theme.CardBackground)
             .CornerRadius(8)
             .WithBorder(Theme.CardStroke),

            SubHeading("Opacity Adjustment"),
            TextBlock("Decorative elements use lower opacity in light mode to reduce visual noise:")
                .Foreground(Theme.SecondaryText),
            HStack(12,
                DecorativeBox("Normal", 1.0),
                DecorativeBox("Reduced", isDark ? 0.8 : 0.5),
                DecorativeBox("Subtle", isDark ? 0.6 : 0.3)
            ),

            SubHeading("RequestedTheme Awareness"),
            TextBlock("UseColorScheme reads the app-level theme. Components inside a RequestedTheme " +
                 "subtree see that override reflected in the actual theme of their mounted control.")
                .Foreground(Theme.SecondaryText)
        ).Padding(24);
    }

    static Element InfoCard(string label, string value) =>
        Border(
            VStack(4,
                Caption(label).Foreground(Theme.SecondaryText),
                TextBlock(value).FontSize(20).Bold().Foreground(Theme.AccentText)
            ).Padding(12)
        ).Background(Theme.CardBackground)
         .CornerRadius(6)
         .WithBorder(Theme.CardStroke)
         .Width(160);

    static Element DecorativeBox(string label, double opacity) =>
        VStack(4,
            Border(TextBlock(""))
                .Background(Theme.Accent)
                .Width(80).Height(50)
                .CornerRadius(6)
                .Opacity(opacity),
            Caption(label).Foreground(Theme.SecondaryText)
                .HAlign(HorizontalAlignment.Center)
        );
}

// ════════════════════════════════════════════════════════════════════════
//  Tab 4: Lightweight Styling
// ════════════════════════════════════════════════════════════════════════

class LightweightStylingDemo : Component
{
    public override Element Render()
    {
        return VStack(20,
            SectionHeader("Lightweight Styling",
                "Per-control resource overrides via .Resources(). WinUI's VisualStateManager " +
                "picks up the overrides — hover, pressed, and disabled states all work automatically."),

            SubHeading("Brand-Colored Buttons"),
            TextBlock("Override ButtonBackground, ButtonBackgroundPointerOver, and ButtonBackgroundPressed " +
                 "to create branded buttons without custom templates.")
                .Foreground(Theme.SecondaryText),
            HStack(12,
                // Blue brand button
                Button("Primary Action", () => { })
                    .Resources(r => r
                        .Set("ButtonBackground", "#0078D4")
                        .Set("ButtonBackgroundPointerOver", "#106EBE")
                        .Set("ButtonBackgroundPressed", "#005A9E")
                        .Set("ButtonForeground", "#FFFFFF")
                        .Set("ButtonForegroundPointerOver", "#FFFFFF")
                        .Set("ButtonForegroundPressed", "#FFFFFF")),

                // Green success button
                Button("Confirm", () => { })
                    .Resources(r => r
                        .Set("ButtonBackground", "#107C10")
                        .Set("ButtonBackgroundPointerOver", "#0E6B0E")
                        .Set("ButtonBackgroundPressed", "#0B5A0B")
                        .Set("ButtonForeground", "#FFFFFF")
                        .Set("ButtonForegroundPointerOver", "#FFFFFF")
                        .Set("ButtonForegroundPressed", "#FFFFFF")),

                // Red danger button
                Button("Delete", () => { })
                    .Resources(r => r
                        .Set("ButtonBackground", "#D13438")
                        .Set("ButtonBackgroundPointerOver", "#C42B30")
                        .Set("ButtonBackgroundPressed", "#A4262C")
                        .Set("ButtonForeground", "#FFFFFF")
                        .Set("ButtonForegroundPointerOver", "#FFFFFF")
                        .Set("ButtonForegroundPressed", "#FFFFFF")),

                // Default button for comparison
                Button("Default", () => { })
            ),

            SubHeading("Theme-Reactive Resources"),
            TextBlock("Use ThemeRef values in .Resources() — they re-resolve automatically on theme change.")
                .Foreground(Theme.SecondaryText),
            HStack(12,
                Button("Accent from Theme", () => { })
                    .Resources(r => r
                        .Set("ButtonBackground", Theme.Accent)
                        .Set("ButtonBackgroundPointerOver", Theme.AccentSecondary)
                        .Set("ButtonBackgroundPressed", Theme.AccentTertiary)),

                Button("Subtle from Theme", () => { })
                    .Resources(r => r
                        .Set("ButtonBackground", Theme.SubtleFill)
                        .Set("ButtonBackgroundPointerOver", Theme.ControlFillSecondary)
                        .Set("ButtonBackgroundPressed", Theme.ControlFillTertiary))
            ),

            SubHeading("Numeric & CornerRadius Overrides"),
            TextBlock("Override non-brush resources like border thickness and corner radius.")
                .Foreground(Theme.SecondaryText),
            HStack(12,
                Button("Rounded", () => { })
                    .Resources(r => r
                        .Set("ControlCornerRadius", new Microsoft.UI.Xaml.CornerRadius(16))),

                Button("Thick Border", () => { })
                    .Resources(r => r
                        .Set("ButtonBorderThemeThickness", 3.0))
            ),

            SubHeading("Cascading from Parent"),
            TextBlock("Resources set on a parent panel cascade to all child controls.")
                .Foreground(Theme.SecondaryText),
            Border(
                HStack(12,
                    Button("Child A", () => { }),
                    Button("Child B", () => { }),
                    Button("Child C", () => { })
                ).Padding(16)
                 .Resources(r => r
                     .Set("ButtonBackground", "#0078D4")
                     .Set("ButtonBackgroundPointerOver", "#106EBE")
                     .Set("ButtonForeground", "#FFFFFF")
                     .Set("ButtonForegroundPointerOver", "#FFFFFF"))
            ).Background(Theme.CardBackground)
             .CornerRadius(8)
             .WithBorder(Theme.CardStroke)
        ).Padding(24);
    }
}

// ════════════════════════════════════════════════════════════════════════
//  Tab 5: Style Caching
// ════════════════════════════════════════════════════════════════════════

class StyleCachingDemo : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(50);

        return VStack(20,
            SectionHeader("Style Caching",
                "XamlReader.Load() is called once per unique (TargetType + ThemeRef bindings) " +
                "combination, then cached. Rendering 100 elements with the same theme binding " +
                "only parses XAML once."),

            SubHeading("Cached Theme Binding Grid"),
            TextBlock($"Each card below uses .Background(Theme.CardBackground) — all {count} share " +
                 "a single cached Style object. Try increasing the count.")
                .Foreground(Theme.SecondaryText),

            HStack(12,
                Button("-10", () => setCount(Math.Max(10, count - 10))),
                TextBlock($"{count} items").Foreground(Theme.PrimaryText).VAlign(VerticalAlignment.Center),
                Button("+10", () => setCount(Math.Min(200, count + 10)))
            ),

            WrapGrid(
                Enumerable.Range(0, count).Select(i =>
                    Border(
                        TextBlock($"#{i + 1}").Foreground(Theme.SecondaryText).FontSize(11)
                            .HAlign(HorizontalAlignment.Center)
                            .VAlign(VerticalAlignment.Center)
                    ).Background(Theme.CardBackground)
                     .WithBorder(Theme.CardStroke)
                     .CornerRadius(4)
                     .Width(55).Height(36)
                ).ToArray()
            ),

            SubHeading("Cache Invalidation"),
            TextBlock("The cache is cleared on theme change (Light/Dark toggle). This is conservative " +
                 "memory cleanup — WinUI's {ThemeResource} setters handle the actual theme switch " +
                 "natively. Clearing the cache just frees memory from the old theme's compiled styles.")
                .Foreground(Theme.SecondaryText)
        ).Padding(24);
    }
}

// ════════════════════════════════════════════════════════════════════════
//  Shared helpers — called as SectionHeader() from components via
//  'using static StylingHelpers'
// ════════════════════════════════════════════════════════════════════════

static class StylingHelpers
{
    public static Element SectionHeader(string title, string description) =>
        VStack(8,
            Heading(title),
            TextBlock(description).Foreground(Theme.SecondaryText)
        );
}
