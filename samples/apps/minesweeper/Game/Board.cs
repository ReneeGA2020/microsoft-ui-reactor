namespace Minesweeper.Game;

/// <summary>
/// What's underneath a cell. Hidden cells display as covered until revealed.
/// </summary>
public enum CellMark
{
    None,
    Flag,
    Question,
}

/// <summary>
/// A single grid cell. Records the underlying mine state and the player's
/// visible state. Adjacent count is precomputed at mine-placement time.
/// </summary>
public sealed record Cell(
    bool IsMine,
    int AdjacentMines,
    bool IsRevealed,
    CellMark Mark)
{
    public static readonly Cell EmptyHidden = new(false, 0, false, CellMark.None);
}

/// <summary>
/// Phase the game is in. Reducer transitions: NotStarted → Playing on first reveal,
/// Playing → Won when all non-mine cells revealed, Playing → Lost when a mine clicked.
/// </summary>
public enum GamePhase
{
    NotStarted,
    Playing,
    Won,
    Lost,
}

/// <summary>
/// Immutable game state. The reducer (<see cref="BoardReducer"/>) returns a new
/// instance for every action. Storing it as a single record makes UseReducer
/// trivial and the whole thing trivially testable.
/// </summary>
public sealed record Board
{
    public Difficulty Difficulty { get; init; }
    public Cell[,] Cells { get; init; }
    public GamePhase Phase { get; init; }

    /// <summary>(row, col) of the mine the player clicked, or null if not Lost.</summary>
    public (int Row, int Col)? ExplodedAt { get; init; }

    /// <summary>
    /// Reveal-counter so reducer actions stay O(1) for win checks. Tracks how
    /// many non-mine cells the player has uncovered.
    /// </summary>
    public int RevealedSafeCount { get; init; }

    /// <summary>
    /// How many cells the player has marked with a flag. Used for the
    /// classic "mines remaining" display (mines − flags).
    /// </summary>
    public int FlagCount { get; init; }

    public int Rows => Difficulty.Rows;
    public int Columns => Difficulty.Columns;
    public int MinesRemaining => Difficulty.MineCount - FlagCount;
    public int TotalSafeCells => Rows * Columns - Difficulty.MineCount;

    private Board(
        Difficulty difficulty,
        Cell[,] cells,
        GamePhase phase,
        (int Row, int Col)? explodedAt,
        int revealedSafeCount,
        int flagCount)
    {
        Difficulty = difficulty;
        Cells = cells;
        Phase = phase;
        ExplodedAt = explodedAt;
        RevealedSafeCount = revealedSafeCount;
        FlagCount = flagCount;
    }

    /// <summary>
    /// Creates a fresh board for the given difficulty. Mines are NOT placed yet —
    /// they're placed on the first reveal so the click is guaranteed safe.
    /// </summary>
    public static Board NewGame(Difficulty difficulty)
    {
        var cells = new Cell[difficulty.Rows, difficulty.Columns];
        for (int r = 0; r < difficulty.Rows; r++)
            for (int c = 0; c < difficulty.Columns; c++)
                cells[r, c] = Cell.EmptyHidden;

        return new Board(
            difficulty,
            cells,
            GamePhase.NotStarted,
            explodedAt: null,
            revealedSafeCount: 0,
            flagCount: 0);
    }

    internal Board With(
        Cell[,]? cells = null,
        GamePhase? phase = null,
        (int Row, int Col)? explodedAt = null,
        int? revealedSafeCount = null,
        int? flagCount = null,
        bool clearExplodedAt = false) =>
        new(
            Difficulty,
            cells ?? Cells,
            phase ?? Phase,
            clearExplodedAt ? null : (explodedAt ?? ExplodedAt),
            revealedSafeCount ?? RevealedSafeCount,
            flagCount ?? FlagCount);

    public bool InBounds(int r, int c) =>
        r >= 0 && r < Rows && c >= 0 && c < Columns;
}

