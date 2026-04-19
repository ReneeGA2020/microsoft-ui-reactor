using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor.ControlPages.Navigation;

class BreadcrumbBarPage : Component
{
    public override Element Render()
    {
        var (path, setPath) = UseState(new[] { "Home", "Documents", "Reports" });
        var (clicked, setClicked) = UseState("(none)");

        return ScrollView(
            VStack(16,
                PageHeader("BreadcrumbBar",
                    "A trail of links showing the user's navigation path."),

                SampleCard("Basic BreadcrumbBar",
                    VStack(8,
                        BreadcrumbBar(
                            path.Select(p => Breadcrumb(p)).ToArray(),
                            item => setClicked(item.Label)),
                        TextBlock($"Last clicked: {clicked}").Foreground(Theme.SecondaryText)
                    ),
                    @"BreadcrumbBar(
    new[] { Breadcrumb(""Home""), Breadcrumb(""Docs""), Breadcrumb(""Reports"") },
    item => setClicked(item.Label))"),

                SampleCard("Dynamic Breadcrumb",
                    VStack(8,
                        BreadcrumbBar(
                            path.Select(p => Breadcrumb(p)).ToArray(),
                            item =>
                            {
                                var idx = Array.IndexOf(path, item.Label);
                                if (idx >= 0)
                                    setPath(path.Take(idx + 1).ToArray());
                            }),
                        HStack(8,
                            Button("Add Level", () =>
                                setPath(path.Append($"Level {path.Length}").ToArray())),
                            Button("Reset", () =>
                                setPath(new[] { "Home", "Documents", "Reports" }))
                        )
                    ),
                    @"BreadcrumbBar(items, item => {
    var idx = Array.IndexOf(path, item.Label);
    if (idx >= 0) setPath(path.Take(idx + 1).ToArray());
})")
            ).Margin(36, 24, 36, 36)
        );
    }
}
