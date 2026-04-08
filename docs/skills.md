# Duct Skills Guide — Navigation System

## Overview

Duct provides a declarative, type-safe navigation system built on three primitives:

- **`UseNavigation<TRoute>(initialRoute)`** — creates a navigation stack (root mode)
- **`UseNavigation<TRoute>()`** — retrieves an ancestor's navigation handle (child mode)
- **`NavigationHost(nav, routeMap)`** — renders the current route's component

Routes are C# records. Navigation is driven by calling methods on the `NavigationHandle<TRoute>`.

## Route Type Design

Define routes as a sealed record hierarchy:

```csharp
abstract record AppRoute;
sealed record Home : AppRoute;
sealed record Detail(int Id) : AppRoute;
sealed record Settings : AppRoute;
sealed record Profile(string Name) : AppRoute;
```

**Guidelines:**
- Use `abstract record` as the base for structural equality and pattern matching.
- Use parameters for route data (e.g., `Detail(int Id)`).
- Keep routes immutable — never use mutable fields.
- For nested navigation, define a separate route hierarchy (e.g., `SettingsRoute`).

## Basic Navigation Pattern

```csharp
class AppShell : Component
{
    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>(new Home());

        return NavigationHost(nav, route => route switch
        {
            Home => Component<HomePage>(),
            Detail d => Component<DetailPage, int>(d.Id),
            Settings => Component<SettingsPage>(),
            _ => Text("Unknown route"),
        });
    }
}

class HomePage : Component
{
    public override Element Render()
    {
        var nav = UseNavigation<AppRoute>(); // child mode
        return Button("View Details", () => nav.Navigate(new Detail(42)));
    }
}
```

## NavigationView Integration

Use `.WithNavigation()` to auto-sync a `NavigationView` with a `NavigationHandle`:

```csharp
NavigationView(
    [
        NavItem("Home", icon: "\uE80F", tag: "home"),
        NavItem("Settings", icon: "\uE713", tag: "settings"),
    ],
    NavigationHost(nav, routeMap)
).WithNavigation(nav,
    routeToTag: route => route switch { Home => "home", Settings => "settings", _ => null },
    tagToRoute: tag => tag switch { "settings" => new Settings(), _ => new Home() })
```

This automatically sets:
- `SelectedTag` from the current route
- `IsBackEnabled` from `CanGoBack`
- `OnSelectionChanged` to navigate (skips if route unchanged)
- `OnBackRequested` to `GoBack()`

## TitleBar Integration

```csharp
TitleBar("My App").WithNavigation(nav)
```

Sets `IsBackButtonVisible`, `IsBackButtonEnabled`, and `OnBackRequested`.

## Choosing Transitions

| Transition | When to use |
|---|---|
| `Slide()` | Lateral navigation (tabs, sibling pages) |
| `DrillIn()` | Hierarchical navigation (list → detail) |
| `Fade()` | Modal-style or settings sub-pages |
| `Spring(dampingRatio, period, direction)` | Playful/bouncy navigation |
| `NavigationTransition.None` | Instant swap (tests, low-motion preference) |

Set per-host default:
```csharp
NavigationHost(nav, routeMap) with { Transition = NavigationTransition.Slide() }
```

Override per-navigation:
```csharp
nav.Navigate(new Detail(1), new NavigateOptions { Transition = NavigationTransition.DrillIn() });
```

## Page Caching

Enable caching to preserve component state (scroll position, form values) across navigations:

```csharp
NavigationHost(nav, routeMap) with
{
    CacheMode = NavigationCacheMode.Enabled,
    CacheSize = 10,
}
```

- `Disabled` (default): unmount on navigate-away, fresh mount on navigate-back.
- `Enabled`: LRU cache, evicted pages are unmounted.
- `Required`: never evicted (use sparingly).

Best for: scroll-heavy list pages, form pages with partially filled data.

## Navigation Guards

Use `UseNavigationLifecycle` to block navigation when there are unsaved changes:

```csharp
class EditPage : Component
{
    public override Element Render()
    {
        var (dirty, setDirty) = UseState(false);

        UseNavigationLifecycle(
            onNavigatingFrom: ctx =>
            {
                if (dirty) ctx.Cancel(); // blocks navigation
            });

        return TextField(value, v => { setValue(v); setDirty(true); });
    }
}
```

Lifecycle hooks:
- `onNavigatedTo` — fires after page becomes active (load data here)
- `onNavigatingFrom` — fires before navigating away (cancel to block)
- `onNavigatedFrom` — fires after page is no longer active (cleanup)

## Nested Navigation

Create independent navigation stacks within a page:

```csharp
class SettingsPage : Component
{
    public override Element Render()
    {
        var nestedNav = UseNavigation<SettingsRoute>(new GeneralSettings());

        return VStack(
            HStack(
                Button("General", () => nestedNav.Navigate(new GeneralSettings(),
                    new NavigateOptions { PushToBackStack = false })),
                Button("About", () => nestedNav.Navigate(new AboutSettings(),
                    new NavigateOptions { PushToBackStack = false }))
            ),
            NavigationHost(nestedNav, route => route switch { ... })
        );
    }
}
```

Use `PushToBackStack = false` for tab-style sub-navigation (Replace instead of Push).

## System Back Button

```csharp
if (DuctApp.ActiveHost?.Window is { } window)
    UseSystemBackButton(nav, window);
```

Handles Alt+Left and VirtualKey.GoBack keyboard events.

## Deep Linking

```csharp
var deepLinks = new DeepLinkMap<AppRoute>()
    .Map("/", _ => new Home())
    .Map("/detail/{id:int}", args => new Detail(args.Get<int>("id")),
        () => [new Home()])  // synthetic back stack
    .Map("/profile/{name}", args => new Profile(args.GetString("name") ?? "Guest"));

var result = deepLinks.Resolve("/detail/42");
if (result.Matched)
{
    // result.Routes contains [Home(), Detail(42)]
}
```

## State Serialization

Save and restore the full navigation stack:

```csharp
string json = nav.GetState();  // serialize back + current + forward stacks
nav.SetState(json);             // restore (fires Navigated with Mode = Reset)
```

Route types must support `System.Text.Json` serialization. For polymorphic hierarchies,
add `[JsonPolymorphic]` and `[JsonDerivedType]` attributes to the base type.

## Anti-Patterns

- **Don't use `UseState<string>` for page switching.** Use `UseNavigation` + `NavigationHost` instead. This gives you back/forward, guards, transitions, and caching for free.
- **Don't create route types with mutable fields.** Routes must have structural equality (use records).
- **Don't call `nav.Navigate()` during render.** Use event handlers or `UseEffect` instead.
- **Don't forget `NavigationTransition.None` in tests.** Transition animations add async delays that complicate test assertions.

## Migration: UseState to UseNavigation

Before:
```csharp
var (page, setPage) = UseState("home");
var content = page switch { "detail" => ..., _ => ... };
```

After:
```csharp
var nav = UseNavigation<AppRoute>(new Home());
return NavigationHost(nav, route => route switch { ... });
// Child components: var nav = UseNavigation<AppRoute>(); nav.Navigate(new Detail(1));
```
