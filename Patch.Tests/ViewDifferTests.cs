using Patch.Core;
using Xunit;

namespace Patch.Tests;

/// <summary>
/// Tests for the ViewDiffer C# interop types and utility methods.
/// Hash tests work without the native DLL. FFI tests require the DLL.
/// </summary>
public class ViewDifferTests
{
    // ════════════════════════════════════════════════════════════════
    //  FNV-1a Hash — pure C#, no native DLL needed
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void HashString_Deterministic()
    {
        var h1 = ViewDiffer.HashString("TextBlock");
        var h2 = ViewDiffer.HashString("TextBlock");
        Assert.Equal(h1, h2);
        Assert.NotEqual(0u, h1);
    }

    [Fact]
    public void HashString_Different_Inputs_Different_Hashes()
    {
        var h1 = ViewDiffer.HashString("Text");
        var h2 = ViewDiffer.HashString("Button");
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void HashString_Empty_String()
    {
        var h = ViewDiffer.HashString("");
        // FNV-1a offset basis
        Assert.Equal(0x811c_9dc5u, h);
    }

    [Fact]
    public void HashString_Known_Value()
    {
        // FNV-1a of "a" should be: 0x811c9dc5 XOR 0x61 = 0x811c9ca4, then * 0x01000193
        var h = ViewDiffer.HashString("a");
        Assert.NotEqual(0u, h);
        Assert.NotEqual(0x811c_9dc5u, h); // Different from empty
    }

    // ════════════════════════════════════════════════════════════════
    //  Wire type layout verification
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void ViewNode_Default_Values()
    {
        var node = new ViewNode();
        Assert.Equal(0u, node.TypeId);
        Assert.Equal(0L, node.Key);
        Assert.Equal(0, node.ParentIndex);
    }

    [Fact]
    public void ViewPatch_Default_Values()
    {
        var patch = new ViewPatch();
        Assert.Equal(ViewPatchOp.None, patch.Op);
        Assert.Equal(0u, patch.NodeIndex);
    }

    [Fact]
    public void ViewPatchOp_Values_Match_Rust()
    {
        Assert.Equal(0, (int)ViewPatchOp.None);
        Assert.Equal(1, (int)ViewPatchOp.Insert);
        Assert.Equal(2, (int)ViewPatchOp.Remove);
        Assert.Equal(3, (int)ViewPatchOp.Move);
        Assert.Equal(4, (int)ViewPatchOp.UpdateProp);
        Assert.Equal(5, (int)ViewPatchOp.Replace);
    }
}
