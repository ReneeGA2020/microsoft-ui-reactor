// Duct Word Puzzle — A sliding tile puzzle, ported from the .NET MAUI sample.
// No XAML. No data binding. Just C# with Duct's declarative component model.

using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using static Duct.UI;

DuctApp.Run<WordPuzzleApp>("Duct Word Puzzle", width: 520, height: 620);

// ─── Main game component ───────────────────────────────────────────────────────

class WordPuzzleApp : Component
{
    const int NUM = 4;
    const int TOTAL = NUM * NUM;
    const string NormText = ".NET MAUI ROCKS";
    const string WinTextStr = "CONGRATULATIONS";

    public override Element Render()
    {
        // --- State ---
        var (tiles, setTiles) = UseState(InitialTiles());
        var (emptyPos, setEmptyPos) = UseState(TOTAL - 1);
        var (isPlaying, setIsPlaying) = UseState(false);
        var (hasWon, setHasWon) = UseState(false);
        var (showWinText, setShowWinText) = UseState(false);
        var (elapsed, setElapsed) = UseState(TimeSpan.Zero);
        var timerRef = UseRef<DispatcherTimer?>(null);

        // --- Timer effect: start/stop based on isPlaying ---
        UseEffect(() =>
        {
            if (isPlaying && timerRef.Current == null)
            {
                var start = DateTime.Now;
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (_, _) =>
                {
                    var ts = DateTime.Now - start + TimeSpan.FromSeconds(0.5);
                    setElapsed(new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds));
                };
                timer.Start();
                timerRef.Current = timer;
            }
            else if (!isPlaying && timerRef.Current != null)
            {
                timerRef.Current.Stop();
                timerRef.Current = null;
            }
        }, isPlaying);

        // --- Tile click handler ---
        void OnTileClicked(int clickedPos)
        {
            if (!isPlaying || hasWon) return;

            int tappedRow = clickedPos / NUM, tappedCol = clickedPos % NUM;
            int eRow = emptyPos / NUM, eCol = emptyPos % NUM;

            // Only move if in the same row or column as the empty space
            if (tappedRow != eRow && tappedCol != eCol) return;
            if (clickedPos == emptyPos) return;

            var newTiles = (int[])tiles.Clone();
            int newEmpty = emptyPos;

            if (tappedRow == eRow && tappedCol != eCol)
            {
                // Shift tiles horizontally
                int inc = Math.Sign(tappedCol - eCol);
                for (int col = eCol + inc; col != tappedCol + inc; col += inc)
                {
                    int from = tappedRow * NUM + col;
                    newTiles[newEmpty] = newTiles[from];
                    newTiles[from] = -1;
                    newEmpty = from;
                }
            }
            else if (tappedCol == eCol && tappedRow != eRow)
            {
                // Shift tiles vertically
                int inc = Math.Sign(tappedRow - eRow);
                for (int row = eRow + inc; row != tappedRow + inc; row += inc)
                {
                    int from = row * NUM + tappedCol;
                    newTiles[newEmpty] = newTiles[from];
                    newTiles[from] = -1;
                    newEmpty = from;
                }
            }

            setTiles(newTiles);
            setEmptyPos(newEmpty);

            // Check for win
            bool won = true;
            for (int i = 0; i < TOTAL - 1; i++)
            {
                if (newTiles[i] != i) { won = false; break; }
            }
            if (won)
            {
                setHasWon(true);
                setIsPlaying(false);
                setShowWinText(true);
            }
        }

        // --- Randomize handler ---
        void OnRandomize()
        {
            var rand = new Random();
            var t = InitialTiles(); // start from solved state
            int ep = TOTAL - 1;

            // Simulate 200 random moves (same algorithm as MAUI sample)
            for (int i = 0; i < 100; i++)
            {
                int eRow = ep / NUM, eCol = ep % NUM;

                // Vertical shift: pick a random row, same column as empty
                int rRow = rand.Next(NUM);
                if (rRow != eRow)
                {
                    int inc = Math.Sign(rRow - eRow);
                    for (int row = eRow + inc; row != rRow + inc; row += inc)
                    {
                        int from = row * NUM + eCol;
                        t[ep] = t[from];
                        t[from] = -1;
                        ep = from;
                    }
                }

                eRow = ep / NUM; eCol = ep % NUM;

                // Horizontal shift: same row as empty, pick a random column
                int rCol = rand.Next(NUM);
                if (rCol != eCol)
                {
                    int inc = Math.Sign(rCol - eCol);
                    for (int col = eCol + inc; col != rCol + inc; col += inc)
                    {
                        int from = eRow * NUM + col;
                        t[ep] = t[from];
                        t[from] = -1;
                        ep = from;
                    }
                }
            }

            setTiles(t);
            setEmptyPos(ep);
            setHasWon(false);
            setShowWinText(false);
            setElapsed(TimeSpan.Zero);

            // Stop any existing timer before starting fresh
            if (timerRef.Current != null)
            {
                timerRef.Current.Stop();
                timerRef.Current = null;
            }
            setIsPlaying(true);
        }

        // --- Build the UI ---
        string timeDisplay = isPlaying
            ? elapsed.ToString(@"mm\:ss")
            : hasWon ? "You win! 🎉" : "";

        return VStack(12,
            // Header
            VStack(8,
                Button("Randomize", OnRandomize)
                    .HAlign(HorizontalAlignment.Center),
                Text(timeDisplay).FontSize(26).Bold()
                    .HAlign(HorizontalAlignment.Center)
            ).Margin(0, 12, 0, 4),

            // Game board
            Grid(
                ["*", "*", "*", "*"],
                ["*", "*", "*", "*"],
                BuildCells(tiles, showWinText, OnTileClicked)
            )
            .Set(g =>
            {
                g.Width = 440;
                g.Height = 440;
                g.RowSpacing = 4;
                g.ColumnSpacing = 4;
            })
            .HAlign(HorizontalAlignment.Center)
        );
    }

    static GridChild[] BuildCells(int[] tiles, bool showWin, Action<int> onTileClicked)
    {
        var cells = new List<GridChild>();

        for (int pos = 0; pos < TOTAL; pos++)
        {
            int tileIndex = tiles[pos];
            if (tileIndex < 0) continue; // empty space — no cell

            int row = pos / NUM, col = pos % NUM;
            char displayChar = showWin ? WinTextStr[tileIndex] : NormText[tileIndex];
            string label = $"{displayChar}";
            int capturedPos = pos;

            var tile = Button(label, () => onTileClicked(capturedPos))
                .Set(b =>
                {
                    b.FontSize = 32;
                    b.FontWeight = FontWeights.Bold;
                    b.HorizontalAlignment = HorizontalAlignment.Stretch;
                    b.VerticalAlignment = VerticalAlignment.Stretch;
                    b.HorizontalContentAlignment = HorizontalAlignment.Center;
                    b.VerticalContentAlignment = VerticalAlignment.Center;
                    b.CornerRadius = new CornerRadius(6);
                })
                .WithKey($"tile-{tileIndex}");

            cells.Add(Cell(tile, row, col));
        }

        return [.. cells];
    }

    static int[] InitialTiles()
    {
        var tiles = new int[TOTAL];
        for (int i = 0; i < TOTAL - 1; i++) tiles[i] = i;
        tiles[TOTAL - 1] = -1; // empty
        return tiles;
    }
}
