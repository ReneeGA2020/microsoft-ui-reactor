using Minesweeper.Game;
using Minesweeper.Persistence;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Minesweeper;

public class HighScoreStoreTests : IDisposable
{
    readonly string _tempDir;
    readonly string _filePath;

    public HighScoreStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ReactorMinesweeperTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "highscores.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmpty()
    {
        var store = new HighScoreStore(_filePath);
        var loaded = store.Load();
        Assert.Equal(HighScores.Empty, loaded);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsAllSlots()
    {
        var store = new HighScoreStore(_filePath);
        var scores = new HighScores(
            Beginner: new HighScoreEntry(12, "2026-01-01T12:00:00Z"),
            Intermediate: new HighScoreEntry(87, "2026-01-02T12:00:00Z"),
            Expert: new HighScoreEntry(320, "2026-01-03T12:00:00Z"));

        Assert.True(store.Save(scores));
        var loaded = store.Load();
        Assert.Equal(scores, loaded);
    }

    [Fact]
    public void Save_CreatesParentDirectory()
    {
        var nested = Path.Combine(_tempDir, "deep", "nested", "highscores.json");
        var store = new HighScoreStore(nested);
        Assert.True(store.Save(new HighScores(Beginner: new HighScoreEntry(1, "x"))));
        Assert.True(File.Exists(nested));
    }

    [Fact]
    public void Load_CorruptFile_ReturnsEmpty()
    {
        File.WriteAllText(_filePath, "{ this is not valid json ::: ");
        var store = new HighScoreStore(_filePath);
        Assert.Equal(HighScores.Empty, store.Load());
    }

    [Fact]
    public void IsNewRecord_NoExisting_ReturnsTrue()
    {
        Assert.True(HighScoreStore.IsNewRecord(HighScores.Empty, DifficultyKind.Beginner, 999));
    }

    [Fact]
    public void IsNewRecord_FasterThanExisting_ReturnsTrue()
    {
        var current = new HighScores(Beginner: new HighScoreEntry(50, "x"));
        Assert.True(HighScoreStore.IsNewRecord(current, DifficultyKind.Beginner, 49));
    }

    [Fact]
    public void IsNewRecord_SlowerThanExisting_ReturnsFalse()
    {
        var current = new HighScores(Beginner: new HighScoreEntry(50, "x"));
        Assert.False(HighScoreStore.IsNewRecord(current, DifficultyKind.Beginner, 51));
        Assert.False(HighScoreStore.IsNewRecord(current, DifficultyKind.Beginner, 50));
    }

    [Fact]
    public void IsNewRecord_CustomDifficulty_AlwaysFalse()
    {
        Assert.False(HighScoreStore.IsNewRecord(HighScores.Empty, DifficultyKind.Custom, 1));
    }

    [Fact]
    public void TryUpdate_WritesNewBest_AndPreservesOtherSlots()
    {
        var current = new HighScores(
            Beginner: new HighScoreEntry(30, "x"),
            Intermediate: new HighScoreEntry(60, "y"));

        var updated = HighScoreStore.TryUpdate(current, DifficultyKind.Beginner, 25);

        Assert.NotNull(updated.Beginner);
        Assert.Equal(25, updated.Beginner!.Seconds);
        Assert.Equal(current.Intermediate, updated.Intermediate); // untouched
        Assert.Null(updated.Expert);
    }

    [Fact]
    public void TryUpdate_DoesNothingIfNotFaster()
    {
        var current = new HighScores(Beginner: new HighScoreEntry(10, "x"));
        var updated = HighScoreStore.TryUpdate(current, DifficultyKind.Beginner, 11);
        Assert.Same(current, updated);
    }
}
