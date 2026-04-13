using System.ComponentModel;

namespace PerfBench.Priorities.Bound;

public sealed class SearchItemViewModel : INotifyPropertyChanged
{
#pragma warning disable CS0067 // Required by INotifyPropertyChanged but Text is read-only
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    public SearchItemViewModel(string text)
    {
        Text = text;
    }

    public string Text { get; }
}
