using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests verifying that the native DiffTrees reconciliation path and the C# fallback
/// path produce equivalent results. Each test runs both paths and compares outcomes.
/// </summary>
public class DiffTreesReconcilerTests
{
    private static bool NativeAvailable()
    {
        try { using var d = new ViewDiffer(); return true; }
        catch (DllNotFoundException) { return false; }
    }

    /// <summary>
    /// Tests that create WinUI controls need a full app host.
    /// Returns false if control creation would throw COMException.
    /// </summary>
    private static bool CanCreateControls()
    {
        try
        {
            _ = new TextBlock();
            return true;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  ReconcileMode basics
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ReconcileMode_Default_Is_Auto()
    {
        using var reconciler = new Reconciler();
        Assert.Equal(ReconcileMode.Auto, reconciler.Mode);
    }

    [Fact]
    public void ReconcileMode_Can_Set_CSharpFallback()
    {
        using var reconciler = new Reconciler();
        reconciler.Mode = ReconcileMode.CSharpFallback;
        Assert.Equal(ReconcileMode.CSharpFallback, reconciler.Mode);
    }

    [Fact]
    public void ReconcileMode_Can_Set_NativeDiffTree()
    {
        using var reconciler = new Reconciler();
        reconciler.Mode = ReconcileMode.NativeDiffTree;
        Assert.Equal(ReconcileMode.NativeDiffTree, reconciler.Mode);
    }

    // ════════════════════════════════════════════════════════════════
    //  TreeSerializer.SerializeWithMapping
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void SerializeWithMapping_Produces_Parallel_Element_Array()
    {
        var registry = new PropValueRegistry();
        var serializer = new TreeSerializer(registry);

        var tree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("B"),
        ]);

        var result = serializer.SerializeWithMapping(tree);

        // BFS order: StackElement, TextElement("A"), TextElement("B")
        Assert.Equal(3, result.Nodes.Length);
        Assert.Equal(3, result.Elements.Length);
        Assert.IsType<StackElement>(result.Elements[0]);
        Assert.IsType<TextElement>(result.Elements[1]);
        Assert.IsType<TextElement>(result.Elements[2]);
        Assert.Equal("A", ((TextElement)result.Elements[1]).Content);
        Assert.Equal("B", ((TextElement)result.Elements[2]).Content);
    }

    [Fact]
    public void SerializeWithMapping_Nested_Tree_BFS_Order()
    {
        var registry = new PropValueRegistry();
        var serializer = new TreeSerializer(registry);

        var tree = new StackElement(Orientation.Vertical, [
            new StackElement(Orientation.Horizontal, [
                new TextElement("inner"),
            ]),
            new TextElement("sibling"),
        ]);

        var result = serializer.SerializeWithMapping(tree);

        // BFS: outer stack, inner stack, "sibling" text, "inner" text
        Assert.Equal(4, result.Elements.Length);
        Assert.IsType<StackElement>(result.Elements[0]); // outer
        Assert.IsType<StackElement>(result.Elements[1]); // inner
        Assert.IsType<TextElement>(result.Elements[2]);   // "sibling"
        Assert.IsType<TextElement>(result.Elements[3]);   // "inner"
    }

    [Fact]
    public void SerializeWithMapping_Empty_Element_Returns_Empty()
    {
        var registry = new PropValueRegistry();
        var serializer = new TreeSerializer(registry);

        var result = serializer.SerializeWithMapping(new EmptyElement());

        Assert.Empty(result.Nodes);
        Assert.Empty(result.Elements);
    }

    // ════════════════════════════════════════════════════════════════
    //  DiffTrees path — property updates detected
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DiffTrees_Detects_Text_Property_Change()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        var registry = new PropValueRegistry();
        var serializer = new TreeSerializer(registry);

        var oldTree = new TextElement("Hello");
        var newTree = new TextElement("World");

        var oldResult = serializer.SerializeWithMapping(oldTree);
        registry = new PropValueRegistry();
        serializer = new TreeSerializer(registry);
        var newResult = serializer.SerializeWithMapping(newTree);

        using var differ = new ViewDiffer();
        var patches = differ.DiffTrees(
            oldResult.Nodes, oldResult.Props,
            newResult.Nodes, newResult.Props);

