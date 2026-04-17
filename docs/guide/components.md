
# Components

Components are the building blocks of a Reactor app. Each component is a class
with a `Render()` method that returns an element tree describing its UI.

## Basic Component

Extend `Component` and override `Render()`:

```csharp
class Greeting : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("World");

        return VStack(12,
            Text($"Hello, {name}!").FontSize(20).Bold(),
            TextField(name, setName, placeholder: "Your name")
                .Width(200)
        ).Padding(16);
    }
}
```

![Basic component](images/components/basic-component.png)

`Render()` is called every time state changes. You call [hooks](hooks.md) like `UseState`
at the top, then return an element tree. Reactor diffs the result against the
previous render and patches only the controls that changed.

## Props with Records

When a component needs input from its parent, define a C# record for its
props and extend `Component<TProps>`:

```csharp
record AlertProps(string Title, string Message, string Severity = "info");
```

```csharp
class Alert : Component<AlertProps>
{
    public override Element Render()
    {
        var bg = Props.Severity switch
        {
            "error" => "#FDE7E9",
            "warning" => "#FFF4CE",
            _ => "#DFF6DD"
        };

        return Border(
            VStack(4,
                Text(Props.Title).Bold(),
                Text(Props.Message)
            ).Padding(12)
        ).Background(bg).CornerRadius(4);
    }
}
```

![Props component](images/components/props-component.png)

Records give you immutable data with value equality. Reactor uses this for
automatic memoization — if the parent re-renders but the props haven't
changed structurally, the child skips its `Render()` call.

Access props via `Props.PropertyName` inside `Render()`. The parent sets
props by assigning the `Props` property when creating the component instance.

## Custom ShouldUpdate

Override `ShouldUpdate` to control when a component re-renders:

```csharp
record ExpensiveProps(string Label, int Value);

class ExpensiveDisplay : Component<ExpensiveProps>
{
    protected override bool ShouldUpdate(
        ExpensiveProps? oldProps, ExpensiveProps? newProps)
    {
        // Only re-render when the Value changes, ignore Label
        return oldProps?.Value != newProps?.Value;
    }

    public override Element Render()
    {
        return Text($"Value: {Props.Value}").FontSize(18).Bold();
    }
}
```

The default behavior for `Component<TProps>` uses `Equals()` — which with
records means structural equality. Override `ShouldUpdate` when you want
coarser control, like ignoring cosmetic prop changes.

For propless `Component`, `ShouldUpdate()` returns `false` by default,
meaning the component only re-renders from its own state changes. Override
and return `true` to re-render whenever the parent re-renders.

## Function Components

Not everything needs a class. Use `Func` for lightweight inline components:

```csharp
class FunctionComponentDemo : Component
{
    public override Element Render()
    {
        return VStack(12,
            SubHeading("Function components"),
            Func(ctx =>
            {
                var (on, setOn) = ctx.UseState(false);
                return HStack(8,
                    ToggleSwitch(on, setOn),
                    Text(on ? "Active" : "Inactive")
                );
            }),
            Memo(ctx =>
            {
                return Text("I only re-render when deps change")
                    .Opacity(0.6);
            }, "stable-dep")
        ).Padding(16);
    }
}
```

![Function component](images/components/function-component.png)

- **`Func(ctx => { ... })`** — an inline component with its own hook state.
  The `ctx` parameter is a `RenderContext` that provides `UseState`,
  `UseEffect`, and all other hooks.
- **`Memo(ctx => { ... }, deps)`** — like `Func`, but only re-renders when
  the dependency values change. Use this for expensive subtrees (see [Hooks](hooks.md) for `UseMemo`).

You can also use a function component as the app root:

<!-- ai:lock -->
```csharp
ReactorApp.Run("Title", ctx => {
    var (n, setN) = ctx.UseState(0);
    return Text($"{n}");
}, width: 400, height: 300);
```
<!-- /ai:lock -->

## Composition

Build complex UIs by nesting components:

```csharp
class ComponentsApp : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);

        return ScrollView(
            VStack(16,
                Heading("Component Patterns"),
                Component<Greeting>(),
                Component<Alert, AlertProps>(new("Success", "It works!")),
                Component<Alert, AlertProps>(new("Oops", "Something broke",
                    "error")),
                HStack(8,
                    Button("+1", () => setCount(count + 1)),
                    Component<ExpensiveDisplay, ExpensiveProps>(
                        new("Counter", count))
                ),
                Component<FunctionComponentDemo>()
            ).Padding(24)
        );
    }
}
```

![Composed components](images/components/composition.png)

Each component manages its own state independently. The parent creates child
components with `new` and sets their `Props`. Reactor handles the rest —
mounting, updating, and unmounting as the tree changes.

## Tips

**Use records for props.** They give you immutable data, value equality, and
`with` expressions for free. Reactor's memoization depends on `Equals()` working
correctly.

**Prefer composition over deep inheritance.** `Component` and
`Component<TProps>` are the only base classes you need. Build complexity
through nesting, not class hierarchies.

**Use `Func` for one-off components.** If a component is only used in one
place and has simple state, an inline `Func(ctx => ...)` avoids the class
boilerplate.

**Keep Render() pure.** Don't mutate external state or perform I/O inside
`Render()`. Use [`UseEffect`](effects.md) for side effects. `Render()` should be a pure
function from (state + props) to elements.

**Name components after what they display, not what they do.** `Alert`,
`UserCard`, `SettingsPanel` — not `AlertHandler`, `UserManager`,
`SettingsProcessor`.

## Next Steps

- **[Hooks](hooks.md)** — Next: deep dive into UseState, UseReducer, UseEffect, and all the hooks
- **[Dev Tooling](dev-tooling.md)** — Previous: hot reload and preview mode for faster iteration
- **[Layout](layout.md)** — Arrange components with VStack, HStack, Grid, and responsive patterns
- **[Styling and Theming](styling.md)** — Apply colors, typography, and themes to your components
