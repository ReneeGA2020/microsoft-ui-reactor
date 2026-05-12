using System.Text.Json.Serialization;

namespace Minesweeper.Persistence;

/// <summary>
/// One entry in the high-score table — the time for a single difficulty
/// preset. This is a single-player game; scores are saved per-user in
/// LocalAppData and aren't shared, so we don't track names.
/// Custom games are not tracked (the original game's behavior).
/// </summary>
public sealed record HighScoreEntry(int Seconds, string AchievedAtIso);

/// <summary>
/// Top score per classic difficulty. A null entry means "no record yet".
/// </summary>
public sealed record HighScores(
    HighScoreEntry? Beginner = null,
    HighScoreEntry? Intermediate = null,
    HighScoreEntry? Expert = null)
{
    public static readonly HighScores Empty = new();
}

[JsonSerializable(typeof(HighScores))]
[JsonSerializable(typeof(HighScoreEntry))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class HighScoresJsonContext : JsonSerializerContext
{
}
