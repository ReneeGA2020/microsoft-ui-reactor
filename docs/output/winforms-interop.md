
# WinForms Interop

Duct components can run inside WinForms applications using XAML Islands.
The `Duct.Interop.WinForms` package provides `XamlIslandControl` — a
standard WinForms control that hosts a Duct component tree with full
keyboard, accessibility, and theming support.

## Bootstrap

Every WinForms + Duct app starts with `XamlIslandBootstrap.Run()`. This
initializes the WinAppSDK/WinUI runtime and then calls your callback to
show the WinForms UI:

```csharp
XamlIslandBootstrap.Run(() =>
{
    var form = new SWF.Form
    {
        Text = "My WinForms + Duct App",
        Width = 800,
        Height = 500
    };

    var island = new XamlIslandControl
    {
        ComponentType = typeof(WinFormsHostDemo),
        Dock = SWF.DockStyle.Fill
    };

    form.Controls.Add(island);
    form.Show();
});
```

The bootstrap handles:

- DispatcherQueue creation for WinUI
- Theme resource loading
- Keyboard message filtering (so WinUI controls receive key events)
- DPI awareness configuration
- Clean shutdown when `Application.Exit()` is called

Call `XamlIslandBootstrap.Run()` once, at the very start of your
application. WinForms owns the message loop — Duct runs inside it.

## XamlIslandControl

`XamlIslandControl` is a `System.Windows.Forms.Control` that wraps a
`DesktopWindowXamlSource`. Add it to any WinForms form or panel:

```csharp
class WinFormsHostDemo : Component
{
    public override Element Render()
    {
        // This component is hosted via XamlIslandControl.ComponentType
        var (count, setCount) = UseState(0);

        return VStack(12,
            Heading("Duct in WinForms"),
            Text($"Count: {count}").FontSize(24),
            Button("+1", () => setCount(count + 1))
        ).Padding(24).Background(SolidBackground);
    }
}
```

![Duct component in a WinForms form](images/winforms-interop/island-control.png)

There are three ways to set the content:

| Property | Use case |
|----------|---------|
| `ComponentType` | Set a Duct `Component` type — works in the designer |
| `ContentFactory` | Provide a factory function for custom initialization |
| `XamlContent` | Set raw WinUI `UIElement` content directly |

`ComponentType` is the simplest path: set it to `typeof(MyComponent)` and
the control creates and hosts the component automatically.

## Designer Support

`XamlIslandControl` has full WinForms designer support. Drag it onto a
form and set `ComponentType` from the Properties grid — the dropdown lists
all concrete `Component` subclasses with parameterless constructors:

```csharp
// In the form's Designer.cs file:
//
// this.ductIsland = new XamlIslandControl();
// this.ductIsland.ComponentType = typeof(DashboardComponent);
// this.ductIsland.Dock = DockStyle.Fill;
// this.panel1.Controls.Add(this.ductIsland);
//
// The Properties grid shows a dropdown of all Component subclasses
// with parameterless constructors. Select your component and the
// designer serializes it as typeof(DashboardComponent).
```

![Designer with ComponentType dropdown](images/winforms-interop/designer.png)

At design time, the control renders a placeholder with a border showing the
component name. No WinUI objects are created until the app runs, so the
designer stays lightweight.

The `DuctComponentTypeConverter` powers this integration. It converts
between `Type` objects and type name strings, and enumerates available
components for the dropdown.

## Keyboard and Tab Navigation

XAML Islands need explicit keyboard bridging to interoperate with WinForms
tab order. `XamlIslandControl` handles this automatically:

```csharp
class KeyboardDemo : Component
{
    public override Element Render()
    {
        var (text, setText) = UseState("");

        // Tab moves focus into this Duct tree from WinForms controls.
        // Tab/Shift+Tab cycles through Duct controls normally.
        // Tab out of the last Duct control returns focus to WinForms.
        return VStack(12,
            TextField(text, setText, placeholder: "Type here...")
                .TabIndex(0),
            Button("Submit", () => { })
                .TabIndex(1)
                .AccessKey("S")
        ).Padding(24).Background(SolidBackground);
    }
}
```

- **Tab/Shift+Tab** moves focus between WinForms controls and into/out of
  the Duct component tree
