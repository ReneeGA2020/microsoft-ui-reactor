// Navigation Demo — Demonstrates the full Reactor navigation system.
// No XAML. Single-file WinUI 3 app using Reactor functional projection.

using System.Diagnostics;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Navigation;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;
using static Microsoft.UI.Reactor.Core.Theme;

// ─── Deep link map (configured once, immutable) ───────────────────────────────
// Demonstrates: required params, optional params, query strings, wildcards, back stacks

var deepLinks = new DeepLinkMap<AppRoute>()
    .Map("/", _ => new Home())
    .Map("/detail/{id:int}", args =>
        new Detail(args.Get<int>("id"), args.Query<string>("tab", "overview")),
        () => [new Home()])
    .Map("/settings", _ => new Settings())
    .Map("/profile/{name?}", args =>
        new Profile(args.GetOrDefault<string>("name", "Guest")),
        () => [new Home()])
    .Map("/docs/**", args =>
        new DocsPage(args.GetWildcard() ?? "index"));

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

ReactorApp.Run<AppShell>("Navigation Demo", width: 1200, height: 800);

// ─── Deep link state (static, consumed once by AppShell) ──────────────────────

static class DeepLinkState
{
    public static AppRoute? Route;
    public static AppRoute[]? BackStack;
}

/// <summary>
/// Simulated auth state — toggled from the Home page to demonstrate destination guards.
/// </summary>
static class AuthState
{
    public static bool IsAuthorized;
}

// ─── Route types ──────────────────────────────────────────────────────────────

