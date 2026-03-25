using System.Runtime.CompilerServices;

namespace StressPerf.Shared;

public record struct StockItem(string Symbol, double PrevPrice, double CurrentPrice, bool IsUp);

public sealed class StockDataSource
{
    public const int Columns = 70;
    public const int Rows = 70;
    public const int TotalItems = Columns * Rows;

    private readonly StockItem[] _items = new StockItem[TotalItems];
    private readonly Random _rng = new(42); // deterministic seed

    public StockItem[] Items => _items;

    public StockDataSource()
    {
        var rng = _rng;
        for (int i = 0; i < TotalItems; i++)
        {
            int row = i / Columns;
            int col = i % Columns;
            char c1 = (char)('A' + (row % 26));
            char c2 = (char)('A' + (col / 3 % 26));
            char c3 = (char)('A' + (col % 26));
            string symbol = string.Create(3, (c1, c2, c3), static (span, s) =>
            {
                span[0] = s.c1;
                span[1] = s.c2;
                span[2] = s.c3;
            });
            double price = Math.Round(10.0 + rng.NextDouble() * 990.0, 2);
            _items[i] = new StockItem(symbol, price, price, true);
        }
    }

    /// <summary>
    /// Mutate a percentage of items. Returns the list of changed indices.
    /// </summary>
    public List<int> Update(double percent)
    {
        int count = Math.Max(1, (int)(TotalItems * percent / 100.0));
        var changed = new List<int>(count);
        var rng = _rng;

        for (int i = 0; i < count; i++)
        {
            int idx = rng.Next(TotalItems);
            ref var item = ref _items[idx];
            // +/- up to 2%, biased slightly upward (0.48 center)
            double delta = ((rng.NextDouble() - 0.48) * 2.0) * item.CurrentPrice * 0.02;
            double newPrice = Math.Max(0.01, Math.Round(item.CurrentPrice + delta, 2));
            item = new StockItem(item.Symbol, item.CurrentPrice, newPrice, newPrice >= item.CurrentPrice);
            changed.Add(idx);
        }

        return changed;
    }

    /// <summary>
    /// Returns a snapshot copy of the current items array (used by Duct variant).
    /// </summary>
    public StockItem[] Snapshot()
    {
        var copy = new StockItem[TotalItems];
        Array.Copy(_items, copy, TotalItems);
        return copy;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatCell(in StockItem item) => $"{item.Symbol} {item.CurrentPrice:F2}";
}
