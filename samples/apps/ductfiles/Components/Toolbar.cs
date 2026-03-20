using Duct;
using Duct.Core;
using DuctFiles.Models;
using Microsoft.UI.Xaml;
using static Duct.UI;

namespace DuctFiles.Components;

/// <summary>
/// Props for the Toolbar component.
/// </summary>
internal sealed record ToolbarProps(
    string CurrentPath,
    ViewMode ViewMode,
    string Filter,
    SortField SortField,
    SortDirection SortDirection,
    bool IsLoading,
    Action<ViewMode> OnViewModeChanged,
    Action<string> OnFilterChanged,
    Action<SortField> OnSortFieldChanged,
    Action OnToggleSortDirection,
    Action<string> OnNavigate
);

/// <summary>
/// Top toolbar with breadcrumb, view mode buttons, filter, and sort controls.
/// </summary>
internal sealed class Toolbar : Component<ToolbarProps>
{
    public override Element Render()
    {
        // Build breadcrumb items from path segments
        var segments = BuildBreadcrumbs(Props.CurrentPath);

        var breadcrumb = BreadcrumbBar(
            segments.Select(s => Breadcrumb(s.Label, s.Path)).ToArray(),
            item =>
            {
                if (item.Tag is string path)
                    Props.OnNavigate(path);
            }
        );

        // View mode buttons
        var viewButtons = CommandBar(
            primaryCommands: [
                AppBarButton("Details", () => Props.OnViewModeChanged(ViewMode.Details),
                    Props.ViewMode == ViewMode.Details ? "\uE8FD" : "\uE8A4"),
                AppBarButton("List", () => Props.OnViewModeChanged(ViewMode.List),
                    Props.ViewMode == ViewMode.List ? "\uE8FD" : "\uE8A4"),
                AppBarButton("Large Icons", () => Props.OnViewModeChanged(ViewMode.LargeIcons),
                    Props.ViewMode == ViewMode.LargeIcons ? "\uE8FD" : "\uE8A4"),
                AppBarButton("Small Icons", () => Props.OnViewModeChanged(ViewMode.SmallIcons),
                    Props.ViewMode == ViewMode.SmallIcons ? "\uE8FD" : "\uE8A4"),
            ]
        ) with
        {
            DefaultLabelPosition = Microsoft.UI.Xaml.Controls.CommandBarDefaultLabelPosition.Right
        };

        // Sort controls
        var sortFields = new[] { "Name", "Size", "Modified", "Type" };
        var sortCombo = ComboBox(
            sortFields,
            (int)Props.SortField,
            idx => Props.OnSortFieldChanged((SortField)idx)
        ).Set(cb => cb.Width = 120);

        var sortDirButton = Button(
            Props.SortDirection == SortDirection.Ascending ? "\uE74A" : "\uE74B",
            Props.OnToggleSortDirection
        ).Set(b =>
        {
            b.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets");
        });

        // Filter
        var filterBox = TextField(
            Props.Filter,
            Props.OnFilterChanged,
            "Filter files..."
        ).Set(tb => tb.Width = 200);

        // Loading indicator
        var loading = Props.IsLoading
            ? ProgressRing().Set(pr => { pr.Width = 20; pr.Height = 20; pr.IsActive = true; })
            : (Element)Empty();

        return VStack(4,
            breadcrumb,
            HStack(8,
                viewButtons,
                HStack(4,
                    sortCombo.VAlign(VerticalAlignment.Center),
                    sortDirButton.VAlign(VerticalAlignment.Center)
                ),
                filterBox.VAlign(VerticalAlignment.Center),
                loading.VAlign(VerticalAlignment.Center)
            )
        ).Margin(8, 4, 8, 4);
    }

    private static (string Label, string Path)[] BuildBreadcrumbs(string path)
    {
        var segments = new List<(string Label, string Path)>();

        // Add "This PC" as root
        segments.Add(("This PC", ""));

        if (string.IsNullOrEmpty(path)) return segments.ToArray();

        // Split path into segments
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar,
            StringSplitOptions.RemoveEmptyEntries);

        var running = "";
        foreach (var part in parts)
        {
            running = running.Length == 0 ? part + Path.DirectorySeparatorChar : Path.Combine(running, part);
            segments.Add((part, running));
        }

        return segments.ToArray();
    }
}
