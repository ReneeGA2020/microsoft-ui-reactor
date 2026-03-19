using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI = Microsoft.UI.Xaml.Controls;

namespace Duct.Core;

/// <summary>
/// Native Rust DiffTrees reconciliation path.
/// Serializes old/new element trees, calls the Rust differ to compute patches,
/// then applies those patches using the existing Mount/Update/Unmount infrastructure.
///
/// Components (ComponentElement, FuncElement) are opaque to the serializer —
/// they appear as leaf nodes. After Rust patches are applied, all component nodes
/// are reconciled via the imperative C# path so internal state changes propagate.
/// </summary>
public sealed partial class Reconciler
{
    private TreeSerializer? _treeSerializer;
    private PropValueRegistry? _oldRegistry;
    private PropValueRegistry? _newRegistry;

    // Cached state from the previous render pass for the next diff
    private SerializationResult? _cachedOldSerialization;
    private UIElement?[]? _cachedOldControls;

    // Guard against nested DiffTrees calls (Update → ReconcileComponent → Reconcile)
    private bool _inDiffTreesPass;

    /// <summary>
    /// Returns true for element types that the serializer cannot fully represent:
    /// components (opaque) and containers whose children GetChildren() doesn't enumerate.
    /// These need targeted imperative reconciliation after DiffTrees patches.
    /// </summary>
    private static bool IsGapNode(Element element) => element is
        ComponentElement or FuncElement or
        TabViewElement or NavigationViewElement or PivotElement or
        TreeViewElement or BreadcrumbBarElement or
        MenuBarElement or MenuFlyoutElement or CommandBarElement or
        RadioButtonsElement or ComboBoxElement or
        DropDownButtonElement or SplitButtonElement or ToggleSplitButtonElement;

    private UIElement? ReconcileWithDiffTrees(
        Element oldElement, Element newElement,
        UIElement existingControl, Action requestRerender)
    {
        if (Differ is null)
            throw new InvalidOperationException(
                "Native differ DLL not available. Set Mode to Auto or CSharpFallback.");

        _inDiffTreesPass = true;
        try
        {
            return ReconcileWithDiffTreesCore(oldElement, newElement, existingControl, requestRerender);
        }
        finally
        {
            _inDiffTreesPass = false;
        }
    }

    private UIElement? ReconcileWithDiffTreesCore(
        Element oldElement, Element newElement,
        UIElement existingControl, Action requestRerender)
    {
        _oldRegistry ??= new PropValueRegistry();
        _newRegistry ??= new PropValueRegistry();

        // Serialize the old tree (reuse cache from last pass if available)
        var oldSerializer = new TreeSerializer(_oldRegistry);
        var oldSerialized = _cachedOldSerialization
            ?? oldSerializer.SerializeWithMapping(oldElement);

        // Build old control map (reuse cache or walk the live tree)
        var oldControls = _cachedOldControls
            ?? oldSerializer.BuildControlMap(oldElement, existingControl);

        // Serialize the new tree
        _treeSerializer = new TreeSerializer(_newRegistry);
        var newSerialized = _treeSerializer.SerializeWithMapping(newElement);

        if (oldSerialized.Nodes.Length == 0 && newSerialized.Nodes.Length == 0)
            return existingControl;

        if (oldSerialized.Nodes.Length == 0)
            return Mount(newElement, requestRerender);

        if (newSerialized.Nodes.Length == 0)
        {
            Unmount(existingControl);
            _cachedOldSerialization = null;
            _cachedOldControls = null;
            return null;
        }

        // Call Rust DiffTrees
        var patches = Differ!.DiffTrees(
            oldSerialized.Nodes, oldSerialized.Props,
            newSerialized.Nodes, newSerialized.Props);

        // Track which nodes were handled by patches so gap reconciliation skips them
        var handledNodes = new HashSet<int>();

        // Apply Rust patches
        UIElement resultControl = existingControl;
        if (patches.Length > 0)
        {
            resultControl = ApplyPatches(
                patches,
                oldSerialized, oldControls,
                newSerialized,
                existingControl,
                handledNodes,
                requestRerender);
        }

        // Targeted imperative reconciliation for gap nodes (components + unserialized containers)
        // Skip nodes already handled by ApplyPatches
        ReconcileGapNodes(oldSerialized, oldControls, newSerialized, handledNodes, requestRerender);

        // Cache for next render
        _cachedOldSerialization = newSerialized;
        _cachedOldControls = _treeSerializer.BuildControlMap(newElement, resultControl);

        // Swap registries
        (_oldRegistry, _newRegistry) = (_newRegistry, _oldRegistry);
        _newRegistry!.Clear();

        return resultControl;
    }