abstract record AppRoute;
sealed record Home : AppRoute;
sealed record Detail(int Id, string Tab = "overview") : AppRoute;
sealed record Settings : AppRoute;
sealed record Profile(string Name) : AppRoute;
sealed record DocsPage(string Path) : AppRoute;
sealed record AdminPage : AppRoute;

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
        DocsPage => "docs",
        AdminPage => "admin",
        _ => null,
    };

    static AppRoute TagToRoute(string tag) => tag switch
    {
        "settings" => new Settings(),
        "profile" => new Profile("User"),
        "docs" => new DocsPage("index"),
        "admin" => new AdminPage(),
        _ => new Home(),
    };

    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>(new Home());
        var (diagLog, updateDiagLog) = UseReducer(new List<string>());

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

        // Subscribe to navigation diagnostics for the log panel
        UseEffect(() =>
        {
            void onCompleted(NavigationDiagnosticEvent e) =>
                updateDiagLog(log => [.. log.TakeLast(19), $"[{DateTime.Now:HH:mm:ss}] {e.Mode}: {e.From} -> {e.To}"]);
            void onCancelled(NavigationDiagnosticEvent e) =>
                updateDiagLog(log => [.. log.TakeLast(19), $"[{DateTime.Now:HH:mm:ss}] BLOCKED: {e.Mode} {e.From} -> {e.To} ({e.Reason})"]);
            void onCacheHit(CacheDiagnosticEvent e) =>
                updateDiagLog(log => [.. log.TakeLast(19), $"[{DateTime.Now:HH:mm:ss}] Cache HIT: {e.Route}"]);
            void onCacheMiss(CacheDiagnosticEvent e) =>
                updateDiagLog(log => [.. log.TakeLast(19), $"[{DateTime.Now:HH:mm:ss}] Cache MISS: {e.Route}"]);

            NavigationDiagnostics.NavigationCompleted += onCompleted;
            NavigationDiagnostics.NavigationCancelled += onCancelled;
            NavigationDiagnostics.CacheHit += onCacheHit;
            NavigationDiagnostics.CacheMiss += onCacheMiss;
            return () =>
            {
                NavigationDiagnostics.NavigationCompleted -= onCompleted;
                NavigationDiagnostics.NavigationCancelled -= onCancelled;
                NavigationDiagnostics.CacheHit -= onCacheHit;
                NavigationDiagnostics.CacheMiss -= onCacheMiss;
            };
        });

        // SystemBackButton is now default on NavigationHost — no UseSystemBackButton call needed.
        // Alt+Left and VirtualKey.GoBack automatically call nav.GoBack().

        // Grid layout: Row 0 = auto (TitleBar), Row 1 = * (fill).
        // Col 0 = * (NavigationView), Col 1 = auto (diagnostics panel).
        // Grid constrains Row 1 height so ScrollViewer inside pages can scroll.
        return FlexColumn(
            TitleBar("Navigation Demo")
                .WithNavigation(nav)
                .Subtitle(RouteDescription(nav.CurrentRoute))
                .Grid(row: 0, column: 0, columnSpan: 2),

            FlexRow(
                (NavigationView(
                    [
                        NavItem("Home", icon: "\uE80F", tag: "home"),
                        NavItem("Settings", icon: "\uE713", tag: "settings"),
                        NavItem("Profile", icon: "\uE77B", tag: "profile"),
                        NavItem("Docs", icon: "\uE736", tag: "docs"),
                        NavItem("Admin", icon: "\uE7EF", tag: "admin"),
                    ],
                    NavigationHost(nav, RouteToElement) with
                    {
                        CacheMode = NavigationCacheMode.Enabled,
                        CacheSize = 5,
                    }
                ).WithNavigation(nav, RouteToTag, TagToRoute) with
                {
                    IsSettingsVisible = false,
                    IsBackEnabled = false,
                    IsPaneOpen = false,
                    PaneDisplayMode = Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.LeftCompact,
                }).OnMount(fe =>
                {
                    var nv = (Microsoft.UI.Xaml.Controls.NavigationView)fe;
                    nv.IsBackButtonVisible = Microsoft.UI.Xaml.Controls.NavigationViewBackButtonVisible.Collapsed;
                    nv.IsPaneToggleButtonVisible = false;
                }).Flex(grow:1),

                DiagnosticsPanel(diagLog, nav)
            )
        );
    }

    static Element DiagnosticsPanel(List<string> log, NavigationHandle<AppRoute> nav)
    {
        return Border(
            VStack(4,
                TextBlock("Navigation Diagnostics").FontSize(12).SemiBold().Opacity(0.6),
                TextBlock($"Depth: {nav.Depth} | CanGoBack: {nav.CanGoBack} | Route: {nav.CurrentRoute}")
                    .FontSize(11).Opacity(0.5),
                VStack(0, log.Select(line =>
                    TextBlock(line).FontSize(10).Opacity(line.Contains("BLOCKED") ? 1.0 : 0.7)
                        .Foreground(line.Contains("BLOCKED") ? SystemCritical :
                                    line.Contains("HIT") ? SystemSuccess : SecondaryText)
                ).ToArray())
            ).Padding(8)
        ).Width(320).Set(b =>
        {
            b.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                global::Windows.UI.Color.FromArgb(30, 128, 128, 128));
            b.BorderThickness = new Thickness(1, 0, 0, 0);
        });
    }

    static string RouteDescription(AppRoute route) => route switch
    {
        Home => "Browse items",
        Detail d => $"Item #{d.Id} ({d.Tab})",
        Settings => "Settings",
        Profile p => $"Profile: {p.Name}",
        DocsPage d => $"Docs: {d.Path}",
        AdminPage => "Admin (restricted)",
        _ => "",
    };

    static Element RouteToElement(AppRoute route) => route switch
    {
        Home => Component<HomePage>(),
        Detail d => Component<DetailPage, Detail>(d),
        Settings => Component<SettingsPage>(),
        Profile p => Component<ProfilePage, string>(p.Name),
        DocsPage d => Component<DocsPageView, string>(d.Path),
        AdminPage => Component<AdminPageView>(),
        _ => TextBlock("Unknown route"),
    };
}

// ─── Home page ────────────────────────────────────────────────────────────────

class HomePage : Component
{
    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>();
        var (loaded, setLoaded) = UseState(false);
        var (isAuthorized, setIsAuthorized) = UseState(AuthState.IsAuthorized);

        UseNavigationLifecycle(
            onNavigatedTo: ctx => setLoaded(true));