        // Should detect property change
        Assert.True(patches.Length > 0, "Expected UpdateProp patches for text change");
        bool hasUpdateProp = false;
        foreach (var p in patches)
            if (p.Op == ViewPatchOp.UpdateProp) hasUpdateProp = true;
        Assert.True(hasUpdateProp);
    }

    [Fact]
    public void DiffTrees_Detects_Child_Addition()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        var registry = new PropValueRegistry();
        var serializer = new TreeSerializer(registry);

        var oldTree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
        ]);
        var oldResult = serializer.SerializeWithMapping(oldTree);

        registry = new PropValueRegistry();
        serializer = new TreeSerializer(registry);
        var newTree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("B"),
        ]);
        var newResult = serializer.SerializeWithMapping(newTree);

        using var differ = new ViewDiffer();
        var patches = differ.DiffTrees(
            oldResult.Nodes, oldResult.Props,
            newResult.Nodes, newResult.Props);

        bool hasInsert = false;
        foreach (var p in patches)
            if (p.Op == ViewPatchOp.Insert) hasInsert = true;
        Assert.True(hasInsert, "Expected Insert patch for new child");
    }

    [Fact]
    public void DiffTrees_No_Patches_For_Identical_Trees()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        var tree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("B"),
        ]);

        var registry1 = new PropValueRegistry();
        var ser1 = new TreeSerializer(registry1);
        var result1 = ser1.SerializeWithMapping(tree);

        var registry2 = new PropValueRegistry();
        var ser2 = new TreeSerializer(registry2);
        var result2 = ser2.SerializeWithMapping(tree);

        using var differ = new ViewDiffer();
        var patches = differ.DiffTrees(
            result1.Nodes, result1.Props,
            result2.Nodes, result2.Props);

        Assert.Equal(0, patches.Length);
    }

    // ════════════════════════════════════════════════════════════════
    //  A/B equivalence: both paths produce same WinUI control tree
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void AB_Text_Update_Both_Paths_Produce_Same_Result()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        var oldTree = new TextElement("Hello");
        var newTree = new TextElement("World");

        // Path A: C# fallback
        var reconA = new Reconciler { Mode = ReconcileMode.CSharpFallback };
        var controlA = reconA.Mount(oldTree, () => { });
        var resultA = reconA.Reconcile(oldTree, newTree, controlA, () => { });

        // Path B: Native DiffTrees
        var reconB = new Reconciler { Mode = ReconcileMode.NativeDiffTree };
        var controlB = reconB.Mount(oldTree, () => { });
        var resultB = reconB.Reconcile(oldTree, newTree, controlB, () => { });

        // Both should produce TextBlocks with "World"
        var tbA = Assert.IsType<TextBlock>(resultA);
        var tbB = Assert.IsType<TextBlock>(resultB);
        Assert.Equal("World", tbA.Text);
        Assert.Equal("World", tbB.Text);

        reconA.Dispose();
        reconB.Dispose();
    }

    [Fact]
    public void AB_Stack_With_Children_Both_Paths_Produce_Same_Result()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        var oldTree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("B"),
        ]);
        var newTree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("C"), // B → C
        ]);

        // Path A: C# fallback
        var reconA = new Reconciler { Mode = ReconcileMode.CSharpFallback };
        var controlA = reconA.Mount(oldTree, () => { });
        var resultA = reconA.Reconcile(oldTree, newTree, controlA, () => { });

        // Path B: Native DiffTrees
        var reconB = new Reconciler { Mode = ReconcileMode.NativeDiffTree };
        var controlB = reconB.Mount(oldTree, () => { });
        var resultB = reconB.Reconcile(oldTree, newTree, controlB, () => { });

        // Both should produce StackPanels with 2 children: "A" and "C"
        var spA = Assert.IsType<StackPanel>(resultA);
        var spB = Assert.IsType<StackPanel>(resultB);
        Assert.Equal(2, spA.Children.Count);
        Assert.Equal(2, spB.Children.Count);
        Assert.Equal("A", ((TextBlock)spA.Children[0]).Text);
        Assert.Equal("C", ((TextBlock)spA.Children[1]).Text);
        Assert.Equal("A", ((TextBlock)spB.Children[0]).Text);
        Assert.Equal("C", ((TextBlock)spB.Children[1]).Text);

        reconA.Dispose();
        reconB.Dispose();
    }

    [Fact]
    public void AB_Child_Addition_Both_Paths_Produce_Same_Result()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        var oldTree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
        ]);
        var newTree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("B"),
        ]);

        // Path A: C# fallback
        var reconA = new Reconciler { Mode = ReconcileMode.CSharpFallback };
        var controlA = reconA.Mount(oldTree, () => { });
        var resultA = reconA.Reconcile(oldTree, newTree, controlA, () => { });

        // Path B: Native DiffTrees
        var reconB = new Reconciler { Mode = ReconcileMode.NativeDiffTree };
        var controlB = reconB.Mount(oldTree, () => { });
        var resultB = reconB.Reconcile(oldTree, newTree, controlB, () => { });

        var spA = Assert.IsType<StackPanel>(resultA);
        var spB = Assert.IsType<StackPanel>(resultB);
        Assert.Equal(2, spA.Children.Count);
        Assert.Equal(2, spB.Children.Count);
        Assert.Equal("B", ((TextBlock)spA.Children[1]).Text);
        Assert.Equal("B", ((TextBlock)spB.Children[1]).Text);

        reconA.Dispose();
        reconB.Dispose();
    }

    [Fact]
    public void AB_Child_Removal_Both_Paths_Produce_Same_Result()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        var oldTree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("B"),
            new TextElement("C"),
        ]);
        var newTree = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("C"),
        ]);

        // Path A: C# fallback
        var reconA = new Reconciler { Mode = ReconcileMode.CSharpFallback };
        var controlA = reconA.Mount(oldTree, () => { });
        var resultA = reconA.Reconcile(oldTree, newTree, controlA, () => { });

        // Path B: Native DiffTrees
        var reconB = new Reconciler { Mode = ReconcileMode.NativeDiffTree };
        var controlB = reconB.Mount(oldTree, () => { });
        var resultB = reconB.Reconcile(oldTree, newTree, controlB, () => { });

        var spA = Assert.IsType<StackPanel>(resultA);
        var spB = Assert.IsType<StackPanel>(resultB);
        Assert.Equal(2, spA.Children.Count);
        Assert.Equal(2, spB.Children.Count);
        Assert.Equal("A", ((TextBlock)spA.Children[0]).Text);
        Assert.Equal("C", ((TextBlock)spA.Children[1]).Text);
        Assert.Equal("A", ((TextBlock)spB.Children[0]).Text);
        Assert.Equal("C", ((TextBlock)spB.Children[1]).Text);

        reconA.Dispose();
        reconB.Dispose();
    }

    [Fact]
    public void AB_Type_Change_Both_Paths_Replace_Control()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        var oldTree = new TextElement("Hello");
        var newTree = new ButtonElement("Click");

        // Path A: C# fallback
        var reconA = new Reconciler { Mode = ReconcileMode.CSharpFallback };
        var controlA = reconA.Mount(oldTree, () => { });
        var resultA = reconA.Reconcile(oldTree, newTree, controlA, () => { });

        // Path B: Native DiffTrees
        var reconB = new Reconciler { Mode = ReconcileMode.NativeDiffTree };
        var controlB = reconB.Mount(oldTree, () => { });
        var resultB = reconB.Reconcile(oldTree, newTree, controlB, () => { });

        // Both should produce a Button (not a TextBlock)
        Assert.IsType<Button>(resultA);
        Assert.IsType<Button>(resultB);

        reconA.Dispose();
        reconB.Dispose();
    }

    [Fact]
    public void AB_No_Change_Both_Paths_Return_Same_Control()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        var tree = new TextElement("Hello");

        // Path A: C# fallback
        var reconA = new Reconciler { Mode = ReconcileMode.CSharpFallback };
        var controlA = reconA.Mount(tree, () => { });
        var resultA = reconA.Reconcile(tree, tree, controlA, () => { });

        // Path B: Native DiffTrees
        var reconB = new Reconciler { Mode = ReconcileMode.NativeDiffTree };
        var controlB = reconB.Mount(tree, () => { });
        var resultB = reconB.Reconcile(tree, tree, controlB, () => { });

        // Both should return a TextBlock with "Hello"
        var tbA = Assert.IsType<TextBlock>(resultA);
        var tbB = Assert.IsType<TextBlock>(resultB);
        Assert.Equal("Hello", tbA.Text);
        Assert.Equal("Hello", tbB.Text);

        reconA.Dispose();
        reconB.Dispose();
    }

    // ════════════════════════════════════════════════════════════════
    //  CSharpFallback still works independently
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void CSharpFallback_Mount_And_Update_Work_Without_Native()
    {
        if (!CanCreateControls()) return;
        using var reconciler = new Reconciler { Mode = ReconcileMode.CSharpFallback };

        var oldTree = new TextElement("Hello");
        var control = reconciler.Mount(oldTree, () => { });
        Assert.IsType<TextBlock>(control);
        Assert.Equal("Hello", ((TextBlock)control!).Text);

        var newTree = new TextElement("World");
        var result = reconciler.Reconcile(oldTree, newTree, control, () => { });
        Assert.IsType<TextBlock>(result);
        Assert.Equal("World", ((TextBlock)result!).Text);
    }

    [Fact]
    public void CSharpFallback_Handles_Null_To_Element()
    {
        if (!CanCreateControls()) return;
        using var reconciler = new Reconciler { Mode = ReconcileMode.CSharpFallback };
        var result = reconciler.Reconcile(null, new TextElement("Hi"), null, () => { });
        Assert.IsType<TextBlock>(result);
    }

    [Fact]
    public void CSharpFallback_Handles_Element_To_Null()
    {
        if (!CanCreateControls()) return;
        using var reconciler = new Reconciler { Mode = ReconcileMode.CSharpFallback };
        var control = reconciler.Mount(new TextElement("Hi"), () => { });
        var result = reconciler.Reconcile(new TextElement("Hi"), null, control, () => { });
        Assert.Null(result);
    }

    // ════════════════════════════════════════════════════════════════
    //  NativeDiffTree path works independently
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void NativeDiffTree_Mount_And_Update_Work()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        using var reconciler = new Reconciler { Mode = ReconcileMode.NativeDiffTree };

        var oldTree = new TextElement("Hello");
        var control = reconciler.Mount(oldTree, () => { });
        Assert.IsType<TextBlock>(control);

        var newTree = new TextElement("World");
        var result = reconciler.Reconcile(oldTree, newTree, control, () => { });
        Assert.IsType<TextBlock>(result);
        Assert.Equal("World", ((TextBlock)result!).Text);
    }

    [Fact]
    public void NativeDiffTree_Handles_Null_To_Element()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        using var reconciler = new Reconciler { Mode = ReconcileMode.NativeDiffTree };
        var result = reconciler.Reconcile(null, new TextElement("Hi"), null, () => { });
        Assert.IsType<TextBlock>(result);
    }

    [Fact]
    public void NativeDiffTree_Handles_Element_To_Null()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        using var reconciler = new Reconciler { Mode = ReconcileMode.NativeDiffTree };
        var control = reconciler.Mount(new TextElement("Hi"), () => { });
        var result = reconciler.Reconcile(new TextElement("Hi"), null, control, () => { });
        Assert.Null(result);
    }

    [Fact]
    public void NativeDiffTree_Multiple_Updates_In_Sequence()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        using var reconciler = new Reconciler { Mode = ReconcileMode.NativeDiffTree };

        Element current = new TextElement("V1");
        var control = reconciler.Mount(current, () => { });

        // Update 1: V1 → V2
        var next = new TextElement("V2");
        control = reconciler.Reconcile(current, next, control, () => { });
        Assert.Equal("V2", ((TextBlock)control!).Text);
        current = next;

        // Update 2: V2 → V3
        next = new TextElement("V3");
        control = reconciler.Reconcile(current, next, control, () => { });
        Assert.Equal("V3", ((TextBlock)control!).Text);
        current = next;

        // Update 3: V3 → V4
        next = new TextElement("V4");
        control = reconciler.Reconcile(current, next, control, () => { });
        Assert.Equal("V4", ((TextBlock)control!).Text);
    }

    [Fact]
    public void NativeDiffTree_Stack_Multiple_Updates()
    {
        if (!NativeAvailable() || !CanCreateControls()) return;

        using var reconciler = new Reconciler { Mode = ReconcileMode.NativeDiffTree };

        Element current = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("B"),
        ]);
        var control = reconciler.Mount(current, () => { });

        // Update: change B → C
        var next = new StackElement(Orientation.Vertical, [
            new TextElement("A"),
            new TextElement("C"),
        ]);
        control = reconciler.Reconcile(current, next, control, () => { });
        var sp = Assert.IsType<StackPanel>(control);
        Assert.Equal(2, sp.Children.Count);
        Assert.Equal("C", ((TextBlock)sp.Children[1]).Text);
    }
}
