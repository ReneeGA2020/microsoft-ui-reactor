using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace RegeditWinUI.Dialogs;

public static class EditStringDialog
{
    public static async Task<string?> ShowAsync(XamlRoot xamlRoot, string valueName, string? currentData, bool isExpandString)
    {
        var nameBox = new TextBox
        {
            Text = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName,
            IsReadOnly = true,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var dataBox = new TextBox
        {
            Text = currentData ?? string.Empty,
            AcceptsReturn = false
        };

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = Strings.LabelValueName });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = Strings.LabelValueData });
        panel.Children.Add(dataBox);

        var dialog = new ContentDialog
        {
            Title = isExpandString ? Strings.DialogEditExpandString : Strings.DialogEditString,
            Content = panel,
            PrimaryButtonText = Strings.ButtonOK,
            CloseButtonText = Strings.ButtonCancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary ? dataBox.Text : null;
    }
}