    /// <summary>
    /// After DiffTrees patches, do targeted imperative reconciliation on gap nodes
    /// (components + unserialized containers) that weren't already handled by patches.
    /// </summary>
    private void ReconcileGapNodes(
        SerializationResult oldTree, UIElement?[] oldControls,
        SerializationResult newTree,
        HashSet<int> handledNodes,
        Action requestRerender)
    {
        int count = Math.Min(oldTree.Elements.Length, newTree.Elements.Length);
        count = Math.Min(count, oldControls.Length);

        for (int i = 0; i < count; i++)
        {
            if (handledNodes.Contains(i)) continue;

            var oldEl = oldTree.Elements[i];
            var newEl = newTree.Elements[i];

            if (!IsGapNode(oldEl) && !IsGapNode(newEl)) continue;

            var control = oldControls[i];
            if (control is null) continue;

            if (CanUpdate(oldEl, newEl))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DiffTrees] Gap node {i}: {oldEl.GetType().Name} — imperative Update");

                var replacement = Update(oldEl, newEl, control, requestRerender);
                if (replacement is not null)
                {
                    SwapControlInParent(control, replacement);
                    oldControls[i] = replacement;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DiffTrees] Gap node {i}: type change " +
                    $"{oldEl.GetType().Name} → {newEl.GetType().Name} — replacing");

