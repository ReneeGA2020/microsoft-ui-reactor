namespace DuctFiles.Services;

/// <summary>
/// Wraps FileSystemWatcher with debouncing. Fires a single callback
/// after a burst of changes settles (300ms quiet period).
/// </summary>
internal sealed class FileWatcherService : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly Action _onChange;
    private CancellationTokenSource? _debounceCts;
    private bool _disposed;

    public FileWatcherService(string path, Action onChange)
    {
        _onChange = onChange;
        _watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName
                         | NotifyFilters.DirectoryName
                         | NotifyFilters.LastWrite
                         | NotifyFilters.Size,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnChange;
        _watcher.Deleted += OnChange;
        _watcher.Renamed += OnChange;
        _watcher.Changed += OnChange;
    }

    private void OnChange(object sender, FileSystemEventArgs e)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        Task.Delay(TimeSpan.FromMilliseconds(300), token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                _onChange();
        }, TaskScheduler.Default);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _debounceCts?.Cancel();
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
    }
}
