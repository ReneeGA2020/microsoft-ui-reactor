
# Context

Context lets you pass data through the [component](components.md) tree without
threading it through every level of props. Define a `ReactorContext<T>`, provide
a value at any level, and any descendant can read it with `UseContext`.

## Creating a Context

A context is a static field with a default value:

```csharp
static class Contexts
{
    public static Context<string> ThemeMode = new("light");
    public static Context<string> UserName = new("Guest");
    public static Context<int> FontScale = new(16);
}
```

The default value is used when a component calls `UseContext` but no ancestor
has provided a value. Choose a sensible default — it makes components work
standalone during development.

## Providing and Consuming

Use `.Provide()` on any element to supply a value to its subtree. Use
`UseContext()` in any descendant to read it:

```csharp
class ProvideConsumeExample : Component
{
    public override Element Render()
    {
        return VStack(12,
            Text("Outside: no provider"),
            VStack(12,
                Component<Greeting>()
            ).Provide(Contexts.UserName, "Alice")
        ).Padding(24);
    }
}

class Greeting : Component
{
    public override Element Render()
    {
        var name = UseContext(Contexts.UserName);
        return Text($"Hello, {name}!").FontSize(20).Bold();
    }
}
```

![Provider and consumer components](images/context/provide-consume.png)

`.Provide()` is a modifier like `.Padding()` or `.Background()` (see
[Styling and Theming](styling.md)) — it works on any element. The value
propagates to every descendant, no matter how deep.

## Theme Switching with Context

A common use case is a theme toggle that affects an entire subtree. Here the
root component provides a theme value, and child components consume it:

```csharp
class ThemeSwitchExample : Component
{
    public override Element Render()
    {
        var (isDark, setIsDark) = UseState(false);
        var mode = isDark ? "dark" : "light";
        return VStack(16,
            ToggleSwitch(isDark, setIsDark, onContent: "Dark", offContent: "Light"),
            VStack(12, Component<ThemePanel>()).Provide(Contexts.ThemeMode, mode)
        ).Padding(24);
    }
}

class ThemePanel : Component
{
    public override Element Render()
    {
        var theme = UseContext(Contexts.ThemeMode);
        var elTheme = theme == "dark" ? ElementTheme.Dark : ElementTheme.Light;
        return Border(
            VStack(8,
                Text($"Current theme: {theme}").Bold(),
                Text("Panel adapts to context.").Foreground(Theme.SecondaryText)
            ).Padding(16)
        ).Background(Theme.CardBackground)
         .CornerRadius(8)
         .Set(b => b.RequestedTheme = elTheme);
    }
}
```

![Theme toggle with light and dark panels](images/context/theme-switch.png)

The `ThemePanel` component reads the context and applies `ElementTheme`
accordingly. When the toggle changes the provided value, all consumers
re-render with the new theme.

## Nested Context Overrides

A child provider overrides its parent's value for its own subtree. Siblings
keep the original value:

```csharp
class NestedOverrideExample : Component
{
    public override Element Render()
    {
        return HStack(16,
            VStack(8,
                Caption("Parent value"),
                Component<NameDisplay>()
            ).Provide(Contexts.UserName, "Alice"),
            VStack(8,
                Caption("Overridden child"),
                VStack(4, Component<NameDisplay>())
                    .Provide(Contexts.UserName, "Bob")
            ).Provide(Contexts.UserName, "Alice")
        ).Padding(24);
    }
}

class NameDisplay : Component
{
    public override Element Render()
    {
        var name = UseContext(Contexts.UserName);
        return Text(name).FontSize(18).SemiBold().Foreground(Theme.Accent);
    }
}
```

![Nested contexts with different values](images/context/nested-override.png)

The inner `.Provide()` only affects descendants of that element. The sibling
subtree still sees the parent's value. This lets you create local overrides
without affecting the rest of the tree.

## Multiple Contexts

Components can provide and consume multiple contexts simultaneously. Each
context is independent:

```csharp
class MultipleContextsExample : Component
{
    public override Element Render()
    {
        return VStack(8,
            Component<ProfileCard>()
        ).Provide(Contexts.UserName, "Charlie")
         .Provide(Contexts.FontScale, 22)
         .Padding(24);
    }
}

class ProfileCard : Component
{
    public override Element Render()
    {
        var name = UseContext(Contexts.UserName);
        var fontSize = UseContext(Contexts.FontScale);

        return Border(
            VStack(8,
                Text(name).FontSize(fontSize).Bold(),
                Text($"Font scale from context: {fontSize}px")
                    .Foreground(Theme.SecondaryText)
            ).Padding(16)
        ).Background(Theme.CardBackground).CornerRadius(8);
    }
}
```

![Component reading two contexts](images/context/multiple-contexts.png)

Each `ReactorContext<T>` is a separate channel. Providing one doesn't affect
another. A component can call `UseContext` as many times as it needs.

## Tips

**Use context for cross-cutting concerns.** Theme, locale, auth state, and
feature flags are good candidates. Props are better for component-specific
data.

**Always set a meaningful default.** Components that read context should work
even without a provider — the default enables standalone testing and preview.

**Keep context values immutable.** Provide a new value to trigger re-renders.
Mutating the existing object won't notify consumers.

**Don't overuse context.** If data only flows one or two levels down, pass it
as props. Context is for data that many components at different depths need.

**Combine with [`UseState`](hooks.md) for dynamic context.** Store the value
in state at the provider level, and consumers update automatically when you
call the setter.

## Next Steps

- **[Commanding](commanding.md)** — Previous: bundle actions with labels, icons, and keyboard accelerators
- **[Accessibility](accessibility.md)** — Next: add screen reader support and keyboard navigation
- **[Hooks](hooks.md)** — Learn about UseContext, UseState, and other hooks that power context
- **[Styling and Theming](styling.md)** — Use context to propagate theme values across your app
- **[Effects and Lifecycle](effects.md)** — Trigger side effects when context values change
