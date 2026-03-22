using Duct;
using Duct.Core;
using DuctFiles.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace DuctFiles.Components;

/// <summary>
/// Props for the FileListPane component.
/// </summary>
internal sealed record FileListPaneProps(
    IReadOnlyList<FileEntry> Files,
    ViewMode ViewMode,
    Action<FileEntry> OnItemActivated
);

/// <summary>
/// Virtualized file list supporting 4 view modes.
/// </summary>
internal sealed class FileListPane : Component<FileListPaneProps>
{
    // Segoe MDL2 Assets glyph codes
    private const string FolderIcon = "\uE8B7";
    private const string FileIcon = "\uE8A5";

    public override Element Render()
    {
        var files = Props.Files;
        var viewMode = Props.ViewMode;

        return viewMode switch
        {
            ViewMode.Details => RenderDetails(files),
            ViewMode.List => RenderList(files),
            ViewMode.LargeIcons => RenderIcons(files, large: true),
            ViewMode.SmallIcons => RenderIcons(files, large: false),
            _ => RenderDetails(files)
        };
    }

    private Element RenderDetails(IReadOnlyList<FileEntry> files)
    {
        // Column header
        var header = Grid(
            ["36", "2*", "*", "*", "*"],
            ["32"],
            Text("").Grid(row: 0, column: 0),
            Text("Name").SemiBold().Grid(row: 0, column: 1),
            Text("Date modified").SemiBold().Grid(row: 0, column: 2),
            Text("Type").SemiBold().Grid(row: 0, column: 3),
            Text("Size").SemiBold().Grid(row: 0, column: 4)
        ).Set(g =>
        {
            g.Padding = new Thickness(4, 0, 4, 0);
            g.BorderThickness = new Thickness(0, 0, 0, 1);
            g.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];
        });

        var list = LazyVStack<FileEntry>(
            files,
            f => f.FullPath,
            (file, _) => RenderDetailRow(file)
        ) with { EstimatedItemSize = 32, Spacing = 0 };

        return Grid(
            ["*"],
            ["Auto", "*"],
            header.Grid(row: 0, column: 0),
            list.Grid(row: 1, column: 0)
        );
    }

    private Element RenderDetailRow(FileEntry file)
    {
        var icon = Text(file.IsDirectory ? FolderIcon : FileIcon)
            .Set(tb =>
            {
                tb.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets");
                tb.FontSize = 16;
            });

        return Grid(
            ["36", "2*", "*", "*", "*"],
            ["32"],
            icon.HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 0),
            Text(file.Name).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 1),
            Text(file.Modified.ToString("g")).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 2),
            Text(file.TypeDescription).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 3),
            Text(file.FormattedSize).HAlign(HorizontalAlignment.Right).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 4)
        ).Set(g =>
        {
            g.Padding = new Thickness(4, 0, 4, 0);
            g.PointerEntered += (s, _) =>
                ((Microsoft.UI.Xaml.Controls.Grid)s!).Background =
                    (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"];
            g.PointerExited += (s, _) =>
                ((Microsoft.UI.Xaml.Controls.Grid)s!).Background = null;
            g.DoubleTapped += (_, _) => Props.OnItemActivated(file);
        });
    }

    private Element RenderList(IReadOnlyList<FileEntry> files)
    {
        return LazyVStack<FileEntry>(
            files,
            f => f.FullPath,
            (file, _) =>
            {
                var icon = Text(file.IsDirectory ? FolderIcon : FileIcon)
                    .Set(tb =>
                    {
                        tb.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets");
                        tb.FontSize = 14;
                    });

                return HStack(6,
                    icon.VAlign(VerticalAlignment.Center),
                    Text(file.Name).VAlign(VerticalAlignment.Center)
                ).Set(sp =>
                {
                    sp.Padding = new Thickness(4, 2, 4, 2);
                    sp.DoubleTapped += (_, _) => Props.OnItemActivated(file);
                });
            }
        ) with { EstimatedItemSize = 28, Spacing = 0 };
    }

    private Element RenderIcons(IReadOnlyList<FileEntry> files, bool large)
    {
        double iconSize = large ? 48 : 24;
        double itemWidth = large ? 100 : 180;
        double itemHeight = large ? 90 : 36;

        return LazyVStack<FileEntry>(
            files,
            f => f.FullPath,
            (file, _) =>
            {
                var icon = Text(file.IsDirectory ? FolderIcon : FileIcon)
                    .Set(tb =>
                    {
                        tb.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets");
                        tb.FontSize = iconSize;
                    });

                if (large)
                {
                    return VStack(4,
                        icon.HAlign(HorizontalAlignment.Center),
                        Text(file.Name).Set(tb =>
                        {
                            tb.TextAlignment = TextAlignment.Center;
                            tb.TextWrapping = TextWrapping.NoWrap;
                            tb.TextTrimming = TextTrimming.CharacterEllipsis;
                            tb.MaxWidth = itemWidth - 8;
                        })
                    ).Set(sp =>
                    {
                        sp.Width = itemWidth;
                        sp.Height = itemHeight;
                        sp.Padding = new Thickness(4);
                        sp.DoubleTapped += (_, _) => Props.OnItemActivated(file);
                    });
                }
                else
                {
                    return HStack(6,
                        icon.VAlign(VerticalAlignment.Center),
                        Text(file.Name).Set(tb =>
                        {
                            tb.TextTrimming = TextTrimming.CharacterEllipsis;
                            tb.MaxWidth = itemWidth - 40;
                        }).VAlign(VerticalAlignment.Center)
                    ).Set(sp =>
                    {
                        sp.Padding = new Thickness(4, 2, 4, 2);
                        sp.DoubleTapped += (_, _) => Props.OnItemActivated(file);
                    });
                }
            }
        ) with { EstimatedItemSize = itemHeight, Spacing = 0 };
    }
}
