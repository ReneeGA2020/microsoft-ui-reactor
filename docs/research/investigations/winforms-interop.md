# Investigation: WinForms ↔ Reactor/WinUI Interop

## Summary

| Direction | Feasibility | Mechanism |
|---|---|---|
| **Reactor/WinUI inside WinForms** | Supported (WinAppSDK 1.4+) | `DesktopWindowXamlSource` (XAML Islands) |
| **WinForms inside Reactor/WinUI** | No official API; workaround available | `CreateWindowEx` child HWND with `WS_EX_LAYERED` |

---

## Direction 1: Hosting Reactor/WinUI Content in a WinForms App

This is the well-supported direction. Microsoft ships an official sample and the API graduated from experimental in WinAppSDK 1.4.

### Architecture

```
┌──────────────────────────────────────────────────┐
│  WinForms Window (top-level HWND)                │
│  ┌────────────────────────────────────────────┐  │
│  │  System.Windows.Forms.Control subclass     │  │
│  │  (wraps DesktopWindowXamlSource)           │  │
│  │  ┌──────────────────────────────────────┐  │  │
│  │  │  DesktopChildSiteBridge child HWND   │  │  │
│  │  │  ┌────────────────────────────────┐  │  │  │
│  │  │  │  WinUI XAML content             │  │  │  │
│  │  │  │  (ReactorHostControl or raw XAML)  │  │  │  │
│  │  │  └────────────────────────────────┘  │  │  │
│  │  └──────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────┘  │
│  [other WinForms controls...]                    │
└──────────────────────────────────────────────────┘
```

### How It Works

`DesktopWindowXamlSource` creates a child HWND (via `DesktopChildSiteBridge`) inside the WinForms window. That child HWND hosts the WinUI compositor and renders WinUI content. You set its `.Content` to any `UIElement` — including a `ReactorHostControl`.

### Step-by-Step Setup

#### 1. Project Configuration (`.csproj`)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <WindowsPackageType>None</WindowsPackageType>
    <!-- Prevent WPF XAML compiler from interfering with WinUI XAML -->
    <ImportFrameworkWinFXTargets>true</ImportFrameworkWinFXTargets>
    <!-- Must target a specific platform, not AnyCPU -->
    <Platforms>x64;x86;arm64</Platforms>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.*" />
    <!-- Project reference to Reactor -->
    <ProjectReference Include="..\..\Reactor\Reactor.csproj" />
  </ItemGroup>
</Project>
```

#### 2. Application Bootstrap (`Program.cs`)

The WinForms app must initialize the WinAppSDK runtime before any WinUI content is created:

```csharp
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System.Runtime.InteropServices;

class Program
{
    // ContentPreTranslateMessage must be hooked into the WinForms message loop
    // so WinAppSDK can process keyboard/input messages before WinForms does.
    [DllImport("Microsoft.UI.Windowing.Core.dll", EntryPoint = "ContentPreTranslateMessage")]
    private static extern int ContentPreTranslateMessage(ref MSG msg);

    [STAThread]
    static void Main()
    {
        // 1. Create DispatcherQueue on the WinForms UI thread.
        //    This is required before any WinUI objects are created.
        var controller = DispatcherQueueController.CreateOnCurrentThread();

        // 2. Create the WinUI Application object.
        //    Required for XamlControlsResources, metadata providers, and theming.
        var xamlApp = new WinFormsHostApp();

        // 3. Hook ContentPreTranslateMessage into WinForms message loop.
        //    Without this, keyboard input inside XAML Islands won't work.
        Application.AddMessageFilter(new XamlMessageFilter());

        // 4. Run WinForms as normal.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());

        // 5. Clean shutdown.
        controller.ShutdownQueue();
    }
}

/// <summary>
/// Minimal WinUI Application that loads theme resources and provides metadata.
/// No Window created — the WinForms app owns the window.
/// </summary>
class WinFormsHostApp : Application, IXamlMetadataProvider
{
    private readonly XamlControlsXamlMetaDataProvider _provider = new();

    public WinFormsHostApp()
    {
        Resources.MergedDictionaries.Add(new XamlControlsResources());
    }

    public IXamlType GetXamlType(Type type) => _provider.GetXamlType(type);
    public IXamlType GetXamlType(string fullName) => _provider.GetXamlType(fullName);
    public XmlnsDefinition[] GetXmlnsDefinitions() => _provider.GetXmlnsDefinitions();
}

