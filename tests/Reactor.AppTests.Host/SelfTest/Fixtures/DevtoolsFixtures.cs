using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Hosting;
using Microsoft.UI.Reactor.Hosting.Devtools;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Self-host MCP fixtures: mount a small Reactor tree inside the harness window,
/// spin up a <see cref="DevtoolsMcpServer"/> against it on a free loopback port,
/// and exercise the tool surface via in-test JSON-RPC calls.
/// Covers the self-host rows of §2.17 and §3.11 that need a real WinUI window
/// in-process but don't need Appium's second-process rigging.
/// </summary>
internal static class DevtoolsFixtures
{
    // -- Shared MCP harness ------------------------------------------------------

    /// <summary>
    /// Wires a <see cref="DevtoolsMcpServer"/> + registries + tool surface around
    /// the selftest harness window so fixtures can make real JSON-RPC calls over
    /// HTTP. Disposed per-fixture so ports don't leak and event handlers on the
    /// shared window don't accumulate past the fixture's run.
    /// </summary>
    internal sealed class McpHarness : IDisposable
    {
        public DevtoolsMcpServer Server { get; }
        public WindowRegistry Windows { get; }
        public NodeRegistry Nodes { get; }
        private readonly HttpClient _client;
        private readonly string _currentComponent;

        public McpHarness(
            Window window,
            Func<Component?> rootComponent,
            string currentComponent,
            IReadOnlyList<string>? components = null)
        {
            Server = new DevtoolsMcpServer(window.DispatcherQueue, window);
            Windows = new WindowRegistry(Server.BuildTag);
            Nodes = new NodeRegistry();
            Windows.Attach(window, isMain: true);
            _currentComponent = currentComponent;

            var available = components ?? new[] { currentComponent };
            DevtoolsTools.RegisterCore(Server, new DevtoolsTools.ToolHostContext
            {
                GetComponents = () => available,
                GetCurrentComponent = () => _currentComponent,
                SwitchComponent = _ => false, // fixtures don't exercise component switching
                RequestReload = () => { /* reload is Appium-only; no-op here */ },
                Windows = Windows,
            });
            DevtoolsUiaTools.RegisterUiaTools(Server, Nodes, Windows);
            DevtoolsStateTool.Register(Server, rootComponent);
            DevtoolsFireTool.Register(Server, rootComponent);

            Server.Start();
            _client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{Server.Port}/") };
        }

