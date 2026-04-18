using System.Text.Json;
using Microsoft.UI.Reactor.Cli.Devtools;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Devtools;

/// <summary>
/// Contract tests for <c>mur devtools --print-config</c>. The output is a
/// mixed text/JSON document (human headers + three JSON fragments), and the
/// critical guarantees are: each fragment is valid JSON, the endpoint URL
/// matches the requested port, and the tool prints to stdout only (tested
/// here indirectly by building the string in-process).
/// </summary>
public class PrintConfigTests
{
    [Fact]
    public void Payload_ContainsEndpointUrlForGivenPort()
    {
        var s = DevtoolsSupervisor.BuildPrintConfigPayload(54321);
        Assert.Contains("http://127.0.0.1:54321/mcp", s);
    }

    [Fact]
    public void Payload_HasHeadersForAllThreeAgents()
    {
        var s = DevtoolsSupervisor.BuildPrintConfigPayload(12345);
        Assert.Contains("Claude Code", s);
        Assert.Contains("VS Code", s);
        Assert.Contains("Copilot", s);
    }

    [Fact]
    public void Payload_FragmentsAreValidJson()
    {
        // Extract each JSON block between the `##` headers and assert it
        // parses. Keeps the assertion resilient to label wording changes.
        var s = DevtoolsSupervisor.BuildPrintConfigPayload(9876);
        var blocks = ExtractJsonBlocks(s);

        Assert.Equal(3, blocks.Count);
        foreach (var block in blocks)
        {
            using var doc = JsonDocument.Parse(block);
            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        }
    }

    [Fact]
    public void ClaudeCode_FragmentUsesMcpServersKey()
    {
        var s = DevtoolsSupervisor.BuildPrintConfigPayload(9000);
        var blocks = ExtractJsonBlocks(s);
        using var claudeCode = JsonDocument.Parse(blocks[0]);
        Assert.True(claudeCode.RootElement.TryGetProperty("mcpServers", out var servers));
        Assert.True(servers.TryGetProperty("reactor", out var reactor));
        Assert.Equal("http", reactor.GetProperty("type").GetString());
        Assert.Equal("http://127.0.0.1:9000/mcp", reactor.GetProperty("url").GetString());
    }

    [Fact]
    public void VsCodeAndCopilot_FragmentsUseServersKey()
    {
        // VS Code and Copilot share the flat `servers` shape today. If the
        // formats diverge we bump one and leave the other alone.
        var s = DevtoolsSupervisor.BuildPrintConfigPayload(9001);
        var blocks = ExtractJsonBlocks(s);
        foreach (var block in blocks.Skip(1))
        {
            using var doc = JsonDocument.Parse(block);
            Assert.True(doc.RootElement.TryGetProperty("servers", out var servers));
            Assert.True(servers.TryGetProperty("reactor", out var reactor));
            Assert.Equal("http://127.0.0.1:9001/mcp", reactor.GetProperty("url").GetString());
        }
    }

    private static List<string> ExtractJsonBlocks(string payload)
    {
        var blocks = new List<string>();
        var lines = payload.Split('\n');
        var current = new List<string>();
        bool inBlock = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("##"))
            {
                if (inBlock && current.Count > 0)
                {
                    blocks.Add(string.Join('\n', current).Trim());
                    current.Clear();
                }
                inBlock = true;
                continue;
            }
            if (inBlock) current.Add(line);
        }
        if (inBlock && current.Count > 0)
            blocks.Add(string.Join('\n', current).Trim());

        return blocks;
    }
}
