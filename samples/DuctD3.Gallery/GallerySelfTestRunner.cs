// Self-test harness for DuctD3.Gallery
// Launched via `--self-test` flag. Starts the gallery, navigates into every sample
// and back to the landing page, verifying no crashes occur.
// Outputs TAP (Test Anything Protocol) results to stdout.

using Duct;
using Duct.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DuctD3.Gallery;

static class GallerySelfTestRunner
{
    static Window? _window;
    static int _passes;
    static int _failures;

    public static void RunAll()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new DuctApplication();
            var dispatcher = DispatcherQueue.GetForCurrentThread();

            _window = new Window { Title = "DuctD3 Gallery" };
            _window.AppWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));
            var host = new DuctHost(_window);
            DuctApp.ActiveHost = host;
            XamlInterop.Register(host.Reconciler);
            host.Mount(new GalleryApp());
            _window.Activate();

            dispatcher.TryEnqueue(async () =>
            {
                await Task.Delay(2000); // Wait for initial render + layout

                await RunSampleNavigationTests();

                Console.WriteLine($"# Done: {_passes} passed, {_failures} failed out of {_passes + _failures} tests");
                Console.Out.Flush();
                Environment.Exit(_failures > 0 ? 1 : 0);
            });
        });
    }

    static async Task RunSampleNavigationTests()
    {
        Check("Gallery_Launches_With_Landing_Page",
            FindText("DuctD3 Gallery") != null);

        foreach (var sample in SampleRegistry.All)
        {
            await CheckAsync($"Sample_{sample.IconName}", async () =>
            {
                // Click into the sample
                ClickSampleButton(sample.Title);
                await Render();

                // Verify sample page rendered
                var backBtn = FindButton("< Back");
                if (backBtn == null)
                {
                    await Render();
                    backBtn = FindButton("< Back");
                }

                if (backBtn == null)
                    return false;

                // Navigate back
                ClickButton("< Back");
                await Render();

                // Verify landing page is restored
                var landing = FindText("DuctD3 Gallery");
                if (landing == null)
                {
                    await Render();
                    landing = FindText("DuctD3 Gallery");
                }

                return landing != null;
            });
        }
    }

    // ── Helpers ─────────────────────────────────────────────────

    static void Check(string name, bool result)
    {
        if (result) { Console.WriteLine($"ok {name}"); _passes++; }
        else { Console.WriteLine($"not ok {name}"); _failures++; }
    }

    static async Task CheckAsync(string name, Func<Task<bool>> test)
    {
        try { Check(name, await test()); }
        catch (Exception ex)
        {
            Console.WriteLine($"not ok {name} - {ex.GetType().Name}: {ex.Message}");
            _failures++;
        }
    }

    static Task Render() => Task.Delay(300);

    static void ClickButton(string label)
    {
        var btn = FindButton(label);
        if (btn is { IsEnabled: true })
        {
            var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(btn);
            var invokeProvider = (Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)
                peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke);
            invokeProvider.Invoke();
        }
    }

    /// <summary>
    /// Gallery sample buttons contain a VStack with an Image + TextBlock,
    /// so we find the button by locating the TextBlock with the title inside it.
    /// </summary>
    static void ClickSampleButton(string title)
    {
        var content = _window?.Content;
        if (content == null) return;

        var tb = FindInTree<TextBlock>(content, t => t.Text == title && t.FontSize == 12);
        if (tb == null) return;

        // Walk up the visual tree to find the containing Button
        DependencyObject? current = tb;
        while (current != null)
        {
            if (current is Button btn)
            {
                if (btn.IsEnabled)
                {
                    var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(btn);
                    var invokeProvider = (Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)
                        peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke);
                    invokeProvider.Invoke();
                }
                return;
            }
            current = VisualTreeHelper.GetParent(current);
        }
    }

    static Button? FindButton(string label)
        => FindControl<Button>(b => b.Content is string s && s == label);

    static TextBlock? FindText(string text)
        => FindControl<TextBlock>(tb => tb.Text == text);

    static T? FindControl<T>(Func<T, bool> predicate) where T : DependencyObject
    {
        var content = _window?.Content;
        if (content == null) return default;
        return FindInTree(content, predicate);
    }

    static T? FindInTree<T>(DependencyObject root, Func<T, bool> predicate) where T : DependencyObject
    {
        if (root is T match && predicate(match)) return match;
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var found = FindInTree(VisualTreeHelper.GetChild(root, i), predicate);
            if (found != null) return found;
        }
        return null;
    }
}
