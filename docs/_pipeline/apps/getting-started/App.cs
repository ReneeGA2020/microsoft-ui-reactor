using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;
using Microsoft.UI.Xaml;

ReactorApp.Run<GettingStartedApp>("Getting Started", width: 600, height: 400
#if DEBUG
    , preview: true
#endif
);

// <snippet:hello-world>
class GettingStartedApp : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("World");

        return VStack(16,
            Text($"Hello, {name}!").FontSize(24).Bold(),
            TextField(name, setName, placeholder: "Enter your name").Width(250)
        ).Padding(24);
    }
}
// </snippet:hello-world>

// <snippet:usestate-counter>
class CounterExample : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);

        return VStack(12,
            Text($"Count: {count}").FontSize(20).SemiBold(),
            HStack(8,
                Button("- 1", () => setCount(count - 1)),
                Button("Reset", () => setCount(0)),
                Button("+ 1", () => setCount(count + 1))
            )
        ).Padding(24);
    }
}
// </snippet:usestate-counter>

// <snippet:layout-basics>
class LayoutBasicsExample : Component
{
    public override Element Render()
    {
        return VStack(16,
            Heading("Layout Demo"),

            SubHeading("Horizontal Stack"),
            HStack(8,
                Button("One"),
                Button("Two"),
                Button("Three")
            ),

            SubHeading("Nested Layout"),
            HStack(16,
                VStack(4,
                    Text("Left Column").Bold(),
                    Text("Item A"),
                    Text("Item B")
                ),
                VStack(4,
                    Text("Right Column").Bold(),
                    Text("Item X"),
                    Text("Item Y")
                )
            )
        ).Padding(24);
    }
}
// </snippet:layout-basics>

// <snippet:multiple-state>
class MultipleStateExample : Component
{
    public override Element Render()
    {
        var (firstName, setFirstName) = UseState("");
        var (lastName, setLastName) = UseState("");
        var (fontSize, setFontSize) = UseState(16.0);

        var fullName = string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName)
            ? "Anonymous"
            : $"{firstName} {lastName}".Trim();

        return VStack(12,
            Text($"Hello, {fullName}!").FontSize(fontSize).Bold(),
            TextField(firstName, setFirstName, placeholder: "First name").Width(200),
            TextField(lastName, setLastName, placeholder: "Last name").Width(200),
            HStack(8,
                Text("Font size:"),
                Slider(fontSize, 10, 40, setFontSize).Width(200),
                Text($"{fontSize:F0}px")
            )
        ).Padding(24);
    }
}
// </snippet:multiple-state>
