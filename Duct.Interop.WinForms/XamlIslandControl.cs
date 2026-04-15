using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Windows.Graphics;
using SWF = global::System.Windows.Forms;

namespace Duct.Interop.WinForms;

/// <summary>
/// A WinForms control that hosts WinUI/XAML content via DesktopWindowXamlSource (XAML Islands).
/// Place this in any WinForms layout — Panel, SplitContainer, TabPage, etc.
///
/// Set <see cref="XamlContent"/> to any WinUI UIElement, including <see cref="DuctHostControl"/>
/// for hosting Duct component trees.
///
/// Prerequisites (call before creating this control):
///   - DispatcherQueue must exist on the UI thread
///   - A WinUI Application must be instantiated (for theme resources / metadata)
///   - ContentPreTranslateMessage must be hooked (for keyboard input)
///   Use <see cref="XamlIslandBootstrap.Initialize"/> to handle all three.
/// </summary>
public class XamlIslandControl : SWF.Control
{
    private DesktopWindowXamlSource? _source;
    private UIElement? _pendingContent;

    /// <summary>
    /// The WinUI content to display. Can be set before or after the control handle is created.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public UIElement? XamlContent
    {
        get => _source?.Content ?? _pendingContent;
        set
        {
            if (_source is not null)
                _source.Content = value;
            else
                _pendingContent = value;
        }
    }

    /// <summary>
    /// The underlying DesktopWindowXamlSource. Available after the control handle is created.
    /// </summary>
    [Browsable(false)]
    public DesktopWindowXamlSource? Source => _source;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        _source = new DesktopWindowXamlSource();

        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(Handle);
        _source.Initialize(windowId);

        // Constrain WinUI popups/flyouts/tooltips to the work area so they don't
        // escape the WinForms window bounds.
        _source.ShouldConstrainPopupsToWorkArea = true;

        // Initial size — may be default size before Dock layout runs
        UpdateBridgeSize();

        if (_pendingContent is not null)
        {
            _source.Content = _pendingContent;
            _pendingContent = null;
        }

        // When XAML releases focus (Tab out), move to the next WinForms control.
        // Only handle First (forward Tab) and Last (Shift+Tab) — ignore Programmatic,
        // Restore, and directional reasons which shouldn't leave the island.
        _source.TakeFocusRequested += (_, args) =>
        {
            var reason = args.Request.Reason;
            if (reason is XamlSourceFocusNavigationReason.First
                       or XamlSourceFocusNavigationReason.Last)
            {
                bool forward = reason == XamlSourceFocusNavigationReason.First;
                Parent?.SelectNextControl(this, forward,
                    tabStopOnly: true, nested: true, wrap: true);
            }
        };
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateBridgeSize();
    }

    protected override void OnLayout(SWF.LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        // OnLayout fires when Dock/Anchor layout adjusts this control's bounds —
        // more reliable than OnResize alone for catching the initial Dock=Fill sizing.
        UpdateBridgeSize();
    }

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    private void UpdateBridgeSize()
    {
        if (_source is null || !IsHandleCreated) return;

        // Get the physical pixel size of this control's HWND
        if (!GetClientRect(Handle, out var rc)) return;
        int w = rc.Right - rc.Left;
        int h = rc.Bottom - rc.Top;
        if (w <= 0 || h <= 0) return;

        // Resize via the SiteBridge API
        _source.SiteBridge.MoveAndResize(new RectInt32(0, 0, w, h));

        // Also directly resize the bridge's child HWND as a fallback —
        // some WinAppSDK experimental versions don't fully apply MoveAndResize.
        try
        {
            var bridgeHwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(
                _source.SiteBridge.WindowId);
            if (bridgeHwnd != IntPtr.Zero)
                SetWindowPos(bridgeHwnd, IntPtr.Zero, 0, 0, w, h,
                    SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
        catch { /* WindowId may not be available in all configurations */ }

        // Debug: uncomment to diagnose sizing issues
        // System.Diagnostics.Debug.WriteLine(
        //     $"[XamlIsland] UpdateBridgeSize: {w}x{h}  dpi={DeviceDpi}");
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        // Check Shift state at the moment focus arrives: if Shift is held, the user
        // Shift+Tabbed backward into the island → focus the last element. Otherwise
        // focus the first element (forward Tab or programmatic focus).
        bool shiftHeld = (SWF.Control.ModifierKeys & SWF.Keys.Shift) != 0;
        var reason = shiftHeld
            ? XamlSourceFocusNavigationReason.Last
            : XamlSourceFocusNavigationReason.First;
        _source?.NavigateFocus(new XamlSourceFocusNavigationRequest(reason));
    }

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateBridgeSize();
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
