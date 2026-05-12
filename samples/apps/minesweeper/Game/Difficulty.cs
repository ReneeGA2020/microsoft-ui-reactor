namespace Minesweeper.Game;

/// <summary>
/// One of the three classic difficulty presets, or Custom.
/// </summary>
public enum DifficultyKind
{
    Beginner,
    Intermediate,
    Expert,
    Custom,
}

/// <summary>
/// Board dimensions + mine count for a single game configuration.
/// Records the "level identity" used as the high-score key.
/// </summary>
public sealed record Difficulty(DifficultyKind Kind, int Rows, int Columns, int MineCount)
{
    public static readonly Difficulty Beginner =
        new(DifficultyKind.Beginner, Rows: 9, Columns: 9, MineCount: 10);

    public static readonly Difficulty Intermediate =
        new(DifficultyKind.Intermediate, Rows: 16, Columns: 16, MineCount: 40);

    public static readonly Difficulty Expert =
        new(DifficultyKind.Expert, Rows: 16, Columns: 30, MineCount: 99);

    /// <summary>The classic preset matching <paramref name="kind"/>, or null if Custom.</summary>
    public static Difficulty? Preset(DifficultyKind kind) => kind switch
    {
        DifficultyKind.Beginner => Beginner,
        DifficultyKind.Intermediate => Intermediate,
        DifficultyKind.Expert => Expert,
        _ => null,
    };

    public string DisplayName => Kind switch
    {
        DifficultyKind.Beginner => "Beginner",
        DifficultyKind.Intermediate => "Intermediate",
        DifficultyKind.Expert => "Expert",
        _ => $"Custom ({Rows}×{Columns}, {MineCount})",
    };

    /// <summary>
    /// Validates a custom-board request. Mirrors the limits the original
    /// game enforced: at least 1 mine, no more than (rows*cols - 9) so that
    /// the first-click safe pocket always fits.
    /// </summary>
    public static bool IsValidCustom(int rows, int columns, int mines, out string? error)
    {
        if (rows < 4 || rows > 24)
        {
            error = "Rows must be between 4 and 24.";
            return false;
        }

        if (columns < 4 || columns > 30)
        {
            error = "Columns must be between 4 and 30.";
            return false;
        }

        var maxMines = rows * columns - 9;
        if (mines < 1 || mines > maxMines)
        {
            error = $"Mine count must be between 1 and {maxMines}.";
            return false;
        }

        error = null;
        return true;
    }
}
