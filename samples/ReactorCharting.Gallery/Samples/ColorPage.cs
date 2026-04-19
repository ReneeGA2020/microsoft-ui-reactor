// Color page — Showcases WinUI 3 theme resource brushes via Reactor's Theme API.
// Inspired by WinUI-Gallery's ColorPage: displays brush swatches organized by category
// (Text, Fill, Stroke, Background, Signal) so developers can see what's available.

using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Microsoft.UI.Reactor.Factories;

namespace ReactorCharting.Gallery;

public class ColorPageSample : GallerySample
{
    public override string Title => "Color & Theme Brushes";
    public override string Description =>
        "WinUI 3 theme resource brushes you can reference in Reactor. " +
        "These brushes automatically adapt when the app theme changes (Light ↔ Dark).";
    public override string Category => "Design";

    public override string SourceCode => """
        // Reference built-in semantic tokens:
        TextBlock("Hello").Foreground(Theme.PrimaryText)
        Button("Go").Background(Theme.Accent)
        VStack(children).Background(Theme.CardBackground)

        // Reference any WinUI resource by key:
        TextBlock("Custom").Foreground(Theme.Ref("SystemFillColorSuccessBrush"))
        """;

    public override Element Render()
    {
        return VStack(16,
            TextBlock("The brushes below are part of WinUI 3. Reference them in Reactor via the Theme API:")
                .Foreground(Theme.SecondaryText),

            HStack(8,
                TextBlock("TextBlock(\"Hello\").Foreground(").Foreground(Theme.SecondaryText),
                TextBlock("Theme.PrimaryText").Foreground(Theme.AccentText).SemiBold(),
                TextBlock(")").Foreground(Theme.SecondaryText)
            ),

            // ── Text brushes ────────────────────────────────────────
            SectionHeader("Text"),
            SwatchGrid(TextBrushes()),

            // ── Accent / Fill brushes ───────────────────────────────
            SectionHeader("Fill"),
            SwatchGrid(FillBrushes()),

            // ── Stroke / Border brushes ─────────────────────────────
            SectionHeader("Stroke"),
            SwatchGrid(StrokeBrushes()),

            // ── Background / Surface brushes ────────────────────────
            SectionHeader("Background"),
            SwatchGrid(BackgroundBrushes()),

            // ── Signal / Status brushes ─────────────────────────────
            SectionHeader("Signal"),
            SwatchGrid(SignalBrushes())

        ).Margin(24, 16, 24, 24);
    }

    // ── Section header ──────────────────────────────────────────────

    private static Element SectionHeader(string title) =>
        (TextBlock(title) with { FontSize = 18 })
            .SemiBold()
            .Foreground(Theme.PrimaryText)
            .Margin(0, 12, 0, 4);

    // ── Swatch grid: 3-column grid of brush swatches ────────────────

    private static Element SwatchGrid(BrushInfo[] brushes)
    {
        var rows = new List<Element>();
        for (int i = 0; i < brushes.Length; i += 3)
        {
            var cols = new List<Element>();
            for (int j = i; j < Math.Min(i + 3, brushes.Length); j++)
                cols.Add(BrushSwatch(brushes[j]));

            // Pad to 3 columns
            while (cols.Count < 3)
                cols.Add(Empty());

            rows.Add(HStack(12, cols.ToArray()));
        }
        return VStack(8, rows.ToArray());
    }

    // ── Individual brush swatch ─────────────────────────────────────

    private static Element BrushSwatch(BrushInfo info)
    {
        var swatch = (Border(Empty()) with { CornerRadius = 4 })
            .Width(32).Height(32)
            .Background(Theme.Ref(info.ResourceKey))
            .WithBorder(Theme.ControlStroke);

        return HStack(8,
            swatch.VAlign(VerticalAlignment.Center),
            VStack(0,
                (TextBlock(info.TokenName) with { FontSize = 12 })
                    .SemiBold()
                    .Foreground(Theme.PrimaryText),
                (TextBlock(info.ResourceKey) with { FontSize = 11 })
                    .Foreground(Theme.TertiaryText)
            ).VAlign(VerticalAlignment.Center)
        ).Width(300);
    }

