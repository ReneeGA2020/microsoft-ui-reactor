using System.Runtime.InteropServices;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SWF = global::System.Windows.Forms;

namespace Duct.Interop.WinForms;

/// <summary>
/// A Duct element that embeds a WinForms control inside a WinUI/Duct component tree.
///
/// The WinForms control is reparented as a Win32 child window of the host HWND and
/// positioned to overlay a placeholder Border in the XAML layout tree.
///
/// KNOWN LIMITATIONS:
///   - Airspace: The WinForms control renders ON TOP of all WinUI content.
///     WinUI popups, flyouts, and tooltips will appear behind it.
///   - Scrolling: The Win32 child window won't clip to a WinUI ScrollViewer viewport.
///   - Input: Click to focus — keyboard focus does not auto-transfer between frameworks.
///
/// Usage in a Duct component:
///   new WinFormsHostElement(
///       Factory: () => new DataGridView { ... },
///       Updater: ctrl => ((DataGridView)ctrl).Rows.Clear(),
///       Width: 500, Height: 300)
/// </summary>
public record WinFormsHostElement(
    Func<SWF.Control> Factory,
    Action<SWF.Control>? Updater = null,
    double Width = 300,
    double Height = 200
) : Element;

/// <summary>
/// Registers <see cref="WinFormsHostElement"/> with a Duct Reconciler so that
/// WinForms controls can be embedded in Duct component trees.
///
/// Call once during app startup, before mounting any component that uses WinFormsHostElement.
///
/// Usage:
///   // DuctApp.Run mode:
///   WinFormsHostBridge.Register(host.Reconciler,
///       () => WinRT.Interop.WindowNative.GetWindowHandle(host.Window));
///
///   // DuctHostControl inside a WinForms Form:
///   WinFormsHostBridge.Register(ductHost.Reconciler, () => form.Handle);
/// </summary>
public static class WinFormsHostBridge
{
    #region Win32 interop

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter,
        string lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_CLIPCHILDREN = 0x02000000;
    private const int WS_CLIPSIBLINGS = 0x04000000;
    private const int WS_EX_LAYERED = 0x00080000;
    private const uint LWA_ALPHA = 0x02;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private const int SW_HIDE = 0;
    private const int SW_SHOWNORMAL = 1;

    #endregion

    /// <summary>
    /// Registers the WinFormsHostElement with the given Reconciler.
    /// </summary>
    /// <param name="reconciler">
    /// The Reconciler to register with — from <see cref="DuctHost.Reconciler"/>
    /// or <see cref="DuctHostControl.Reconciler"/>.
    /// </param>
    /// <param name="getParentHwnd">
    /// Returns the HWND that Win32 child windows will be parented to.
    /// For DuctApp: <c>() => WindowNative.GetWindowHandle(host.Window)</c>.
    /// For WinForms island: <c>() => form.Handle</c>.
    /// </param>
    public static void Register(Reconciler reconciler, Func<IntPtr> getParentHwnd)
    {
        reconciler.RegisterType<WinFormsHostElement, Border>(
            mount: (r, el, rerender) => MountControl(el, getParentHwnd),
            update: (r, oldEl, newEl, border, rerender) => UpdateControl(oldEl, newEl, border),
            unmount: (r, border) => UnmountControl(border));
    }

    private const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int GWL_HWNDPARENT = -8; // sets the owner window

