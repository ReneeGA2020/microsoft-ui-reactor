using Duct;
using Duct.Core;
using Duct.Interop.WinForms;
using Microsoft.UI.Xaml;
using static Duct.UI;
using WinForms = System.Windows.Forms;

namespace WinFormsInterop.Sample;

/// <summary>
/// Duct component that demonstrates hosting a WinForms control inside a Duct tree.
/// Shows Duct UI (title, counter, buttons) alongside an embedded WinForms DataGridView.
/// </summary>
class DuctOutsideComponent : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        var (gridRows, setGridRows) = UseState(3);

        return VStack(
            // ── Header ──────────────────────────────────
            Text("Duct / WinUI on the Outside")
                .FontSize(24)
                .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                .Margin(0, 0, 0, 4),

            Text("This window is rendered by Duct/WinUI. The DataGridView below is a WinForms control embedded via WinFormsHostElement.")
                .Opacity(0.6)
                .Margin(0, 0, 0, 16),

            // ── Duct interactive controls ────────────────
            HStack(
                Button($"Count: {count}", () => setCount(count + 1)),
                Button($"Add Row (rows={gridRows})", () => setGridRows(gridRows + 1)),
                Button("Reset", () => { setCount(0); setGridRows(3); })
            ).Margin(0, 0, 0, 16),

            // ── Embedded WinForms DataGridView ──────────
            Text("WinForms DataGridView (embedded via child HWND)")
                .FontSize(11)
                .Opacity(0.4)
                .Margin(0, 0, 0, 4),

            new WinFormsHostElement(
                Factory: CreateDataGridView,
                Updater: ctrl => UpdateDataGridView((WinForms.DataGridView)ctrl, gridRows, count),
                Width: 840,
                Height: 280
            ),

            // ── Footer ──────────────────────────────────
            Text($"Duct render count: {count} | Grid rows: {gridRows}")
                .Margin(0, 16, 0, 0)
                .FontSize(11)
                .Opacity(0.4)

        ).Padding(24);
    }

    private static WinForms.DataGridView CreateDataGridView()
    {
        var grid = new WinForms.DataGridView
        {
            AutoSizeColumnsMode = WinForms.DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            BackgroundColor = System.Drawing.Color.FromArgb(32, 32, 32),
            ForeColor = System.Drawing.Color.White,
            GridColor = System.Drawing.Color.FromArgb(60, 60, 60),
            BorderStyle = WinForms.BorderStyle.None,
            ColumnHeadersBorderStyle = WinForms.DataGridViewHeaderBorderStyle.Single,
            EnableHeadersVisualStyles = false,
            ColumnHeadersDefaultCellStyle = new WinForms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(45, 45, 45),
                ForeColor = System.Drawing.Color.White,
                SelectionBackColor = System.Drawing.Color.FromArgb(45, 45, 45),
            },
            DefaultCellStyle = new WinForms.DataGridViewCellStyle
            {
                BackColor = System.Drawing.Color.FromArgb(32, 32, 32),
                ForeColor = System.Drawing.Color.White,
                SelectionBackColor = System.Drawing.Color.FromArgb(0, 120, 212),
            },
        };
        grid.Columns.Add("Name", "Name");
        grid.Columns.Add("Framework", "Framework");
        grid.Columns.Add("Status", "Status");
        return grid;
    }

    private static void UpdateDataGridView(WinForms.DataGridView grid, int rowCount, int activeCount)
    {
        grid.Rows.Clear();
        for (int i = 0; i < rowCount; i++)
        {
            grid.Rows.Add(
                $"Item {i + 1}",
                "WinForms",
                i < activeCount ? "Active" : "Pending");
        }
    }
}
