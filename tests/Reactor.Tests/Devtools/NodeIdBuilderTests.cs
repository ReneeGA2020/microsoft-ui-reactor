using Microsoft.UI.Reactor.Hosting.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

public class NodeIdBuilderTests
{
    [Fact]
    public void AutomationId_ProducesComponentDotAutomationIdForm()
    {
        var node = new NodeDescriptor(
            WindowId: "main",
            ComponentName: "CounterDemo",
            AutomationId: "btn-inc",
            ReactorSource: null,
            TypeName: "Button",
            SiblingIndex: 0,
            StableAncestor: null);

        Assert.Equal("r:main/CounterDemo.btn-inc", NodeIdBuilder.Build(node));
    }

    [Fact]
    public void ReactorSource_ProducesFileLineSiblingForm()
    {
        var node = new NodeDescriptor(
            WindowId: "main",
            ComponentName: "CounterDemo",
            AutomationId: null,
            ReactorSource: new ReactorSourceRef("CounterDemo.cs", 42, 1),
            TypeName: "Button",
            SiblingIndex: 1,
            StableAncestor: null);

        Assert.Equal("r:main/CounterDemo.CounterDemo.cs:42:1", NodeIdBuilder.Build(node));
    }

    [Fact]
    public void AutomationId_WinsOverReactorSource()
    {
        var node = new NodeDescriptor(
            WindowId: "main",
            ComponentName: "CounterDemo",
            AutomationId: "btn",
            ReactorSource: new ReactorSourceRef("CounterDemo.cs", 42, 1),
            TypeName: "Button",
            SiblingIndex: 0,
            StableAncestor: null);

        Assert.Equal("r:main/CounterDemo.btn", NodeIdBuilder.Build(node));
    }

    [Fact]
    public void ContentAddressed_WithoutAncestor_FallsBackToTypeSiblingPath()
    {
        // No AutomationId, no source, no stable ancestor — a bare anonymous node.
        // The id still has to be deterministic and refer to the containing component.
        var node = new NodeDescriptor(
            WindowId: "main",
            ComponentName: "CounterDemo",
            AutomationId: null,
            ReactorSource: null,
            TypeName: "Border",
            SiblingIndex: 2,
            StableAncestor: null);

        Assert.Equal("r:main/CounterDemo~/Border[2]", NodeIdBuilder.Build(node));
    }

    [Fact]
    public void ContentAddressed_AnchorsAtNearestStableAncestor()
    {
        // Templated parts live under a button with an AutomationId.
        // The id should anchor on the button's id, not on the ancestor chain above it.
        var anchor = new NodeDescriptor(
            WindowId: "main",
            ComponentName: "CounterDemo",
            AutomationId: "btn-inc",
            ReactorSource: null,
            TypeName: "Button",
            SiblingIndex: 0,
            StableAncestor: null);

        var templatedPart = new NodeDescriptor(
            WindowId: "main",
            ComponentName: "CounterDemo",
            AutomationId: null,
            ReactorSource: null,
            TypeName: "ContentPresenter",
            SiblingIndex: 0,
            StableAncestor: anchor);

        Assert.Equal(
            "r:main/CounterDemo.btn-inc/~ContentPresenter[0]",
            NodeIdBuilder.Build(templatedPart));
    }

    [Fact]
    public void SameDescriptor_ProducesSameId()
    {
        var a = new NodeDescriptor("main", "C", "btn", null, "Button", 0, null);
        var b = new NodeDescriptor("main", "C", "btn", null, "Button", 0, null);
        Assert.Equal(NodeIdBuilder.Build(a), NodeIdBuilder.Build(b));
    }

    [Fact]
    public void WindowIdIsScopedInOutput()
    {
        var a = new NodeDescriptor("main", "C", "btn", null, "Button", 0, null);
        var b = new NodeDescriptor("aux", "C", "btn", null, "Button", 0, null);
        Assert.NotEqual(NodeIdBuilder.Build(a), NodeIdBuilder.Build(b));
        Assert.StartsWith("r:main/", NodeIdBuilder.Build(a));
        Assert.StartsWith("r:aux/", NodeIdBuilder.Build(b));
    }
}
