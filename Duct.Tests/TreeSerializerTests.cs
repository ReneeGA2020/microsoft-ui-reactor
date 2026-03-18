using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Xunit;
using static Duct.UI;

namespace Duct.Tests;

public class TreeSerializerTests
{
    private readonly PropValueRegistry _registry = new();
    private readonly TreeSerializer _serializer;

    public TreeSerializerTests()
    {
        _serializer = new TreeSerializer(_registry);
    }

    [Fact]
    public void Single_TextElement_Produces_One_Node()
    {
        var (nodes, props) = _serializer.Serialize(Text("hello"));

        Assert.Single(nodes);
        Assert.Equal(ViewDiffer.HashString("TextElement"), nodes[0].TypeId);
        Assert.Equal(-1, nodes[0].ParentIndex);
        Assert.Equal(0, nodes[0].ChildCount);
        Assert.True(props.Length > 0); // At least Content prop
    }

    [Fact]
    public void BFS_Order_Children_Are_Contiguous()
    {
        // VStack(Text("a"), Text("b")) => root + 2 children
        var tree = VStack(Text("a"), Text("b"));
        var (nodes, _) = _serializer.Serialize(tree);

        Assert.Equal(3, nodes.Length);
        // Root is a StackElement
        Assert.Equal(ViewDiffer.HashString("StackElement"), nodes[0].TypeId);
        Assert.Equal(2, nodes[0].ChildCount);
        // Children are at indices 1 and 2
        Assert.Equal((uint)1, nodes[0].FirstChild);
        // Both children have parent = 0
        Assert.Equal(0, nodes[1].ParentIndex);
        Assert.Equal(0, nodes[2].ParentIndex);
    }

    [Fact]
    public void Modifier_Unwrapping_Serializes_Inner_Element()
    {
        var tree = Text("modified").Margin(10);
        var (nodes, _) = _serializer.Serialize(tree);

        // ModifiedElement should be unwrapped to TextElement
        Assert.Single(nodes);
        Assert.Equal(ViewDiffer.HashString("TextElement"), nodes[0].TypeId);
    }

    [Fact]
    public void Empty_Element_Is_Skipped()
    {
        var (nodes, _) = _serializer.Serialize(Empty());
        Assert.Empty(nodes);
    }

    [Fact]
    public void Nested_Containers_BFS()
    {
        // VStack(HStack(Text("x")))
        var tree = VStack(HStack(Text("x")));
        var (nodes, _) = _serializer.Serialize(tree);

        // BFS: VStack, HStack, Text
        Assert.Equal(3, nodes.Length);
        Assert.Equal(ViewDiffer.HashString("StackElement"), nodes[0].TypeId); // VStack
        Assert.Equal(ViewDiffer.HashString("StackElement"), nodes[1].TypeId); // HStack
        Assert.Equal(ViewDiffer.HashString("TextElement"), nodes[2].TypeId);  // Text
    }

    [Fact]
    public void Keyed_Elements_Set_Key_Field()
    {
        var tree = Text("keyed").WithKey("my-key");
        var (nodes, _) = _serializer.Serialize(tree);

        Assert.Single(nodes);
        Assert.Equal((long)ViewDiffer.HashString("my-key"), nodes[0].Key);
    }

    [Fact]
    public void Complex_Props_Stored_In_Registry()
    {
        var clicked = false;
        var tree = Button("Click", () => clicked = true);
        _registry.Clear();
        var (nodes, props) = _serializer.Serialize(tree);

        Assert.Single(nodes);
        // Should have at least Label, IsEnabled, OnClick
        Assert.True(props.Length >= 3);

        // Find the OnClick prop — its ValueHash should be a registry ID
        var onClickDpId = ViewDiffer.HashString("OnClick");
        var onClickProp = props.First(p => p.DpId == onClickDpId);
        var handler = _registry.Retrieve(onClickProp.ValueHash) as Action;
        Assert.NotNull(handler);
        handler!();
        Assert.True(clicked);
    }

    [Fact]
    public void ScrollView_Has_One_Child()
    {
        var tree = ScrollView(Text("inner"));
        var (nodes, _) = _serializer.Serialize(tree);

        Assert.Equal(2, nodes.Length);
        Assert.Equal(1, nodes[0].ChildCount);
        Assert.Equal(ViewDiffer.HashString("ScrollViewElement"), nodes[0].TypeId);
    }

    [Fact]
    public void Border_Has_One_Child()
    {
        var tree = Border(Text("inner"));
        var (nodes, _) = _serializer.Serialize(tree);

        Assert.Equal(2, nodes.Length);
        Assert.Equal(1, nodes[0].ChildCount);
    }

    [Fact]
    public void Empty_Children_Filtered_In_Stack()
    {
        // VStack with a null child that gets filtered by FilterChildren
        var tree = VStack(Text("a"), Empty(), Text("b"));
        var (nodes, _) = _serializer.Serialize(tree);

        // VStack + 2 text nodes (Empty is skipped during serialization)
        // The VStack itself filters nulls, but Empty passes through as an element
        // The serializer should skip EmptyElement children
        Assert.Equal(3, nodes.Length); // VStack + a + b
    }

    [Fact]
    public void Component_Serialized_As_Opaque_Leaf()
    {
        var tree = new ComponentElement(typeof(string)); // dummy type
        var (nodes, _) = _serializer.Serialize(tree);

        Assert.Single(nodes);
        Assert.Equal(ViewDiffer.HashString("ComponentElement"), nodes[0].TypeId);
        Assert.Equal(0, nodes[0].ChildCount);
    }
}
