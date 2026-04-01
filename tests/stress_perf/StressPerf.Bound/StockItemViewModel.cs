using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace StressPerf.Bound;

public sealed class StockItemViewModel : INotifyPropertyChanged
{
    private static readonly SolidColorBrush GreenBrush = new(Colors.LimeGreen);
    private static readonly SolidColorBrush RedBrush = new(Microsoft.UI.Colors.Red);

    private string _displayText = "";
    private SolidColorBrush _priceBrush = GreenBrush;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DisplayText
    {
        get => _displayText;
        set
        {
            if (_displayText == value) return;
            _displayText = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
        }
    }

    public SolidColorBrush PriceBrush
    {
        get => _priceBrush;
        set
        {
            if (ReferenceEquals(_priceBrush, value)) return;
            _priceBrush = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriceBrush)));
        }
    }

    public void Update(string symbol, double price, bool isUp)
    {
        DisplayText = $"{symbol} {price:F2}";
        PriceBrush = isUp ? GreenBrush : RedBrush;
    }
}
