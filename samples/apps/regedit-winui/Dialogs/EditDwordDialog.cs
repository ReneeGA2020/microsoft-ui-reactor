using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace RegeditWinUI.Dialogs;

public static class EditDwordDialog
{
    public static async Task<int?> ShowAsync(XamlRoot xamlRoot, string valueName, int currentValue)
    {
        var nameBox = new TextBox
        {
            Text = string.IsNullOrEmpty(valueName) ? "(Default)" : valueName,
            IsReadOnly = true,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var dataBox = new TextBox
        {
            Text = currentValue.ToString("x8"),
            MaxLength = 8
        };

        var hexRadio = new RadioButton { Content = Strings.LabelHexadecimal, IsChecked = true, GroupName = "Base" };
        var decRadio = new RadioButton { Content = Strings.LabelDecimal, GroupName = "Base" };

        hexRadio.Checked += (s, e) =>
        {
            if (int.TryParse(dataBox.Text, out int val))
                dataBox.Text = val.ToString("x8");
            dataBox.MaxLength = 8;
        };
        decRadio.Checked += (s, e) =>
        {
            if (int.TryParse(dataBox.Text, NumberStyles.HexNumber, null, out int val))
                dataBox.Text = val.ToString();
            dataBox.MaxLength = 10;
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
            Title = Strings.DialogEditDWORD,
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
            if (int.TryParse(dataBox.Text, NumberStyles.HexNumber, null, out int hexVal))
                return hexVal;
        }
        else
        {
            if (int.TryParse(dataBox.Text, out int decVal))
                return decVal;
        }
        return currentValue;
    }
}
