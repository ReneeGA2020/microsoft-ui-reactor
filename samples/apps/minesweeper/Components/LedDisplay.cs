using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Text;
using static Microsoft.UI.Reactor.Factories;

namespace Minesweeper.Components;

/// <summary>
/// Three-digit "LED-style" display. Renders a fixed-width red-on-black panel
/// reminiscent of the classic mine-counter / timer readouts. Implemented with
/// a monospace font and bright-red foreground — purely functional styling, no
/// imported artwork.
/// </summary>
public static class LedDisplay
{
    public const int MaxValue = 999;
    public const int MinValue = -99;

    public static Element Render(int value, double height = 36)
    {
        // Clamp into the displayable range and format as 3 chars wide. Negative
        // values use a leading minus sign and 2 digits (matches classic look).
        var clamped = Math.Clamp(value, MinValue, MaxValue);
        var text = clamped < 0 ? $"-{(-clamped):D2}" : $"{clamped:D3}";

        // A single-character width in a typical monospace font is ~0.6 of the
        // height; pad a tiny bit for breathing room.
        var width = height * 0.6 * 3 + 12;

        return Border(
            TextBlock(text)
                .FontFamily("Cascadia Mono, Consolas, Lucida Console, Courier New")
                .FontSize(height * 0.78)
                .FontWeight(FontWeights.Bold)
                .Foreground("#FF3B30")
                .HAlign(HorizontalAlignment.Center)
                .VAlign(VerticalAlignment.Center)
        )
        .Background("#1A0000")
        .CornerRadius(2)
        .WithBorder("#400000", 1)
        .Width(width)
        .Height(height)
        .Padding(0, 0);
    }
}