/// <summary>
/// Pure functions that evolve a <see cref="Board"/>. Every method returns a new
/// Board (or the same instance if the action is a no-op). Suitable for use as
/// a UseReducer reducer; trivially unit-testable with no UI dependencies.
/// </summary>
public static class BoardReducer
{
    /// <summary>
    /// Reveals the cell at (row, col). On the very first reveal, mines are
    /// placed using <paramref name="rng"/> with the guarantee that neither the
    /// clicked cell nor any of its 8 neighbors will contain a mine. Cascades
    /// reveals through empty (zero-adjacent) regions. If a mine is hit, the
    /// game enters the Lost phase and every mine is uncovered (wrong flags
    /// remain flagged so the UI can mark them with an ✗).
    /// </summary>
    public static Board Reveal(Board board, int row, int col, Random rng)
    {
        if (board.Phase is GamePhase.Won or GamePhase.Lost) return board;
        if (!board.InBounds(row, col)) return board;

        var cell = board.Cells[row, col];
        if (cell.IsRevealed) return board;
        if (cell.Mark == CellMark.Flag) return board;

        // First-click rule: lay mines now, avoiding the click + 8 neighbors.
        if (board.Phase == GamePhase.NotStarted)
            board = PlaceMinesAvoiding(board, row, col, rng);

        cell = board.Cells[row, col]; // re-fetch after mine placement

        if (cell.IsMine)
            return RevealAllMines(board, row, col);

        return CascadeReveal(board, row, col);
    }

    /// <summary>
    /// Cycles the mark on a hidden cell: None → Flag → Question → None.
    /// Right-click semantics. Revealed cells are not affected.
    /// </summary>
    public static Board ToggleFlag(Board board, int row, int col)
    {
        if (board.Phase is GamePhase.Won or GamePhase.Lost) return board;
        if (!board.InBounds(row, col)) return board;

        var cell = board.Cells[row, col];
        if (cell.IsRevealed) return board;

        var nextMark = cell.Mark switch
        {
            CellMark.None => CellMark.Flag,
            CellMark.Flag => CellMark.Question,
            _ => CellMark.None,
        };

        var flagDelta = (nextMark == CellMark.Flag ? 1 : 0)
                      - (cell.Mark == CellMark.Flag ? 1 : 0);

        var cells = (Cell[,])board.Cells.Clone();
        cells[row, col] = cell with { Mark = nextMark };

        var phase = board.Phase == GamePhase.NotStarted ? GamePhase.NotStarted : board.Phase;

        return board.With(cells: cells, phase: phase, flagCount: board.FlagCount + flagDelta);
    }

    /// <summary>
    /// "Chord" reveal: if (row, col) is a revealed numbered cell whose neighboring
    /// flag count equals its number, reveal all neighboring un-flagged cells.
    /// If any of those neighbors is an un-flagged mine the player just lost.
    /// </summary>
    public static Board Chord(Board board, int row, int col, Random rng)
    {
        if (board.Phase != GamePhase.Playing) return board;
        if (!board.InBounds(row, col)) return board;

        var cell = board.Cells[row, col];
        if (!cell.IsRevealed || cell.IsMine || cell.AdjacentMines == 0) return board;

        int adjacentFlags = 0;
        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int nr = row + dr, nc = col + dc;
                if (!board.InBounds(nr, nc)) continue;
                if (board.Cells[nr, nc].Mark == CellMark.Flag) adjacentFlags++;
            }

        if (adjacentFlags != cell.AdjacentMines) return board;

