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
    /// Reconcile using the native Rust DiffTrees engine.
    /// Serializes both trees, diffs via Rust, then applies patches.
    /// </summary>
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

        // Handle edge case: old tree was empty
        if (oldSerialized.Nodes.Length == 0)
            return Mount(newElement, requestRerender);

        // Handle edge case: new tree is empty
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

        // Apply Rust patches (structural changes + property updates)
        UIElement resultControl = existingControl;
        if (patches.Length > 0)
        {
            resultControl = ApplyPatches(
                patches,
                oldSerialized, oldControls,
                newSerialized,
                existingControl,
                requestRerender);
        }

        // The serializer can't fully represent all element types:
        //   - Components/FuncElements are opaque (internal state changes invisible to Rust)
        //   - Some containers (TabView, NavigationView, etc.) don't enumerate children
        // Do targeted imperative reconciliation on ONLY those gap nodes, not the whole tree.
        ReconcileGapNodes(oldSerialized, oldControls, newSerialized, requestRerender);

        // Cache the new serialization + control map for next render
        _cachedOldSerialization = newSerialized;
        _cachedOldControls = _treeSerializer.BuildControlMap(newElement, resultControl);

        // Swap registries for next pass
        (_oldRegistry, _newRegistry) = (_newRegistry, _oldRegistry);
        _newRegistry!.Clear();

        return resultControl;
    }

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

    /// <summary>
    /// After DiffTrees patches, do targeted imperative reconciliation on nodes
    /// that the serializer can't fully represent. Avoids re-walking the entire tree.
    /// </summary>
    private void ReconcileGapNodes(
        SerializationResult oldTree, UIElement?[] oldControls,
        SerializationResult newTree,
        Action requestRerender)
    {
        int count = Math.Min(oldTree.Elements.Length, newTree.Elements.Length);
        count = Math.Min(count, oldControls.Length);

        for (int i = 0; i < count; i++)
        {
            var oldEl = oldTree.Elements[i];
            var newEl = newTree.Elements[i];

            // Only target gap nodes (components + unserialized containers)
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
                // Type mismatch — unmount old, mount new
                System.Diagnostics.Debug.WriteLine(
                    $"[DiffTrees] Gap node {i}: {oldEl.GetType().Name} → {newEl.GetType().Name} — replacing");

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

    /// <summary>
    /// Applies DiffTrees patches to the live WinUI control tree.
    /// Groups UpdateProp patches by node and delegates to existing Update() methods.
    /// Processes structural patches (Insert/Remove/Move/Replace) in the correct order.
    /// </summary>
    private UIElement ApplyPatches(
        ReadOnlySpan<ViewPatch> patches,
        SerializationResult oldTree, UIElement?[] oldControls,
        SerializationResult newTree,
        UIElement rootControl,
        Action requestRerender)
    {
        if (patches.Length == 0)
            return rootControl;

        // Collect nodes that have property updates (deduplicate — one Update() per node)
        var nodesToUpdate = new HashSet<uint>();
        // Collect structural operations
        var removals = new List<ViewPatch>();
        var insertions = new List<ViewPatch>();
        var moves = new List<ViewPatch>();
        var replacements = new List<ViewPatch>();

        foreach (var patch in patches)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[DiffTrees] Patch: {patch.Op} node={patch.NodeIndex} target={patch.TargetIndex} dpId={patch.DpId}");

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

        // Phase 1: Apply property updates (using existing Update dispatch)
        foreach (var nodeIdx in nodesToUpdate)
        {
            int idx = (int)nodeIdx;
            if (idx >= oldTree.Elements.Length || idx >= newTree.Elements.Length)
                continue;
            if (idx >= oldControls.Length || oldControls[idx] is null)
                continue;

            var oldEl = oldTree.Elements[idx];
            var newEl = newTree.Elements[idx];
            var control = oldControls[idx]!;

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
                    $"[DiffTrees] UpdateProp node {idx}: CanUpdate=false ({oldEl.GetType().Name}), replacing");
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

        // Phase 2: Handle replacements (type changed — unmount old, mount new)
        foreach (var patch in replacements)
        {
            int newIdx = (int)patch.NodeIndex;
            int oldIdx = (int)patch.TargetIndex;

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

        // Phase 3: Removals — process in reverse index order for stable indices
        removals.Sort((a, b) => b.NodeIndex.CompareTo(a.NodeIndex));
        foreach (var patch in removals)
        {
            int idx = (int)patch.NodeIndex;
            if (idx >= oldControls.Length || oldControls[idx] is not UIElement ctrl)
                continue;

            RemoveControlFromParent(ctrl);
            UnmountAndPool(ctrl);
            oldControls[idx] = null;
        }

        // Phase 4: Insertions
        foreach (var patch in insertions)
        {
            int newIdx = (int)patch.NodeIndex;
            if (newIdx >= newTree.Elements.Length) continue;

            var newEl = newTree.Elements[newIdx];
            var newCtrl = Mount(newEl, requestRerender);
            if (newCtrl is null) continue;

            // Find the parent control for this insertion
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
            if (oldIdx >= oldControls.Length || oldControls[oldIdx] is not UIElement ctrl)
                continue;

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

    /// <summary>
    /// Swaps a control in its parent container (Panel, Border, ScrollViewer, etc.).
    /// </summary>
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

    /// <summary>
    /// Removes a control from its parent container.
    /// </summary>
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

    /// <summary>
    /// Computes the position of a child within its parent's children
    /// based on the serialized node ordering.
    /// </summary>
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
