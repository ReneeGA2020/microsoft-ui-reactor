using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Minesweeper.Game;
using static Microsoft.UI.Reactor.Factories;

namespace Minesweeper.Components;

public sealed record BoardViewProps(
    Board Board,
    double CellSize,
    bool IsDarkTheme,
    (int Row, int Col)? ChordPreview,
    Action<int, int> OnReveal,
    Action<int, int> OnFlag,
    Action<int, int> OnChord,
    Action<int, int> OnBeginChordPreview,
    Action<bool> OnEndChordPreview);

/// <summary>
/// Renders the full grid of <see cref="CellComponent"/>s. Wraps it in a
/// sunken bevel and a fixed-size container, so the board's footprint matches
/// the cell count exactly. Each cell is keyed by (row, col) so the
/// reconciler can diff in place when mines/flags change.
/// </summary>
public sealed class BoardView : Component<BoardViewProps>
{
    public override Element Render()
    {
        var p = Props;
        var b = p.Board;

        // Build the cell grid as a flat list of children. Each cell sets its
        // own width/height; wrapping VStack/HStack layout would jitter on
        // odd column counts. Use a Grid with row/column tracks instead.
        var rowTracks = new GridSize[b.Rows];
        var colTracks = new GridSize[b.Columns];
        for (int i = 0; i < b.Rows; i++) rowTracks[i] = GridSize.Px(p.CellSize);
        for (int i = 0; i < b.Columns; i++) colTracks[i] = GridSize.Px(p.CellSize);

        var children = new Element[b.Rows * b.Columns];
        for (int r = 0; r < b.Rows; r++)
            for (int c = 0; c < b.Columns; c++)
            {
                var cell = b.Cells[r, c];
                var inPreview = p.ChordPreview is var cp && cp.HasValue
                    && Math.Abs(cp.Value.Row - r) <= 1
                    && Math.Abs(cp.Value.Col - c) <= 1;

                var props = new CellProps(
                    Row: r,
                    Column: c,
                    Cell: cell,
                    IsExploded: b.ExplodedAt is var ex && ex.HasValue && ex.Value.Row == r && ex.Value.Col == c,
                    IsLost: b.Phase == GamePhase.Lost,
                    IsWon: b.Phase == GamePhase.Won,
                    IsChordPreview: inPreview,
                    Size: p.CellSize,
                    IsDarkTheme: p.IsDarkTheme,
                    OnReveal: p.OnReveal,
                    OnFlag: p.OnFlag,
                    OnChord: p.OnChord,
                    OnBeginChordPreview: p.OnBeginChordPreview,
                    OnEndChordPreview: p.OnEndChordPreview);

                children[r * b.Columns + c] = Component<CellComponent, CellProps>(props)
                    .Grid(row: r, column: c)
                    .WithKey($"cell-{r}-{c}");
            }

        var grid = Grid(colTracks, rowTracks, children);

        // Frame the board with a sunken bevel. We deliberately don't set
        // an explicit Width/Height here — the inner Grid has fixed-pixel
        // row/column tracks, so it computes its own natural size, and the
        // outer Border's 1-px frame adds *outside* that. Setting Width
        // explicitly used to clip the rightmost column / bottom row by
        // 2px because BorderThickness eats into the Width slot in WinUI.
        return Border(grid)
            .Background(Theme.LayerFill)
            .WithBorder(Theme.CardStroke, 1);
    }
}

