using System.Text.Json;
using Minesweeper.Game;

namespace Minesweeper.Persistence;

/// <summary>
/// Reads and writes the local high-score table. The file lives at
/// <c>%LocalAppData%/ReactorMinesweeper/highscores.json</c>. Writes are atomic:
/// we serialise to a temp file in the same directory and then call
/// <see cref="File.Move(string, string, bool)"/>, so a crash mid-write can
/// never leave a half-written record.
/// </summary>
public sealed class HighScoreStore
{
    public string FilePath { get; }

    public HighScoreStore() : this(DefaultFilePath())
    {
    }

    /// <summary>Test-friendly constructor — lets unit tests pass a temp path.</summary>
    public HighScoreStore(string filePath)
    {
        FilePath = filePath;
    }

    public static string DefaultFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReactorMinesweeper",
            "highscores.json");

    /// <summary>
    /// Loads the current high-score table. Returns <see cref="HighScores.Empty"/>
    /// on missing-file or any read/parse error — corrupt history should never
    /// crash the game.
    /// </summary>
    public HighScores Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return HighScores.Empty;
            using var stream = File.OpenRead(FilePath);
            var loaded = JsonSerializer.Deserialize(stream, HighScoresJsonContext.Default.HighScores);
            return loaded ?? HighScores.Empty;
        }
        catch (Exception)
        {
            return HighScores.Empty;
        }
    }

    /// <summary>
    /// Persists <paramref name="scores"/> atomically. Creates the parent
    /// directory if needed. Errors are swallowed — saving a high score should
    /// never crash the game.
    /// </summary>
    public bool Save(HighScores scores)
    {
        try
        {
            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var tempPath = FilePath + ".tmp";
            using (var stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, scores, HighScoresJsonContext.Default.HighScores);
            }
            File.Move(tempPath, FilePath, overwrite: true);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// If <paramref name="seconds"/> beats the existing record for
    /// <paramref name="kind"/>, returns the updated table. Otherwise returns
    /// <paramref name="current"/> unchanged. Custom difficulties are never
    /// recorded.
    /// </summary>
    public static HighScores TryUpdate(HighScores current, DifficultyKind kind, int seconds)
    {
        var existing = GetEntry(current, kind);
        if (existing != null && existing.Seconds <= seconds) return current;

        var entry = new HighScoreEntry(seconds, DateTime.UtcNow.ToString("O"));

        return kind switch
        {
            DifficultyKind.Beginner => current with { Beginner = entry },
            DifficultyKind.Intermediate => current with { Intermediate = entry },
            DifficultyKind.Expert => current with { Expert = entry },
            _ => current,
        };
    }

    public static bool IsNewRecord(HighScores current, DifficultyKind kind, int seconds)
    {
        if (kind == DifficultyKind.Custom) return false;
        var existing = GetEntry(current, kind);
        return existing == null || seconds < existing.Seconds;
    }

    public static HighScoreEntry? GetEntry(HighScores scores, DifficultyKind kind) => kind switch
    {
        DifficultyKind.Beginner => scores.Beginner,
        DifficultyKind.Intermediate => scores.Intermediate,
        DifficultyKind.Expert => scores.Expert,
        _ => null,
    };
}
