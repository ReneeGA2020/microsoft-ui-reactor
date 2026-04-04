using Duct;
using Duct.Core;
using Duct.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class NavigationFixtures
{
    internal class TabSwitching(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (tab, setTab) = ctx.UseState("Home");

                return VStack(
                    HStack(
                        Button("Home", () => setTab("Home"))
                            .Disabled(tab == "Home"),
                        Button("Settings", () => setTab("Settings"))
                            .Disabled(tab == "Settings"),
                        Button("About", () => setTab("About"))
                            .Disabled(tab == "About")
                    ),
                    Border(
                        tab switch
                        {
                            "Home" => VStack(Text("Welcome Home"), Text("This is the home page.")),
                            "Settings" => VStack(Text("Settings Page"), Text("Configure your preferences.")),
                            "About" => VStack(Text("About Page"), Text("Version 1.0")),
                            _ => Empty()
                        }
                    ).Padding(16)
                );
            });

            await Harness.Render();

            H.Check("Navigation_TabSwitching_DefaultTabIsHome",
                H.FindText("Welcome Home") is not null);

            H.Check("Navigation_TabSwitching_HomeButtonDisabled", () =>
            {
                var btn = H.FindButton("Home");
                return btn is not null && !btn.IsEnabled;
            });

            H.ClickButton("Settings");
            await Harness.Render();

            H.Check("Navigation_TabSwitching_SettingsContentShown",
                H.FindText("Settings Page") is not null);

            H.Check("Navigation_TabSwitching_HomeContentGone",
                H.FindText("Welcome Home") is null);

            H.ClickButton("About");
            await Harness.Render();

            H.Check("Navigation_TabSwitching_AboutContentShown",
                H.FindText("Version 1.0") is not null);

            H.ClickButton("Home");
            await Harness.Render();

            H.Check("Navigation_TabSwitching_BackToHome",
                H.FindText("Welcome Home") is not null);
        }
    }
}
