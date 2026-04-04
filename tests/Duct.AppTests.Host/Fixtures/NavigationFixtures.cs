using Duct;
using Duct.Core;
using static Duct.UI;

namespace Duct.AppTests.Host.Fixtures;

internal static class NavigationFixtures
{
    // Interactive: tab switching with buttons
    internal class TabSwitchingComponent : Component
    {
        public override Element Render()
        {
            var (tab, setTab) = UseState("Home");

            return VStack(
                HStack(
                    Button("Home", () => setTab("Home"))
                        .Disabled(tab == "Home").AutomationId("HomeTab"),
                    Button("Settings", () => setTab("Settings"))
                        .Disabled(tab == "Settings").AutomationId("SettingsTab"),
                    Button("About", () => setTab("About"))
                        .Disabled(tab == "About").AutomationId("AboutTab")
                ),
                Border(
                    tab switch
                    {
                        "Home" => VStack(
                            Text("Welcome Home").AutomationId("HomeTitle"),
                            Text("This is the home page.").AutomationId("HomeContent")),
                        "Settings" => VStack(
                            Text("Settings Page").AutomationId("SettingsTitle"),
                            Text("Configure your preferences.").AutomationId("SettingsContent")),
                        "About" => VStack(
                            Text("About Page").AutomationId("AboutTitle"),
                            Text("Version 1.0").AutomationId("AboutVersion")),
                        _ => Empty()
                    }
                ).Padding(16).AutomationId("TabContent")
            );
        }
    }

    internal static Element TabSwitching(RenderContext ctx) =>
        Component<TabSwitchingComponent>();
}
