using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

internal static class MonacoFixtures
{
    internal class EditorMounts(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
                VStack(
                    TextBlock("Monaco Editor Test"),
                    MonacoEditor(
                        text: "function hello() {\n  return 42;\n}",
                        language: "javascript",
                        theme: "vs-dark"
                    ).Width(700).Height(400)
                )
            );

            // Monaco needs extra time for WebView2 initialization
            await Harness.Render(2000);

            H.Check("MonacoEditor_Mounts_TitleVisible",
                H.FindText("Monaco Editor Test") is not null);

            // Verify a WebView2 control was created (Monaco uses WebView2)
            var webView = H.FindControl<WebView2>(_ => true);
            H.Check("MonacoEditor_Mounts_WebView2Created",
                webView is not null);

            // Verify the Monaco editor control wrapper exists
            var monacoControl = H.FindControl<Microsoft.UI.Reactor.Monaco.MonacoEditor>(_ => true);
            H.Check("MonacoEditor_Mounts_EditorControlExists",
                monacoControl is not null);
        }
    }
}
