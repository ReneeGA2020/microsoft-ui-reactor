using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace RegeditWinUI.Dialogs;

public static class ExportDialog
{
    public static async Task<(bool ExportAll, string BranchPath)?> ShowAsync(XamlRoot xamlRoot, string currentPath)
    {
        var allRadio = new RadioButton
        {
            Content = Strings.LabelExportAll,
            IsChecked = false,
            GroupName = "ExportRange"
        };
        var branchRadio = new RadioButton
        {
            Content = Strings.LabelExportSelectedBranch,
            IsChecked = true,
            GroupName = "ExportRange"
        };
        var branchBox = new TextBox
        {
            Text = currentPath,
            Margin = new Thickness(24, 0, 0, 0)
        };

        allRadio.Checked += (s, e) => branchBox.IsEnabled = false;
        branchRadio.Checked += (s, e) => branchBox.IsEnabled = true;

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = Strings.LabelExportRange, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(allRadio);
        panel.Children.Add(branchRadio);
        panel.Children.Add(branchBox);

        var dialog = new ContentDialog
        {
            Title = Strings.DialogExport,
            Content = panel,
            PrimaryButtonText = Strings.ButtonOK,
            CloseButtonText = Strings.ButtonCancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return null;

        return (allRadio.IsChecked == true, branchBox.Text);
    }
}