/// <summary>
/// Routes messages through WinAppSDK before WinForms processes them.
/// Required for keyboard input, Tab navigation, and accelerators inside XAML Islands.
/// </summary>
class XamlMessageFilter : IMessageFilter
{
    public bool PreFilterMessage(ref Message m)
    {
        var msg = new MSG
        {
            hwnd = m.HWnd,
            message = (uint)m.Msg,
            wParam = m.WParam,
            lParam = m.LParam,
        };
        return ContentPreTranslateMessage(ref msg) != 0;
    }
}
```

#### 3. The XAML Island WinForms Control

This is a `System.Windows.Forms.Control` that wraps `DesktopWindowXamlSource`:

```csharp
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using WinRT.Interop;

/// <summary>
/// WinForms control that hosts WinUI content via XAML Islands.
/// Drop into any WinForms layout — Panel, SplitContainer, TabPage, etc.
/// </summary>
class XamlIslandControl : System.Windows.Forms.Control
{
    private DesktopWindowXamlSource? _source;
    private Microsoft.UI.Xaml.UIElement? _content;

    /// <summary>
    /// The WinUI content to display. Set before or after the control is created.
    /// </summary>
    public Microsoft.UI.Xaml.UIElement? XamlContent
    {
        get => _content;
        set
        {
            _content = value;
            if (_source is not null)
                _source.Content = value;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        _source = new DesktopWindowXamlSource();
        // Attach to this WinForms control's HWND
        var windowId = Win32Interop.GetWindowIdFromWindow(Handle);
        _source.Initialize(windowId);

        // Size the island to fill this control
        _source.SiteBridge.MoveAndResize(
            new RectInt32(0, 0, Width, Height));

        if (_content is not null)
            _source.Content = _content;

        // Focus interop: when WinUI wants to release focus (e.g., Tab out),
        // move focus to the next WinForms control.
        _source.TakeFocusRequested += (_, args) =>
        {
            var reason = args.Request.Reason;
            bool forward = reason == Microsoft.UI.Xaml.Hosting.XamlSourceFocusNavigationReason.First;
            Parent?.SelectNextControl(this, forward, true, true, true);
        };
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _source?.SiteBridge.MoveAndResize(
            new RectInt32(0, 0, Width, Height));
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _source?.NavigateFocus(
            new Microsoft.UI.Xaml.Hosting.XamlSourceFocusNavigationRequest(
                Microsoft.UI.Xaml.Hosting.XamlSourceFocusNavigationReason.Programmatic));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _source?.Dispose();
            _source = null;
        }
        base.Dispose(disposing);
    }
}
```

#### 4. Using It — Reactor Component Inside WinForms

```csharp
class MainForm : Form
{
    public MainForm()
    {
        Text = "WinForms + Reactor Demo";
        Size = new Size(1024, 768);

        // Standard WinForms controls
        var label = new Label { Text = "WinForms Label", Dock = DockStyle.Top, Height = 30 };

        // XAML Island hosting a Reactor component
        var island = new XamlIslandControl { Dock = DockStyle.Fill };

        // Create a ReactorHostControl and mount a Reactor component into it
        var reactorHost = new Reactor.ReactorHostControl();
        reactorHost.Mount(new MyReactorComponent());
        island.XamlContent = reactorHost;

        Controls.Add(island);
        Controls.Add(label);
    }
}
```

### Key Concerns

| Issue | Details |
|---|---|
| **Platform target** | Must be x64/x86/arm64, not AnyCPU |
| **WinAppSDK runtime** | Must be installed on target machine (framework-dependent) or use self-contained deployment |
| **Keyboard input** | Requires `ContentPreTranslateMessage` hook in WinForms message filter |
| **Focus/Tab** | Manual `NavigateFocus` + `TakeFocusRequested` handling between WinForms and WinUI |
| **Threading** | Must be STA thread; `DispatcherQueue` must be created before any WinUI objects |
| **Popups/Flyouts** | WinUI popups may escape the WinForms window bounds; use `ShouldConstrainPopupsToWorkArea` |
| **Designer** | No WinForms designer support — `XamlIslandControl` is positioned in code or as a generic control |
| **DPI** | Both WinForms (PerMonitorV2) and WinUI handle DPI natively; ensure the WinForms app is DPI-aware |

---

## Direction 2: Hosting a WinForms Control Inside Reactor/WinUI

This direction is **not officially supported by Microsoft**. WinUI 3 has no equivalent to WPF's `WindowsFormsHost` or `HwndHost`. There are workarounds with significant limitations.

### Why It's Hard

WinUI 3's rendering model is fundamentally different from Win32/WinForms:

- **WinUI** renders through a **composition visual tree** (DirectComposition / DWM). There is one top-level HWND; all content is rendered by the compositor, not by individual child HWNDs.
- **WinForms** controls are individual **child HWNDs** that paint via `WM_PAINT` / GDI/GDI+.
- Mixing the two creates the classic **airspace problem**: Win32 child windows always render on top of the compositor surface, regardless of XAML z-order.

### Approach A: Child HWND with WS_EX_LAYERED (Best Available Workaround)

Create the WinForms control as a Win32 child window of the WinUI window's HWND.

```
┌──────────────────────────────────────────────────┐
│  WinUI Window (top-level HWND)                   │
│  ┌────────────────────────────────────────────┐  │
│  │  WinUI XAML content (compositor layer)     │  │
│  │                                            │  │
│  │  ┌──────────────────────┐  ← always on top │  │
│  │  │  WinForms child HWND │    (airspace)    │  │
│  │  │  (GDI rendering)     │                  │  │
│  │  └──────────────────────┘                  │  │
│  └────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘
```

#### Implementation: WinFormsHostElement

A custom Reactor element that embeds a WinForms control:

```csharp
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Reactor element that hosts a WinForms control inside the WinUI tree.
///
/// LIMITATIONS:
///   - The WinForms control renders ON TOP of all WinUI content (airspace problem).
///     WinUI popups, flyouts, and tooltips will render behind it.
///   - The control is positioned manually via SetWindowPos — it does NOT participate
///     in WinUI layout. You must set explicit Width/Height.
///   - Requires WS_EX_LAYERED to prevent WinUI compositor from overpainting.
/// </summary>
public record WinFormsHostElement(
    Func<System.Windows.Forms.Control> Factory,
    Action<System.Windows.Forms.Control>? Updater = null,
    int Width = 300,
    int Height = 200
) : Element;

