using Duct;
using Duct.Core;
using Duct.Core.Navigation;
using Duct.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

/// <summary>
/// Selfhost fixtures targeting Duct\Core\Navigation coverage gaps:
/// NavigationHandle (GoForward, Replace, Reset, PopTo, GetState/SetState),
/// DeepLinkMap, NavigationCache, NavigationTransition factories.
/// </summary>
internal static class NavigationCoverageFixtures
{
    // ════════════════════════════════════════════════════════════════════════
    //  1. NavigationHandle — GoForward, Replace, Reset, PopTo
    //     Targets: NavigationHandle.cs uncovered methods
    // ════════════════════════════════════════════════════════════════════════

    internal class NavHandleAdvancedOps(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            NavigationHandle<string>? navHandle = null;

            host.Mount(ctx =>
            {
                var nav = ctx.UseNavigation("Home");
                navHandle = nav;
                return VStack(
                    NavigationHost<string>(nav, route => route switch
                    {
                        "Home" => Text("Page:Home"),
                        "Settings" => Text("Page:Settings"),
                        "Profile" => Text("Page:Profile"),
                        "Deep" => Text("Page:Deep"),
                        _ => Text($"Page:{route}"),
                    }),
                    Text($"Route:{nav.CurrentRoute}"),
                    Text($"CanBack:{nav.CanGoBack}"),
                    Text($"CanFwd:{nav.CanGoForward}"),
                    Text($"Depth:{nav.Depth}"),
                    Button("GoSettings", () => nav.Navigate("Settings")),
                    Button("GoProfile", () => nav.Navigate("Profile")),
                    Button("GoDeep", () => nav.Navigate("Deep")),
                    Button("Back", () => nav.GoBack()),
                    Button("Forward", () => nav.GoForward()),
                    Button("Replace", () => nav.Replace("Replaced")),
                    Button("Reset", () => nav.Reset("Home"))
                );
            });

            await Harness.Render();
            H.Check("NavAdv_Initial", H.FindText("Route:Home") is not null);
            H.Check("NavAdv_InitialDepth", H.FindText("Depth:1") is not null);

            // Navigate: Home → Settings → Profile
            H.ClickButton("GoSettings");
            await Harness.Render();
            H.Check("NavAdv_AtSettings", H.FindText("Route:Settings") is not null);

            H.ClickButton("GoProfile");
            await Harness.Render();
            H.Check("NavAdv_AtProfile", H.FindText("Route:Profile") is not null);
            H.Check("NavAdv_Depth3", H.FindText("Depth:3") is not null);

            // GoBack → Settings, creates forward stack
            H.ClickButton("Back");
            await Harness.Render();
            H.Check("NavAdv_BackToSettings", H.FindText("Route:Settings") is not null);
            H.Check("NavAdv_CanForward", H.FindText("CanFwd:True") is not null);

            // GoForward → Profile
            H.ClickButton("Forward");
            await Harness.Render();
            H.Check("NavAdv_ForwardToProfile", H.FindText("Route:Profile") is not null);

            // Replace current with "Replaced"
            H.ClickButton("Replace");
            await Harness.Render();
            H.Check("NavAdv_Replaced", H.FindText("Route:Replaced") is not null);

            // Reset to Home
            H.ClickButton("Reset");
            await Harness.Render();
            H.Check("NavAdv_Reset", H.FindText("Route:Home") is not null);
            H.Check("NavAdv_ResetDepth", H.FindText("Depth:1") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  2. NavigationHandle — PopTo
    //     Targets: NavigationHandle.PopTo, NavigationStack.PopTo
    // ════════════════════════════════════════════════════════════════════════

    internal class NavHandlePopTo(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            NavigationHandle<string>? navHandle = null;

            host.Mount(ctx =>
            {
                var nav = ctx.UseNavigation("A");
                navHandle = nav;
                return VStack(
                    NavigationHost<string>(nav, route => Text($"Pop:{route}")),
                    Text($"Current:{nav.CurrentRoute}"),
                    Text($"Depth:{nav.Depth}"),
                    Button("GoB", () => nav.Navigate("B")),
                    Button("GoC", () => nav.Navigate("C")),
                    Button("GoD", () => nav.Navigate("D")),
                    Button("PopToB", () => nav.PopTo(r => r == "B"))
                );
            });

            await Harness.Render();
            // Build stack: A → B → C → D
            H.ClickButton("GoB");
            await Harness.Render();
            H.ClickButton("GoC");
            await Harness.Render();
            H.ClickButton("GoD");
            await Harness.Render();
            H.Check("NavPopTo_AtD", H.FindText("Current:D") is not null);
            H.Check("NavPopTo_Depth4", H.FindText("Depth:4") is not null);

            // PopTo B — should skip C and D
            H.ClickButton("PopToB");
            await Harness.Render();
            H.Check("NavPopTo_AtB", H.FindText("Current:B") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  3. NavigationHandle — GetState / SetState (serialization)
    //     Targets: NavigationHandle.GetState, SetState, NavigationStack.RestoreState
    // ════════════════════════════════════════════════════════════════════════

    internal class NavHandleSerialization(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            NavigationHandle<string>? navHandle = null;
            string? savedState = null;

            host.Mount(ctx =>
            {
                var nav = ctx.UseNavigation("Home");
                navHandle = nav;
                return VStack(
                    NavigationHost<string>(nav, route => Text($"Ser:{route}")),
                    Text($"Cur:{nav.CurrentRoute}"),
                    Button("GoA", () => nav.Navigate("A")),
                    Button("GoB", () => nav.Navigate("B")),
                    Button("Save", () => savedState = nav.GetState()),
                    Button("Reset", () => nav.Reset("Empty")),
                    Button("Restore", () => { if (savedState is not null) nav.SetState(savedState); })
                );
            });

            await Harness.Render();
            H.ClickButton("GoA");
            await Harness.Render();
            H.ClickButton("GoB");
            await Harness.Render();
            H.Check("NavSer_AtB", H.FindText("Cur:B") is not null);

            // Save state
            H.ClickButton("Save");
            await Harness.Render();
            H.Check("NavSer_StateSaved", savedState is not null);
            H.Check("NavSer_StateHasB", savedState?.Contains("B") == true);

            // Reset to clear everything
            H.ClickButton("Reset");
            await Harness.Render();
            H.Check("NavSer_ResetToEmpty", H.FindText("Cur:Empty") is not null);

            // Restore saved state
            H.ClickButton("Restore");
            await Harness.Render();
            H.Check("NavSer_Restored", H.FindText("Cur:B") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  4. NavigationHandle — Navigate with options (replace mode)
    //     Targets: NavigateOptions, PushToBackStack: false path
    // ════════════════════════════════════════════════════════════════════════

    internal class NavHandleNavigateOptions(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();

            host.Mount(ctx =>
            {
                var nav = ctx.UseNavigation("Start");
                return VStack(
                    NavigationHost<string>(nav, route => Text($"Opt:{route}")),
                    Text($"Cur:{nav.CurrentRoute}"),
                    Text($"Depth:{nav.Depth}"),
                    Button("NavReplace", () => nav.Navigate("Replaced", new NavigateOptions { PushToBackStack = false })),
                    Button("NavPush", () => nav.Navigate("Pushed"))
                );
            });

            await Harness.Render();
            H.Check("NavOpt_Initial", H.FindText("Cur:Start") is not null);

            // Navigate with PushToBackStack = false (replace mode)
            H.ClickButton("NavReplace");
            await Harness.Render();
            H.Check("NavOpt_Replaced", H.FindText("Cur:Replaced") is not null);
            // Depth should still be 1 since it was a replace
            H.Check("NavOpt_DepthStill1", H.FindText("Depth:1") is not null);

            // Navigate with push
            H.ClickButton("NavPush");
            await Harness.Render();
            H.Check("NavOpt_Pushed", H.FindText("Cur:Pushed") is not null);
            H.Check("NavOpt_Depth2", H.FindText("Depth:2") is not null);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  5. DeepLinkMap — pattern matching and parameter extraction
    //     Targets: DeepLinkMap.Map, Resolve, CompilePattern, RouteArgs.Get<T>
    // ════════════════════════════════════════════════════════════════════════

    internal class DeepLinkMapExercise(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var map = new DeepLinkMap<string>()
                .Map("/", args => "Home")
                .Map("/users/{id:int}", args => $"User:{args.Get<int>("id")}")
                .Map("/items/{name}", args => $"Item:{args.Get<string>("name")}")
                .Map("/deep/{id:int}", args => $"Deep:{args.Get<int>("id")}", () => new[] { "Home", "List" });

            // Match root
            var r1 = map.Resolve("/");
            H.Check("DeepLink_RootMatched", r1.Matched);
            H.Check("DeepLink_RootRoute", r1.Routes.Length == 1 && r1.Routes[0] == "Home");

            // Match with int param
            var r2 = map.Resolve("/users/42");
            H.Check("DeepLink_UserMatched", r2.Matched);
            H.Check("DeepLink_UserRoute", r2.Routes[0] == "User:42");

            // Match with string param
            var r3 = map.Resolve("/items/widget");
            H.Check("DeepLink_ItemMatched", r3.Matched);
            H.Check("DeepLink_ItemRoute", r3.Routes[0] == "Item:widget");

            // Match with back stack
            var r4 = map.Resolve("/deep/7");
            H.Check("DeepLink_DeepMatched", r4.Matched);
            H.Check("DeepLink_DeepBackStack", r4.Routes.Length == 3);
            H.Check("DeepLink_DeepRoute", r4.Routes[^1] == "Deep:7");

            // No match
            var r5 = map.Resolve("/unknown/path");
            H.Check("DeepLink_NoMatch", !r5.Matched);

            // Resolve with Uri overload
            var r6 = map.Resolve(new Uri("app://host/users/99"));
            H.Check("DeepLink_UriMatch", r6.Matched && r6.Routes[0] == "User:99");

            // RouteArgs.GetString
            var map2 = new DeepLinkMap<string>()
                .Map("/test/{val}", args =>
                {
                    var raw = args.GetString("val");
                    var missing = args.GetString("nope");
                    return $"Raw:{raw},Missing:{missing}";
                });
            var r7 = map2.Resolve("/test/hello");
            H.Check("DeepLink_GetString", r7.Routes[0] == "Raw:hello,Missing:");

            // Dummy render to satisfy harness
            var host = H.CreateHost();
            host.Mount(ctx => Text("DeepLink done"));
            await Harness.Render();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  6. NavigationTransition — factory methods
    //     Targets: NavigationTransition.Slide, Fade, DrillIn, Connected, Spring
    // ════════════════════════════════════════════════════════════════════════

    internal class NavTransitionFactories(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var slide = NavigationTransition.Slide(SlideDirection.FromLeft, TimeSpan.FromMilliseconds(300));
            H.Check("NavTrans_Slide", slide is SlideTransition s && s.Direction == SlideDirection.FromLeft);

            var fade = NavigationTransition.Fade(TimeSpan.FromMilliseconds(200));
            H.Check("NavTrans_Fade", fade is FadeTransition);

            var drillIn = NavigationTransition.DrillIn(TimeSpan.FromMilliseconds(250));
            H.Check("NavTrans_DrillIn", drillIn is DrillInTransition);

            var connected = NavigationTransition.Connected("hero-image");
            H.Check("NavTrans_Connected", connected is ConnectedTransition ct && ct.AnimationKey == "hero-image");

            var spring = NavigationTransition.Spring(0.7f, 0.05f, SlideDirection.FromBottom);
            H.Check("NavTrans_Spring", spring is SpringSlideTransition sp && sp.DampingRatio == 0.7f);

            var defaultT = NavigationTransition.Default;
            H.Check("NavTrans_Default", defaultT is SlideTransition);

            var none = NavigationTransition.None;
            H.Check("NavTrans_None", none is SuppressTransition);

            var host = H.CreateHost();
            host.Mount(ctx => Text("Transitions done"));
            await Harness.Render();
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  7. NavigationHandle — Navigated event + BackStack/ForwardStack
    //     Targets: NavigationHandle.Navigated, BackStack, ForwardStack properties
    // ════════════════════════════════════════════════════════════════════════

    internal class NavHandleEvents(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            var events = new List<string>();

            host.Mount(ctx =>
            {
                var nav = ctx.UseNavigation("Root");

                // Subscribe to Navigated event on first render only
                ctx.UseEffect(() =>
                {
                    nav.Navigated += args => events.Add($"{args.Mode}:{args.Route}");
                    return () => { };
                });

                return VStack(
                    NavigationHost<string>(nav, route => Text($"Evt:{route}")),
                    Text($"Back:{nav.BackStack.Count}"),
                    Text($"Fwd:{nav.ForwardStack.Count}"),
                    Button("Nav1", () => nav.Navigate("Page1")),
                    Button("Nav2", () => nav.Navigate("Page2")),
                    Button("Back", () => nav.GoBack())
                );
            });

            await Harness.Render();

            H.ClickButton("Nav1");
            await Harness.Render();
            H.ClickButton("Nav2");
            await Harness.Render();
            H.Check("NavEvt_BackStack", H.FindText("Back:2") is not null);

            H.ClickButton("Back");
            await Harness.Render();
            H.Check("NavEvt_ForwardStack", H.FindText("Fwd:1") is not null);
            H.Check("NavEvt_EventsFired", events.Count >= 3);
        }
    }
}
