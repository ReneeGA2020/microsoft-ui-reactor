using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.Fixtures;

internal static class MonacoFixtures
{
    internal static Element EditorMounts(RenderContext ctx) =>
        VStack(
            TextBlock("Monaco Editor Test").AutomationId("MonacoTitle"),
            MonacoEditor(
                text: "function hello() {\n  return 42;\n}",
                language: "javascript",
                theme: "vs-dark"
            ).Width(700).Height(400).AutomationId("MonacoEditor")
        );
}
