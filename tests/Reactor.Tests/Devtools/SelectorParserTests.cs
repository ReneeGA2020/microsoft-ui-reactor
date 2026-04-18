using Microsoft.UI.Reactor.Hosting.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

public class SelectorParserTests
{
    [Fact]
    public void NodeId_Parses()
    {
        var ir = SelectorParser.Parse("r:main/CounterDemo.btn-inc");
        Assert.Equal(SelectorKind.NodeId, ir.Kind);
        Assert.Equal("r:main/CounterDemo.btn-inc", ir.NodeId);
    }

    [Fact]
    public void AutomationId_Parses()
    {
        var ir = SelectorParser.Parse("#btn-inc");
        Assert.Equal(SelectorKind.AutomationId, ir.Kind);
        Assert.Equal("btn-inc", ir.AutomationId);
    }

    [Fact]
    public void AutomationId_EmptyId_Throws()
    {
        Assert.Throws<FormatException>(() => SelectorParser.Parse("#"));
    }

    [Theory]
    [InlineData("[name='Increment']", "Increment")]
    [InlineData("[name=\"Submit\"]", "Submit")]
    [InlineData("[name='Hello World']", "Hello World")]
    public void AutomationName_Parses(string input, string expected)
    {
        var ir = SelectorParser.Parse(input);
        Assert.Equal(SelectorKind.AutomationName, ir.Kind);
        Assert.Equal(expected, ir.AutomationName);
    }

    [Fact]
    public void TypePath_SingleType_NoIndex()
    {
        var ir = SelectorParser.Parse("Button");
        Assert.Equal(SelectorKind.TypePath, ir.Kind);
        Assert.NotNull(ir.TypePath);
        Assert.Single(ir.TypePath);
        Assert.Equal("Button", ir.TypePath![0].TypeName);
        Assert.Null(ir.TypePath[0].Index);
    }

    [Fact]
    public void TypePath_WithIndex()
    {
        var ir = SelectorParser.Parse("Button[2]");
        Assert.Equal("Button", ir.TypePath![0].TypeName);
        Assert.Equal(2, ir.TypePath![0].Index);
    }

    [Fact]
    public void TypePath_Chained()
    {
        var ir = SelectorParser.Parse("StackPanel > Button");
        Assert.Equal(2, ir.TypePath!.Count);
        Assert.Equal("StackPanel", ir.TypePath![0].TypeName);
        Assert.Equal("Button", ir.TypePath![1].TypeName);
    }

    [Fact]
    public void TypePath_ChainedWithIndex()
    {
        var ir = SelectorParser.Parse("StackPanel > Button[1]");
        Assert.Equal(2, ir.TypePath!.Count);
        Assert.Equal(1, ir.TypePath![1].Index);
    }

    [Fact]
    public void ReactorSource_Parses()
    {
        var ir = SelectorParser.Parse("{component:'CounterDemo',line:42}");
        Assert.Equal(SelectorKind.ReactorSource, ir.Kind);
        Assert.Equal("CounterDemo", ir.ReactorComponent);
        Assert.Equal(42, ir.ReactorLine);
    }

    [Fact]
    public void ReactorSource_WithWhitespace_Parses()
    {
        var ir = SelectorParser.Parse("{ component: 'Foo', line: 7 }");
        Assert.Equal("Foo", ir.ReactorComponent);
        Assert.Equal(7, ir.ReactorLine);
    }

    [Fact]
    public void Empty_Throws()
    {
        Assert.Throws<FormatException>(() => SelectorParser.Parse(""));
        Assert.Throws<FormatException>(() => SelectorParser.Parse("   "));
    }

    [Fact]
    public void Garbage_Throws()
    {
        Assert.Throws<FormatException>(() => SelectorParser.Parse("!!!"));
    }
}
