using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace RegeditWinUI.Dialogs;

public static class EditQwordDialog
{
    public static async Task<long?> ShowAsync(XamlRoot xamlRoot, string valueName, long currentValue)
    {
        var nameBox = new TextBox
        {
            Text = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName,
            IsReadOnly = true,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var dataBox = new TextBox
        {
            Text = currentValue.ToString("x16"),
            MaxLength = 16
        };

        var hexRadio = new RadioButton { Content = Strings.LabelHexadecimal, IsChecked = true, GroupName = "Base" };
        var decRadio = new RadioButton { Content = Strings.LabelDecimal, GroupName = "Base" };

        hexRadio.Checked += (s, e) =>
        {
            if (long.TryParse(dataBox.Text, out long val))
                dataBox.Text = val.ToString("x16");
            dataBox.MaxLength = 16;
        };
        decRadio.Checked += (s, e) =>
        {
            if (long.TryParse(dataBox.Text, NumberStyles.HexNumber, null, out long val))
                dataBox.Text = val.ToString();
            dataBox.MaxLength = 20;
        };

        var basePanel = new StackPanel { Spacing = 4 };
        basePanel.Children.Add(new TextBlock { Text = Strings.LabelBase, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        basePanel.Children.Add(hexRadio);
        basePanel.Children.Add(decRadio);

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = Strings.LabelValueName });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = Strings.LabelValueData });
        panel.Children.Add(dataBox);
        panel.Children.Add(basePanel);

        var dialog = new ContentDialog
        {
            Title = Strings.DialogEditQWORD,
            Content = panel,
            PrimaryButtonText = Strings.ButtonOK,
            CloseButtonText = Strings.ButtonCancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return null;

        if (hexRadio.IsChecked == true)
        {
            if (long.TryParse(dataBox.Text, NumberStyles.HexNumber, null, out long hexVal))
                return hexVal;
        }
        else
        {
            if (long.TryParse(dataBox.Text, out long decVal))
                return decVal;
        }
        return currentValue;
    }
}
