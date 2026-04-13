using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Duct.AppTests.Host.SelfTest;

/// <summary>
/// Test harness that runs assertions against a WinUI window and outputs TAP results.
/// Each fixture receives a Harness instance and calls Check/CheckAsync to report results.
/// </summary>
internal sealed class Harness
{
    private readonly Window _window;
    private int _failures;

    // Persistent title bar with visual test-result segments
    private TextBlock? _subtitleText;
    private readonly List<Border> _testSegments = new();
    private Border? _contentArea;

    public Harness(Window window) { _window = window; _currentWindow = window; }
    public Window Window => _window;
    public int Failures => _failures;

    // -- TitleBar setup ---------------------------------------------------

    public void SetupTitleBar(int totalTests)
    {
        _testSegments.Clear();

        // Grid of equal-width columns — one per test, colored on completion
        var segmentBar = new Grid { IsHitTestVisible = false };
        for (int i = 0; i < totalTests; i++)
        {
            segmentBar.ColumnDefinitions.Add(
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var seg = new Border
            {
                Background = new SolidColorBrush(
                    Windows.UI.Color.FromArgb(30, 200, 200, 200)),
            };
            Grid.SetColumn(seg, i);
            segmentBar.Children.Add(seg);
            _testSegments.Add(seg);
        }

        // Subtitle label in a semi-transparent pill for readability over the bar
        _subtitleText = new TextBlock
        {
            FontSize = 12,
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false,
        };
        var textPill = new Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 0, 0, 0)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 2, 8, 2),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            IsHitTestVisible = false,
            Child = _subtitleText,
        };

        var titleBarArea = new Grid { Height = 48 };
        titleBarArea.Children.Add(segmentBar);
        titleBarArea.Children.Add(textPill);

        var rootGrid = new Grid();
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        Grid.SetRow(titleBarArea, 0);
        rootGrid.Children.Add(titleBarArea);

        _contentArea = new Border();
        Grid.SetRow(_contentArea, 1);
        rootGrid.Children.Add(_contentArea);

        _window.Content = rootGrid;
        _window.ExtendsContentIntoTitleBar = true;
        _window.SetTitleBar(titleBarArea);
    }

    public void UpdateProgress(int current, string fixtureName)
    {
        if (_subtitleText is not null)
            _subtitleText.Text = $"{current}/{_testSegments.Count} \u2014 {fixtureName}";
    }

    /// <summary>
    /// Colors the segment at <paramref name="index"/> green (pass) or red (fail).
    /// </summary>
    public void MarkFixtureResult(int index, bool passed)
    {
        if (index < 0 || index >= _testSegments.Count) return;
        _testSegments[index].Background = new SolidColorBrush(
            passed
                ? Windows.UI.Color.FromArgb(255, 76, 175, 80)   // green
                : Windows.UI.Color.FromArgb(255, 244, 67, 54)); // red
    }

    public Duct.DuctHost CreateHost()
    {
        var host = new Duct.DuctHost(_window);
        if (_contentArea is not null)
            host.ContentTarget = _contentArea;
        return host;
    }

    /// <summary>
    /// Places arbitrary content into the test content area (below the TitleBar).
    /// Use this instead of setting Window.Content directly to avoid overwriting
    /// the TitleBar and progress bar.
    /// </summary>
    public void SetContent(UIElement? content)
    {
        if (_contentArea is not null)
            _contentArea.Child = content;
        else
            _window.Content = content;
    }

    // -- TAP assertion helpers -------------------------------------------

    public void Check(string name, bool result)
    {
        if (result)
            Console.WriteLine($"ok {name}");
        else
        {
            Console.WriteLine($"not ok {name} - assertion failed");
            _failures++;
        }
    }

    public void Check(string name, Func<bool> test)
    {
        try { Check(name, test()); }
        catch (Exception ex)
        {
            Console.WriteLine($"not ok {name} - {ex.GetType().Name}: {ex.Message}");
            _failures++;
        }
    }

    public async Task CheckAsync(string name, Func<Task<bool>> test)
    {
        try { Check(name, await test()); }
        catch (Exception ex)
        {
            Console.WriteLine($"not ok {name} - {ex.GetType().Name}: {ex.Message}");
            _failures++;
        }
    }

    // -- Render / timing -------------------------------------------------

    /// <summary>
    /// Waits for DuctHost to finish all pending render passes, then forces a
    /// synchronous WinUI layout update so ActualWidth/ActualHeight are current.
    /// Pass a non-zero <paramref name="ms"/> only for genuinely async operations
    /// (e.g. WebView2 initialization) that need wall-clock time beyond the render.
    /// </summary>
    public static async Task Render(int ms = 0)
    {
        // Wait for Duct's render loop to go idle (all pending + re-renders done)
        if (DuctApp.ActiveHost is { } host)
        {
            await host.WaitForIdleAsync();
        }
        else
        {
            // No active host — yield once at Low priority as a fallback
            var dq = DispatcherQueue.GetForCurrentThread();
            var tcs = new TaskCompletionSource();
            dq.TryEnqueue(DispatcherQueuePriority.Low, () => tcs.SetResult());
            await tcs.Task;
        }

        // Force synchronous layout so ActualWidth/ActualHeight are ready
        (_currentWindow?.Content as UIElement)?.UpdateLayout();

        // Small breathing room for the compositor to finish processing
        // visual tree changes. Without this, rapid fixture transitions can
        // outpace the WinUI compositor and cause native segfaults.
        await Task.Delay(50 + ms);
    }

    private static Window? _currentWindow;

    // -- VisualTree query helpers ----------------------------------------

    /// <summary>Search root: the content area (below TitleBar) if set up, else Window.Content.</summary>
    private DependencyObject? SearchRoot => (DependencyObject?)_contentArea?.Child ?? _window.Content;

    public T? FindControl<T>(Func<T, bool> predicate) where T : DependencyObject
    {
        var root = SearchRoot;
        if (root is null) return default;
        return FindInTree(root, predicate);
    }

    public List<T> FindAllControls<T>(Func<T, bool> predicate) where T : DependencyObject
    {
        var results = new List<T>();
        var root = SearchRoot;
        if (root is not null)
            FindAllInTree(root, predicate, results);
        return results;
    }

    public Button? FindButton(string label)
        => FindControl<Button>(b => b.Content is string s && s == label);

    public TextBlock? FindText(string text)
        => FindControl<TextBlock>(tb => tb.Text == text);

    public TextBlock? FindTextContaining(string substring)
        => FindControl<TextBlock>(tb => tb.Text?.Contains(substring) == true);

    public int CountControls<T>() where T : DependencyObject
        => FindAllControls<T>(_ => true).Count;

    // -- Interaction helpers ----------------------------------------------

    public void ClickButton(string label)
    {
        var btn = FindButton(label);
        if (btn is not null && btn.IsEnabled)
        {
            var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(btn);
            var invokeProvider = (Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)
                peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke);
            invokeProvider.Invoke();
        }
    }

    public void ToggleCheckBox(string label)
    {
        var cb = FindControl<CheckBox>(c => c.Content is string s && s == label);
        if (cb is not null)
            cb.IsChecked = cb.IsChecked != true;
    }

    // -- Tree walking ----------------------------------------------------

    private static T? FindInTree<T>(DependencyObject root, Func<T, bool> predicate) where T : DependencyObject
    {
        if (root is T match && predicate(match)) return match;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var found = FindInTree(VisualTreeHelper.GetChild(root, i), predicate);
            if (found is not null) return found;
        }
        return null;
    }

    private static void FindAllInTree<T>(DependencyObject root, Func<T, bool> predicate, List<T> results) where T : DependencyObject
    {
        if (root is T match && predicate(match)) results.Add(match);
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
            FindAllInTree(VisualTreeHelper.GetChild(root, i), predicate, results);
    }
}
