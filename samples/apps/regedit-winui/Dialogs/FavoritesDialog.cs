using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RegeditWinUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RegeditWinUI.Dialogs;

public static class FavoritesDialog
{
    public static async Task<string?> ShowAddAsync(XamlRoot xamlRoot, string currentPath)
    {
        var nameBox = new TextBox
        {
            Text = Services.RegistryService.GetKeyName(currentPath),
            PlaceholderText = Strings.LabelFavoriteName
        };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = Strings.LabelFavoriteName });
        panel.Children.Add(nameBox);

        var dialog = new ContentDialog
        {
            Title = Strings.DialogAddFavorite,
            Content = panel,
            PrimaryButtonText = Strings.ButtonOK,
            CloseButtonText = Strings.ButtonCancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return null;
        if (string.IsNullOrWhiteSpace(nameBox.Text)) return null;
        return nameBox.Text;
    }

    public static async Task<string?> ShowRemoveAsync(XamlRoot xamlRoot)
    {
        var favorites = FavoritesService.GetFavorites();
        if (favorites.Count == 0)
        {
            var emptyDialog = new ContentDialog
            {
                Title = Strings.DialogRemoveFavorite,
                Content = new TextBlock { Text = "No favorites to remove." },
                CloseButtonText = Strings.ButtonOK,
                XamlRoot = xamlRoot
            };
            await emptyDialog.ShowAsync();
            return null;
        }

        var listView = new ListView
        {
            SelectionMode = ListViewSelectionMode.Single,
            Height = 200
        };
        foreach (var fav in favorites)
            listView.Items.Add(fav.Key);

        if (listView.Items.Count > 0)
            listView.SelectedIndex = 0;

        var dialog = new ContentDialog
        {
            Title = Strings.DialogRemoveFavorite,
            Content = listView,
            PrimaryButtonText = Strings.ButtonOK,
            CloseButtonText = Strings.ButtonCancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return null;

        return listView.SelectedItem as string;
    }
}