                var newCtrl = Mount(newEl, requestRerender);
                if (newCtrl is not null)
                {
                    SwapControlInParent(control, newCtrl);
                    Unmount(control);
                    oldControls[i] = newCtrl;
                }
            }
        }
    }

    private UIElement ApplyPatches(
        ReadOnlySpan<ViewPatch> patches,
        SerializationResult oldTree, UIElement?[] oldControls,
        SerializationResult newTree,
        UIElement rootControl,
        HashSet<int> handledNodes,
        Action requestRerender)
    {
        // Categorize patches
        var nodesToUpdate = new HashSet<uint>();
        var removals = new List<ViewPatch>();
        var insertions = new List<ViewPatch>();
        var moves = new List<ViewPatch>();
        var replacements = new List<ViewPatch>();

        foreach (var patch in patches)
        {
            switch (patch.Op)
            {
                case ViewPatchOp.UpdateProp:
                    nodesToUpdate.Add(patch.NodeIndex);
                    break;
                case ViewPatchOp.Remove:
                    removals.Add(patch);
                    break;
                case ViewPatchOp.Insert:
                    insertions.Add(patch);
                    break;
                case ViewPatchOp.Move:
                    moves.Add(patch);
                    break;
                case ViewPatchOp.Replace:
                    replacements.Add(patch);
                    break;
            }
        }

        System.Diagnostics.Debug.WriteLine(
            $"[DiffTrees] Summary: {nodesToUpdate.Count} updates, {insertions.Count} inserts, " +
            $"{removals.Count} removes, {moves.Count} moves, {replacements.Count} replaces");

        // Phase 1: Apply property updates
        foreach (var nodeIdx in nodesToUpdate)
        {
            int idx = (int)nodeIdx;
            if (idx >= oldTree.Elements.Length || idx >= newTree.Elements.Length) continue;
            if (idx >= oldControls.Length || oldControls[idx] is null) continue;

            var oldEl = oldTree.Elements[idx];
            var newEl = newTree.Elements[idx];
            var control = oldControls[idx]!;

            handledNodes.Add(idx);

            if (CanUpdate(oldEl, newEl))
            {
                var replacement = Update(oldEl, newEl, control, requestRerender);
                if (replacement is not null)
                {
                    SwapControlInParent(control, replacement);
                    oldControls[idx] = replacement;
                    if (idx == 0) rootControl = replacement;
                }
            }
            else
            {
                // Type mismatch (e.g., Component<A> → Component<B>): unmount old, mount new
                System.Diagnostics.Debug.WriteLine(
                    $"[DiffTrees] UpdateProp node {idx}: CanUpdate=false " +
                    $"old={oldEl.GetType().Name}" +
                    (oldEl is ComponentElement oc ? $"<{oc.ComponentType.Name}>" : "") +
                    $" new={newEl.GetType().Name}" +
                    (newEl is ComponentElement nc ? $"<{nc.ComponentType.Name}>" : ""));

                var newCtrl = Mount(newEl, requestRerender);
                if (newCtrl is not null)
                {
                    SwapControlInParent(control, newCtrl);
                    Unmount(control);
                    oldControls[idx] = newCtrl;
                    if (idx == 0) rootControl = newCtrl;
                }
            }
        }

        // Phase 2: Replacements
        foreach (var patch in replacements)
        {
            int newIdx = (int)patch.NodeIndex;
            int oldIdx = (int)patch.TargetIndex;
            handledNodes.Add(oldIdx);

            if (oldIdx < oldControls.Length && oldControls[oldIdx] is UIElement oldCtrl)
            {
                var newEl = newIdx < newTree.Elements.Length ? newTree.Elements[newIdx] : null;
                if (newEl is not null)
                {
                    var newCtrl = Mount(newEl, requestRerender);
                    if (newCtrl is not null)
                    {
                        SwapControlInParent(oldCtrl, newCtrl);
                        Unmount(oldCtrl);
                        oldControls[oldIdx] = newCtrl;
                        if (oldIdx == 0) rootControl = newCtrl;
                    }
                }
            }
        }

        // Phase 3: Removals (reverse order)
        removals.Sort((a, b) => b.NodeIndex.CompareTo(a.NodeIndex));
        foreach (var patch in removals)
        {
            int idx = (int)patch.NodeIndex;
            handledNodes.Add(idx);
            if (idx >= oldControls.Length || oldControls[idx] is not UIElement ctrl) continue;

            RemoveControlFromParent(ctrl);
            UnmountAndPool(ctrl);
            oldControls[idx] = null;
        }

        // Phase 4: Insertions
        foreach (var patch in insertions)
        {
            int newIdx = (int)patch.NodeIndex;
            handledNodes.Add(newIdx);
            if (newIdx >= newTree.Elements.Length) continue;

            var newEl = newTree.Elements[newIdx];
            var newCtrl = Mount(newEl, requestRerender);
            if (newCtrl is null) continue;

            int parentNodeIdx = newTree.Nodes[newIdx].ParentIndex;
            if (parentNodeIdx >= 0 && parentNodeIdx < oldControls.Length
                && oldControls[parentNodeIdx] is WinUI.Panel parentPanel)
            {
                int childPos = ComputeChildPosition(newTree, newIdx, parentNodeIdx);
                if (childPos <= parentPanel.Children.Count)
                    parentPanel.Children.Insert(childPos, newCtrl);
                else
                    parentPanel.Children.Add(newCtrl);
            }
        }

        // Phase 5: Moves
        foreach (var patch in moves)
        {
            int oldIdx = (int)patch.NodeIndex;
            int targetPos = (int)patch.TargetIndex;
            handledNodes.Add(oldIdx);
            if (oldIdx >= oldControls.Length || oldControls[oldIdx] is not UIElement ctrl) continue;

            var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(ctrl);
            if (parent is WinUI.Panel panel)
            {
                int currentPos = panel.Children.IndexOf(ctrl);
                if (currentPos >= 0 && currentPos != targetPos)
                {
                    panel.Children.RemoveAt(currentPos);
                    int insertAt = Math.Min(targetPos, panel.Children.Count);
                    panel.Children.Insert(insertAt, ctrl);
                }
            }
        }

        return rootControl;
    }

    private static void SwapControlInParent(UIElement oldCtrl, UIElement newCtrl)
    {
        var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(oldCtrl);
        switch (parent)
        {
            case WinUI.Panel panel:
                var idx = panel.Children.IndexOf(oldCtrl);
                if (idx >= 0) panel.Children[idx] = newCtrl;
                break;
            case WinUI.Border border:
                border.Child = newCtrl;
                break;
            case WinUI.ScrollViewer sv:
                sv.Content = newCtrl;
                break;
            case ContentControl cc:
                cc.Content = newCtrl;
                break;
        }
    }

    private static void RemoveControlFromParent(UIElement ctrl)
    {
        var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(ctrl);
        switch (parent)
        {
            case WinUI.Panel panel:
                panel.Children.Remove(ctrl);
                break;
            case WinUI.Border border:
                border.Child = null;
                break;
            case WinUI.ScrollViewer sv:
                sv.Content = null;
                break;
            case ContentControl cc:
                cc.Content = null;
                break;
        }
    }

    private static int ComputeChildPosition(SerializationResult tree, int nodeIdx, int parentIdx)
    {
        var parentNode = tree.Nodes[parentIdx];
        int pos = 0;
        for (uint i = parentNode.FirstChild; i < parentNode.FirstChild + parentNode.ChildCount; i++)
        {
            if ((int)i == nodeIdx) return pos;
            pos++;
        }
        return pos;
    }
}