        return ScrollView(
            VStack(16,
                TextBlock("Home").FontSize(28).SemiBold(),
                TextBlock("Select an item, try deep links, or test the destination guard below.").Opacity(0.7),

                When(loaded, () =>
                    VStack(8, SampleData.Items.Select(item =>
                        Button($"{item.Title} (#{item.Id})", () =>
                            nav.Navigate(new Detail(item.Id),
                                new NavigateOptions { Transition = NavigationTransition.DrillIn() }))
                    ).ToArray())
                ),

                // Deep link examples
                TextBlock("Deep Link Examples").FontSize(18).SemiBold().Margin(0, 24, 0, 0),
                TextBlock("These simulate URI-based deep links with query strings, optional params, and wildcards.").Opacity(0.6),
                HStack(8,
                    Button("/detail/3?tab=related", () =>
                    {
                        var result = new DeepLinkMap<AppRoute>()
                            .Map("/detail/{id:int}", a =>
                                new Detail(a.Get<int>("id"), a.Query<string>("tab", "overview")))
                            .Resolve("/detail/3?tab=related");
                        if (result.Matched) nav.Navigate(result.Routes[0]);
                    }),
                    Button("/profile (optional, no name)", () =>
                    {
                        var result = new DeepLinkMap<AppRoute>()
                            .Map("/profile/{name?}", a =>
                                new Profile(a.GetOrDefault<string>("name", "Guest")))
                            .Resolve("/profile");
                        if (result.Matched) nav.Navigate(result.Routes[0]);
                    }),
                    Button("/docs/guide/setup (wildcard)", () =>
                    {
                        var result = new DeepLinkMap<AppRoute>()
                            .Map("/docs/**", a =>
                                new DocsPage(a.GetWildcard() ?? "index"))
                            .Resolve("/docs/guide/setup");
                        if (result.Matched) nav.Navigate(result.Routes[0]);
                    })
                ),

                // Transition distance demo
                TextBlock("Slide Distance").FontSize(18).SemiBold().Margin(0, 24, 0, 0),
                TextBlock("Custom slide distance — compare 100px vs 400px.").Opacity(0.6),
                HStack(8,
                    Button("Slide 100px to Settings", () =>
                        nav.Navigate(new Settings(),
                            new NavigateOptions { Transition = NavigationTransition.Slide(distance: 100f) })),
                    Button("Slide 400px to Settings", () =>
                        nav.Navigate(new Settings(),
                            new NavigateOptions { Transition = NavigationTransition.Slide(distance: 400f) }))
                ),

                // Destination guard demo
                TextBlock("Destination Guard").FontSize(18).SemiBold().Margin(0, 24, 0, 0),
                TextBlock("The Admin page has a destination-side guard (onNavigatingTo). Toggle auth, then try to navigate.").Opacity(0.6),
                HStack(8,
                    ToggleSwitch(isAuthorized, v => { AuthState.IsAuthorized = v; setIsAuthorized(v); },
                        header: $"Authorized: {(isAuthorized ? "Yes" : "No")}"),
                    Button("Go to Admin", () => nav.Navigate(new AdminPage()))
                ),
                When(!isAuthorized, () =>
                    TextBlock("Try clicking 'Go to Admin' — the destination guard will block. Toggle the switch first to allow it.")
                        .Opacity(0.6).FontSize(12))
            ).Padding(24)
        );
    }
}

// ─── Detail page ──────────────────────────────────────────────────────────────
// Now receives a Detail record with Id and Tab (from query string deep links)

class DetailPage : Component<Detail>
{
    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>();
        var detail = Props;
        var id = detail.Id;
        var tab = detail.Tab;

        var item = SampleData.Items.FirstOrDefault(i => i.Id == id);
        var title = item.Title ?? $"Item #{id}";
        var description = item.Description ?? "No description available.";

        var nextId = id < SampleData.Items.Length ? id + 1 : (int?)null;
        var prevId = id > 1 ? id - 1 : (int?)null;

