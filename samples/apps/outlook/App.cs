using System.Runtime.InteropServices;
using Duct;
using Duct.Core;
using Duct.Core.Navigation;
using DuctOutlook.Components;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

// Parse --screenshot [view] [output-path]
var cliArgs = Environment.GetCommandLineArgs();
var ssIdx = Array.IndexOf(cliArgs, "--screenshot");
if (ssIdx >= 0)
{
    AppSettings.ScreenshotMode = true;
    if (ssIdx + 1 < cliArgs.Length && !cliArgs[ssIdx + 1].StartsWith("-"))
    {
        AppSettings.ScreenshotView = cliArgs[ssIdx + 1];
        if (ssIdx + 2 < cliArgs.Length && !cliArgs[ssIdx + 2].StartsWith("-"))
            AppSettings.ScreenshotPath = cliArgs[ssIdx + 2];
    }
}

DuctApp.Run<OutlookApp>("Outlook", width: 1800, height: 1100,
    configure: host =>
    {
        CursorBorderRegistration.Register(host.Reconciler);

        if (AppSettings.ScreenshotMode)
        {
            host.Window.Activated += async (_, _) =>
            {
                await Task.Delay(5000);
                await CaptureWindowScreenshot(host.Window, AppSettings.ScreenshotPath);
                host.Window.Close();
            };
        }
    });

static async Task CaptureWindowScreenshot(Window window, string outputPath)
{
    var fullPath = System.IO.Path.GetFullPath(outputPath);
    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
    GetWindowRect(hwnd, out var rect);
    int w = rect.Right - rect.Left;
    int h = rect.Bottom - rect.Top;
    SetForegroundWindow(hwnd);
    await Task.Delay(200);
    using var bmp = new System.Drawing.Bitmap(w, h);
    using (var g = System.Drawing.Graphics.FromImage(bmp))
        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(w, h));
    bmp.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
}

[DllImport("user32.dll")]
static extern bool GetWindowRect(nint hWnd, out RECT rect);

[DllImport("user32.dll")]
static extern bool SetForegroundWindow(nint hWnd);

[StructLayout(LayoutKind.Sequential)]
struct RECT { public int Left, Top, Right, Bottom; }

static class AppSettings
{
    public static bool ScreenshotMode;
    public static string ScreenshotView = "mail";
    public static string ScreenshotPath = "screenshot.png";
}

abstract record OutlookRoute;
sealed record MailRoute : OutlookRoute;
sealed record CalendarRoute : OutlookRoute;

class OutlookApp : Component
{
    static string? RouteToTag(OutlookRoute route) => route switch
    {
        MailRoute => "mail",
        CalendarRoute => "calendar",
        _ => null,
    };

    static OutlookRoute TagToRoute(string tag) => tag switch
    {
        "calendar" => new CalendarRoute(),
        _ => new MailRoute(),
    };

    public override Element Render()
    {
        var initialRoute = AppSettings.ScreenshotMode && AppSettings.ScreenshotView == "calendar"
            ? (OutlookRoute)new CalendarRoute()
            : new MailRoute();
        var nav = UseNavigation(initialRoute);

        // Left NavigationView rail for Mail/Calendar switching
        return (NavigationView(
            [
                NavItem("Mail", icon: "\uE715", tag: "mail"),
                NavItem("Calendar", icon: "\uE787", tag: "calendar"),
            ],
            NavigationHost(nav, route => route switch
            {
                CalendarRoute => (Element)Component<DuctOutlook.Components.Calendar.CalendarViewComponent>(),
                _ => Component<DuctOutlook.Components.Email.EmailView>(),
            }) with { Transition = NavigationTransition.Slide() }
        ) with
        {
            SelectedTag = RouteToTag(nav.CurrentRoute),
            OnSelectionChanged = tag =>
            {
                if (tag is not null)
                {
                    var route = TagToRoute(tag);
                    if (!Equals(route, nav.CurrentRoute))
                        nav.Navigate(route, new NavigateOptions { PushToBackStack = false });
                }
            },
            PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
            IsSettingsVisible = false,
            IsBackEnabled = false,
            IsPaneOpen = false,
        }).OnMount(fe =>
        {
            var nv = (NavigationView)fe;
            nv.IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed;
            nv.IsPaneToggleButtonVisible = false;
            nv.CompactPaneLength = 48;
        });
    }
}
