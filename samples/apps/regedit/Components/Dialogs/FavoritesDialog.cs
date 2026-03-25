using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using static Duct.UI;

namespace DuctRegedit.Components.Dialogs;

// ─── Add Favorite ────────────────────────────────────────────────────────────

internal sealed record AddFavoriteDialogProps(
    bool IsOpen,
    string FavoriteName,
    Action<string> OnNameChanged,
    Action OnSave,
    Action OnCancel
);

internal sealed class AddFavoriteDialog : Component<AddFavoriteDialogProps>
{
    public override Element Render()
    {
        return ContentDialog(
            Strings.AddFavoriteTitle,
            VStack(8,
                Text(Strings.FavoriteName),
                TextField(Props.FavoriteName, Props.OnNameChanged)
            ).Width(350),
            Strings.OK
        ) with
        {
            IsOpen = Props.IsOpen,
            SecondaryButtonText = Strings.Cancel,
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
            OnClosed = _ => Props.OnCancel(),
        };
    }
}

// ─── Remove Favorite ─────────────────────────────────────────────────────────

internal sealed record RemoveFavoriteDialogProps(
    bool IsOpen,
    string[] FavoriteNames,
    int SelectedIndex,
    Action<int> OnSelectionChanged,
    Action OnRemove,
    Action OnCancel
);

internal sealed class RemoveFavoriteDialog : Component<RemoveFavoriteDialogProps>
{
    public override Element Render()
    {
        return ContentDialog(
            Strings.RemoveFavoriteTitle,
            VStack(8,
                Text(Strings.SelectFavorite),
                ListBox(
                    Props.FavoriteNames,
                    Props.SelectedIndex,
                    Props.OnSelectionChanged
                ).Height(200)
            ).Width(350),
            Strings.Delete
        ) with
        {
            IsOpen = Props.IsOpen,
            SecondaryButtonText = Strings.Cancel,
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
            OnClosed = _ => Props.OnCancel(),
        };
    }
}