- **Arrow keys, Enter, Escape** are routed to WinUI controls inside the
  island
- **Alt+key shortcuts** work for both WinForms menus and Duct `AccessKey`
  modifiers

The bootstrap's `ContentPreTranslateMessage` hook ensures keyboard messages
reach WinUI controls. No additional configuration is needed.

## Accessibility

Screen readers see Duct components inside XAML Islands as part of the
WinForms automation tree. The interop layer bridges:

```csharp
class AccessibleIslandComponent : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("");

        // All accessibility modifiers work inside XAML Islands
        return VStack(12,
            Heading("Registration")
                .HeadingLevel(AutomationHeadingLevel.Level1),
            TextField(name, setName, header: "Full Name")
                .AutomationName("Full name")
                .Required()
                .TabIndex(0),
            Button("Register", () => { })
                .AutomationName("Submit registration")
                .TabIndex(1)
        ).Padding(24)
         .Landmark(AutomationLandmarkType.Form)
         .Background(SolidBackground);
    }
}
```

- **AutomationName**, **HeadingLevel**, and other [accessibility](accessibility.md)
  modifiers work identically to a pure Duct app
- **UseAnnounce** live regions are forwarded through the island boundary
- **Tab order** flows correctly between WinForms and Duct controls
- **Focus trapping** via `UseFocusTrap` works within the Duct subtree

See [Accessibility](accessibility.md) for the full modifier reference.

## Background and Sizing

XAML Islands do not provide an implicit background or stretch behavior.
Duct components hosted in WinForms must manage their own background:

```csharp
class BackgroundDemo : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);

        // Always set an explicit background on root content.
        // XAML Islands have no default background — without this,
        // content renders on a transparent surface.
        return VStack(12,
            Text("Theme-aware background").Bold(),
            Text($"Count: {count}"),
            Button("Increment", () => setCount(count + 1))
        ).Padding(24).Background(SolidBackground);
    }
}
```

![Component with explicit background](images/winforms-interop/background.png)

Wrap your component's root in `Grid(...)` with `.Background(...)` to fill
the island area. Without this, the component renders on a transparent
background and may not stretch to fill the WinForms control bounds.

## Advanced: ContentFactory

For components that need constructor parameters or custom host configuration,
use `ContentFactory` instead of `ComponentType`:

```csharp
// Use ContentFactory for components needing parameters:
//
// var island = new XamlIslandControl
// {
//     ContentFactory = () =>
//     {
//         var host = new DuctHostControl();
//         host.SetComponent<ConfigurableComponent>();
//         return host;
//     }
// };

class ConfigurableComponent : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        return VStack(12,
            Heading("Dashboard"),
            Text($"Value: {count}"),
            Button("+1", () => setCount(count + 1))
        ).Padding(24).Background(SolidBackground);
    }
}
```

The factory function runs on the UI thread after the XAML Island is ready.
Return any `UIElement` — typically a `DuctHostControl` wrapping your
component.

## Tips

**Call `XamlIslandBootstrap.Run()` first.** Before any WinForms forms are
shown. The bootstrap must initialize the WinUI runtime before any
`XamlIslandControl` instances are created.

**Use `ComponentType` for simple cases.** It is designer-friendly and
handles lifecycle automatically. Reserve `ContentFactory` for components
that need parameters or custom host setup.

**Set an explicit background on root components.** XAML Islands have no
default background. Use `.Background(SolidBackgroundFillColorBase)` for
theme-aware backgrounds.

**Dock or anchor the island control.** `XamlIslandControl` supports
standard WinForms layout: `Dock = DockStyle.Fill` to fill a panel,
or anchoring for proportional resize.

**Test Tab navigation end-to-end.** Tab between WinForms controls and Duct
controls to verify focus flows correctly. The bridging handles most cases
automatically, but complex tab orders may need `.TabIndex()` hints.

## Next Steps

- **[Duct](readme.md)** — back to the index: overview of the framework and full topic list
- **[Data System](data-system.md)** — previous topic: DataGrid with sort, filter, and editing
- **[Accessibility](accessibility.md)** — accessibility modifiers and screen reader support
- **[Advanced Patterns](advanced.md)** — error boundaries, Memo, and escape hatches
- **[Components](components.md)** — building reusable components to host in WinForms