        public async Task<JsonElement> CallAsync(string method, object? args = null)
        {
            var envelope = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "tools/call",
                @params = new { name = method, arguments = args },
            };
            var body = JsonSerializer.Serialize(envelope, DevtoolsMcpServer.JsonOpts);
            var req = new HttpRequestMessage(HttpMethod.Post, "mcp")
            {
                Content = new StringContent(body, Encoding.UTF8),
            };
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var resp = await _client.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);
            // Clone so the element survives disposing the document.
            return doc.RootElement.Clone();
        }

        public void Dispose()
        {
            try { _client.Dispose(); } catch { }
            try { Server.Dispose(); } catch { }
        }
    }

    // Small helper — returns the "result" element if present, or null when the
    // response is an error envelope. Fixtures that expect errors read "error".
    private static JsonElement? Result(JsonElement response) =>
        response.TryGetProperty("result", out var r) ? r : null;

    private static JsonElement? Error(JsonElement response) =>
        response.TryGetProperty("error", out var e) ? e : null;

    // -- Test component ----------------------------------------------------------

    /// <summary>
    /// A component that exposes the surfaces the tool tests poke at: a button
    /// with a hooked counter, a textbox, a checkbox, and a handler that mutates
    /// state on a timer (for waitFor). AutomationIds let selector tests pin
    /// specific elements without tree-walking.
    /// </summary>
    private sealed class DevtoolsFixtureRoot : Component
    {
        public override Element Render()
        {
            var (count, setCount) = UseState(0);
            var (text, setText) = UseState(string.Empty);
            var (toggled, setToggled) = UseState(false);

            return VStack(
                Factories.Text($"count:{count}").AutomationId("count-label"),
                Button("Increment", () => setCount(count + 1)).AutomationId("btn-increment"),
                TextField(text, setText).AutomationId("txt-input"),
                CheckBox(toggled, setToggled, label: "Accept").AutomationId("chk-accept"),
                // Delayed-update button: used by waitFor.
                Button("DelayedBump", async () =>
                {
                    await Task.Delay(120);
                    setCount(count + 10);
                }).AutomationId("btn-delayed")
            );
        }
    }

    private static DevtoolsFixtureRoot MountRoot(Harness h)
    {
        var host = h.CreateHost();
        var root = new DevtoolsFixtureRoot();
        host.Mount(root);
        return root;
    }

    // -- Fixtures ----------------------------------------------------------------

    internal sealed class VersionTool(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("version");
            var result = Result(resp) ?? throw new Exception("missing result");

            H.Check("Devtools_Version_HasBuild",
                result.TryGetProperty("build", out var b) && b.ValueKind == JsonValueKind.String && b.GetString()!.Length > 0);
            H.Check("Devtools_Version_HasPid",
                result.TryGetProperty("pid", out var pid) && pid.GetInt32() > 0);
            H.Check("Devtools_Version_HasMcpPort",
                result.TryGetProperty("mcpPort", out var port) && port.GetInt32() > 0);
        }
    }

    internal sealed class ComponentsTool(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot),
                components: new[] { "Alpha", "Beta", nameof(DevtoolsFixtureRoot) });
            var resp = await mcp.CallAsync("components");
            var result = Result(resp) ?? throw new Exception("missing result");

            var names = result.GetProperty("components").EnumerateArray().Select(e => e.GetString()).ToArray();
            H.Check("Devtools_Components_ListsAllNames",
                names.Contains("Alpha") && names.Contains("Beta") && names.Contains(nameof(DevtoolsFixtureRoot)));
            H.Check("Devtools_Components_CurrentMatches",
                result.GetProperty("current").GetString() == nameof(DevtoolsFixtureRoot));
        }
    }

    internal sealed class WindowsTool(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("windows");
            var result = Result(resp) ?? throw new Exception("missing result");

            var entries = result.GetProperty("windows").EnumerateArray().ToArray();
            H.Check("Devtools_Windows_HasEntry", entries.Length >= 1);
            var first = entries[0];
            H.Check("Devtools_Windows_EntryShape",
                first.TryGetProperty("id", out _) &&
                first.TryGetProperty("title", out _) &&
                first.TryGetProperty("bounds", out _) &&
                first.TryGetProperty("isMain", out var ismain) && ismain.GetBoolean());
        }
    }

    internal sealed class TreeSummary(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("tree", new { });
            var result = Result(resp) ?? throw new Exception("missing result");

            H.Check("Devtools_Tree_SchemaPinned",
                result.GetProperty("$schema").GetString() == "reactor-tree/1");

            var nodes = result.GetProperty("nodes").EnumerateArray().ToArray();
            H.Check("Devtools_Tree_HasNodes", nodes.Length > 0);

            bool sawButton = nodes.Any(n =>
                n.TryGetProperty("automationId", out var aid) &&
                aid.ValueKind == JsonValueKind.String &&
                aid.GetString() == "btn-increment");
            H.Check("Devtools_Tree_FindsAutomationId", sawButton);

            bool allIdsScoped = nodes.All(n =>
                n.GetProperty("id").GetString()!.StartsWith("r:", StringComparison.Ordinal));
            H.Check("Devtools_Tree_IdsPrefixed", allIdsScoped);
        }
    }

    internal sealed class TreeFullView(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("tree", new { view = "full" });
            var result = Result(resp) ?? throw new Exception("missing result");

            var nodes = result.GetProperty("nodes").EnumerateArray().ToArray();
            // At least one node should carry full-view fields (layout info or desiredSize).
            bool anyFullField = nodes.Any(n =>
                n.TryGetProperty("layout", out var l) && l.ValueKind == JsonValueKind.Object);
            H.Check("Devtools_TreeFull_HasLayoutBlock", anyFullField);

            bool anyDesiredSize = nodes.Any(n =>
                n.TryGetProperty("desiredSize", out var d) && d.ValueKind == JsonValueKind.Object);
            H.Check("Devtools_TreeFull_HasDesiredSize", anyDesiredSize);
        }
    }

    internal sealed class TreeSelectorScope(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("tree", new { selector = "#btn-increment" });
            var result = Result(resp) ?? throw new Exception("missing result");

            var nodes = result.GetProperty("nodes").EnumerateArray().ToArray();
            // Rooting at the button should produce just the button (+ optional visual children).
            H.Check("Devtools_TreeScope_NodeCountBounded", nodes.Length >= 1 && nodes.Length < 15);
            H.Check("Devtools_TreeScope_RootIsButton",
                nodes[0].GetProperty("type").GetString() == "Button");
        }
    }

    internal sealed class ClickInvokesButton(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            H.Check("Devtools_Click_InitialCount", H.FindText("count:0") is not null);

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("click", new { selector = "#btn-increment" });
            var result = Result(resp) ?? throw new Exception("missing result");

            H.Check("Devtools_Click_ViaInvoke",
                result.GetProperty("via").GetString() == "invoke");

            await Harness.Render();
            H.Check("Devtools_Click_CountIncremented", H.FindText("count:1") is not null);
        }
    }

    internal sealed class TypeSetsTextBox(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("type", new { selector = "#txt-input", text = "hello", clear = true });
            var result = Result(resp) ?? throw new Exception("missing result");

            H.Check("Devtools_Type_Ok", result.GetProperty("ok").GetBoolean());

            await Harness.Render();
            var tb = H.FindControl<TextBox>(x => AutomationProperties.GetAutomationId(x) == "txt-input");
            H.Check("Devtools_Type_TextApplied", tb is not null && tb.Text == "hello");

            // Append (clear false) should concatenate.
            await mcp.CallAsync("type", new { selector = "#txt-input", text = "-world" });
            await Harness.Render();
            tb = H.FindControl<TextBox>(x => AutomationProperties.GetAutomationId(x) == "txt-input");
            H.Check("Devtools_Type_Appends", tb is not null && tb.Text == "hello-world");
        }
    }

    internal sealed class FocusElement(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("focus", new { selector = "#btn-increment" });
            var result = Result(resp) ?? throw new Exception("missing result");

            // WinUI may decline focus if the control isn't yet visible to the
            // compositor — assert either an "ok: true" or a structured false.
            // The tool returns `{ ok: bool }`, never an error.
            H.Check("Devtools_Focus_ResponseShape",
                result.TryGetProperty("ok", out var ok) && ok.ValueKind is JsonValueKind.True or JsonValueKind.False);
        }
    }

    internal sealed class WaitForTextChange(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));

            // Click the delayed button; it bumps count by 10 after ~120ms.
            _ = await mcp.CallAsync("click", new { selector = "#btn-delayed" });

            var resp = await mcp.CallAsync("waitFor", new
            {
                predicate = new
                {
                    selector = "#count-label",
                    textEquals = "count:10",
                },
                timeoutMs = 2000,
            });
            var result = Result(resp) ?? throw new Exception("missing result");

            H.Check("Devtools_WaitFor_Succeeded",
                result.TryGetProperty("ok", out var ok) && ok.GetBoolean());
            H.Check("Devtools_WaitFor_ReportedElapsed",
                result.TryGetProperty("elapsedMs", out var e) && e.ValueKind == JsonValueKind.Number);
        }
    }

    internal sealed class WaitForTimeout(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            // Predicate can never become true (count starts at 0, no mutation scheduled).
            var resp = await mcp.CallAsync("waitFor", new
            {
                predicate = new
                {
                    selector = "#count-label",
                    textEquals = "count:999",
                },
                timeoutMs = 150,
            });
            var result = Result(resp) ?? throw new Exception("missing result");

            H.Check("Devtools_WaitFor_Timeout_NotOk",
                result.TryGetProperty("ok", out var ok) && !ok.GetBoolean());
            H.Check("Devtools_WaitFor_Timeout_Reason",
                result.TryGetProperty("reason", out var r) && r.GetString() == "timeout");
        }
    }

    internal sealed class ToggleFlipsCheckBox(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("toggle", new { selector = "#chk-accept" });
            var result = Result(resp) ?? throw new Exception("missing result");

            H.Check("Devtools_Toggle_Ok", result.GetProperty("ok").GetBoolean());
            H.Check("Devtools_Toggle_StateOn",
                result.GetProperty("state").GetString() == "on");

            // Second toggle flips back off.
            resp = await mcp.CallAsync("toggle", new { selector = "#chk-accept" });
            result = Result(resp) ?? throw new Exception("missing result");
            H.Check("Devtools_Toggle_StateOff",
                result.GetProperty("state").GetString() == "off");
        }
    }

    internal sealed class InvokeDirectPattern(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("invoke", new { selector = "#btn-increment" });
            var result = Result(resp) ?? throw new Exception("missing result");

            H.Check("Devtools_Invoke_Ok", result.GetProperty("ok").GetBoolean());
            await Harness.Render();
            H.Check("Devtools_Invoke_HandlerFired", H.FindText("count:1") is not null);

            // Calling invoke on a non-invokable element (the textbox) returns a structured error.
            resp = await mcp.CallAsync("invoke", new { selector = "#txt-input" });
            var err = Error(resp) ?? throw new Exception("expected error envelope");
            H.Check("Devtools_Invoke_NoPatternError",
                err.TryGetProperty("data", out var data) &&
                data.TryGetProperty("code", out var code) &&
                code.GetString() == "no-pattern");
        }
    }

    internal sealed class StateReadsHooks(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));

            // Initial read — count is 0.
            var resp = await mcp.CallAsync("state");
            var result = Result(resp) ?? throw new Exception("missing result");
            var hooks = result.GetProperty("hooks").EnumerateArray().ToArray();
            H.Check("Devtools_State_HasHooks", hooks.Length >= 3);

            // First useState is the count. Value is the raw primitive per §12.
            var firstHook = hooks[0];
            H.Check("Devtools_State_ComponentName",
                firstHook.GetProperty("component").GetString() == nameof(DevtoolsFixtureRoot));
            H.Check("Devtools_State_InitialCountZero",
                firstHook.GetProperty("value").GetInt32() == 0);

            // Mutate via click, re-read, observe new value.
            _ = await mcp.CallAsync("click", new { selector = "#btn-increment" });
            await Harness.Render();
            resp = await mcp.CallAsync("state");
            result = Result(resp) ?? throw new Exception("missing result");
            hooks = result.GetProperty("hooks").EnumerateArray().ToArray();
            H.Check("Devtools_State_CountReflectsClick",
                hooks[0].GetProperty("value").GetInt32() == 1);
        }
    }

    internal sealed class ScreenshotReturnsPng(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("screenshot", new { });
            var result = Result(resp);
            if (result is null)
            {
                // Some CI hosts (headless or off-screen) may refuse PrintWindow —
                // record the condition instead of flapping the test. The response
                // shape is still the expected JSON-RPC error envelope.
                var err = Error(resp) ?? throw new Exception("no result and no error");
                H.Check("Devtools_Screenshot_ErrorHasCode",
                    err.TryGetProperty("code", out _));
                return;
            }

            H.Check("Devtools_Screenshot_BoundsReported",
                result.Value.TryGetProperty("bounds", out var b) && b.ValueKind == JsonValueKind.Object);

            var png = result.Value.GetProperty("png").GetString()!;
            H.Check("Devtools_Screenshot_PngNonEmpty", png.Length > 0);

            // Validate it parses as base64 and decodes to something PNG-ish (starts with 0x89 'PNG').
            var bytes = Convert.FromBase64String(png);
            H.Check("Devtools_Screenshot_PngMagic",
                bytes.Length > 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47);
        }
    }

    internal sealed class UnknownSelectorStructuredError(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var root = MountRoot(H);
            await Harness.Render();

            using var mcp = new McpHarness(H.Window, () => root, nameof(DevtoolsFixtureRoot));
            var resp = await mcp.CallAsync("click", new { selector = "#does-not-exist" });
            var err = Error(resp) ?? throw new Exception("expected error envelope");

            H.Check("Devtools_Error_HasCode",
                err.TryGetProperty("code", out _));
            H.Check("Devtools_Error_StructuredData",
                err.TryGetProperty("data", out var data) &&
                data.TryGetProperty("code", out var ec) &&
                ec.GetString() == "unknown-selector");
        }
    }
}
