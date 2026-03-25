using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace RegeditWinUI.Dialogs;

public static class EditMultiStringDialog
{
    public static async Task<string[]?> ShowAsync(XamlRoot xamlRoot, string valueName, string[]? currentLines)
    {
        var nameBox = new TextBox
        {
            Text = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName,
            IsReadOnly = true,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var dataBox = new TextBox
        {
            Text = currentLines != null ? string.Join(Environment.NewLine, currentLines) : string.Empty,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 200
        };

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = Strings.LabelValueName });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = Strings.LabelValueData });
        panel.Children.Add(dataBox);

        var dialog = new ContentDialog
        {
            Title = Strings.DialogEditMultiString,
            Content = panel,
            PrimaryButtonText = Strings.ButtonOK,
            CloseButtonText = Strings.ButtonCancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return null;

        return dataBox.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }
}