        return ScrollView(
            VStack(16,
                TextBlock(title).FontSize(28).SemiBold(),
                TextBlock(description).FontSize(16).Opacity(0.8),
                TextBlock($"Item ID: {id}  |  Tab: {tab}").Opacity(0.5),

                // Tab selection — demonstrates query string deep link result
                TextBlock("Tabs (from query string)").FontSize(18).SemiBold().Margin(0, 16, 0, 0),
                HStack(8,
                    TabButton("Overview", "overview", tab, id, nav),
                    TabButton("Related", "related", tab, id, nav),
                    TabButton("History", "history", tab, id, nav)
                ),

                // Tab content
                Border(
                    tab switch
                    {
                        "related" => VStack(8,
                            TextBlock("Related Items").FontSize(16).SemiBold(),
                            When(prevId.HasValue, () =>
                                Button($"Previous (#{prevId})", () =>
                                    nav.Navigate(new Detail(prevId!.Value),
                                        new NavigateOptions { Transition = NavigationTransition.DrillIn() }))),
                            When(nextId.HasValue, () =>
                                Button($"Next (#{nextId})", () =>
                                    nav.Navigate(new Detail(nextId!.Value),
                                        new NavigateOptions { Transition = NavigationTransition.DrillIn() })))
                        ),
                        "history" => VStack(8,
                            TextBlock("History").FontSize(16).SemiBold(),
                            TextBlock($"Item #{id} was created on day {id} of the project."),
                            TextBlock($"Last modified: {id * 3} days ago.")
                        ),
                        _ => VStack(8,
                            TextBlock("Overview").FontSize(16).SemiBold(),
                            TextBlock(description),
                            TextBlock($"This is item #{id} of {SampleData.Items.Length} total items.")
                        ),
                    }
                ).Padding(16).Margin(0, 8, 0, 0),

                // Transition demos
                TextBlock("Transition Demos").FontSize(18).SemiBold().Margin(0, 16, 0, 0),
                HStack(8,
                    Button("Slide to Home", () =>
                        nav.Navigate(new Home(),
                            new NavigateOptions { Transition = NavigationTransition.Slide() })),
                    Button("Fade to Settings", () =>
                        nav.Navigate(new Settings(),
                            new NavigateOptions { Transition = NavigationTransition.Fade() })),
                    Button("Spring to Home", () =>
                        nav.Navigate(new Home(),
                            new NavigateOptions { Transition = NavigationTransition.Spring() }))
                )
            ).Padding(24)
        );
    }

    static Element TabButton(string label, string tab, string activeTab, int id,
        NavigationHandle<AppRoute> nav) =>
        Button(label, () => nav.Navigate(new Detail(id, tab),
            new NavigateOptions { PushToBackStack = false }))
            .Disabled(tab == activeTab);
}

// ─── Docs page (wildcard route target) ──────────────────────────────────────

class DocsPageView : Component<string>
{
    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>();
        var path = Props;
        var segments = path.Split('/');

        return ScrollView(
            VStack(16,
                TextBlock("Documentation").FontSize(28).SemiBold(),
                TextBlock($"Path: /docs/{path}").FontSize(14).Opacity(0.6),

                // Breadcrumb from wildcard path
                TextBlock("Breadcrumb").FontSize(18).SemiBold().Margin(0, 16, 0, 0),
                HStack(4,
                    Button("docs", () => nav.Navigate(new DocsPage("index"),
                        new NavigateOptions { PushToBackStack = false })),
                    TextBlock("/").Opacity(0.3),
                    HStack(4, segments.Select((seg, i) => (Element)HStack(4,
                        Button(seg, () =>
                        {
                            var subPath = string.Join("/", segments.Take(i + 1));
                            nav.Navigate(new DocsPage(subPath),
                                new NavigateOptions { PushToBackStack = false });
                        }),
                        When(i < segments.Length - 1, () => TextBlock("/").Opacity(0.3))
                    )).ToArray())
                ),

                // Simulated doc content
                TextBlock("Content").FontSize(18).SemiBold().Margin(0, 16, 0, 0),
                TextBlock($"This is the documentation page for '{segments[^1]}'."),
                TextBlock("Wildcard routes (/docs/**) capture the entire remaining path,"),
                TextBlock("enabling documentation-style hierarchical navigation."),

                // Navigate to child paths
                TextBlock("Sub-pages").FontSize(18).SemiBold().Margin(0, 16, 0, 0),
                HStack(8,
                    Button($"{path}/overview", () =>
                        nav.Navigate(new DocsPage($"{path}/overview"))),
                    Button($"{path}/examples", () =>
                        nav.Navigate(new DocsPage($"{path}/examples")))
                )
            ).Padding(24)
        );
    }
}

// ─── Admin page (destination-side guard) ────────────────────────────────────
// Demonstrates onNavigatingTo: the destination page rejects navigation.

class AdminPageView : Component
{
    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>();

        // Destination-side guard: reject navigation if not authorized.
        // Reads from shared AuthState (toggled on the Home page).
        // This fires when the page is mounted — if it cancels, the reconciler
        // reverts to the previous page.
        UseNavigationLifecycle(
            onNavigatingTo: ctx =>
            {
                if (!AuthState.IsAuthorized)
                {
                    Debug.WriteLine("[Admin] Access denied — destination guard cancelled navigation.");
                    ctx.Cancel();
                }
            },
            onNavigatedTo: ctx =>
            {
                Debug.WriteLine("[Admin] Welcome, authorized user.");
            });