    // ── Brush data ──────────────────────────────────────────────────

    private record BrushInfo(string TokenName, string ResourceKey);

    private static BrushInfo[] TextBrushes() =>
    [
        // Text
        new("Text / Primary",         "TextFillColorPrimaryBrush"),
        new("Text / Secondary",       "TextFillColorSecondaryBrush"),
        new("Text / Tertiary",        "TextFillColorTertiaryBrush"),
        new("Text / Disabled",        "TextFillColorDisabledBrush"),
        // Accent Text
        new("Accent Text / Primary",  "AccentTextFillColorPrimaryBrush"),
        new("Accent Text / Secondary","AccentTextFillColorSecondaryBrush"),
        new("Accent Text / Tertiary", "AccentTextFillColorTertiaryBrush"),
        new("Accent Text / Disabled", "AccentTextFillColorDisabledBrush"),
        // Text on Accent
        new("Text on Accent / Primary",      "TextOnAccentFillColorPrimaryBrush"),
        new("Text on Accent / Secondary",    "TextOnAccentFillColorSecondaryBrush"),
        new("Text on Accent / Disabled",     "TextOnAccentFillColorDisabledBrush"),
        new("Text on Accent / Selected Text","TextOnAccentFillColorSelectedTextBrush"),
    ];

    private static BrushInfo[] FillBrushes() =>
    [
        // Control Fill
        new("Control / Default",       "ControlFillColorDefaultBrush"),
        new("Control / Secondary",     "ControlFillColorSecondaryBrush"),
        new("Control / Tertiary",      "ControlFillColorTertiaryBrush"),
        new("Control / Disabled",      "ControlFillColorDisabledBrush"),
        new("Control / Transparent",   "ControlFillColorTransparentBrush"),
        new("Control / Input Active",  "ControlFillColorInputActiveBrush"),
        // Control Alt Fill
        new("Control Alt / Transparent","ControlAltFillColorTransparentBrush"),
        new("Control Alt / Secondary",  "ControlAltFillColorSecondaryBrush"),
        new("Control Alt / Tertiary",   "ControlAltFillColorTertiaryBrush"),
        new("Control Alt / Quarternary","ControlAltFillColorQuarternaryBrush"),
        new("Control Alt / Disabled",   "ControlAltFillColorDisabledBrush"),
        // Control Solid / Strong
        new("Control Solid / Default",  "ControlSolidFillColorDefaultBrush"),
        new("Control Strong / Default", "ControlStrongFillColorDefaultBrush"),
        new("Control Strong / Disabled","ControlStrongFillColorDisabledBrush"),
        // Subtle Fill
        new("Subtle / Transparent",    "SubtleFillColorTransparentBrush"),
        new("Subtle / Secondary",      "SubtleFillColorSecondaryBrush"),
        new("Subtle / Tertiary",       "SubtleFillColorTertiaryBrush"),
        new("Subtle / Disabled",       "SubtleFillColorDisabledBrush"),
        // Accent Fill
        new("Accent / Default",        "AccentFillColorDefaultBrush"),
        new("Accent / Secondary",      "AccentFillColorSecondaryBrush"),
        new("Accent / Tertiary",       "AccentFillColorTertiaryBrush"),
        new("Accent / Disabled",       "AccentFillColorDisabledBrush"),
        new("Accent / Selected Text BG","AccentFillColorSelectedTextBackgroundBrush"),
        // Control On Image
        new("On Image / Default",      "ControlOnImageFillColorDefaultBrush"),
        new("On Image / Secondary",    "ControlOnImageFillColorSecondaryBrush"),
        new("On Image / Tertiary",     "ControlOnImageFillColorTertiaryBrush"),
        new("On Image / Disabled",     "ControlOnImageFillColorDisabledBrush"),
    ];

