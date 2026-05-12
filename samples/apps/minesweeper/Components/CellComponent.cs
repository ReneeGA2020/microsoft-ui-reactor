using Microsoft.UI;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using Minesweeper.Game;
using static Microsoft.UI.Reactor.Factories;

namespace Minesweeper.Components;

/// <summary>
/// Visual style for a cell. Drives bevel direction, background, glyph color.
/// </summary>
public enum CellSkin
{
    Covered,        // raised bevel, awaiting a click
    CoveredPreview, // raised → flat: cell is in a chord preview box
    Revealed,       // sunken bevel, shows count or empty
    ExplodedMine,   // sunken, red background — the mine the player clicked
    RevealedMine,   // sunken, neutral — a non-clicked mine shown on loss
    WrongFlag,      // sunken, ✗ overlay — flagged but no mine on loss
}

/// <summary>
/// Props for a single cell. Records carry value equality so the reconciler
/// can skip cells whose state is unchanged between renders.
/// </summary>
public sealed record CellProps(
    int Row,
    int Column,
    Cell Cell,
    bool IsExploded,
    bool IsLost,
    bool IsWon,
    bool IsChordPreview,
    double Size,
    bool IsDarkTheme,
    Action<int, int> OnReveal,
    Action<int, int> OnFlag,
    Action<int, int> OnChord,
    Action<int, int> OnBeginChordPreview,
    Action<bool> OnEndChordPreview);

/// <summary>
/// One cell of the Minesweeper grid. Renders a raised "covered" bevel until
/// revealed, then switches to a sunken bevel showing the adjacent count, a
/// mine, or a flag/question mark. Theme-aware colors.
/// Pointer handling: left-tap reveals, right-tap on a covered cell flags,
/// long-press flags (touch). Right-press-and-hold on a revealed numbered
/// cell shows a "chord preview" highlighting the 8 neighbors; releasing
/// the right button commits the chord.
/// </summary>
public sealed class CellComponent : Component<CellProps>
{
    public override Element Render()
    {
        var p = Props;
        var skin = ComputeSkin(p);
        var (bg, glyph, glyphColor) = StyleFor(skin, p.Cell, p.IsDarkTheme);

        // Glyph: shrink for emoji-heavy skins so the bomb / flag don't get
        // clipped at the bottom (emoji baselines sit lower than digits).
        var glyphFontFactor = IsEmojiGlyph(glyph) ? 0.42 : 0.55;
        var glyphContent = string.IsNullOrEmpty(glyph)
            ? (Element)Border(TextBlock(""))                                   // invisible filler
            : ApplyForeground(
                TextBlock(glyph)
                    .FontSize(p.Size * glyphFontFactor)
                    .FontWeight(FontWeights.Bold)
                    .HAlign(HorizontalAlignment.Center)
                    .VAlign(VerticalAlignment.Center)
                    .Set(tb =>
                    {
                        tb.LineHeight = p.Size;        // pin baseline so emoji descenders don't clip
                        tb.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
                        tb.TextAlignment = TextAlignment.Center;
                        tb.Padding = new Thickness(0);
                    }),
                glyphColor);

        var bevel = BuildBevel(skin, p.Size, glyphContent, bg, p.IsDarkTheme)
            .Width(p.Size).Height(p.Size);

        // Wire the input — right-press on a revealed numbered cell shows a
        // chord preview (highlights the 8 neighbors); commit happens on
        // right-release. Right-press on a covered cell flags as usual on
        // RightTapped. Left tap reveals (or chords if double-clicked on
        // a number — see OnTapped). PointerExited cancels an in-flight
        // preview so dragging off the cell aborts the chord.
        var chordableHere = p.Cell.IsRevealed && p.Cell.AdjacentMines > 0;
        bevel = bevel
            .OnPointerPressed((sender, e) =>
            {
                if (p.IsLost || p.IsWon) return;
                var fe = sender as FrameworkElement;
                var pp = fe is null ? null : e.GetCurrentPoint(fe);
                if (pp is null) return;
                if (pp.Properties.IsRightButtonPressed && chordableHere)
                {
                    p.OnBeginChordPreview(p.Row, p.Column);
                    e.Handled = true;
                }
            })
            .OnPointerReleased((sender, e) =>
            {
                if (p.IsLost || p.IsWon) return;
                if (p.IsChordPreview)
                {
                    p.OnEndChordPreview(true);
                    e.Handled = true;
                }
            })
            .OnPointerExited((_, _) =>
            {
                if (p.IsChordPreview) p.OnEndChordPreview(false);
            })
            .OnTapped((_, e) =>
            {
                if (p.IsLost || p.IsWon) return;
                // Shift/Ctrl + left-tap on a covered cell behaves like a
                // right-click (toggles flag → question → clear). Helpful on
                // trackpads/laptops where right-click is awkward.
                var shift = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                             & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
                var ctrl = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                            & Windows.UI.Core.CoreVirtualKeyStates.Down) != 0;
                if ((shift || ctrl) && !p.Cell.IsRevealed)
                {
                    p.OnFlag(p.Row, p.Column);
                    e.Handled = true;
                    return;
                }
                if (chordableHere)
                    p.OnChord(p.Row, p.Column);                // double-click chord shortcut
                else
                    p.OnReveal(p.Row, p.Column);
                e.Handled = true;
            })
            .OnRightTapped((_, e) =>
            {
                if (p.IsLost || p.IsWon) return;
                // RightTapped fires after PointerReleased. If we just resolved
                // a chord on a revealed numbered cell, swallow it; otherwise
                // toggle a flag on a covered cell.
                if (chordableHere) { e.Handled = true; return; }
                if (!p.Cell.IsRevealed) p.OnFlag(p.Row, p.Column);
                e.Handled = true;
            })
            .AutomationName(BuildAutomationName(p));

