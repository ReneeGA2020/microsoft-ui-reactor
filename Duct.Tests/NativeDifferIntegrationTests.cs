using Duct.Core;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests verifying the Rust native differ integration.
/// Tests that require the native DLL are guarded by NativeAvailable
/// and skip gracefully when the DLL is not present.
/// </summary>
public class NativeDifferIntegrationTests : IDisposable
{
    private ViewDiffer? _differ;
    private readonly bool _nativeAvailable;

    public NativeDifferIntegrationTests()
    {
        try
        {
            _differ = new ViewDiffer();
            _nativeAvailable = true;
        }
        catch (DllNotFoundException)
        {
            _nativeAvailable = false;
        }
    }

    public void Dispose()
    {
        _differ?.Dispose();
    }

    private bool NativeAvailable => _nativeAvailable;

    // ════════════════════════════════════════════════════════════════
    //  ViewDiffer.ReconcileKeys — FFI basic operations
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ReconcileKeys_Identical_Lists_No_Patches()
    {
        if (!NativeAvailable) return;
        var keys = new long[] { 1, 2, 3 };
        var patches = _differ!.ReconcileKeys(keys, keys);
        Assert.Equal(0, patches.Length);
    }

    [Fact]
    public void ReconcileKeys_All_New_Emits_Inserts()
    {
        if (!NativeAvailable) return;
        var oldKeys = ReadOnlySpan<long>.Empty;
        var newKeys = new long[] { 10, 20, 30 };
        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        Assert.Equal(3, patches.Length);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(ViewPatchOp.Insert, patches[i].Op);
            Assert.Equal((uint)i, patches[i].NodeIndex);
        }
    }

    [Fact]
    public void ReconcileKeys_All_Removed_Emits_Removes()
    {
        if (!NativeAvailable) return;
        var oldKeys = new long[] { 10, 20, 30 };
        var newKeys = ReadOnlySpan<long>.Empty;
        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        Assert.Equal(3, patches.Length);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(ViewPatchOp.Remove, patches[i].Op);
            Assert.Equal((uint)i, patches[i].NodeIndex);
        }
    }

    [Fact]
    public void ReconcileKeys_Simple_Reorder_Emits_Move()
    {
        if (!NativeAvailable) return;
        // [A=1, B=2, C=3] → [A=1, C=3, B=2]
        var oldKeys = new long[] { 1, 2, 3 };
        var newKeys = new long[] { 1, 3, 2 };
        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        // Should emit exactly one Move (B moves from old pos 1 to new pos 2)
        Assert.True(patches.Length >= 1, $"Expected at least 1 patch, got {patches.Length}");
        bool hasMove = false;
        foreach (var p in patches)
        {
            if (p.Op == ViewPatchOp.Move)
                hasMove = true;
        }
        Assert.True(hasMove, "Expected at least one Move patch");
    }

    [Fact]
    public void ReconcileKeys_Insert_In_Middle()
    {
        if (!NativeAvailable) return;
        // [A=1, C=3] → [A=1, B=2, C=3]
        var oldKeys = new long[] { 1, 3 };
        var newKeys = new long[] { 1, 2, 3 };
        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        bool hasInsert = false;
        foreach (var p in patches)
        {
            if (p.Op == ViewPatchOp.Insert && p.NodeIndex == 1)
                hasInsert = true;
        }
        Assert.True(hasInsert, "Expected Insert patch at index 1");
    }

    [Fact]
    public void ReconcileKeys_Remove_From_Middle()
    {
        if (!NativeAvailable) return;
        // [A=1, B=2, C=3] → [A=1, C=3]
        var oldKeys = new long[] { 1, 2, 3 };
        var newKeys = new long[] { 1, 3 };
        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        bool hasRemove = false;
        foreach (var p in patches)
        {
            if (p.Op == ViewPatchOp.Remove && p.NodeIndex == 1)
                hasRemove = true;
        }
        Assert.True(hasRemove, "Expected Remove patch for old index 1");
    }

    [Fact]
    public void ReconcileKeys_Full_Reversal()
    {
        if (!NativeAvailable) return;
        // [1, 2, 3, 4] → [4, 3, 2, 1]
        var oldKeys = new long[] { 1, 2, 3, 4 };
        var newKeys = new long[] { 4, 3, 2, 1 };
        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        // LIS of reversed = 1, so 3 items need to Move
        int moveCount = 0;
        foreach (var p in patches)
        {
            if (p.Op == ViewPatchOp.Move) moveCount++;
        }
        Assert.Equal(3, moveCount);
    }

    [Fact]
    public void ReconcileKeys_Empty_Both()
    {
        if (!NativeAvailable) return;
        var patches = _differ!.ReconcileKeys(ReadOnlySpan<long>.Empty, ReadOnlySpan<long>.Empty);
        Assert.Equal(0, patches.Length);
    }

    [Fact]
    public void ReconcileKeys_Mixed_Insert_Remove_Move()
    {
        if (!NativeAvailable) return;
        // [A=1, B=2, C=3, D=4] → [D=4, A=1, E=5]
        var oldKeys = new long[] { 1, 2, 3, 4 };
        var newKeys = new long[] { 4, 1, 5 };
        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        int removes = 0, inserts = 0, moves = 0;
        foreach (var p in patches)
        {
            switch (p.Op)
            {
                case ViewPatchOp.Remove: removes++; break;
                case ViewPatchOp.Insert: inserts++; break;
                case ViewPatchOp.Move: moves++; break;
            }
        }

        Assert.Equal(2, removes); // B and C removed
        Assert.Equal(1, inserts); // E inserted
        Assert.True(moves >= 1, "Expected at least one Move (D)");
    }

    // ════════════════════════════════════════════════════════════════
    //  Reconciler.Differ — lazy initialization and availability
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Reconciler_Differ_Property_Returns_Same_Instance()
    {
        using var reconciler = new Reconciler();
        var differ1 = reconciler.Differ;
        var differ2 = reconciler.Differ;
        Assert.Same(differ1, differ2);
    }

    [Fact]
    public void Reconciler_Dispose_Cleans_Up_Differ()
    {
        var reconciler = new Reconciler();
        _ = reconciler.Differ; // Force initialization
        reconciler.Dispose();
        // Should not throw
    }

    // ════════════════════════════════════════════════════════════════
    //  Native vs Fallback equivalence
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Native_And_Fallback_LIS_Agree_On_Simple_Reorder()
    {
        if (!NativeAvailable) return;

        // [A, B, C, D] → [A, C, D, B]
        var newToOld = new int[] { 0, 2, 3, 1 };
        var csharpLIS = ChildReconciler.ComputeLIS(newToOld);

        var oldKeys = new long[] { 100, 200, 300, 400 };
        var newKeys = new long[] { 100, 300, 400, 200 };
        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        // C# LIS: items at new indices {0, 1, 2} stay (A, C, D)
        Assert.Contains(0, csharpLIS);
        Assert.Contains(1, csharpLIS);
        Assert.Contains(2, csharpLIS);
        Assert.DoesNotContain(3, csharpLIS);

        // Rust: exactly one Move for B (old_index=1 → new_position=3)
        int moveCount = 0;
        foreach (var p in patches)
        {
            if (p.Op == ViewPatchOp.Move)
            {
                Assert.Equal(1u, p.NodeIndex);     // old index of B
                Assert.Equal(3u, p.TargetIndex);    // new position
                moveCount++;
            }
        }
        Assert.Equal(1, moveCount);
    }

    [Fact]
    public void Native_And_Fallback_LIS_Agree_On_Complex_Shuffle()
    {
        if (!NativeAvailable) return;

        // [A, B, C, D, E] → [E, C, A, D, B]
        var newToOld = new int[] { 4, 2, 0, 3, 1 };
        var csharpLIS = ChildReconciler.ComputeLIS(newToOld);

        var oldKeys = new long[] { 10, 20, 30, 40, 50 };
        var newKeys = new long[] { 50, 30, 10, 40, 20 };
        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        int expectedMoves = 5 - csharpLIS.Count;
        int rustMoves = 0;
        foreach (var p in patches)
        {
            if (p.Op == ViewPatchOp.Move) rustMoves++;
        }

        // Both implementations agree on the number of moves
        Assert.Equal(expectedMoves, rustMoves);
    }

    // ════════════════════════════════════════════════════════════════
    //  ViewDiffer.DiffTrees — full tree diff FFI
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void DiffTrees_Identical_Trees_No_Patches()
    {
        if (!NativeAvailable) return;

        var nodes = new ViewNode[]
        {
            new() { TypeId = 1, Key = 0, ParentIndex = -1, PropCount = 1, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
        };
        var props = new ViewProp[]
        {
            new() { DpId = 100, ValueHash = 42 },
        };

        var patches = _differ!.DiffTrees(nodes, props, nodes, props);
        Assert.Equal(0, patches.Length);
    }

    [Fact]
    public void DiffTrees_Property_Change_Emits_UpdateProp()
    {
        if (!NativeAvailable) return;

        var oldNodes = new ViewNode[]
        {
            new() { TypeId = 1, Key = 0, ParentIndex = -1, PropCount = 1, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
        };
        var oldProps = new ViewProp[] { new() { DpId = 100, ValueHash = 42 } };

        var newNodes = new ViewNode[]
        {
            new() { TypeId = 1, Key = 0, ParentIndex = -1, PropCount = 1, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
        };
        var newProps = new ViewProp[] { new() { DpId = 100, ValueHash = 99 } };

        var patches = _differ!.DiffTrees(oldNodes, oldProps, newNodes, newProps);
        Assert.Equal(1, patches.Length);
        Assert.Equal(ViewPatchOp.UpdateProp, patches[0].Op);
        Assert.Equal(100u, patches[0].DpId);
        Assert.Equal(99u, patches[0].NewValueHash);
    }

    [Fact]
    public void DiffTrees_Type_Change_Emits_Replace()
    {
        if (!NativeAvailable) return;

        var oldNodes = new ViewNode[]
        {
            new() { TypeId = 1, Key = 0, ParentIndex = -1, PropCount = 0, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
        };
        var newNodes = new ViewNode[]
        {
            new() { TypeId = 2, Key = 0, ParentIndex = -1, PropCount = 0, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
        };

        var patches = _differ!.DiffTrees(oldNodes, Array.Empty<ViewProp>(), newNodes, Array.Empty<ViewProp>());
        Assert.Equal(1, patches.Length);
        Assert.Equal(ViewPatchOp.Replace, patches[0].Op);
    }

    [Fact]
    public void DiffTrees_Empty_To_Nodes_Emits_Inserts()
    {
        if (!NativeAvailable) return;

        var newNodes = new ViewNode[]
        {
            new() { TypeId = 1, Key = 0, ParentIndex = -1, PropCount = 0, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
            new() { TypeId = 2, Key = 0, ParentIndex = -1, PropCount = 0, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
        };

        var patches = _differ!.DiffTrees(
            ReadOnlySpan<ViewNode>.Empty, ReadOnlySpan<ViewProp>.Empty,
            newNodes, ReadOnlySpan<ViewProp>.Empty);

        Assert.Equal(2, patches.Length);
        Assert.Equal(ViewPatchOp.Insert, patches[0].Op);
        Assert.Equal(ViewPatchOp.Insert, patches[1].Op);
    }

    [Fact]
    public void DiffTrees_Nodes_To_Empty_Emits_Removes()
    {
        if (!NativeAvailable) return;

        var oldNodes = new ViewNode[]
        {
            new() { TypeId = 1, Key = 0, ParentIndex = -1, PropCount = 0, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
        };

        var patches = _differ!.DiffTrees(
            oldNodes, ReadOnlySpan<ViewProp>.Empty,
            ReadOnlySpan<ViewNode>.Empty, ReadOnlySpan<ViewProp>.Empty);

        Assert.Equal(1, patches.Length);
        Assert.Equal(ViewPatchOp.Remove, patches[0].Op);
    }

    [Fact]
    public void DiffTrees_Child_Added()
    {
        if (!NativeAvailable) return;

        var oldNodes = new ViewNode[]
        {
            new() { TypeId = 1, Key = 0, ParentIndex = -1, PropCount = 0, ChildCount = 1, FirstChild = 1, FirstProp = 0 },
            new() { TypeId = 2, Key = 0, ParentIndex = 0, PropCount = 0, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
        };

        var newNodes = new ViewNode[]
        {
            new() { TypeId = 1, Key = 0, ParentIndex = -1, PropCount = 0, ChildCount = 2, FirstChild = 1, FirstProp = 0 },
            new() { TypeId = 2, Key = 0, ParentIndex = 0, PropCount = 0, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
            new() { TypeId = 3, Key = 0, ParentIndex = 0, PropCount = 0, ChildCount = 0, FirstChild = 0, FirstProp = 0 },
        };

        var patches = _differ!.DiffTrees(
            oldNodes, ReadOnlySpan<ViewProp>.Empty,
            newNodes, ReadOnlySpan<ViewProp>.Empty);

        bool hasInsert = false;
        foreach (var p in patches)
        {
            if (p.Op == ViewPatchOp.Insert && p.NodeIndex == 2)
                hasInsert = true;
        }
        Assert.True(hasInsert, "Expected Insert patch for new child at index 2");
    }

    // ════════════════════════════════════════════════════════════════
    //  Hash consistency
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void HashString_FNV1a_Known_Values()
    {
        Assert.Equal(0x811c_9dc5u, ViewDiffer.HashString("")); // FNV offset basis
        uint ha = ViewDiffer.HashString("a");
        Assert.NotEqual(0u, ha);
        Assert.Equal(ha, ViewDiffer.HashString("a")); // deterministic
        Assert.NotEqual(ViewDiffer.HashString("TextElement"), ViewDiffer.HashString("ButtonElement"));
    }

    [Fact]
    public void ReconcileKeys_Reuse_Context_Works()
    {
        if (!NativeAvailable) return;

        for (int round = 0; round < 5; round++)
        {
            var oldKeys = new long[] { 1, 2, 3 };
            var newKeys = new long[] { 3, 1, 2 };
            var patches = _differ!.ReconcileKeys(oldKeys, newKeys);
            Assert.True(patches.Length > 0, $"Round {round}: expected patches");
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Stress tests
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ReconcileKeys_Large_List_Reverse()
    {
        if (!NativeAvailable) return;

        const int n = 500;
        var oldKeys = new long[n];
        var newKeys = new long[n];
        for (int i = 0; i < n; i++)
        {
            oldKeys[i] = i + 1;
            newKeys[i] = n - i;
        }

        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        int moveCount = 0;
        foreach (var p in patches)
        {
            if (p.Op == ViewPatchOp.Move) moveCount++;
        }
        Assert.Equal(n - 1, moveCount);
    }

    [Fact]
    public void ReconcileKeys_Large_List_Shuffle()
    {
        if (!NativeAvailable) return;

        const int n = 1000;
        var oldKeys = new long[n];
        var newKeys = new long[n];
        for (int i = 0; i < n; i++)
        {
            oldKeys[i] = i;
            newKeys[i] = i;
        }

        var rng = new Random(42);
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (newKeys[i], newKeys[j]) = (newKeys[j], newKeys[i]);
        }

        var patches = _differ!.ReconcileKeys(oldKeys, newKeys);

        int removes = 0, inserts = 0;
        foreach (var p in patches)
        {
            if (p.Op == ViewPatchOp.Remove) removes++;
            if (p.Op == ViewPatchOp.Insert) inserts++;
        }
        Assert.Equal(0, removes);
        Assert.Equal(0, inserts);
    }
}
