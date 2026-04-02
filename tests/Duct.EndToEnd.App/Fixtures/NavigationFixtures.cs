using Duct;
using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.EndToEnd.App.Fixtures;

internal static class NavigationFixtures
{
    internal class TabSwitching(Harness h) : FixtureBase(h)
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

            // Switch to Settings
            H.ClickButton("Settings");
            await Harness.Render();

            H.Check("Navigation_TabSwitching_SettingsContentShown",
                H.FindText("Settings Page") is not null);

            H.Check("Navigation_TabSwitching_HomeContentGone",
                H.FindText("Welcome Home") is null);

            // Switch to About
            H.ClickButton("About");
            await Harness.Render();

            H.Check("Navigation_TabSwitching_AboutContentShown",
                H.FindText("Version 1.0") is not null);

            // Switch back to Home
            H.ClickButton("Home");
            await Harness.Render();

            H.Check("Navigation_TabSwitching_BackToHome",
                H.FindText("Welcome Home") is not null);
        }
    }
}