        // Touch / pen ergonomics: long-press also flags, on covered cells only.
        if (skin == CellSkin.Covered)
            bevel = bevel.OnLongPress(_ => p.OnFlag(p.Row, p.Column), enableMouseEmulation: false);

        // Subtle compositor-level press feedback for live cells (no reconcile).
        if (skin == CellSkin.Covered)
            bevel = bevel.InteractionStates(s => s
                .PointerOver(opacity: 0.94f)
                .Pressed(scale: 0.95f, opacity: 0.85f));

        return bevel;
    }

    static bool IsEmojiGlyph(string s) => s.Length > 0 && char.IsSurrogate(s, 0);

    static CellSkin ComputeSkin(CellProps p)
    {
        var c = p.Cell;
        if (p.IsLost)
        {
            if (c.IsMine && p.IsExploded) return CellSkin.ExplodedMine;
            if (c.IsMine && c.IsRevealed && c.Mark != CellMark.Flag) return CellSkin.RevealedMine;
            if (!c.IsMine && c.Mark == CellMark.Flag) return CellSkin.WrongFlag;
        }
        if (c.IsRevealed) return CellSkin.Revealed;
        // Covered cells inside a chord-preview box flatten so the player can
        // see exactly which neighbors will be revealed if they release the chord.
        return p.IsChordPreview && c.Mark != CellMark.Flag
            ? CellSkin.CoveredPreview
            : CellSkin.Covered;
    }

    /// <summary>
    /// Background color, glyph string, and glyph color (one each per skin).
    /// Numbers use the classic palette; the dark-mode variants are tuned for
    /// WCAG-AA contrast against the revealed-cell background.
    /// </summary>
    static (object Background, string Glyph, object GlyphColor) StyleFor(CellSkin skin, Cell c, bool isDark)
    {
        string num(int n) => isDark
            ? n switch
            {
                1 => "#64B5F6", 2 => "#81C784", 3 => "#E57373",
                4 => "#7986CB", 5 => "#BA68C8", 6 => "#4DD0E1",
                7 => "#EEEEEE", _ => "#BDBDBD",
            }
            : n switch
            {
                1 => "#1976D2", 2 => "#2E7D32", 3 => "#D32F2F",
                4 => "#0D47A1", 5 => "#7B1FA2", 6 => "#00838F",
                7 => "#212121", _ => "#616161",
            };

        switch (skin)
        {
            case CellSkin.ExplodedMine:
                return ("#D32F2F", "💣", "#FFFFFF");
            case CellSkin.RevealedMine:
                return ((object)Theme.SubtleFill, "💣", (object)Theme.PrimaryText);
            case CellSkin.WrongFlag:
                return ((object)Theme.SubtleFill, "✗", "#D32F2F");
            case CellSkin.Revealed:
                if (c.AdjacentMines > 0)
                    return ((object)Theme.LayerFill, c.AdjacentMines.ToString(), (object)num(c.AdjacentMines));
                return ((object)Theme.LayerFill, "", (object)Theme.PrimaryText);
            case CellSkin.CoveredPreview:
                // Same glyph as Covered but flat background — matches what the
                // cell would look like immediately after release-revealing.
                var pglyph = c.Mark switch
                {
                    CellMark.Flag => "🚩",
                    CellMark.Question => "?",
                    _ => "",
                };
                var pcol = c.Mark == CellMark.Question ? (object)Theme.AccentText : (object)Theme.PrimaryText;
                return ((object)Theme.LayerFill, pglyph, pcol);
            default: // Covered
                var glyph = c.Mark switch
                {
                    CellMark.Flag => "🚩",
                    CellMark.Question => "?",
                    _ => "",
                };
                var col = c.Mark == CellMark.Question ? (object)Theme.AccentText : (object)Theme.PrimaryText;
                return ((object)Theme.ControlFill, glyph, col);
        }
    }

    /// <summary>
    /// Two-tone bevel: covered cells get a raised look (light top/left + dark
    /// bottom/right). Revealed and preview cells get a thin neutral stroke.
    /// Dark mode adjusts highlight/shadow opacity so the bevel still reads.
    /// </summary>
    static Element BuildBevel(CellSkin skin, double size, Element content, object background, bool isDark)
    {
        var inner = WithBackground(Border(content), background);

        if (skin != CellSkin.Covered)
            return inner.WithBorder(Theme.CardStroke, 1);

        // Raised bevel — two stacked Borders. The outer paints the highlight on
        // top/left, the inner paints the shadow on bottom/right (as a 2px
        // border that also frames the content).
        var highlightAlpha = isDark ? (byte)180 : (byte)220;
        var shadowAlpha = isDark ? (byte)160 : (byte)90;
        var highlight = new SolidColorBrush(ColorHelper.FromArgb(highlightAlpha, 255, 255, 255));
        var shadow = new SolidColorBrush(ColorHelper.FromArgb(shadowAlpha, 0, 0, 0));

        var withShadow = inner.Set(b =>
        {
            b.BorderThickness = new Thickness(0, 0, 2, 2);
            b.BorderBrush = shadow;
        });

        return Border(withShadow).Set(b =>
        {
            b.BorderThickness = new Thickness(2, 2, 0, 0);
            b.BorderBrush = highlight;
        });
    }

    static BorderElement WithBackground(BorderElement el, object bg) => bg switch
    {
        ThemeRef tr => el.Background(tr),
        string hex => el.Background(hex),
        _ => el,
    };

    static TextBlockElement ApplyForeground(TextBlockElement el, object fg) => fg switch
    {
        ThemeRef tr => el.Foreground(tr),
        string hex => el.Foreground(hex),
        _ => el,
    };

    static string BuildAutomationName(CellProps p)
    {
        var c = p.Cell;
        if (c.IsRevealed)
        {
            if (c.IsMine) return $"Row {p.Row + 1}, Column {p.Column + 1}, mine";
            if (c.AdjacentMines == 0) return $"Row {p.Row + 1}, Column {p.Column + 1}, empty";
            return $"Row {p.Row + 1}, Column {p.Column + 1}, {c.AdjacentMines} adjacent mines";
        }
        return c.Mark switch
        {
            CellMark.Flag => $"Row {p.Row + 1}, Column {p.Column + 1}, flagged",
            CellMark.Question => $"Row {p.Row + 1}, Column {p.Column + 1}, question marked",
            _ => $"Row {p.Row + 1}, Column {p.Column + 1}, hidden",
        };
    }
}
