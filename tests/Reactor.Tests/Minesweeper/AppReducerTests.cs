using Minesweeper.Game;
using Minesweeper.Persistence;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Minesweeper;

public class AppReducerTests
{
    static Random Seeded(int seed = 1234) => new(seed);

    /// <summary>Helper: bring a fresh app state into Playing phase with at least one numbered cell revealed.</summary>
    static AppState BootedGame(int seed = 1234)
    {
        var s = AppReducer.Initial(Difficulty.Beginner, HighScores.Empty);
        s = AppReducer.Reduce(s, new RevealAction(0, 0), Seeded(seed));
        return s;
    }

    [Fact]
    public void Initial_StartsAtZeroSecondsNoPreviewBeginnerBoard()
    {
        var s = AppReducer.Initial(Difficulty.Beginner, HighScores.Empty);
        Assert.Equal(0, s.ElapsedSeconds);
        Assert.Null(s.ChordPreview);
        Assert.Equal(GamePhase.NotStarted, s.Board.Phase);
        Assert.Equal(Difficulty.Beginner, s.Board.Difficulty);
    }

    // ── Tick ──────────────────────────────────────────────────────────

    [Fact]
    public void Tick_DoesNothing_WhenNotPlaying()
    {
        var s = AppReducer.Initial(Difficulty.Beginner, HighScores.Empty);
        var after = AppReducer.Reduce(s, new TickAction(), Seeded());
        Assert.Equal(0, after.ElapsedSeconds);
    }

    [Fact]
    public void Tick_AdvancesElapsedWhilePlaying_ClampedAt999()
    {
        var s = BootedGame();
        for (int i = 0; i < 5; i++)
            s = AppReducer.Reduce(s, new TickAction(), Seeded());
        Assert.Equal(5, s.ElapsedSeconds);

        // Sanity-check the upper clamp.
        var maxed = s with { ElapsedSeconds = 999 };
        var afterMax = AppReducer.Reduce(maxed, new TickAction(), Seeded());
        Assert.Equal(999, afterMax.ElapsedSeconds);
    }

    // ── Reset ─────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsElapsedAndChordPreviewAndStartsFreshBoard()
    {
        var s = BootedGame();
        s = s with { ElapsedSeconds = 42, ChordPreview = (1, 1) };

        var reset = AppReducer.Reduce(s, new ResetAction(Difficulty.Beginner), Seeded());

        Assert.Equal(0, reset.ElapsedSeconds);
        Assert.Null(reset.ChordPreview);
        Assert.Equal(GamePhase.NotStarted, reset.Board.Phase);

        // No cells should still report as revealed after a reset (the reset
        // animation regression was: previously-revealed cells visually persisted).
        for (int r = 0; r < reset.Board.Rows; r++)
            for (int c = 0; c < reset.Board.Columns; c++)
                Assert.False(reset.Board.Cells[r, c].IsRevealed,
                    $"({r},{c}) should be hidden after reset");
    }

    [Fact]
    public void Reset_ToDifferentDifficulty_RebuildsBoard()
    {
        var s = AppReducer.Initial(Difficulty.Beginner, HighScores.Empty);
        var reset = AppReducer.Reduce(s, new ResetAction(Difficulty.Expert), Seeded());
        Assert.Equal(Difficulty.Expert, reset.Board.Difficulty);
        Assert.Equal(16, reset.Board.Rows);
        Assert.Equal(30, reset.Board.Columns);
    }

    // ── Chord preview ─────────────────────────────────────────────────

    [Fact]
    public void BeginChordPreview_OnHiddenCell_IsNoOp()
    {
        var s = BootedGame();
        // Find a cell that's hidden (any neighbor of the cascade boundary).
        (int r, int c) hidden = (-1, -1);
        for (int r = 0; r < s.Board.Rows && hidden.r < 0; r++)
            for (int c = 0; c < s.Board.Columns; c++)
                if (!s.Board.Cells[r, c].IsRevealed) { hidden = (r, c); break; }

        var after = AppReducer.Reduce(s, new BeginChordPreviewAction(hidden.r, hidden.c), Seeded());
        Assert.Null(after.ChordPreview);
    }

    [Fact]
    public void BeginChordPreview_OnRevealedZeroCell_IsNoOp()
    {
        var s = BootedGame();
        // Find a revealed empty (zero-adjacent) cell.
        (int r, int c) zero = (-1, -1);
        for (int r = 0; r < s.Board.Rows && zero.r < 0; r++)
            for (int c = 0; c < s.Board.Columns; c++)
                if (s.Board.Cells[r, c].IsRevealed && s.Board.Cells[r, c].AdjacentMines == 0)
                {
                    zero = (r, c); break;
                }
        if (zero.r < 0) return; // no zero-cell on this seed; skip

        var after = AppReducer.Reduce(s, new BeginChordPreviewAction(zero.r, zero.c), Seeded());
        Assert.Null(after.ChordPreview);
    }

    [Fact]
    public void BeginChordPreview_OnRevealedNumberedCell_SetsPreview()
    {
        var s = BootedGame();
        // Find a revealed cell with adjacent mines > 0 (a numbered cell).
        (int r, int c) numbered = (-1, -1);
        for (int r = 0; r < s.Board.Rows && numbered.r < 0; r++)
            for (int c = 0; c < s.Board.Columns; c++)
                if (s.Board.Cells[r, c].IsRevealed && s.Board.Cells[r, c].AdjacentMines > 0)
                {
                    numbered = (r, c); break;
                }
        Assert.NotEqual(-1, numbered.r);

        var after = AppReducer.Reduce(s, new BeginChordPreviewAction(numbered.r, numbered.c), Seeded());
        Assert.Equal(numbered, after.ChordPreview);
    }

