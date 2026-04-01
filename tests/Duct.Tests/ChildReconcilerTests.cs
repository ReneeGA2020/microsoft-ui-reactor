using Duct.Core;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for the LIS algorithm and keyed child reconciliation logic.
/// These are pure algorithmic tests — no WinUI thread needed.
/// </summary>
public class ChildReconcilerTests
{
    // ════════════════════════════════════════════════════════════════
    //  LIS (Longest Increasing Subsequence) algorithm
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void LIS_Empty_Array()
    {
        var result = ChildReconciler.ComputeLIS([]);
        Assert.Empty(result);
    }

    [Fact]
    public void LIS_Single_Element()
    {
        var result = ChildReconciler.ComputeLIS([5]);
        Assert.Single(result);
        Assert.Contains(0, result);
    }

    [Fact]
    public void LIS_Already_Sorted()
    {
        // [0, 1, 2, 3] — all in LIS
        var result = ChildReconciler.ComputeLIS([0, 1, 2, 3]);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void LIS_Reversed()
    {
        // [3, 2, 1, 0] — LIS length = 1
        var result = ChildReconciler.ComputeLIS([3, 2, 1, 0]);
        Assert.Single(result);
    }

    [Fact]
    public void LIS_With_Reorder()
    {
        // Reorder [A,B,C,D] to [A,C,D,B]
        // Old indices in new order: [0, 2, 3, 1]
        // LIS = [0, 2, 3] → items at new indices 0, 1, 2 (A, C, D)
        var result = ChildReconciler.ComputeLIS([0, 2, 3, 1]);
        Assert.Equal(3, result.Count);
        // Items at new positions 0, 1, 2 should be in LIS
        Assert.Contains(0, result); // A (old idx 0)
        Assert.Contains(1, result); // C (old idx 2)
        Assert.Contains(2, result); // D (old idx 3)
        // B (old idx 1, at new position 3) should NOT be in LIS
        Assert.DoesNotContain(3, result);
    }

    [Fact]
    public void LIS_Skips_Unmapped_Entries()
    {
        // -1 means unmapped (new item)
        var result = ChildReconciler.ComputeLIS([-1, 0, -1, 2, 3]);
        // Mapped values: 0 (idx 1), 2 (idx 3), 3 (idx 4) → LIS = [1, 3, 4]
        Assert.Equal(3, result.Count);
        Assert.Contains(1, result);
        Assert.Contains(3, result);
        Assert.Contains(4, result);
    }

    [Fact]
    public void LIS_All_Unmapped()
    {
        var result = ChildReconciler.ComputeLIS([-1, -1, -1]);
        Assert.Empty(result);
    }

    [Fact]
    public void LIS_Complex_Sequence()
    {
        // [2, 0, 1, 3] → LIS could be [0, 1, 3] (indices 1, 2, 3) length 3
        var result = ChildReconciler.ComputeLIS([2, 0, 1, 3]);
        Assert.Equal(3, result.Count);
    }

    // ════════════════════════════════════════════════════════════════
    //  Element key utilities
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void WithKey_Preserves_Key_On_Elements()
    {
        var el = new TextElement("hello") { Key = "item-1" };
        Assert.Equal("item-1", el.Key);
    }

    [Fact]
    public void Key_Null_By_Default()
    {
        var el = new TextElement("hello");
        Assert.Null(el.Key);
    }

    // ════════════════════════════════════════════════════════════════
    //  CanUpdate with keys
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void CanUpdate_Same_Type_Different_Key_Returns_False()
    {
        // Different keys mean different logical items — should not be updateable
        var reconciler = new Reconciler();
        var a = new TextElement("a") { Key = "1" };
        var b = new TextElement("b") { Key = "2" };
        Assert.False(reconciler.CanUpdate(a, b));
    }

    // ════════════════════════════════════════════════════════════════
    //  Record equality (Phase 3 fast path)
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Identical_TextElements_Are_Equal()
    {
        var a = new TextElement("hello");
        var b = new TextElement("hello");
        Assert.Equal(a, b);
    }

    [Fact]
    public void Different_TextElements_Are_Not_Equal()
    {
        var a = new TextElement("hello");
        var b = new TextElement("world");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void TextElement_With_Same_Properties_Are_Equal()
    {
        var a = new TextElement("hello") { FontSize = 16 };
        var b = new TextElement("hello") { FontSize = 16 };
        Assert.Equal(a, b);
    }

    [Fact]
    public void Identical_ButtonElements_Are_Equal()
    {
        // Note: ButtonElements with different Action instances won't be equal
        var a = new ButtonElement("Go");
        var b = new ButtonElement("Go");
        Assert.Equal(a, b);
    }

    [Fact]
    public void StackElement_With_Same_Children_Are_Equal()
    {
        var children = new Element[] { new TextElement("A"), new TextElement("B") };
        var a = new StackElement(Microsoft.UI.Xaml.Controls.Orientation.Vertical, children);
        var b = new StackElement(Microsoft.UI.Xaml.Controls.Orientation.Vertical, children);
        Assert.Equal(a, b);
    }
}
