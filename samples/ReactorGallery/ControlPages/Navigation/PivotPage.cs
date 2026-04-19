using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor.ControlPages.Navigation;

class PivotPage : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(16,
                PageHeader("Pivot",
                    "A tabbed interface for switching between content sections."),

                SampleCard("Basic Pivot",
                    Pivot(
                        PivotItem("All", TextBlock("All items displayed here.").Padding(12)),
                        PivotItem("Recent", TextBlock("Recent items displayed here.").Padding(12)),
                        PivotItem("Favorites", TextBlock("Favorite items displayed here.").Padding(12))
                    ).Height(200),
                    @"Pivot(
    PivotItem(""All"", TextBlock(""All items"")),
    PivotItem(""Recent"", TextBlock(""Recent items"")),
    PivotItem(""Favorites"", TextBlock(""Favorite items"")))"),

                SampleCard("Pivot with Rich Content",
                    Pivot(
                        PivotItem("Overview",
                            VStack(8,
                                SubHeading("Overview").Foreground(Theme.PrimaryText),
                                TextBlock("Summary of key metrics.").Foreground(Theme.SecondaryText)
                            ).Padding(12)),
                        PivotItem("Details",
                            VStack(8,
                                SubHeading("Details").Foreground(Theme.PrimaryText),
                                TextBlock("Detailed information goes here.").Foreground(Theme.SecondaryText)
                            ).Padding(12))
                    ).Height(200),
                    @"Pivot(
    PivotItem(""Overview"", VStack(8,
        SubHeading(""Overview""),
        TextBlock(""Summary of key metrics.""))),
    PivotItem(""Details"", VStack(8,
        SubHeading(""Details""),
        TextBlock(""Detailed info.""))))")
            ).Margin(36, 24, 36, 36)
        );
    }
}