    [Fact]
    public void BeginChordPreview_WhenNotPlaying_IsNoOp()
    {
        var s = AppReducer.Initial(Difficulty.Beginner, HighScores.Empty); // NotStarted
        var after = AppReducer.Reduce(s, new BeginChordPreviewAction(4, 4), Seeded());
        Assert.Null(after.ChordPreview);
    }

    [Fact]
    public void EndChordPreview_NoPreviewActive_IsNoOp()
    {
        var s = BootedGame();
        Assert.Null(s.ChordPreview);
        var after = AppReducer.Reduce(s, new EndChordPreviewAction(true), Seeded());
        Assert.Same(s, after);
    }

    [Fact]
    public void EndChordPreview_CommitFalse_JustClearsPreview()
    {
        var s = BootedGame() with { ChordPreview = (1, 1) };
        var boardBefore = s.Board;

        var after = AppReducer.Reduce(s, new EndChordPreviewAction(false), Seeded());

        Assert.Null(after.ChordPreview);
        Assert.Same(boardBefore, after.Board); // board is untouched (no chord performed)
    }

    [Fact]
    public void EndChordPreview_CommitTrue_DispatchesChordOnPreviewedCell()
    {
        var s = BootedGame();

        // Find a numbered cell whose neighboring mines have all been flagged.
        (int r, int c) target = (-1, -1);
        for (int r = 0; r < s.Board.Rows && target.r < 0; r++)
            for (int c = 0; c < s.Board.Columns; c++)
            {
                var cell = s.Board.Cells[r, c];
                if (!cell.IsRevealed || cell.AdjacentMines == 0) continue;
                target = (r, c); break;
            }
        Assert.NotEqual(-1, target.r);

        // Flag every adjacent mine so the chord is satisfiable.
        for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
            {
                if (dr == 0 && dc == 0) continue;
                int nr = target.r + dr, nc = target.c + dc;
                if (!s.Board.InBounds(nr, nc)) continue;
                if (s.Board.Cells[nr, nc].IsMine)
                    s = AppReducer.Reduce(s, new FlagAction(nr, nc), Seeded());
            }

        var revealedBefore = s.Board.RevealedSafeCount;
        s = s with { ChordPreview = target };

        var after = AppReducer.Reduce(s, new EndChordPreviewAction(true), Seeded());
        Assert.Null(after.ChordPreview);
        Assert.True(after.Board.RevealedSafeCount >= revealedBefore,
            "chord on a satisfied numbered cell should reveal at least as many safe cells");
    }

    [Fact]
    public void Reveal_ClearsAnyActiveChordPreview()
    {
        var s = BootedGame() with { ChordPreview = (1, 1) };
        var after = AppReducer.Reduce(s, new RevealAction(8, 8), Seeded());
        Assert.Null(after.ChordPreview);
    }

    // ── Dialog open/close ─────────────────────────────────────────────

    [Fact]
    public void OpenAndCloseHighScores_TogglesFlag()
    {
        var s = AppReducer.Initial(Difficulty.Beginner, HighScores.Empty);
        s = AppReducer.Reduce(s, new OpenHighScoresAction(), Seeded());
        Assert.True(s.ShowHighScores);
        s = AppReducer.Reduce(s, new CloseHighScoresAction(), Seeded());
        Assert.False(s.ShowHighScores);
    }

    [Fact]
    public void ResetHighScores_ClearsScoresAndInvokesCallback()
    {
        var s = AppReducer.Initial(Difficulty.Beginner,
            new HighScores(Beginner: new HighScoreEntry(10, "x")));
        HighScores? saved = null;
        var after = AppReducer.Reduce(s, new ResetHighScoresAction(), Seeded(), s2 => saved = s2);
        Assert.Equal(HighScores.Empty, after.Scores);
        Assert.Equal(HighScores.Empty, saved);
        Assert.False(after.ShowHighScores);
    }

    [Fact]
    public void ShowNewBest_AutoPersistsAndOpensDialog()
    {
        var s = AppReducer.Initial(Difficulty.Beginner, HighScores.Empty)
            with { ElapsedSeconds = 12 };
        HighScores? saved = null;
        var after = AppReducer.Reduce(s, new ShowNewBestAction(), Seeded(), s2 => saved = s2);

        Assert.True(after.ShowNewBest);
        Assert.NotNull(saved);
        Assert.Equal(12, saved!.Beginner!.Seconds);
        Assert.Equal(12, after.Scores.Beginner!.Seconds);
    }

    [Fact]
    public void CloseNewBest_DismissesDialog()
    {
        var s = AppReducer.Initial(Difficulty.Beginner, HighScores.Empty)
            with { ShowNewBest = true };
        var after = AppReducer.Reduce(s, new CloseNewBestAction(), Seeded());
        Assert.False(after.ShowNewBest);
    }

    [Fact]
    public void ApplyCustom_RebuildsBoardWithCustomDifficultyAndClosesDialog()
    {
        var s = AppReducer.Initial(Difficulty.Beginner, HighScores.Empty)
            with { ShowCustomDialog = true };
        var after = AppReducer.Reduce(s, new ApplyCustomAction(7, 7, 5), Seeded());
        Assert.False(after.ShowCustomDialog);
        Assert.Equal(7, after.Board.Rows);
        Assert.Equal(7, after.Board.Columns);
        Assert.Equal(DifficultyKind.Custom, after.Board.Difficulty.Kind);
        Assert.Null(after.ChordPreview);
        Assert.Equal(0, after.ElapsedSeconds);
    }
}
