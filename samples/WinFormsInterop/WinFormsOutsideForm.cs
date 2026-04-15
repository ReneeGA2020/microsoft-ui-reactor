using Duct;
using Duct.Interop.WinForms;
using WinForms = System.Windows.Forms;

namespace WinFormsInterop.Sample;

/// <summary>
/// WinForms Form that demonstrates hosting Duct/WinUI content via a XAML Island.
/// Left panel: native WinForms controls. Right panel: XAML Island with a Duct component.
/// </summary>
class WinFormsOutsideForm : WinForms.Form
{
    public WinFormsOutsideForm()
    {
        Text = "WinForms hosts Duct";
        Size = new System.Drawing.Size(950, 600);
        BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
        ForeColor = System.Drawing.Color.White;

        // ── Left panel: native WinForms controls ────────────────────
        var leftPanel = new WinForms.Panel
        {
            Dock = WinForms.DockStyle.Left,
            Width = 300,
            Padding = new WinForms.Padding(12),
            BackColor = System.Drawing.Color.FromArgb(40, 40, 40),
        };

        var title = new WinForms.Label
        {
            Text = "WinForms Controls",
            Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.White,
            AutoSize = false,
            Dock = WinForms.DockStyle.Top,
            Height = 36,
        };

        var description = new WinForms.Label
        {
            Text = "This panel is native WinForms.\n\nThe right side is a XAML Island\nhosting a Duct component tree\nwith WinUI controls.",
            ForeColor = System.Drawing.Color.FromArgb(180, 180, 180),
            AutoSize = false,
            Dock = WinForms.DockStyle.Top,
            Height = 80,
        };

        var inputLabel = new WinForms.Label
        {
            Text = "WinForms TextBox:",
            ForeColor = System.Drawing.Color.FromArgb(180, 180, 180),
            Dock = WinForms.DockStyle.Top,
            Height = 20,
        };

        var textBox = new WinForms.TextBox
        {
            Dock = WinForms.DockStyle.Top,
            BackColor = System.Drawing.Color.FromArgb(50, 50, 50),
            ForeColor = System.Drawing.Color.White,
            BorderStyle = WinForms.BorderStyle.FixedSingle,
            Text = "Type here (WinForms)",
        };

        var button = new WinForms.Button
        {
            Text = "WinForms Button — Click Me",
            Dock = WinForms.DockStyle.Top,
            Height = 35,
            FlatStyle = WinForms.FlatStyle.Flat,
            BackColor = System.Drawing.Color.FromArgb(0, 120, 212),
            ForeColor = System.Drawing.Color.White,
            Margin = new WinForms.Padding(0, 8, 0, 0),
        };

        var logLabel = new WinForms.Label
        {
            Text = "Event Log:",
            ForeColor = System.Drawing.Color.FromArgb(140, 140, 140),
            Dock = WinForms.DockStyle.Top,
            Height = 24,
        };

        var logList = new WinForms.ListBox
        {
            Dock = WinForms.DockStyle.Fill,
            BackColor = System.Drawing.Color.FromArgb(25, 25, 25),
            ForeColor = System.Drawing.Color.FromArgb(200, 200, 200),
            BorderStyle = WinForms.BorderStyle.None,
        };

        button.Click += (_, _) =>
            logList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] WinForms button clicked");

        textBox.TextChanged += (_, _) =>
            logList.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Text: {textBox.Text}");

        // WinForms Dock layout: add in reverse order (last = Fill)
        leftPanel.Controls.Add(logList);
        leftPanel.Controls.Add(logLabel);
        leftPanel.Controls.Add(button);
        leftPanel.Controls.Add(textBox);
        leftPanel.Controls.Add(inputLabel);
        leftPanel.Controls.Add(description);
        leftPanel.Controls.Add(title);

        // ── Splitter ────────────────────────────────────────────────
        var splitter = new WinForms.Splitter
        {
            Dock = WinForms.DockStyle.Left,
            Width = 4,
            BackColor = System.Drawing.Color.FromArgb(60, 60, 60),
        };

        // ── Right panel: XAML Island with Duct component ────────────
        var island = new XamlIslandControl
        {
            Dock = WinForms.DockStyle.Fill,
        };

        var ductHost = new DuctHostControl();
        ductHost.Mount(new SampleDuctComponent());

        // DesktopWindowXamlSource doesn't stretch its Content like a Window does.
        // Wrap in a Grid (which fills available space) with a themed background.
        var root = new Microsoft.UI.Xaml.Controls.Grid();
        root.Background = (Microsoft.UI.Xaml.Media.Brush)
            Microsoft.UI.Xaml.Application.Current.Resources["ApplicationPageBackgroundThemeBrush"];
        root.Children.Add(ductHost);
        island.XamlContent = root;

        // Dock layout order: left panel, splitter, then island fills the rest
        Controls.Add(island);
        Controls.Add(splitter);
        Controls.Add(leftPanel);
    }
}
