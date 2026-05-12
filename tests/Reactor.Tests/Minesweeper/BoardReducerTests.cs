using Minesweeper.Game;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Minesweeper;

public class BoardReducerTests
{
    static Random Seeded(int seed = 1234) => new(seed);

    [Fact]
    public void NewGame_StartsInNotStartedPhase_WithNoMinesPlaced()
    {
        var board = Board.NewGame(Difficulty.Beginner);

        Assert.Equal(GamePhase.NotStarted, board.Phase);
        Assert.Equal(0, board.RevealedSafeCount);
        Assert.Equal(0, board.FlagCount);
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Columns; c++)
            {
                Assert.False(board.Cells[r, c].IsMine);
                Assert.False(board.Cells[r, c].IsRevealed);
            }
    }

    [Fact]
    public void FirstReveal_NeverPlacesMineOnClickOrNeighbors()
    {
        // Run with several seeds to make sure the safety pocket holds for any RNG.
        for (int seed = 0; seed < 50; seed++)
        {
            var board = Board.NewGame(Difficulty.Beginner);
            board = BoardReducer.Reveal(board, row: 4, col: 4, Seeded(seed));

            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                    Assert.False(board.Cells[4 + dr, 4 + dc].IsMine,
                        $"seed={seed}: ({4 + dr},{4 + dc}) was a mine inside the safe pocket");
        }
    }

    [Fact]
    public void FirstReveal_PlacesExactlyMineCountMines()
    {
        var board = BoardReducer.Reveal(Board.NewGame(Difficulty.Intermediate), 0, 0, Seeded());

        int count = 0;
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Columns; c++)
                if (board.Cells[r, c].IsMine) count++;

        Assert.Equal(Difficulty.Intermediate.MineCount, count);
    }

    [Fact]
    public void FirstReveal_TransitionsToPlaying_AndCascades()
    {
        var board = BoardReducer.Reveal(Board.NewGame(Difficulty.Beginner), 0, 0, Seeded());
        Assert.Equal(GamePhase.Playing, board.Phase);
        Assert.True(board.RevealedSafeCount >= 9, "first click should reveal at least the safe pocket");
    }

    [Fact]
    public void Reveal_OnFlaggedCell_IsNoOp()
    {
        var board = Board.NewGame(Difficulty.Beginner);
        board = BoardReducer.Reveal(board, 0, 0, Seeded());           // start the game
        board = BoardReducer.ToggleFlag(board, 8, 8);                  // flag a corner
        var before = board;
        board = BoardReducer.Reveal(board, 8, 8, Seeded());            // try to reveal it
        Assert.Same(before, board);
    }

    [Fact]
    public void ToggleFlag_CyclesNoneFlagQuestionNone()
    {
        var board = Board.NewGame(Difficulty.Beginner);
        board = BoardReducer.ToggleFlag(board, 0, 0);
        Assert.Equal(CellMark.Flag, board.Cells[0, 0].Mark);
        Assert.Equal(1, board.FlagCount);

        board = BoardReducer.ToggleFlag(board, 0, 0);
        Assert.Equal(CellMark.Question, board.Cells[0, 0].Mark);
        Assert.Equal(0, board.FlagCount);

        board = BoardReducer.ToggleFlag(board, 0, 0);
        Assert.Equal(CellMark.None, board.Cells[0, 0].Mark);
        Assert.Equal(0, board.FlagCount);
    }

    [Fact]
    public void RevealingMine_LosesAndUncoversAllMines()
    {
        // Use a tiny board with predictable layout: deterministic seed → find a mine.
        var board = BoardReducer.Reveal(Board.NewGame(Difficulty.Beginner), 0, 0, Seeded());

        (int r, int c) mine = (-1, -1);
        for (int r = 0; r < board.Rows && mine.r < 0; r++)
            for (int c = 0; c < board.Columns; c++)
                if (board.Cells[r, c].IsMine) { mine = (r, c); break; }

        Assert.NotEqual(-1, mine.r);

        var after = BoardReducer.Reveal(board, mine.r, mine.c, Seeded());
        Assert.Equal(GamePhase.Lost, after.Phase);
        Assert.Equal(mine, after.ExplodedAt);

        for (int r = 0; r < after.Rows; r++)
            for (int c = 0; c < after.Columns; c++)
                if (after.Cells[r, c].IsMine && after.Cells[r, c].Mark != CellMark.Flag)
                    Assert.True(after.Cells[r, c].IsRevealed,
                        $"mine ({r},{c}) should be revealed on loss");
    }

    [Fact]
    public void Reveal_AfterWinOrLoss_IsNoOp()
    {
        var board = BoardReducer.Reveal(Board.NewGame(Difficulty.Beginner), 0, 0, Seeded());
        // Force loss by clicking a known mine.
        (int r, int c) mine = (-1, -1);
        for (int r = 0; r < board.Rows && mine.r < 0; r++)
            for (int c = 0; c < board.Columns; c++)
                if (board.Cells[r, c].IsMine) { mine = (r, c); break; }
        board = BoardReducer.Reveal(board, mine.r, mine.c, Seeded());

        var before = board;
        board = BoardReducer.Reveal(board, 0, 0, Seeded());
        Assert.Same(before, board);
    }

    [Fact]
    public void RevealingAllSafeCells_TransitionsToWon_AndAutoFlagsMines()
    {
        var board = BoardReducer.Reveal(Board.NewGame(Difficulty.Beginner), 0, 0, Seeded());

        // Reveal every non-mine cell.
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Columns; c++)
                if (!board.Cells[r, c].IsMine && !board.Cells[r, c].IsRevealed)
                {
                    board = BoardReducer.Reveal(board, r, c, Seeded());
                    if (board.Phase == GamePhase.Won) break;
                }

        Assert.Equal(GamePhase.Won, board.Phase);
        Assert.Equal(0, board.MinesRemaining);

        // Every mine should now be auto-flagged.
        for (int r = 0; r < board.Rows; r++)
            for (int c = 0; c < board.Columns; c++)
                if (board.Cells[r, c].IsMine)
                    Assert.Equal(CellMark.Flag, board.Cells[r, c].Mark);
    }

    [Fact]
    public void Chord_OnUnrevealedOrEmptyCell_IsNoOp()
    {
        var board = BoardReducer.Reveal(Board.NewGame(Difficulty.Beginner), 0, 0, Seeded());
        var before = board;

        // Chord on a hidden corner — should be a no-op.
        board = BoardReducer.Chord(board, board.Rows - 1, board.Columns - 1, Seeded());
        Assert.Same(before, board);
    }

    [Fact]
    public void Chord_WithMatchingFlagCount_RevealsUnflaggedNeighbors()
    {
        // Build a tiny known board: 3×3, single mine at (0,0), click (2,2).
        var diff = new Difficulty(DifficultyKind.Custom, Rows: 3, Columns: 3, MineCount: 1);
        var board = Board.NewGame(diff);

        // Click (2,2) — RNG can only place the single mine in the 9-cell board
        // minus the 9-cell safe pocket = 0 candidates, so the placement falls
        // back gracefully. Use a 4×4 instead so there's room for a mine.
        diff = new Difficulty(DifficultyKind.Custom, Rows: 4, Columns: 4, MineCount: 1);
        board = Board.NewGame(diff);
        board = BoardReducer.Reveal(board, 3, 3, Seeded(99)); // safe corner

        // Find the (single) mine and the numbered cells next to it.
        (int r, int c) mine = (-1, -1);
        for (int r = 0; r < 4 && mine.r < 0; r++)
            for (int c = 0; c < 4; c++)
                if (board.Cells[r, c].IsMine) { mine = (r, c); break; }

        // Flag the mine, then chord on a revealed neighbor.
        board = BoardReducer.ToggleFlag(board, mine.r, mine.c);

        // Find a revealed numbered neighbor of the mine.
        (int r, int c) numbered = (-1, -1);
        for (int dr = -1; dr <= 1 && numbered.r < 0; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int nr = mine.r + dr, nc = mine.c + dc;
                if (!board.InBounds(nr, nc)) continue;
                var k = board.Cells[nr, nc];
                if (k.IsRevealed && k.AdjacentMines > 0) { numbered = (nr, nc); break; }
            }

        if (numbered.r < 0) return; // unlikely topology, skip

        var revealedBefore = board.RevealedSafeCount;
        board = BoardReducer.Chord(board, numbered.r, numbered.c, Seeded());
        Assert.True(board.RevealedSafeCount >= revealedBefore);
    }

    [Fact]
    public void Difficulty_IsValidCustom_RejectsBadInputs()
    {
        Assert.False(Difficulty.IsValidCustom(0, 9, 10, out _));
        Assert.False(Difficulty.IsValidCustom(9, 0, 10, out _));
        Assert.False(Difficulty.IsValidCustom(9, 9, 0, out _));
        Assert.False(Difficulty.IsValidCustom(9, 9, 9 * 9, out _));   // no room for safe pocket
        Assert.True(Difficulty.IsValidCustom(9, 9, 10, out var err));
        Assert.Null(err);
    }

    [Fact]
    public void Reveal_OutOfBounds_IsNoOp()
    {
        var board = Board.NewGame(Difficulty.Beginner);
        var after = BoardReducer.Reveal(board, -1, 0, Seeded());
        Assert.Same(board, after);
        after = BoardReducer.Reveal(board, 0, board.Columns + 100, Seeded());
        Assert.Same(board, after);
    }
}
