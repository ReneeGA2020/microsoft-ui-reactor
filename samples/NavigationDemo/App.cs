// Navigation Demo — Demonstrates the full Duct navigation system.
// No XAML. Single-file WinUI 3 app using Duct functional projection.

using Duct;
using Duct.Core;
using Duct.Core.Navigation;
using Microsoft.UI.Xaml;
using static Duct.UI;

// ─── Deep link map (configured once, immutable) ───────────────────────────────

var deepLinks = new DeepLinkMap<AppRoute>()
    .Map("/", _ => new Home())
    .Map("/detail/{id:int}", args => new Detail(args.Get<int>("id")),
        () => [new Home()])
    .Map("/settings", _ => new Settings())
    .Map("/profile/{name}", args => new Profile(args.GetString("name") ?? "Guest"),
        () => [new Home()]);

// Parse --deep-link /path from command line
var dlIdx = Array.IndexOf(args, "--deep-link");
if (dlIdx >= 0 && dlIdx + 1 < args.Length)
{
    var result = deepLinks.Resolve(args[dlIdx + 1]);
    if (result.Matched && result.Routes.Length > 0)
    {
        DeepLinkState.Route = result.Routes[^1];
        if (result.Routes.Length > 1)
            DeepLinkState.BackStack = result.Routes[..^1];
    }
}

DuctApp.Run<AppShell>("Navigation Demo", width: 1200, height: 800);

// ─── Deep link state (static, consumed once by AppShell) ──────────────────────

static class DeepLinkState
{
    public static AppRoute? Route;
    public static AppRoute[]? BackStack;
}

// ─── Route types ──────────────────────────────────────────────────────────────

abstract record AppRoute;
sealed record Home : AppRoute;
sealed record Detail(int Id) : AppRoute;
sealed record Settings : AppRoute;
sealed record Profile(string Name) : AppRoute;

// Settings sub-routes for nested navigation
abstract record SettingsRoute;
sealed record GeneralSettings : SettingsRoute;
sealed record DisplaySettings : SettingsRoute;
sealed record AboutSettings : SettingsRoute;

// ─── Sample data ──────────────────────────────────────────────────────────────

static class SampleData
{
    public static readonly (int Id, string Title, string Description)[] Items =
    [
        (1, "Getting Started", "Learn the basics of the navigation system"),
        (2, "Route Parameters", "Pass typed parameters through route records"),
        (3, "Navigation Guards", "Prevent navigation with unsaved changes"),
        (4, "Page Transitions", "GPU-accelerated slide, fade, and drill-in"),
        (5, "Page Caching", "Preserve scroll position and state across navigations"),
        (6, "Deep Linking", "Map URIs to routes with typed parameters"),
        (7, "State Serialization", "Save and restore the full navigation stack"),
        (8, "Nested Navigation", "Independent navigation stacks within a page"),
    ];
}

// ─── App shell ────────────────────────────────────────────────────────────────

class AppShell : Component
{
    static string? RouteToTag(AppRoute route) => route switch
    {
        Home => "home",
        Settings => "settings",
        Profile => "profile",
        _ => null,  // Detail routes don't map to nav items
    };

    static AppRoute TagToRoute(string tag) => tag switch
    {
        "settings" => new Settings(),
        "profile" => new Profile("User"),
        _ => new Home(),
    };

    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>(new Home());

        // Apply deep link on first render
        var deepLinkApplied = UseRef(false);
        UseEffect(() =>
        {
            if (deepLinkApplied.Current) return;
            deepLinkApplied.Current = true;

            if (DeepLinkState.Route is { } route)
            {
                if (DeepLinkState.BackStack is { } backStack)
                {
                    foreach (var r in backStack)
                        nav.Navigate(r);
                }
                nav.Navigate(route);
            }
        });

        // System back button (Alt+Left, GoBack key)
        if (DuctApp.ActiveHost?.Window is { } window)
            UseSystemBackButton(nav, window);

