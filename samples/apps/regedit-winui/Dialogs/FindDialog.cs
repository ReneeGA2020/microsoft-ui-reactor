using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RegeditWinUI.Models;
using System;
using System.Threading.Tasks;

namespace RegeditWinUI.Dialogs;

public static class FindDialog
{
    public static async Task<(string Text, FindFlags Flags)?> ShowAsync(
        XamlRoot xamlRoot, string? currentText, FindFlags currentFlags)
    {
        var searchBox = new TextBox
        {
            Text = currentText ?? string.Empty,
            PlaceholderText = Strings.LabelFindWhat
        };

        var keysCheck = new CheckBox
        {
            Content = Strings.LabelKeys,
            IsChecked = currentFlags.HasFlag(FindFlags.Keys)
        };
        var valuesCheck = new CheckBox
        {
            Content = Strings.LabelValues,
            IsChecked = currentFlags.HasFlag(FindFlags.Values)
        };
        var dataCheck = new CheckBox
        {
            Content = Strings.LabelData,
            IsChecked = currentFlags.HasFlag(FindFlags.Data)
        };
        var wholeStringCheck = new CheckBox
        {
            Content = Strings.LabelMatchWholeString,
            IsChecked = currentFlags.HasFlag(FindFlags.WholeString)
        };

        var lookAtPanel = new StackPanel { Spacing = 4 };
        lookAtPanel.Children.Add(new TextBlock { Text = Strings.LabelLookAt, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        lookAtPanel.Children.Add(keysCheck);
        lookAtPanel.Children.Add(valuesCheck);
        lookAtPanel.Children.Add(dataCheck);

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = Strings.LabelFindWhat });
        panel.Children.Add(searchBox);
        panel.Children.Add(lookAtPanel);
        panel.Children.Add(wholeStringCheck);

        var dialog = new ContentDialog
        {
            Title = Strings.DialogFind,
            Content = panel,
            PrimaryButtonText = Strings.ButtonFindNext,
            CloseButtonText = Strings.ButtonCancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return null;
        if (string.IsNullOrWhiteSpace(searchBox.Text)) return null;

        var flags = FindFlags.None;
        if (keysCheck.IsChecked == true) flags |= FindFlags.Keys;
        if (valuesCheck.IsChecked == true) flags |= FindFlags.Values;
        if (dataCheck.IsChecked == true) flags |= FindFlags.Data;
        if (wholeStringCheck.IsChecked == true) flags |= FindFlags.WholeString;

        // Must have at least one search target
        if (!flags.HasFlag(FindFlags.Keys) && !flags.HasFlag(FindFlags.Values) && !flags.HasFlag(FindFlags.Data))
            flags |= FindFlags.Keys | FindFlags.Values | FindFlags.Data;

        return (searchBox.Text, flags);
    }
}
