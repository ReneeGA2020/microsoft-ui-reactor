using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Duct.EndToEnd.App;

/// <summary>
/// Test harness that runs assertions against a WinUI window and outputs TAP results.
/// Each fixture receives a Harness instance and calls Check/CheckAsync to report results.
/// </summary>
internal sealed class Harness
{
    private readonly Window _window;
    private int _failures;

    public Harness(Window window) => _window = window;
    public Window Window => _window;
    public int Failures => _failures;

    // ── TAP assertion helpers ───────────────────────────────────────

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

    // ── Render / timing ─────────────────────────────────────────────

    /// <summary>
    /// Yields to the dispatcher so DuctHost's enqueued render pass can execute,
    /// then waits for layout to complete.
    /// </summary>
    public static Task Render(int ms = 300) => Task.Delay(ms);

    // ── VisualTree query helpers ────────────────────────────────────

    public T? FindControl<T>(Func<T, bool> predicate) where T : DependencyObject
    {
        var content = _window.Content;
        if (content is null) return default;
        return FindInTree(content, predicate);
    }

    public List<T> FindAllControls<T>(Func<T, bool> predicate) where T : DependencyObject
    {
        var results = new List<T>();
        var content = _window.Content;
        if (content is not null)
            FindAllInTree(content, predicate, results);
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

    // ── Interaction helpers ─────────────────────────────────────────

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

    // ── Screenshot ──────────────────────────────────────────────────

    public async Task CaptureScreenshotAsync(string fixtureName)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "screenshots");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{fixtureName}.png");
            await ScreenCapture.CaptureWindowAsync(_window, path);
        }
        catch (Exception ex)
        {
            // Screenshot failure is non-fatal — tests still pass on assertions
            Console.Error.WriteLine($"# Screenshot failed: {ex.Message}");
        }
    }

    // ── Tree walking ────────────────────────────────────────────────

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
