using Duct;
using Duct.Core;
using DuctRegedit.Models;
using DuctRegedit.Services;
using static Duct.UI;

namespace DuctRegedit.Components;

internal sealed record RegistryTreeProps(
    HashSet<string> ExpandedPaths,
    Dictionary<string, RegistryKeyEntry[]> TreeChildren,
    string CurrentKeyPath,
    Action<string> OnNavigate,
    Action<string> OnExpand,
    Action<string> OnContextMenu
);

internal sealed class RegistryTree : Component<RegistryTreeProps>
{
    public override Element Render()
    {
        var pathMap = UseRef(new Dictionary<string, string>());

        var nodes = UseMemo(() =>
        {
            pathMap.Current.Clear();
            return BuildNodes(pathMap.Current, Props.ExpandedPaths, Props.TreeChildren);
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
        Dictionary<string, RegistryKeyEntry[]> treeChildren)
    {
        var rootKeys = RegistryService.GetRootKeys();
        var roots = new List<TreeViewNodeData>();

        foreach (var rootKey in rootKeys)
        {
            pathMap[rootKey.Name] = rootKey.FullPath;

            var children = BuildChildNodes(rootKey.FullPath, pathMap, expandedPaths, treeChildren);
            roots.Add(new TreeViewNodeData(rootKey.Name, children)
            {
                IsExpanded = expandedPaths.Contains(rootKey.FullPath)
            });
        }

        // Add "Computer" root that contains all hives
        return [new TreeViewNodeData("Computer", roots.ToArray()) { IsExpanded = true }];
    }

    private static TreeViewNodeData[]? BuildChildNodes(
        string parentPath,
        Dictionary<string, string> pathMap,
        HashSet<string> expandedPaths,
        Dictionary<string, RegistryKeyEntry[]> treeChildren)
    {
        if (!treeChildren.TryGetValue(parentPath, out var children) || children.Length == 0)
        {
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
