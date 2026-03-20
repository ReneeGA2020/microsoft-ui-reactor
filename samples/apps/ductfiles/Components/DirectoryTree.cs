using System.Diagnostics;
using Duct;
using Duct.Core;
using DuctFiles.Models;
using DuctFiles.Services;
using static Duct.UI;

namespace DuctFiles.Components;

/// <summary>
/// Props for the DirectoryTree component.
/// </summary>
internal sealed record DirectoryTreeProps(
    HashSet<string> ExpandedPaths,
    Dictionary<string, FileEntry[]> TreeChildren,
    string CurrentPath,
    Action<string> OnNavigate,
    Action<string> OnExpand
);

/// <summary>
/// TreeView sidebar showing drives and lazy-loaded directory hierarchy.
/// </summary>
internal sealed class DirectoryTree : Component<DirectoryTreeProps>
{
    public override Element Render()
    {
        // Map display names → full paths so we can resolve OnItemInvoked
        var pathMap = UseRef(new Dictionary<string, string>());

        var nodes = UseMemo(() =>
        {
            var sw = Stopwatch.StartNew();
            pathMap.Current.Clear();
            var result = BuildNodes(pathMap.Current, Props.ExpandedPaths, Props.TreeChildren);
            sw.Stop();
            Trace.WriteLine($"[DuctFiles] BuildNodes: {sw.ElapsedMilliseconds}ms, {result.Length} root nodes, expanded={Props.ExpandedPaths.Count}");
            return result;
        }, Props.ExpandedPaths.Count, Props.TreeChildren.Count);

        return TreeView(nodes)
            .Set(tv =>
            {
                tv.SelectionMode = Microsoft.UI.Xaml.Controls.TreeViewSelectionMode.Single;
            }) with
        {
            OnItemInvoked = node =>
            {
                if (pathMap.Current.TryGetValue(node.Content, out var fullPath))
                    Props.OnNavigate(fullPath);
            },
            OnExpanding = node =>
            {
                if (pathMap.Current.TryGetValue(node.Content, out var fullPath))
                {
                    if (!Props.ExpandedPaths.Contains(fullPath))
                        Props.OnExpand(fullPath);
                }
            }
        };
    }

    private static TreeViewNodeData[] BuildNodes(
        Dictionary<string, string> pathMap,
        HashSet<string> expandedPaths,
        Dictionary<string, FileEntry[]> treeChildren)
    {
        // Root nodes are drives
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch
        {
            return [];
        }

        var roots = new List<TreeViewNodeData>();
        foreach (var drive in drives)
        {
            if (!drive.IsReady) continue;
            var label = $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})";
            var drivePath = drive.RootDirectory.FullName;
            pathMap[label] = drivePath;

            var children = BuildChildNodes(drivePath, pathMap, expandedPaths, treeChildren);
            roots.Add(new TreeViewNodeData(label, children)
            {
                IsExpanded = expandedPaths.Contains(drivePath)
            });
        }

        return roots.ToArray();
    }

    private static TreeViewNodeData[]? BuildChildNodes(
        string parentPath,
        Dictionary<string, string> pathMap,
        HashSet<string> expandedPaths,
        Dictionary<string, FileEntry[]> treeChildren)
    {
        if (!treeChildren.TryGetValue(parentPath, out var children) || children.Length == 0)
        {
            // Show placeholder to get expand arrow
            return [new TreeViewNodeData("Loading...")];
        }

        var nodes = new List<TreeViewNodeData>();
        foreach (var child in children.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            pathMap[child.Name] = child.FullPath;

            TreeViewNodeData[]? grandChildren = null;
            if (expandedPaths.Contains(child.FullPath))
            {
                grandChildren = BuildChildNodes(child.FullPath, pathMap, expandedPaths, treeChildren);
            }
            else if (child.HasChildren)
            {
                grandChildren = [new TreeViewNodeData("Loading...")];
            }

            nodes.Add(new TreeViewNodeData(child.Name, grandChildren)
            {
                IsExpanded = expandedPaths.Contains(child.FullPath)
            });
        }

        return nodes.Count > 0 ? nodes.ToArray() : null;
    }
}