        // Reveal each non-flagged neighbor in turn.
        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int nr = row + dr, nc = col + dc;
                if (!board.InBounds(nr, nc)) continue;
                var nb = board.Cells[nr, nc];
                if (nb.IsRevealed || nb.Mark == CellMark.Flag) continue;
                board = Reveal(board, nr, nc, rng);
                if (board.Phase == GamePhase.Lost) return board;
            }

        return board;
    }

    // ───── internals ────────────────────────────────────────────────────────

    private static Board PlaceMinesAvoiding(Board board, int safeRow, int safeCol, Random rng)
    {
        int total = board.Rows * board.Columns;
        var forbidden = new HashSet<int>();
        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                int r = safeRow + dr, c = safeCol + dc;
                if (board.InBounds(r, c)) forbidden.Add(r * board.Columns + c);
            }

        // Reservoir-style: pick MineCount distinct positions outside `forbidden`.
        var allCandidates = new List<int>(total - forbidden.Count);
        for (int i = 0; i < total; i++)
            if (!forbidden.Contains(i)) allCandidates.Add(i);

        // Fisher-Yates partial shuffle: take the first MineCount picks.
        int picks = Math.Min(board.Difficulty.MineCount, allCandidates.Count);
        for (int i = 0; i < picks; i++)
        {
            int j = rng.Next(i, allCandidates.Count);
            (allCandidates[i], allCandidates[j]) = (allCandidates[j], allCandidates[i]);
        }

        var cells = new Cell[board.Rows, board.Columns];
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Columns; c++)
                cells[r, c] = Cell.EmptyHidden;

        for (int i = 0; i < picks; i++)
        {
            int idx = allCandidates[i];
            int r = idx / board.Columns;
            int c = idx % board.Columns;
            cells[r, c] = cells[r, c] with { IsMine = true };
        }

        // Precompute adjacency counts for non-mine cells.
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Columns; c++)
            {
                if (cells[r, c].IsMine) continue;
                int count = 0;
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int nr = r + dr, nc = c + dc;
                        if (board.InBounds(nr, nc) && cells[nr, nc].IsMine) count++;
                    }
                cells[r, c] = cells[r, c] with { AdjacentMines = count };
            }

        return board.With(cells: cells, phase: GamePhase.Playing);
    }

    private static Board CascadeReveal(Board board, int startRow, int startCol)
    {
        var cells = (Cell[,])board.Cells.Clone();
        int newReveals = 0;

        // Iterative BFS to avoid stack overflow on large empty regions.
        var queue = new Queue<(int r, int c)>();
        queue.Enqueue((startRow, startCol));

        while (queue.Count > 0)
        {
            var (r, c) = queue.Dequeue();
            if (!board.InBounds(r, c)) continue;
            var cell = cells[r, c];
            if (cell.IsRevealed) continue;
            if (cell.Mark == CellMark.Flag) continue;
            if (cell.IsMine) continue; // safety net — shouldn't happen on cascade

            cells[r, c] = cell with { IsRevealed = true, Mark = CellMark.None };
            newReveals++;

            // Empty cell — enqueue its 8 neighbors so the flood fill expands.
            if (cell.AdjacentMines == 0)
            {
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        queue.Enqueue((r + dr, c + dc));
                    }
            }
        }

        var newRevealed = board.RevealedSafeCount + newReveals;
        var phase = newRevealed >= board.TotalSafeCells ? GamePhase.Won : GamePhase.Playing;

        // On win, auto-flag every remaining mine — matches classic behavior so
        // the mines-remaining display reads 0.
        int newFlagCount = board.FlagCount;
        if (phase == GamePhase.Won)
        {
            for (int r = 0; r < board.Rows; r++)
                for (int c = 0; c < board.Columns; c++)
                {
                    var k = cells[r, c];
                    if (k.IsMine && k.Mark != CellMark.Flag)
                    {
                        if (k.Mark == CellMark.Flag) continue;
                        cells[r, c] = k with { Mark = CellMark.Flag };
                        newFlagCount++;
                    }
                }
        }

        return board.With(
            cells: cells,
            phase: phase,
            revealedSafeCount: newRevealed,
            flagCount: newFlagCount);
    }

    private static Board RevealAllMines(Board board, int explodedRow, int explodedCol)
    {
        var cells = (Cell[,])board.Cells.Clone();
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Columns; c++)
            {
                var k = cells[r, c];
                if (k.IsMine && k.Mark != CellMark.Flag)
                    cells[r, c] = k with { IsRevealed = true };
            }

        return board.With(
            cells: cells,
            phase: GamePhase.Lost,
            explodedAt: (explodedRow, explodedCol));
    }
}
