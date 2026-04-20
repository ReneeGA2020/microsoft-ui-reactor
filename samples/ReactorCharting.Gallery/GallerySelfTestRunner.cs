// Self-test harness for ReactorCharting.Gallery
// Launched via `--self-test` flag. Starts the gallery, navigates into every sample,
// captures a snapshot of each rendered chart, verifies no crashes, and navigates back.
// Outputs TAP (Test Anything Protocol) results to stdout.
// Snapshots are saved to ./snapshots/ as PNG files (one per sample).

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace ReactorCharting.Gallery;

static class GallerySelfTestRunner
{
    static Window? _window;
    static int _passes;
    static int _failures;
    static string? _snapshotDir;

    public static void RunAll()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new ReactorApplication();
            var dispatcher = DispatcherQueue.GetForCurrentThread();

            _window = new Window { Title = "Reactor Charting Gallery" };
            _window.AppWindow.Resize(new global::Windows.Graphics.SizeInt32(1400, 900));
            var host = new ReactorHost(_window);
            XamlInterop.Register(host.Reconciler);
            host.Mount(new GalleryApp());
            _window.Activate();

            // Create snapshots directory next to the exe
            _snapshotDir = Path.Combine(AppContext.BaseDirectory, "snapshots");
            Directory.CreateDirectory(_snapshotDir);

            dispatcher.TryEnqueue(async () =>
            {
                await Task.Delay(2000); // Wait for initial render + layout

                await RunSampleNavigationTests();

                Console.WriteLine($"# Done: {_passes} passed, {_failures} failed out of {_passes + _failures} tests");
                Console.WriteLine($"# Snapshots saved to: {_snapshotDir}");
                Console.Out.Flush();
                Environment.Exit(_failures > 0 ? 1 : 0);
            });
        });
    }

    static async Task RunSampleNavigationTests()
    {
        Check("Gallery_Launches_With_Landing_Page",
            FindText("Reactor Charting Gallery") != null && IsOnLandingPage());

        // Capture landing page snapshot
        await CaptureSnapshot("_landing");

        foreach (var sample in SampleRegistry.All)
        {
            await CheckAsync($"Sample_{sample.IconName}", async () =>
            {
                // Click into the sample
                ClickSampleButton(sample.Title);
                await Render(800); // Longer wait for charts to render + transition

                // Verify sample page rendered (Heading with sample title, FontSize 28)
                if (!IsOnDetailPage(sample.Title))
                {
                    await Render(500);
                    if (!IsOnDetailPage(sample.Title))
                        return false;
                }

                // Capture snapshot of the rendered chart
                await CaptureSnapshot(sample.IconName);

                // Navigate back via the exposed nav handle (drives the same
                // GoBack the TitleBar's native back button calls).
                GalleryApp.CurrentNav?.GoBack();
                await Render(500);

                // Verify landing page is restored
                if (!IsOnLandingPage())
                {
                    await Render(500);
                    if (!IsOnLandingPage())
                        return false;
                }

                return true;
            });
        }
    }

    /// <summary>
    /// Landing page is active when at least one category SubHeading (e.g., "Bars")
    /// is visible. SubHeading renders a TextBlock with FontSize around 20.
    /// </summary>
    static bool IsOnLandingPage()
    {
        // Bars is always the first category.
        var hit = FindInTree<TextBlock>(_window?.Content!,
            tb => tb.Text == "Bars" && tb.FontSize >= 16);
        return hit != null;
    }

    /// <summary>
    /// Detail page is active when a Heading (FontSize 28) with the sample's
    /// title is in the tree.
    /// </summary>
    static bool IsOnDetailPage(string sampleTitle)
    {
        var hit = FindInTree<TextBlock>(_window?.Content!,
            tb => tb.Text == sampleTitle && tb.FontSize >= 24);
        return hit != null;
    }

    /// <summary>
    /// The WinUI TitleBar renders a native back button whose Content is an icon
    /// (not a string). Our theme toggle Button, also inside the TitleBar, has
    /// a string Content — so we filter for non-string Content.
    /// </summary>
    static bool InvokeTitleBarBackButton()
    {
        var content = _window?.Content;
        if (content == null) return false;
        var titleBar = FindInTree<Microsoft.UI.Xaml.Controls.TitleBar>(content, _ => true);
        if (titleBar == null) return false;

        var backBtn = FindInTree<Button>(titleBar,
            b => b.IsEnabled && b.Content is not string);
        if (backBtn == null) return false;

        var peer = new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(backBtn);
        var invoke = (Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)
            peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke);
        invoke.Invoke();
        return true;
    }

    // ── Snapshot Capture ───────────────────────────────────────────

    static async Task CaptureSnapshot(string name)
    {
        if (_window == null || _snapshotDir == null) return;

        try
        {
            // Small delay to ensure rendering is complete
            await Task.Delay(100);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            if (!GetClientRect(hwnd, out var clientRect)) return;

            int width = clientRect.Right - clientRect.Left;
            int height = clientRect.Bottom - clientRect.Top;
            if (width <= 0 || height <= 0) return;

            var clientOrigin = new POINT { X = 0, Y = 0 };
            ClientToScreen(hwnd, ref clientOrigin);
            GetWindowRect(hwnd, out var windowRect);

            int offsetX = clientOrigin.X - windowRect.Left;
            int offsetY = clientOrigin.Y - windowRect.Top;
            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;
            if (windowWidth <= 0 || windowHeight <= 0) return;

            using var windowBmp = new Bitmap(windowWidth, windowHeight, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(windowBmp))
            {
                IntPtr hdc = g.GetHdc();
                PrintWindow(hwnd, hdc, PW_RENDERFULLCONTENT);
                g.ReleaseHdc(hdc);
            }

            using var clientBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(clientBmp))
            {
                g.DrawImage(windowBmp,
                    new Rectangle(0, 0, width, height),
                    new Rectangle(offsetX, offsetY, width, height),
                    GraphicsUnit.Pixel);
            }

            var path = Path.Combine(_snapshotDir, $"{name}.png");
            clientBmp.Save(path, ImageFormat.Png);
            Console.WriteLine($"# Snapshot: {name}.png ({width}x{height})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"# Snapshot failed for {name}: {ex.Message}");
        }
    }

    // ── TAP Helpers ────────────────────────────────────────────────

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

    static Task Render(int ms = 300) => Task.Delay(ms);

    // ── UI Helpers ─────────────────────────────────────────────────

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

    // ── Win32 P/Invoke for screenshot capture ──────────────────────

    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hwnd, ref POINT lpPoint);
}