        return VStack(0,
            // TitleBar with back button
            TitleBar("Navigation Demo")
                .WithNavigation(nav)
                .Subtitle(RouteDescription(nav.CurrentRoute)),

            // NavigationView with auto-sync
            (NavigationView(
                [
                    NavItem("Home", icon: "\uE80F", tag: "home"),
                    NavItem("Settings", icon: "\uE713", tag: "settings"),
                    NavItem("Profile", icon: "\uE77B", tag: "profile"),
                ],
                // NavigationHost renders the current route's page
                NavigationHost(nav, RouteToElement) with
                {
                    CacheMode = NavigationCacheMode.Enabled,
                    CacheSize = 5,
                }
            ).WithNavigation(nav, RouteToTag, TagToRoute) with
            {
                IsSettingsVisible = false,
            }).Flex(grow: 1)
        ).Flex(grow: 1);
    }

    static string RouteDescription(AppRoute route) => route switch
    {
        Home => "Browse items",
        Detail d => $"Item #{d.Id}",
        Settings => "Settings",
        Profile p => $"Profile: {p.Name}",
        _ => "",
    };

    static Element RouteToElement(AppRoute route) => route switch
    {
        Home => Component<HomePage>(),
        Detail d => Component<DetailPage, int>(d.Id),
        Settings => Component<SettingsPage>(),
        Profile p => Component<ProfilePage, string>(p.Name),
        _ => Text("Unknown route"),
    };
}

// ─── Home page ────────────────────────────────────────────────────────────────

class HomePage : Component
{
    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>();
        var (loaded, setLoaded) = UseState(false);

        // Lifecycle: load data when navigated to
        UseNavigationLifecycle(
            onNavigatedTo: ctx => setLoaded(true));

        return ScrollView(
            VStack(16,
                Text("Home").FontSize(28).SemiBold(),
                Text("Select an item to navigate, or use the sidebar.").Opacity(0.7),

                When(loaded, () =>
                    VStack(8, SampleData.Items.Select(item =>
                        Button($"{item.Title} (#{item.Id})", () =>
                            nav.Navigate(new Detail(item.Id),
                                new NavigateOptions { Transition = NavigationTransition.DrillIn() }))
                    ).ToArray())
                )
            ).Padding(24)
        );
    }
}

// ─── Detail page ──────────────────────────────────────────────────────────────

class DetailPage : Component<int>
{
    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>();
        var id = Props;

        var item = SampleData.Items.FirstOrDefault(i => i.Id == id);
        var title = item.Title ?? $"Item #{id}";
        var description = item.Description ?? "No description available.";

        // Find next/previous items for related navigation
        var nextId = id < SampleData.Items.Length ? id + 1 : (int?)null;
        var prevId = id > 1 ? id - 1 : (int?)null;

        return ScrollView(
            VStack(16,
                Text(title).FontSize(28).SemiBold(),
                Text(description).FontSize(16).Opacity(0.8),
                Text($"Item ID: {id}").Opacity(0.5),

                // Related navigation — demonstrates drill-in transition
                Text("Related Items").FontSize(18).SemiBold().Margin(0, 16, 0, 0),
                HStack(8,
                    When(prevId.HasValue, () =>
                        Button($"Previous (#{prevId})", () =>
                            nav.Navigate(new Detail(prevId!.Value),
                                new NavigateOptions { Transition = NavigationTransition.DrillIn() }))),
                    When(nextId.HasValue, () =>
                        Button($"Next (#{nextId})", () =>
                            nav.Navigate(new Detail(nextId!.Value),
                                new NavigateOptions { Transition = NavigationTransition.DrillIn() })))
                ),

                // Transition demos
                Text("Transition Demos").FontSize(18).SemiBold().Margin(0, 16, 0, 0),
                HStack(8,
                    Button("Slide to Home", () =>
                        nav.Navigate(new Home(),
                            new NavigateOptions { Transition = NavigationTransition.Slide() })),
                    Button("Fade to Settings", () =>
                        nav.Navigate(new Settings(),
                            new NavigateOptions { Transition = NavigationTransition.Fade() }))
                )
            ).Padding(24)
        );
    }
}

// ─── Settings page (nested navigation) ───────────────────────────────────────

