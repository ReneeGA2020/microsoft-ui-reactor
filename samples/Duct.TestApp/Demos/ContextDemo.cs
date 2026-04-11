using System.Diagnostics;
using Duct;
using Duct.Core;
using Duct.Core.Navigation;
using Duct.Flex;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Duct.PropertyGrid;
using static Duct.UI;
using static Duct.Core.Theme;

class ContextDemo : Component
{
    static readonly DuctContext<string> AccentContext = new("#0078D4");
    static readonly DuctContext<string> UserNameContext = new("Guest");

    public override Element Render()
    {
        var (accent, setAccent) = UseState("#0078D4");
        var (userName, setUserName) = UseState("Alice");

        return ScrollView(VStack(16,
            Heading("Context System"),
            Text("DuctContext passes values through the tree without prop drilling."),

            // 1. Controls
            SubHeading("1. Provide context values"),
            HStack(8,
                Text("Accent color:"),
                Button("Blue", () => setAccent("#0078D4")).Disabled(accent == "#0078D4"),
                Button("Red", () => setAccent("#E74C3C")).Disabled(accent == "#E74C3C"),
                Button("Green", () => setAccent("#50C878")).Disabled(accent == "#50C878"),
                Border(Empty()).Background(accent).CornerRadius(4).Size(24, 24)
            ),
            HStack(8,
                Text("User name:"),
                TextField(userName, setUserName, placeholder: "Enter name").Width(200)
            ),

            // 2. Consumers
            SubHeading("2. Consume in descendants"),
            Text("These components call UseContext() — no props passed from parent."),
            VStack(8,
                Component<AccentBadge>(),
                Component<UserGreeting>()
            ).Provide(AccentContext, accent).Provide(UserNameContext, userName),

            // 3. Nested override
            SubHeading("3. Nested provider overrides outer"),
            Text("An inner .Provide() shadows the outer for its subtree only."),
            HStack(16,
                VStack(8,
                    Text("Outer scope").SemiBold(),
                    Component<AccentBadge>()
                ).Provide(AccentContext, accent).Provide(UserNameContext, userName),
                VStack(8,
                    Text("Inner scope (forced purple)").SemiBold(),
                    Component<AccentBadge>()
                ).Provide(AccentContext, "#9B59B6").Provide(UserNameContext, userName)
            ),

            // 4. Default value
            SubHeading("4. Default value (no provider)"),
            Text("Without a .Provide() ancestor, UseContext returns the DuctContext default."),
            Component<AccentBadge>()
        ));
    }

    class AccentBadge : Component
    {
        public override Element Render()
        {
            var accent = UseContext(AccentContext);
            return HStack(8,
                Border(Empty()).Background(accent).CornerRadius(4).Size(24, 24),
                Text($"Accent = {accent}").SemiBold()
            );
        }
    }

    class UserGreeting : Component
    {
        public override Element Render()
        {
            var accent = UseContext(AccentContext);
            var name = UseContext(UserNameContext);
            return Border(
                Text($"Hello, {name}!").Foreground(accent).SemiBold().FontSize(18)
            ).Padding(12).CornerRadius(6).Background(SubtleFill);
        }
    }
}
