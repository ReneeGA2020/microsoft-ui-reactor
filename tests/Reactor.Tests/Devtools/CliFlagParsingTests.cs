using Microsoft.UI.Reactor.Hosting.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

public class CliFlagParsingTests
{
    [Fact]
    public void NoFlags_ReturnsNullSubverb()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe"]);
        Assert.Null(opts.Subverb);
        Assert.False(opts.UsedDeprecatedPreview);
        Assert.False(opts.PreviewAndDevtoolsConflict);
    }

    [Fact]
    public void DevtoolsRun_ParsesAsRun()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run"]);
        Assert.Equal(DevtoolsSubverb.Run, opts.Subverb);
        Assert.False(opts.UsedDeprecatedPreview);
    }

    [Fact]
    public void DevtoolsList_ParsesAsList()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "list"]);
        Assert.Equal(DevtoolsSubverb.List, opts.Subverb);
    }

    [Fact]
    public void DevtoolsScreenshot_ParsesAsScreenshot()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "screenshot", "CounterDemo", "--out", "c:/tmp/out.png"]);
        Assert.Equal(DevtoolsSubverb.Screenshot, opts.Subverb);
        Assert.Equal("CounterDemo", opts.ComponentName);
        Assert.Equal("c:/tmp/out.png", opts.ScreenshotOutputPath);
    }

    [Fact]
    public void DevtoolsTree_ParsesAsTree()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "tree"]);
        Assert.Equal(DevtoolsSubverb.Tree, opts.Subverb);
    }

    [Fact]
    public void DevtoolsBareFlag_DefaultsToRun()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools"]);
        Assert.Equal(DevtoolsSubverb.Run, opts.Subverb);
    }

    [Fact]
    public void DevtoolsRun_WithComponentName()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "MyComponent"]);
        Assert.Equal(DevtoolsSubverb.Run, opts.Subverb);
        Assert.Equal("MyComponent", opts.ComponentName);
    }

    [Fact]
    public void DevtoolsList_WithOutputPath()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "list", "c:/tmp/components.txt"]);
        Assert.Equal(DevtoolsSubverb.List, opts.Subverb);
        Assert.Equal("c:/tmp/components.txt", opts.ListOutputPath);
    }

    [Fact]
    public void PreviewAlias_SetsDeprecatedFlag_AndMapsToRun()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--preview", "MyComp"]);
        Assert.Equal(DevtoolsSubverb.Run, opts.Subverb);
        Assert.True(opts.UsedDeprecatedPreview);
        Assert.Equal("MyComp", opts.ComponentName);
    }

    [Fact]
    public void PreviewListAlias_SetsDeprecatedFlag_AndMapsToList()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--preview-list"]);
        Assert.Equal(DevtoolsSubverb.List, opts.Subverb);
        Assert.True(opts.UsedDeprecatedPreview);
    }

    [Fact]
    public void DevtoolsListLegacyAlias_MapsToList()
    {
        // --devtools-list is the one-step alias used by tools that pre-date the subverb form.
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools-list"]);
        Assert.Equal(DevtoolsSubverb.List, opts.Subverb);
        Assert.False(opts.UsedDeprecatedPreview);
    }

    [Fact]
    public void BothPreviewAndDevtools_IsConflict()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--preview", "--devtools", "run"]);
        Assert.True(opts.PreviewAndDevtoolsConflict);
    }

    [Fact]
    public void BothPreviewListAndDevtoolsList_IsConflict()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--preview-list", "--devtools", "list"]);
        Assert.True(opts.PreviewAndDevtoolsConflict);
    }

    [Fact]
    public void VsCodeFlag_IsPickedUp()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "--vscode"]);
        Assert.True(opts.VsCodeMode);
    }

    [Fact]
    public void FpsFlag_IsClampedToRange()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "--fps", "9999"]);
        Assert.Equal(30, opts.Fps);

        var opts2 = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "--fps", "0"]);
        Assert.Equal(1, opts2.Fps);

        var opts3 = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "--fps", "15"]);
        Assert.Equal(15, opts3.Fps);
    }

    [Fact]
    public void McpPortFlag_IsParsed()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "--mcp-port", "54321"]);
        Assert.Equal(54321, opts.McpPort);
    }

    [Fact]
    public void McpPortFlag_Default_IsNull()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run"]);
        Assert.Null(opts.McpPort);
    }

    [Fact]
    public void Fps_Default_Is10()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run"]);
        Assert.Equal(10, opts.Fps);
    }

    [Fact]
    public void ComponentName_LeadingDashIsNotTreatedAsName()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "--vscode"]);
        Assert.Null(opts.ComponentName);
    }

    [Fact]
    public void UnknownDevtoolsVerb_FallsBackToRun()
    {
        // Defensive: an unknown trailing token is treated as a component name, not a verb.
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "MyComponent"]);
        Assert.Equal(DevtoolsSubverb.Run, opts.Subverb);
        Assert.Equal("MyComponent", opts.ComponentName);
    }

    [Fact]
    public void LogLevelFlag_IsParsed()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "--devtools-log-level", "trace"]);
        Assert.Equal("trace", opts.LogLevel);
    }

    [Fact]
    public void LogLevelFlag_Default_IsNull()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run"]);
        Assert.Null(opts.LogLevel);
    }

    [Fact]
    public void McpTransportFlag_Stdio_IsPicked()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "--mcp-transport", "stdio"]);
        Assert.Equal(McpTransport.Stdio, opts.Transport);
    }

    [Fact]
    public void McpTransportFlag_DefaultsToHttp()
    {
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run"]);
        Assert.Equal(McpTransport.Http, opts.Transport);
    }

    [Fact]
    public void McpTransportFlag_UnknownValue_KeepsHttpDefault()
    {
        // An unknown transport token should not flip to stdio silently —
        // the HTTP default stays, so users don't end up with a silently
        // broken stdout stream.
        var opts = DevtoolsCliParser.Parse(["app.exe", "--devtools", "run", "--mcp-transport", "carrier-pigeon"]);
        Assert.Equal(McpTransport.Http, opts.Transport);
    }
}