class SettingsPage : Component
{
    public override Element Render()
    {
        var nestedNav = UseNavigation<SettingsRoute>(new GeneralSettings());

        return VStack(0,
            Text("Settings").FontSize(28).SemiBold().Margin(24, 24, 24, 8),

            // Sub-navigation tabs
            HStack(8,
                SettingsTab("General", new GeneralSettings(), nestedNav),
                SettingsTab("Display", new DisplaySettings(), nestedNav),
                SettingsTab("About", new AboutSettings(), nestedNav)
            ).Margin(24, 0, 24, 16),

            // Nested NavigationHost
            NavigationHost(nestedNav, route => route switch
            {
                GeneralSettings => GeneralSettingsContent(),
                DisplaySettings => DisplaySettingsContent(),
                AboutSettings => AboutSettingsContent(),
                _ => Text("Unknown settings page"),
            }) with { Transition = NavigationTransition.Fade() }
        );
    }

    static Element SettingsTab(string label, SettingsRoute route, NavigationHandle<SettingsRoute> nav) =>
        Button(label, () =>
        {
            if (!Equals(route, nav.CurrentRoute))
                nav.Navigate(route, new NavigateOptions { PushToBackStack = false });
        }).Disabled(Equals(route, nav.CurrentRoute));

    static Element GeneralSettingsContent() =>
        VStack(12,
            Text("General Settings").FontSize(18).SemiBold(),
            Text("Application Name: Navigation Demo"),
            Text("Version: 1.0.0"),
            Text("Language: English")
        ).Padding(24);

    static Element DisplaySettingsContent() =>
        VStack(12,
            Text("Display Settings").FontSize(18).SemiBold(),
            Text("Theme: System Default"),
            Text("Font Size: Medium"),
            Text("Compact Mode: Off")
        ).Padding(24);

    static Element AboutSettingsContent() =>
        VStack(12,
            Text("About").FontSize(18).SemiBold(),
            Text("Navigation Demo App"),
            Text("Built with Duct — a functional UI framework for WinUI 3."),
            Text("Demonstrates: routes, guards, lifecycle hooks, transitions, caching, deep linking, and state serialization.")
        ).Padding(24);
}

// ─── Profile page (navigation guard) ─────────────────────────────────────────

class ProfilePage : Component<string>
{
    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>();
        var name = Props;
        var (displayName, setDisplayName) = UseState(name);
        var (bio, setBio) = UseState("");
        var (saved, setSaved) = UseState(true);

        var hasUnsavedChanges = !saved;

        // Navigation guard: warn before navigating away with unsaved changes
        UseNavigationLifecycle(
            onNavigatingFrom: ctx =>
            {
                if (hasUnsavedChanges)
                    ctx.Cancel(); // Block navigation — in a real app, show a dialog first
            },
            onNavigatedTo: ctx =>
            {
                // Reset form when navigated to
                setDisplayName(name);
                setBio("");
                setSaved(true);
            });

        return ScrollView(
            VStack(16,
                Text($"Profile: {name}").FontSize(28).SemiBold(),

                // Edit form
                VStack(8,
                    Text("Display Name"),
                    TextField(displayName, v => { setDisplayName(v); setSaved(false); }).Width(300),
                    Text("Bio"),
                    TextField(bio, v => { setBio(v); setSaved(false); }).Width(300),
                    When(hasUnsavedChanges, () =>
                        Text("You have unsaved changes. Navigation is blocked until you save or discard.")
                            .Foreground("#d13438")),
                    HStack(8,
                        Button("Save", () => setSaved(true)),
                        Button("Discard Changes", () =>
                        {
                            setDisplayName(name);
                            setBio("");
                            setSaved(true);
                        }).Disabled(saved)
                    )
                ),

                // Serialization demo
                Text("State Serialization").FontSize(18).SemiBold().Margin(0, 24, 0, 0),
                Text("Save the entire navigation stack to JSON, then restore it."),
                HStack(8,
                    Button("Save State", () =>
                    {
                        var json = nav.GetState();
                        System.Diagnostics.Debug.WriteLine($"Navigation state: {json}");
                    }),
                    Button("Log Stack Info", () =>
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"Depth: {nav.Depth}, CanGoBack: {nav.CanGoBack}, " +
                            $"BackStack: [{string.Join(", ", nav.BackStack)}], " +
                            $"Current: {nav.CurrentRoute}");
                    })
                )
            ).Padding(24)
        );
    }
}
