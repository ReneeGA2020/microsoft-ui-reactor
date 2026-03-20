// DuctFiles — A read-only file explorer showcasing Duct + WinUI performance.
// No XAML. No data binding. Virtualized lists, lazy-loading TreeView, filesystem watching.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using Duct;
using Duct.Core;
using DuctFiles.Components;
using DuctFiles.Models;
using DuctFiles.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using static Duct.UI;

DuctApp.Run<DuctFilesApp>("DuctFiles", width: 1200, height: 800);

// ─── Root component ───────────────────────────────────────────────────────────

class DuctFilesApp : Component
{
    public override Element Render()
    {
        // ── State ──────────────────────────────────────────────────────
        var (currentPath, setCurrentPath) = UseState("");
        var (files, setFiles) = UseState<FileEntry[]>([]);
        var (viewMode, setViewMode) = UseState(ViewMode.Details);
        var (filter, setFilter) = UseState("");
        var (sortField, setSortField) = UseState(SortField.Name);
        var (sortDirection, setSortDirection) = UseState(SortDirection.Ascending);
        var (isLoading, setIsLoading) = UseState(false);

        // Tree state: expanded paths and loaded children per path
        var (expandedPaths, setExpandedPaths) = UseState(new HashSet<string>());
        var (treeChildren, setTreeChildren) = UseState(new Dictionary<string, FileEntry[]>());

        // Watcher ref for cleanup
        var watcherRef = UseRef<FileWatcherService?>(null);

        // ── Enumerate directory on path change ─────────────────────────
        UseEffect((Action)(() =>
        {
            if (string.IsNullOrEmpty(currentPath)) return;

            setIsLoading(true);
            var syncContext = SynchronizationContext.Current;

            Task.Run(async () =>
            {
                var result = await FileSystemService.EnumerateDirectoryAsync(currentPath);
                syncContext?.Post(_ =>
                {
                    setFiles(result);
                    setIsLoading(false);
                }, null);
            });
        }), currentPath);

        // ── File watcher on path change ────────────────────────────────
        UseEffect((Func<Action>)(() =>
        {
            watcherRef.Current?.Dispose();
            watcherRef.Current = null;

            if (!string.IsNullOrEmpty(currentPath))
            {
                try
                {
                    var syncContext = SynchronizationContext.Current;
                    watcherRef.Current = new FileWatcherService(currentPath, () =>
                    {
                        // Re-enumerate on change, marshal to UI thread
                        Task.Run(async () =>
                        {
                            var result = await FileSystemService.EnumerateDirectoryAsync(currentPath);
                            syncContext?.Post(_ => setFiles(result), null);
                        });
                    });
                }
                catch
                {
                    // Watcher can fail on network paths, etc.
                }
            }

            return () =>
            {
                watcherRef.Current?.Dispose();
                watcherRef.Current = null;
            };
        }), currentPath);

        // ── Filtered + sorted file list (memoized) ────────────────────
        var displayFiles = UseMemo<IReadOnlyList<FileEntry>>(() =>
        {
            IEnumerable<FileEntry> result = files;

            // Filter
            if (!string.IsNullOrEmpty(filter))
            {
                result = result.Where(f =>
                    f.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            // Sort: directories first, then by selected field
            result = (sortField, sortDirection) switch
            {
                (SortField.Name, SortDirection.Ascending) =>
                    result.OrderByDescending(f => f.IsDirectory).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase),
                (SortField.Name, SortDirection.Descending) =>
                    result.OrderByDescending(f => f.IsDirectory).ThenByDescending(f => f.Name, StringComparer.OrdinalIgnoreCase),
                (SortField.Size, SortDirection.Ascending) =>
                    result.OrderByDescending(f => f.IsDirectory).ThenBy(f => f.Size),
                (SortField.Size, SortDirection.Descending) =>
                    result.OrderByDescending(f => f.IsDirectory).ThenByDescending(f => f.Size),
                (SortField.Modified, SortDirection.Ascending) =>
                    result.OrderByDescending(f => f.IsDirectory).ThenBy(f => f.Modified),
                (SortField.Modified, SortDirection.Descending) =>
                    result.OrderByDescending(f => f.IsDirectory).ThenByDescending(f => f.Modified),
                (SortField.Type, SortDirection.Ascending) =>
                    result.OrderByDescending(f => f.IsDirectory).ThenBy(f => f.TypeDescription, StringComparer.OrdinalIgnoreCase),
                (SortField.Type, SortDirection.Descending) =>
                    result.OrderByDescending(f => f.IsDirectory).ThenByDescending(f => f.TypeDescription, StringComparer.OrdinalIgnoreCase),
                _ =>
                    result.OrderByDescending(f => f.IsDirectory).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            };

            return result.ToArray();
        }, files, filter, sortField, sortDirection);

        // ── Handlers ───────────────────────────────────────────────────
        void Navigate(string path)
        {
            if (Directory.Exists(path))
                setCurrentPath(path);
        }

        void ExpandTreeNode(string path)
        {
            if (expandedPaths.Contains(path)) return;

            Trace.WriteLine($"[DuctFiles] ExpandTreeNode START: {path}");
            var sw = Stopwatch.StartNew();

            var syncContext = SynchronizationContext.Current;
            Task.Run(async () =>
            {
                var swEnum = Stopwatch.StartNew();
                var subdirs = await FileSystemService.EnumerateSubdirsAsync(path);
                swEnum.Stop();
                Trace.WriteLine($"[DuctFiles] EnumerateSubdirs: {swEnum.ElapsedMilliseconds}ms, {subdirs.Length} items");

                syncContext?.Post(_ =>
                {
                    var swState = Stopwatch.StartNew();

                    var newChildren = new Dictionary<string, FileEntry[]>(treeChildren)
                    {
                        [path] = subdirs
                    };
                    setTreeChildren(newChildren);

                    var newExpanded = new HashSet<string>(expandedPaths) { path };
                    setExpandedPaths(newExpanded);

                    swState.Stop();
                    Trace.WriteLine($"[DuctFiles] SetState (UI thread): {swState.ElapsedMilliseconds}ms");
                    Trace.WriteLine($"[DuctFiles] ExpandTreeNode TOTAL: {sw.ElapsedMilliseconds}ms");
                }, null);
            });
        }

        void OnItemActivated(FileEntry file)
        {
            if (file.IsDirectory)
                Navigate(file.FullPath);
        }

        // ── Layout ─────────────────────────────────────────────────────
        var toolbar = Component<Toolbar, ToolbarProps>(new ToolbarProps(
            CurrentPath: currentPath,
            ViewMode: viewMode,
            Filter: filter,
            SortField: sortField,
            SortDirection: sortDirection,
            IsLoading: isLoading,
            OnViewModeChanged: setViewMode,
            OnFilterChanged: setFilter,
            OnSortFieldChanged: setSortField,
            OnToggleSortDirection: () => setSortDirection(
                sortDirection == SortDirection.Ascending
                    ? SortDirection.Descending
                    : SortDirection.Ascending),
            OnNavigate: Navigate
        ));

        var tree = Component<DirectoryTree, DirectoryTreeProps>(new DirectoryTreeProps(
            ExpandedPaths: expandedPaths,
            TreeChildren: treeChildren,
            CurrentPath: currentPath,
            OnNavigate: Navigate,
            OnExpand: ExpandTreeNode
        ));

        var fileList = Component<FileListPane, FileListPaneProps>(new FileListPaneProps(
            Files: displayFiles,
            ViewMode: viewMode,
            OnItemActivated: OnItemActivated
        ));

        // Splitter grip: a 4px border column that drags to resize the tree pane
        var splitter = Border(Empty())
            .Set(b =>
            {
                b.Width = 4;
                b.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];
                b.SetValue(UIElement.ManipulationModeProperty, ManipulationModes.TranslateX);
                // Cursor hint: highlight on hover
                b.PointerEntered += (s, _) =>
                    ((Microsoft.UI.Xaml.Controls.Border)s!).Background =
                        (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SubtleFillColorTertiaryBrush"];
                b.PointerExited += (s, _) =>
                    ((Microsoft.UI.Xaml.Controls.Border)s!).Background =
                        (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"];
                b.ManipulationDelta += (s, e) =>
                {
                    var border = (Microsoft.UI.Xaml.Controls.Border)s!;
                    var grid = border.Parent as Microsoft.UI.Xaml.Controls.Grid;
                    if (grid is null) return;
                    var col = grid.ColumnDefinitions[0];
                    var newWidth = Math.Max(120, col.ActualWidth + e.Delta.Translation.X);
                    col.Width = new GridLength(newWidth, GridUnitType.Pixel);
                };
            });

        // The outer VStack must not stretch — use a Grid so toolbar gets Auto height
        // and the content area fills remaining space.
        return Grid(
            ["*"],
            ["Auto", "*"],
            Cell(toolbar, 0, 0),
            Cell(Grid(
                ["280", "Auto", "*"],
                ["*"],
                Cell(tree, 0, 0),
                Cell(splitter, 0, 1),
                Cell(fileList, 0, 2)
            ), 1, 0)
        );
    }
}
