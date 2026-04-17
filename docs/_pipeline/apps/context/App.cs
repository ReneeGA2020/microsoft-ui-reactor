using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;
using Microsoft.UI.Xaml;

ReactorApp.Run<ContextApp>("Context", width: 600, height: 600
#if DEBUG
    , preview: true
#endif
);

// <snippet:create-context>
static class Contexts
{
    public static Context<string> ThemeMode = new("light");
    public static Context<string> UserName = new("Guest");
    public static Context<int> FontScale = new(16);
}
// </snippet:create-context>

// <snippet:provide-consume>
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
// </snippet:provide-consume>

// <snippet:theme-switch>
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
// </snippet:theme-switch>

// <snippet:nested-override>
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
// </snippet:nested-override>

// <snippet:multiple-contexts>
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
// </snippet:multiple-contexts>

// Main app
class ContextApp : Component
{
    public override Element Render()
    {
        return ScrollView(
            VStack(24,
                Heading("Context"),
                Component<ProvideConsumeExample>(),
                Component<ThemeSwitchExample>(),
                Component<NestedOverrideExample>(),
                Component<MultipleContextsExample>()
            ).Padding(24)
        );
    }
}