    private static Border MountControl(WinFormsHostElement el, Func<IntPtr> getParentHwnd)
    {
        var parentHwnd = getParentHwnd();

        // Detect WinUI Windows — they use WS_EX_NOREDIRECTIONBITMAP (compositor-only,
        // no GDI surface). GDI-based WinForms controls can't render as children of
        // such windows. Use an owned overlay Form as the rendering surface instead.
        bool useOverlay = (GetWindowLong(parentHwnd, GWL_EXSTYLE)
            & WS_EX_NOREDIRECTIONBITMAP) != 0;

        var control = el.Factory();
        control.Size = new System.Drawing.Size((int)el.Width, (int)el.Height);

        SWF.Form? overlay = null;
        if (useOverlay)
        {
            // Create a borderless tool window owned by the WinUI Window.
            // It has a real GDI surface where WinForms controls can paint.
            overlay = new SWF.Form
            {
                FormBorderStyle = SWF.FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = SWF.FormStartPosition.Manual,
                Size = control.Size,
            };
            // Make it a tool window (no taskbar entry, no Alt-Tab)
            overlay.CreateControl();
            SetWindowLong(overlay.Handle, GWL_EXSTYLE,
                GetWindowLong(overlay.Handle, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
            // Set the WinUI Window as owner — overlay stays on top of it
            SetWindowLong(overlay.Handle, GWL_HWNDPARENT, (int)parentHwnd);

            control.Dock = SWF.DockStyle.Fill;
            overlay.Controls.Add(control);
            overlay.Show();
        }
        else
        {
            // Normal case (WinForms parent with GDI surface) — child HWND directly
            _ = control.Handle;
            SetWindowLong(control.Handle, GWL_STYLE,
                GetWindowLong(control.Handle, GWL_STYLE)
                | WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS);
            SetWindowLong(control.Handle, GWL_EXSTYLE,
                GetWindowLong(control.Handle, GWL_EXSTYLE) | WS_EX_LAYERED);
            SetLayeredWindowAttributes(control.Handle, 0, 255, LWA_ALPHA);
            SetParent(control.Handle, parentHwnd);
            ShowWindow(control.Handle, SW_SHOWNORMAL);
        }

        el.Updater?.Invoke(control);

        var hwndToPosition = useOverlay ? overlay!.Handle : control.Handle;
        var state = new HostState(control, hwndToPosition, el.Width, el.Height, overlay);
        var placeholder = new Border
        {
            Width = el.Width,
            Height = el.Height,
            Tag = state,
        };

        placeholder.Loaded += (_, _) => SyncPosition(placeholder, state);
        placeholder.LayoutUpdated += (_, _) => SyncPosition(placeholder, state);

        return placeholder;
    }

    private static Border? UpdateControl(
        WinFormsHostElement oldEl, WinFormsHostElement newEl, Border border)
    {
        if (border.Tag is not HostState state)
            return null;

        newEl.Updater?.Invoke(state.Control);

        if (Math.Abs(oldEl.Width - newEl.Width) > 0.5 ||
            Math.Abs(oldEl.Height - newEl.Height) > 0.5)
        {
            border.Width = newEl.Width;
            border.Height = newEl.Height;
            state.Width = newEl.Width;
            state.Height = newEl.Height;
            state.Control.Size = new System.Drawing.Size((int)newEl.Width, (int)newEl.Height);
        }

        return null; // updated in place
    }

    private static void UnmountControl(Border border)
    {
        if (border.Tag is not HostState state)
            return;

        ShowWindow(state.Hwnd, SW_HIDE);
        state.Overlay?.Close();
        state.Overlay?.Dispose();
        state.Control.Dispose();
        border.Tag = null;
    }

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    private static void SyncPosition(Border placeholder, HostState state)
    {
        try
        {
            var root = placeholder.XamlRoot?.Content;
            if (root is null) return;

            var transform = placeholder.TransformToVisual(root);
            var pos = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

            // TransformToVisual returns DIPs; SetWindowPos expects device pixels
            var scale = placeholder.XamlRoot?.RasterizationScale ?? 1.0;
            int x = (int)(pos.X * scale);
            int y = (int)(pos.Y * scale);
            int w = (int)(state.Width * scale);
            int h = (int)(state.Height * scale);

            if (state.Overlay is not null)
            {
                // Overlay is a top-level window — convert to screen coordinates.
                // Find the WinUI Window HWND (the overlay's owner) to map from.
                var ownerHwnd = GetParent(state.Hwnd);
                if (ownerHwnd == IntPtr.Zero) ownerHwnd = state.Hwnd;
                var pt = new POINT { X = x, Y = y };
                ClientToScreen(ownerHwnd, ref pt);
                SetWindowPos(state.Hwnd, HWND_TOP, pt.X, pt.Y, w, h,
                    SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
            else
            {
                // Child HWND — coordinates are relative to parent
                SetWindowPos(state.Hwnd, HWND_TOP, x, y, w, h,
                    SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }
        catch
        {
            // TransformToVisual can throw if the element isn't in the visual tree yet
        }
    }

    private class HostState(SWF.Control control, IntPtr hwnd, double width, double height, SWF.Form? overlay = null)
    {
        public SWF.Control Control => control;
        public IntPtr Hwnd => hwnd;
        public double Width { get; set; } = width;
        public double Height { get; set; } = height;
        public SWF.Form? Overlay => overlay;
    }
}
