using Minesweeper.Persistence;

namespace Minesweeper.Game;

/// <summary>
/// Aggregate UI state. Held in a single immutable record so the entire game
/// can be driven off one UseReducer hook. Lives in <c>Game/</c> so the
/// reducer transitions can be exercised by unit tests without referencing
/// any WinUI types.
/// </summary>
public sealed record AppState(
    Board Board,
    int ElapsedSeconds,
    bool IsPressing,
    bool ShowHighScores,
    bool ShowCustomDialog,
    bool ShowNewBest,
    HighScores Scores,
    /// <summary>Center of the L+R "chord preview" box, or null when no chord gesture is in progress.</summary>
    (int Row, int Col)? ChordPreview);

public abstract record AppAction;
public sealed record RevealAction(int Row, int Col) : AppAction;
public sealed record FlagAction(int Row, int Col) : AppAction;
public sealed record ChordAction(int Row, int Col) : AppAction;
public sealed record BeginChordPreviewAction(int Row, int Col) : AppAction;
public sealed record EndChordPreviewAction(bool Commit) : AppAction;
public sealed record TickAction : AppAction;
public sealed record ResetAction(Difficulty Difficulty) : AppAction;
public sealed record OpenCustomDialogAction : AppAction;
public sealed record CloseCustomDialogAction : AppAction;
public sealed record ApplyCustomAction(int Rows, int Columns, int Mines) : AppAction;
public sealed record OpenHighScoresAction : AppAction;
public sealed record CloseHighScoresAction : AppAction;
public sealed record ResetHighScoresAction : AppAction;
public sealed record ShowNewBestAction : AppAction;
public sealed record CloseNewBestAction : AppAction;

/// <summary>
/// Pure reducer for the whole app. Side effects (timer ticks, persisting
/// scores) are handled by the host component; here we only translate
/// (state, action) → next state. Persistence callbacks are passed in so
/// the reducer doesn't reach into <see cref="HighScoreStore"/> directly.
/// </summary>
public static class AppReducer
{
    public static AppState Initial(Difficulty difficulty, HighScores initialScores) => new(
        Board: Board.NewGame(difficulty),
        ElapsedSeconds: 0,
        IsPressing: false,
        ShowHighScores: false,
        ShowCustomDialog: false,
        ShowNewBest: false,
        Scores: initialScores,
        ChordPreview: null);

    /// <summary>
    /// Pure reducer. <paramref name="rng"/> is supplied by the caller so tests
    /// can use a seeded RNG; <paramref name="onScoresChanged"/> lets the host
    /// persist the score table when it changes (called for ResetHighScores
    /// and SaveNewBest actions).
    /// </summary>
    public static AppState Reduce(
        AppState s,
        AppAction a,
        Random rng,
        Action<HighScores>? onScoresChanged = null)
    {
        switch (a)
        {
            case RevealAction r:
                return s with
                {
                    Board = BoardReducer.Reveal(s.Board, r.Row, r.Col, rng),
                    ChordPreview = null,
                };
            case FlagAction f:
                return s with { Board = BoardReducer.ToggleFlag(s.Board, f.Row, f.Col) };
            case ChordAction c:
                return s with
                {
                    Board = BoardReducer.Chord(s.Board, c.Row, c.Col, rng),
                    ChordPreview = null,
                };
            case BeginChordPreviewAction bp:
            {
                if (s.Board.Phase != GamePhase.Playing) return s;
                if (!s.Board.InBounds(bp.Row, bp.Col)) return s;
                var cell = s.Board.Cells[bp.Row, bp.Col];
                if (!cell.IsRevealed || cell.AdjacentMines == 0) return s;
                return s with { ChordPreview = (bp.Row, bp.Col) };
            }
            case EndChordPreviewAction ep:
            {
                if (s.ChordPreview is null) return s;
                if (!ep.Commit) return s with { ChordPreview = null };
                var (r, c) = s.ChordPreview.Value;
                return s with
                {
                    Board = BoardReducer.Chord(s.Board, r, c, rng),
                    ChordPreview = null,
                };
            }
            case TickAction:
                if (s.Board.Phase != GamePhase.Playing) return s;
                return s with { ElapsedSeconds = Math.Min(s.ElapsedSeconds + 1, 999) };
            case ResetAction reset:
                return s with
                {
                    Board = Board.NewGame(reset.Difficulty),
                    ElapsedSeconds = 0,
                    IsPressing = false,
                    ChordPreview = null,
                };
            case OpenCustomDialogAction:
                return s with { ShowCustomDialog = true };
            case CloseCustomDialogAction:
                return s with { ShowCustomDialog = false };
            case ApplyCustomAction ac:
            {
                var diff = new Difficulty(DifficultyKind.Custom, ac.Rows, ac.Columns, ac.Mines);
                return s with
                {
                    Board = Board.NewGame(diff),
                    ElapsedSeconds = 0,
                    ShowCustomDialog = false,
                    ChordPreview = null,
                };
            }
            case OpenHighScoresAction:
                return s with { ShowHighScores = true };
            case CloseHighScoresAction:
                return s with { ShowHighScores = false };
            case ResetHighScoresAction:
                onScoresChanged?.Invoke(HighScores.Empty);
                return s with { Scores = HighScores.Empty, ShowHighScores = false };
            case ShowNewBestAction:
                // Auto-persist the new score as soon as we open the
                // celebration dialog. There's nothing to capture from the
                // user — single-player game, name-less score table.
                {
                    var updated = HighScoreStore.TryUpdate(
                        s.Scores, s.Board.Difficulty.Kind, s.ElapsedSeconds);
                    if (!ReferenceEquals(updated, s.Scores))
                        onScoresChanged?.Invoke(updated);
                    return s with { Scores = updated, ShowNewBest = true };
                }
            case CloseNewBestAction:
                return s with { ShowNewBest = false };
            default:
                return s;
        }
    }
}
