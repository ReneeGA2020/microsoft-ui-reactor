using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Minesweeper.Game;
using static Microsoft.UI.Reactor.Factories;

namespace Minesweeper.Components;

/// <summary>
/// The classic three-segment status bar at the top of a Minesweeper game:
/// mines remaining (left LED) — smiley reset button (center) — elapsed
/// seconds (right LED). The whole strip sits inside its own sunken bevel.
/// </summary>
public static class StatusPanel
{
    public static Element Render(
        int minesRemaining,
        int elapsedSeconds,
        GamePhase phase,
        bool isPressing,
        Action onReset)
    {
        var inner = Grid(
            columns: [GridSize.Auto, GridSize.Star(), GridSize.Auto],
            rows: [GridSize.Auto],

            LedDisplay.Render(minesRemaining)
                .Grid(row: 0, column: 0)
                .HAlign(HorizontalAlignment.Left)
                .VAlign(VerticalAlignment.Center)
                .AutomationName($"Mines remaining: {minesRemaining}")
                .LiveRegion(Microsoft.UI.Xaml.Automation.Peers.AutomationLiveSetting.Polite),

            SmileyButton.Render(phase, isPressing, onReset)
                .Grid(row: 0, column: 1)
                .HAlign(HorizontalAlignment.Center)
                .VAlign(VerticalAlignment.Center),

            LedDisplay.Render(elapsedSeconds)
                .Grid(row: 0, column: 2)
                .HAlign(HorizontalAlignment.Right)
                .VAlign(VerticalAlignment.Center)
                .AutomationName($"Elapsed seconds: {elapsedSeconds}")
                .LiveRegion(Microsoft.UI.Xaml.Automation.Peers.AutomationLiveSetting.Polite)
        ).Padding(8);

        return Border(inner)
            .Background(Theme.LayerFill)
            .CornerRadius(6)
            .WithBorder(Theme.CardStroke, 1);
    }
}