public static class WinFormsInterop
{
    // Win32 interop
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_EX_LAYERED = 0x00080000;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    /// <summary>
    /// Register WinFormsHostElement with a Reactor Reconciler.
    /// Call once during app startup.
    /// </summary>
    public static void Register(Reconciler reconciler, Window winuiWindow)
    {
        var parentHwnd = WinRT.Interop.WindowNative.GetWindowHandle(winuiWindow);

        reconciler.RegisterType<WinFormsHostElement, Border>(
            mount: (r, el, rerender) =>
            {
                // Create WinForms control
                var winFormsControl = el.Factory();
                winFormsControl.Size = new System.Drawing.Size(el.Width, el.Height);

                // Force HWND creation
                var handle = winFormsControl.Handle;

                // Reparent: make the WinForms control a child of the WinUI HWND
                SetWindowLong(handle, GWL_STYLE,
                    (GetWindowLong(handle, GWL_STYLE) | WS_CHILD | WS_VISIBLE));

                // WS_EX_LAYERED prevents WinUI compositor from overpainting
                SetWindowLong(handle, GWL_EXSTYLE,
                    GetWindowLong(handle, GWL_EXSTYLE) | WS_EX_LAYERED);

                SetParent(handle, parentHwnd);
                ShowWindow(handle, 1 /* SW_SHOWNORMAL */);

                el.Updater?.Invoke(winFormsControl);

                // Create a Border placeholder in WinUI that tracks layout position.
                // We use its LayoutUpdated event to reposition the Win32 child.
                var placeholder = new Border
                {
                    Width = el.Width,
                    Height = el.Height,
                    Tag = (winFormsControl, handle),
                };

                placeholder.LayoutUpdated += (_, _) =>
                {
                    // Get the placeholder's position relative to the WinUI window
                    try
                    {
                        var transform = placeholder.TransformToVisual(winuiWindow.Content);
                        var pos = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                        SetWindowPos(handle, IntPtr.Zero,
                            (int)pos.X, (int)pos.Y, el.Width, el.Height,
                            SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    }
                    catch { /* Control may not be in visual tree yet */ }
                };

                return placeholder;
            },
            update: (r, oldEl, newEl, border, rerender) =>
            {
                if (border.Tag is (System.Windows.Forms.Control ctrl, IntPtr hwnd))
                {
                    newEl.Updater?.Invoke(ctrl);
                    border.Width = newEl.Width;
                    border.Height = newEl.Height;
                    ctrl.Size = new System.Drawing.Size(newEl.Width, newEl.Height);
                }
                return null;
            },
            unmount: (r, border) =>
            {
                if (border.Tag is (System.Windows.Forms.Control ctrl, IntPtr _))
                {
                    ctrl.Dispose();
                    border.Tag = null;
                }
            });
    }
}
```

#### Usage in Reactor

```csharp
class MyApp : Component
{
    public override Element Render()
    {
        return VStack(
            Text("This is Reactor/WinUI content"),

            // Embed a WinForms DataGridView
            new WinFormsHostElement(
                Factory: () =>
                {
                    var grid = new System.Windows.Forms.DataGridView();
                    grid.Columns.Add("Name", "Name");
                    grid.Columns.Add("Value", "Value");
                    grid.Rows.Add("Hello", "World");
                    return grid;
                },
                Width: 500,
                Height: 300
            ),

            Text("More Reactor content below")
        );
    }
}

// Registration at startup:
ReactorApp.Run<MyApp>("Demo", configure: host =>
{
    WinFormsInterop.Register(host.Reconciler, host.Window);
});
```

### Approach A Limitations

| Limitation | Impact | Mitigation |
|---|---|---|
| **Airspace problem** | WinForms control always renders on top of WinUI content. Popups, flyouts, tooltips render behind it. | Keep WinForms controls at the edges of the layout, not overlapping with interactive WinUI elements |
| **No WinUI layout participation** | Control is positioned via `SetWindowPos`, not XAML layout. Must set explicit size. | Use `LayoutUpdated` on a placeholder `Border` to track position |
| **DPI scaling** | WinForms and WinUI may use different DPI scaling | Ensure both frameworks are PerMonitorV2 aware |
| **Focus/input** | Tab navigation between WinUI and WinForms needs manual wiring | Handle `WM_SETFOCUS` / `GotFocus` to bridge focus |
| **Lifecycle** | WinForms control must be disposed manually | Handle in `unmount` callback |
| **Scrolling** | If the placeholder scrolls in a WinUI `ScrollViewer`, the Win32 child won't clip properly | Avoid hosting WinForms controls in scrollable regions |

### Approach B: ContentExternalOutputLink (Visual Only — Experimental)

WinAppSDK has an experimental `ContentExternalOutputLink` that can capture a HWND's visuals into a composition surface:

```
IDCompositionDesktopDevice.CreateSurfaceFromHwnd(hwnd)
  → Composition Visual
  → ElementCompositionPreview.SetElementChildVisual(image, visual)
```

This **captures the visual appearance** but does **not forward input**. Useful only for displaying static content (e.g., a preview thumbnail of a WinForms control). Not suitable for interactive embedding.

### Approach C: Separate Window (Pragmatic Alternative)

For complex WinForms controls where airspace is unacceptable, use a **separate WinForms window positioned alongside** the WinUI window:

```csharp
var (winFormsOpen, setWinFormsOpen) = UseState(false);

UseEffect(() =>
{
    if (winFormsOpen)
    {
        var form = new Form { Text = "WinForms Panel", TopMost = true };
        form.Controls.Add(new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = myObj });

        // Position next to WinUI window
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(ReactorApp.ActiveHost!.Window);
        // ... position logic ...

        form.Show();
        return () => form.Close();
    }
    return null;
}, winFormsOpen);
```

This avoids all airspace issues but is a separate top-level window, not an embedded control.

---

## Open Issues in WinUI (microsoft/microsoft-ui-xaml)

- **Issue #10050** — "ThousandIslands Window": proposal for hosting Win32 Islands (WinForms, WPF, ActiveX, MFC) inside WinUI3 via `ContentIsland`. No official commitment.
- **Issue #4501** — "Support embedding another executable inside a UserControl with WinUI 3 desktop". Open since 2021, no resolution.

---

## Recommendation

### For hosting Reactor in WinForms:
Use `DesktopWindowXamlSource` + `ReactorHostControl`. This is the officially supported path. Reactor's `ReactorHostControl` is a self-contained `ContentControl` that works perfectly inside a XAML Island — no `ReactorApp` or `ReactorApplication` bootstrapping needed. The main work is the one-time setup of the WinForms `XamlIslandControl` wrapper and the `Program.cs` bootstrap.

### For hosting WinForms in Reactor:
Use Approach A (child HWND with `WS_EX_LAYERED`) for simple cases where airspace is acceptable (e.g., a PropertyGrid or DataGridView at a fixed position). For complex scenarios, use Approach C (separate window) or consider whether the WinForms control can be replaced with a WinUI equivalent.

If this is a common need, a `WinFormsHostElement` could be added to Reactor's `Hosting/` directory alongside `XamlInterop.cs`, registered via a `WinFormsInterop.Register(reconciler, window)` call similar to `XamlInterop.Register(reconciler)`.
