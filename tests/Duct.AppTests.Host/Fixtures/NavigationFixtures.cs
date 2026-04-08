using Duct;
using Duct.Core;
using Duct.Core.Navigation;
using static Duct.UI;

namespace Duct.AppTests.Host.Fixtures;

internal static class NavigationFixtures
{
    // ── Existing: tab switching with buttons (UseState) ──────────────────

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

    // ── Multi-level navigation (UseNavigation + NavigationHost) ──────────

    abstract record NavRoute;
    sealed record NavHome : NavRoute;
    sealed record NavList : NavRoute;
    sealed record NavDetail(int Id) : NavRoute;
    sealed record NavRelated(int SourceId) : NavRoute;

    internal class MultiLevelNavComponent : Component
    {
        public override Element Render()
        {
            var nav = UseNavigation<NavRoute>(new NavHome());

            return VStack(
                HStack(8,
                    Button("Back", () => nav.GoBack())
                        .Disabled(!nav.CanGoBack)
                        .AutomationId("BackBtn"),
                    Text($"Depth: {nav.Depth}").AutomationId("NavDepth")
                ),
                NavigationHost(nav, route => route switch
                {
                    NavHome => VStack(
                        Text("Home").AutomationId("PageTitle"),
                        Button("Go to List", () => nav.Navigate(new NavList()))
                            .AutomationId("GoListBtn")),
                    NavList => VStack(
                        Text("Item List").AutomationId("PageTitle"),
                        Button("View Detail #1", () => nav.Navigate(new NavDetail(1)))
                            .AutomationId("GoDetail1Btn"),
                        Button("View Detail #2", () => nav.Navigate(new NavDetail(2)))
                            .AutomationId("GoDetail2Btn")),
                    NavDetail d => VStack(
                        Text($"Detail #{d.Id}").AutomationId("PageTitle"),
                        Button("Related Items", () => nav.Navigate(new NavRelated(d.Id)))
                            .AutomationId("GoRelatedBtn")),
                    NavRelated r => VStack(
                        Text($"Related to #{r.SourceId}").AutomationId("PageTitle"),
                        Button("Back to Home", () => nav.Reset(new NavHome()))
                            .AutomationId("ResetBtn")),
                    _ => Text("Unknown")
                }) with { Transition = NavigationTransition.None }
            );
        }
    }

    internal static Element MultiLevelNav(RenderContext ctx) =>
        Component<MultiLevelNavComponent>();

    // ── Navigation with guard ───────────────────────────────────────────

    internal class NavGuardComponent : Component
    {
        public override Element Render()
        {
            var nav = UseNavigation<string>("page-a");

            return VStack(
                HStack(8,
                    Button("Go to A", () => nav.Navigate("page-a"))
                        .AutomationId("GoABtn"),
                    Button("Go to B", () => nav.Navigate("page-b"))
                        .AutomationId("GoBBtn"),
                    Text($"Current: {nav.CurrentRoute}").AutomationId("CurrentRoute")
                ),
                NavigationHost(nav, route => route switch
                {
                    "page-a" => Component<PageA>(),
                    "page-b" => Component<PageB>(),
                    _ => Text("Unknown")
                }) with { Transition = NavigationTransition.None }
            );
        }
    }

    class PageA : Component
    {
        public override Element Render()
        {
            return VStack(
                Text("Page A").AutomationId("PageTitle"),
                Text("No guard on this page.").AutomationId("GuardStatus")
            );
        }
    }

    class PageB : Component
    {
        public override Element Render()
        {
            var (dirty, setDirty) = UseState(false);

            UseNavigationLifecycle(
                onNavigatingFrom: ctx =>
                {
                    if (dirty) ctx.Cancel();
                });

            return VStack(
                Text("Page B").AutomationId("PageTitle"),
                Text(dirty ? "Guard: ACTIVE" : "Guard: inactive").AutomationId("GuardStatus"),
                Button("Toggle Guard", () => setDirty(!dirty)).AutomationId("ToggleGuardBtn")
            );
        }
    }

    internal static Element NavGuard(RenderContext ctx) =>
        Component<NavGuardComponent>();

    // ── NavigationView + NavigationHost integration ─────────────────────

    abstract record ViewRoute;
    sealed record ViewHome : ViewRoute;
    sealed record ViewSettings : ViewRoute;
    sealed record ViewAbout : ViewRoute;

    internal class NavViewIntegrationComponent : Component
    {
        public override Element Render()
        {
            var nav = UseNavigation<ViewRoute>(new ViewHome());

            return (NavigationView(
                [
                    NavItem("Home", tag: "home"),
                    NavItem("Settings", tag: "settings"),
                    NavItem("About", tag: "about"),
                ],
                NavigationHost(nav, route => route switch
                {
                    ViewHome => Text("Home Content").AutomationId("PageContent"),
                    ViewSettings => Text("Settings Content").AutomationId("PageContent"),
                    ViewAbout => Text("About Content").AutomationId("PageContent"),
                    _ => Text("Unknown")
                }) with { Transition = NavigationTransition.None }
            ).WithNavigation(nav,
                route => route switch
                {
                    ViewHome => "home",
                    ViewSettings => "settings",
                    ViewAbout => "about",
                    _ => null,
                },
                tag => tag switch
                {
                    "settings" => new ViewSettings(),
                    "about" => new ViewAbout(),
                    _ => new ViewHome(),
                }) with
            {
                IsSettingsVisible = false,
            }).AutomationId("NavViewHost");
        }
    }

    internal static Element NavViewIntegration(RenderContext ctx) =>
        Component<NavViewIntegrationComponent>();
}
