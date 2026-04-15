using Duct;
using Duct.Interop.WinForms;
using WinForms = System.Windows.Forms;

namespace WinFormsInterop.Sample;

/// <summary>
/// WinForms ↔ Duct interop sample.
///
/// Creates two top-level windows:
///   Window 1: Duct/WinUI on the "outside" hosting a WinForms DataGridView
///   Window 2: WinForms on the "outside" hosting a Duct component via XAML Island
///
/// Usage:
///   dotnet run                   # WinForms-primary mode (default)
///   dotnet run -- --winforms     # WinForms-primary mode (explicit)
///   dotnet run -- --duct         # Duct-primary mode
/// </summary>
static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var mode = args.Contains("--duct") ? "duct" : "winforms";

        Console.WriteLine($"[WinFormsInterop] Starting in {mode}-primary mode");
        Console.WriteLine("[WinFormsInterop] Two windows will open:");
        Console.WriteLine("  1) Duct on the outside, hosting a WinForms DataGridView");
        Console.WriteLine("  2) WinForms on the outside, hosting a Duct component");

        if (mode == "winforms")
            RunWinFormsPrimary();
        else
            RunDuctPrimary();
    }

    /// <summary>
    /// WinUI owns the message loop via Application.Start.
    /// The WinForms window is shown as a secondary modeless window.
    /// </summary>
    static void RunDuctPrimary()
    {
        // WinForms controls need basic initialization even when WinUI owns the process.
        // Without this, WinForms controls (like the embedded DataGridView) may not paint.
        WinForms.Application.EnableVisualStyles();
        WinForms.Application.SetCompatibleTextRenderingDefault(false);

        DuctApp.Run<DuctOutsideComponent>(
            "Duct hosts WinForms [duct-primary]",
            width: 900, height: 700,
            configure: host =>
            {
                // Register WinFormsHostElement so the component can embed WinForms controls
                WinFormsHostBridge.Register(host.Reconciler,
                    () => WinRT.Interop.WindowNative.GetWindowHandle(host.Window));

                // Open the second window: WinForms on the outside, hosting Duct
                var form = new WinFormsOutsideForm();
                form.Show();
            });
    }

    /// <summary>
    /// WinForms-primary: XamlIslandBootstrap.Run() uses Application.Start for the
    /// message loop. WinForms windows work inside it — they're standard Win32 windows.
    /// </summary>
    static void RunWinFormsPrimary()
    {
        WinForms.Application.EnableVisualStyles();
        WinForms.Application.SetCompatibleTextRenderingDefault(false);

        // Application.Start initializes XAML runtime and runs the message loop.
        // The callback fires once everything is ready.
        XamlIslandBootstrap.Run(() =>
        {
            // Window 1: Duct on the outside (full-bleed XAML Island in a WinForms Form)
            var ductWindow = CreateDuctOutsideForm();

            // Window 2: WinForms on the outside with a Duct island
            var winFormsWindow = new WinFormsOutsideForm();

            ductWindow.Show();
            winFormsWindow.Show();

            // Exit when both windows are closed
            int openForms = 2;
            WinForms.FormClosedEventHandler onClosed = (_, _) =>
            {
                if (Interlocked.Decrement(ref openForms) <= 0)
                    Microsoft.UI.Xaml.Application.Current.Exit();
            };
            ductWindow.FormClosed += onClosed;
            winFormsWindow.FormClosed += onClosed;
        });
    }

    /// <summary>
    /// Creates a WinForms Form whose entire client area is a XAML Island
    /// hosting the DuctOutsideComponent (which in turn embeds a WinForms control).
    /// Used in WinForms-primary mode where we can't create a WinUI Window directly.
    /// </summary>
    static WinForms.Form CreateDuctOutsideForm()
    {
        var form = new WinForms.Form
        {
            Text = "Duct hosts WinForms [winforms-primary]",
            Size = new System.Drawing.Size(900, 700),
            BackColor = System.Drawing.Color.FromArgb(30, 30, 30),
        };

        var island = new XamlIslandControl { Dock = WinForms.DockStyle.Fill };

        var ductHost = new DuctHostControl();
        WinFormsHostBridge.Register(ductHost.Reconciler, () => form.Handle);
        ductHost.Mount(new DuctOutsideComponent());

        // DesktopWindowXamlSource doesn't stretch its Content like a Window does.
        // Wrap in a Grid (which fills available space) with a themed background.
        var root = new Microsoft.UI.Xaml.Controls.Grid();
        root.Background = (Microsoft.UI.Xaml.Media.Brush)
            Microsoft.UI.Xaml.Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
        root.Children.Add(ductHost);
        island.XamlContent = root;
        form.Controls.Add(island);

        return form;
    }
}