        // This content only shows if authorized (guard allows navigation through)
        return ScrollView(
            VStack(16,
                TextBlock("Admin Panel").FontSize(28).SemiBold(),
                TextBlock("You are authorized.").Foreground(SystemSuccess),
                TextBlock("This page uses a destination-side guard (onNavigatingTo)."),
                TextBlock("Navigation here was allowed because AuthState.IsAuthorized was true."),
                TextBlock("Toggle it off on the Home page and try navigating here again to see it blocked."),
                Button("Go Home", () => nav.Navigate(new Home()))
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
            TextBlock("Settings").FontSize(28).SemiBold().Margin(24, 24, 24, 8),

            HStack(8,
                SettingsTab("General", new GeneralSettings(), nestedNav),
                SettingsTab("Display", new DisplaySettings(), nestedNav),
                SettingsTab("About", new AboutSettings(), nestedNav)
            ).Margin(24, 0, 24, 16),

            NavigationHost(nestedNav, route => route switch
            {
                GeneralSettings => GeneralSettingsContent(),
                DisplaySettings => DisplaySettingsContent(),
                AboutSettings => AboutSettingsContent(),
                _ => TextBlock("Unknown settings page"),
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
            TextBlock("General Settings").FontSize(18).SemiBold(),
            TextBlock("Application Name: Navigation Demo"),
            TextBlock("Version: 2.0.0"),
            TextBlock("Language: English")
        ).Padding(24);

    static Element DisplaySettingsContent() =>
        VStack(12,
            TextBlock("Display Settings").FontSize(18).SemiBold(),
            TextBlock("Theme: System Default"),
            TextBlock("Font Size: Medium"),
            TextBlock("Compact Mode: Off")
        ).Padding(24);

    static Element AboutSettingsContent() =>
        VStack(12,
            TextBlock("About").FontSize(18).SemiBold(),
            TextBlock("Navigation Demo App"),
            TextBlock("Built with Reactor — a functional UI framework for WinUI 3."),
            TextBlock("Features: type-safe routes, GPU transitions, lifecycle guards,"),
            TextBlock("destination guards, caching, deep linking (query strings,"),
            TextBlock("optional params, wildcards), diagnostics, and state serialization.")
        ).Padding(24);
}

// ─── Profile page (source-side navigation guard) ─────────────────────────────

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

        // Source-side guard: warn before navigating AWAY with unsaved changes
        // (vs. destination-side guard on AdminPage which blocks navigating TO)
        UseNavigationLifecycle(
            onNavigatingFrom: ctx =>
            {
                if (hasUnsavedChanges)
                    ctx.Cancel();
            },
            onNavigatedTo: ctx =>
            {
                setDisplayName(name);
                setBio("");
                setSaved(true);
            });

        return ScrollView(
            VStack(16,
                TextBlock($"Profile: {name}").FontSize(28).SemiBold(),

                VStack(8,
                    TextBlock("Display Name"),
                    TextField(displayName, v => { setDisplayName(v); setSaved(false); }).Width(300),
                    TextBlock("Bio"),
                    TextField(bio, v => { setBio(v); setSaved(false); }).Width(300),
                    When(hasUnsavedChanges, () =>
                        TextBlock("You have unsaved changes. Navigation is blocked until you save or discard.")
                            .Foreground(SystemCritical)),
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
                TextBlock("State Serialization").FontSize(18).SemiBold().Margin(0, 24, 0, 0),
                TextBlock("Save and restore the entire navigation stack as JSON."),
                HStack(8,
                    Button("Save State", () =>
                    {
                        var json = nav.GetState();
                        Debug.WriteLine($"Navigation state: {json}");
                    }),
                    Button("Log Stack Info", () =>
                    {
                        Debug.WriteLine(
                            $"Depth: {nav.Depth}, CanGoBack: {nav.CanGoBack}, " +
                            $"BackStack: [{string.Join(", ", nav.BackStack)}], " +
                            $"Current: {nav.CurrentRoute}");
                    })
                )
            ).Padding(24)
        );
    }
}