    private static BrushInfo[] StrokeBrushes() =>
    [
        // Card Stroke
        new("Card Stroke / Default",       "CardStrokeColorDefaultBrush"),
        new("Card Stroke / Default Solid",  "CardStrokeColorDefaultSolidBrush"),
        // Control Stroke
        new("Control Stroke / Default",     "ControlStrokeColorDefaultBrush"),
        new("Control Stroke / Secondary",   "ControlStrokeColorSecondaryBrush"),
        new("Control Stroke / On Accent Default",  "ControlStrokeColorOnAccentDefaultBrush"),
        new("Control Stroke / On Accent Secondary","ControlStrokeColorOnAccentSecondaryBrush"),
        new("Control Stroke / On Accent Tertiary", "ControlStrokeColorOnAccentTertiaryBrush"),
        new("Control Stroke / On Accent Disabled", "ControlStrokeColorOnAccentDisabledBrush"),
        new("Control Stroke / For Strong On Image","ControlStrokeColorForStrongFillWhenOnImageBrush"),
        // Control Strong Stroke
        new("Control Strong Stroke / Default",  "ControlStrongStrokeColorDefaultBrush"),
        new("Control Strong Stroke / Disabled", "ControlStrongStrokeColorDisabledBrush"),
        // Surface Stroke
        new("Surface Stroke / Default",    "SurfaceStrokeColorDefaultBrush"),
        new("Surface Stroke / Flyout",     "SurfaceStrokeColorFlyoutBrush"),
        // Divider Stroke
        new("Divider Stroke / Default",    "DividerStrokeColorDefaultBrush"),
        // Focus Stroke
        new("Focus Stroke / Outer",        "FocusStrokeColorOuterBrush"),
        new("Focus Stroke / Inner",        "FocusStrokeColorInnerBrush"),
    ];

    private static BrushInfo[] BackgroundBrushes() =>
    [
        // Card Background
        new("Card BG / Default",       "CardBackgroundFillColorDefaultBrush"),
        new("Card BG / Secondary",     "CardBackgroundFillColorSecondaryBrush"),
        // Smoke
        new("Smoke / Default",         "SmokeFillColorDefaultBrush"),
        // Layer
        new("Layer / Default",         "LayerFillColorDefaultBrush"),
        new("Layer / Alt",             "LayerFillColorAltBrush"),
        new("Layer on Acrylic / Default","LayerOnAcrylicFillColorDefaultBrush"),
        // Solid Background
        new("Solid BG / Base",         "SolidBackgroundFillColorBaseBrush"),
        new("Solid BG / Base Alt",     "SolidBackgroundFillColorBaseAltBrush"),
        new("Solid BG / Secondary",    "SolidBackgroundFillColorSecondaryBrush"),
        new("Solid BG / Tertiary",     "SolidBackgroundFillColorTertiaryBrush"),
        new("Solid BG / Quarternary",  "SolidBackgroundFillColorQuarternaryBrush"),
        // Acrylic Background
        new("Acrylic BG / Base",       "AcrylicBackgroundFillColorBaseBrush"),
        new("Acrylic BG / Default",    "AcrylicBackgroundFillColorDefaultBrush"),
    ];

    private static BrushInfo[] SignalBrushes() =>
    [
        new("System / Success",            "SystemFillColorSuccessBrush"),
        new("System / Caution",            "SystemFillColorCautionBrush"),
        new("System / Critical",           "SystemFillColorCriticalBrush"),
        new("System / Attention",          "SystemFillColorAttentionBrush"),
        new("System / Neutral",            "SystemFillColorNeutralBrush"),
        new("System / Solid Neutral",      "SystemFillColorSolidNeutralBrush"),
        new("System / Success BG",         "SystemFillColorSuccessBackgroundBrush"),
        new("System / Caution BG",         "SystemFillColorCautionBackgroundBrush"),
        new("System / Critical BG",        "SystemFillColorCriticalBackgroundBrush"),
        new("System / Attention BG",       "SystemFillColorAttentionBackgroundBrush"),
        new("System / Neutral BG",         "SystemFillColorNeutralBackgroundBrush"),
        new("System / Solid Neutral BG",   "SystemFillColorSolidNeutralBackgroundBrush"),
        new("System / Solid Attention BG", "SystemFillColorSolidAttentionBackgroundBrush"),
    ];
}
