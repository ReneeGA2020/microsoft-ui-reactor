using Duct;
using Duct.Core;
using DuctOutlook.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using static Duct.UI;

namespace DuctOutlook.Components.Email;

internal sealed record FolderPaneProps(
    MailFolder[] Folders,
    string SelectedFolderId,
    Action<string> OnFolderSelected
);

internal sealed class FolderPane : Component<FolderPaneProps>
{
    static readonly SolidColorBrush TransparentBrush = new(Windows.UI.Color.FromArgb(0, 0, 0, 0));
    static readonly SolidColorBrush SelectedBrush = new(Windows.UI.Color.FromArgb(255, 218, 234, 251));
    static readonly SolidColorBrush HoverBrush = new(Windows.UI.Color.FromArgb(255, 243, 243, 243));
    static readonly SolidColorBrush AccentBrush = new(Windows.UI.Color.FromArgb(255, 0, 120, 212));
    static readonly SolidColorBrush AccentHoverBrush = new(Windows.UI.Color.FromArgb(255, 0, 100, 190));
    static readonly SolidColorBrush WhiteBrush = new(Windows.UI.Color.FromArgb(255, 255, 255, 255));

    public override Element Render()
    {
        var favorites = Props.Folders.Where(f => f.IsFavorite).ToArray();
        var others = Props.Folders.Where(f => !f.IsFavorite).ToArray();

        return FlexColumn(
            // New mail button
            Button(
                (FlexRow(
                    MdlIcon("\uE710", 16, "white"),
                    Text("New mail").FontSize(14).Foreground("white").SemiBold()
                ) with { ColumnGap = 8 }),
                null
            ).Set(b =>
            {
                b.Background = AccentBrush;
                b.Foreground = WhiteBrush;
                b.CornerRadius = new CornerRadius(4);
                b.Padding = new Thickness(18, 8, 22, 8);
                b.Margin = new Thickness(16, 14, 16, 10);
                b.Resources["ButtonBackgroundPointerOver"] = AccentHoverBrush;
                b.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(
                    Windows.UI.Color.FromArgb(255, 0, 80, 170));
                b.Resources["ButtonForegroundPointerOver"] = WhiteBrush;
                b.Resources["ButtonForegroundPressed"] = WhiteBrush;
            }),

            // Favorites section
            Text("Favorites")
                .SemiBold().FontSize(13).Foreground("#555")
                .Padding(18, 6, 18, 6),

            VStack(0, favorites.Select(FolderRow).ToArray()),

            // Divider
            Border(Empty()).Height(1).Background("#E8E8E8").Margin(16, 10, 16, 10),

            // All folders section
            Text("Folders")
                .SemiBold().FontSize(13).Foreground("#555")
                .Padding(18, 4, 18, 6),

            ScrollView(
                VStack(0, others.Select(FolderRow).ToArray())
            ).Flex(grow: 1, basis: 0)
        );
    }

    Element FolderRow(MailFolder folder)
    {
        var isSelected = folder.Id == Props.SelectedFolderId;
        var bg = isSelected ? SelectedBrush : TransparentBrush;

        return Button(
            (FlexRow(
                MdlIcon(folder.Icon, 16, "#555"),
                Text(folder.DisplayName).FontSize(14).Flex(grow: 1),
                folder.UnreadCount > 0
                    ? Text(folder.UnreadCount.ToString())
                        .SemiBold().FontSize(13).Foreground("#0078D4")
                    : Empty()
            ) with { ColumnGap = 10 }).Padding(18, 7, 18, 7),
            () => Props.OnFolderSelected(folder.Id)
        ).Set(b =>
        {
            b.Background = bg;
            b.BorderThickness = new Thickness(0);
            b.Padding = new Thickness(0);
            b.CornerRadius = new CornerRadius(0);
            b.HorizontalAlignment = HorizontalAlignment.Stretch;
            b.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            b.Resources["ButtonBackgroundPointerOver"] = isSelected ? SelectedBrush : HoverBrush;
            b.Resources["ButtonBackgroundPressed"] = SelectedBrush;
            b.Resources["ButtonBorderBrushPointerOver"] = TransparentBrush;
            b.Resources["ButtonBorderBrushPressed"] = TransparentBrush;
        });
    }

    static Element MdlIcon(string glyph, double size, string color) =>
        Text(glyph).FontSize(size).Foreground(color)
            .Set(t => t.FontFamily = new FontFamily("Segoe MDL2 Assets"));
}
