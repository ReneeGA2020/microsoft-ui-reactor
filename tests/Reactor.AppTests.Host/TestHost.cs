using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host;

/// <summary>
/// Root component for the test host. Renders a fixture navigator on the left
/// and the selected fixture's UI on the right. All elements have AutomationIds
/// for WinAppDriver/Appium access.
/// </summary>
internal class TestHost : Component
{
    public override Element Render()
    {
        var (currentFixture, setFixture) = UseState<string?>(null);

        var fixtureNames = FixtureRegistry.AllFixtures;

        // Navigator: scrollable list of fixture buttons
        var navigator = ScrollView(
            VStack(2,
                fixtureNames.Select(name =>
                    Button(name, () => setFixture(name))
                        .AutomationId($"Nav_{name}")
                        .HAlign(HorizontalAlignment.Stretch)
                ).ToArray()
            ).Padding(4)
        ).Width(280);

        // Content area: either the selected fixture or a placeholder
        Element content;
        if (currentFixture != null)
        {
            var fixtureElement = FixtureRegistry.Build(currentFixture, new RenderContext());
            content = VStack(4,
                HStack(8,
                    TextBlock($"Loaded: {currentFixture}")
                        .AutomationId("FixtureStatus")
                        .SemiBold(),
                    Button("Reset", () => setFixture(null))
                        .AutomationId("ResetFixture")
                ),
                fixtureElement ?? TextBlock("Unknown fixture").AutomationId("FixtureError")
            );
        }
        else
        {
            content = TextBlock("Ready")
                .AutomationId("FixtureStatus")
                .FontSize(16)
                .Padding(20);
        }

        return HStack(0,
            navigator.Background("#f5f5f5"),
            content.Padding(8)
        ).Set(fe =>
        {
            // Expose DPI scale via the root element's Name property for tests to read.
            // Read the real scale from XamlRoot at mount time.
            var scale = fe.XamlRoot?.RasterizationScale ?? 1.0;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(fe, $"DpiScale:{scale:F4}");
        }).AutomationId("TestHostRoot");
    }
}
