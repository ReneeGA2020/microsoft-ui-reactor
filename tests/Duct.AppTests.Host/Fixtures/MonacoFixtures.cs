using Duct;
using Duct.Core;
using static Duct.UI;

namespace Duct.AppTests.Host.Fixtures;

internal static class MonacoFixtures
{
    internal static Element EditorMounts(RenderContext ctx) =>
        VStack(
            Text("Monaco Editor Test").AutomationId("MonacoTitle"),
            MonacoEditor(
                text: "function hello() {\n  return 42;\n}",
                language: "javascript",
                theme: "vs-dark"
            ).Width(700).Height(400).AutomationId("MonacoEditor")
        );
}
