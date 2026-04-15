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

class SlotsDemo : Component
{
    record CardProps(
        Element? Header = null,
        Element? Body = null,
        Element? Footer = null
    );

    class Card : Component<CardProps>
    {
        public override Element Render()
        {
            return Border(VStack(0,
                Props.Header is not null
                    ? Border(Props.Header)
                        .Padding(12, 8).Background(SubtleFill)
                        .WithBorder(ControlStroke)
                    : Empty(),
                Props.Body is not null
                    ? Border(Props.Body).Padding(16)
                    : Empty(),
                Props.Footer is not null
                    ? Border(Props.Footer)
                        .Padding(8, 12).Background(LayerFill)
                        .WithBorder(ControlStroke)
                    : Empty()
            )).CornerRadius(8).WithBorder(ControlStroke);
        }
    }

    record InfoRowProps(Element? Leading = null, Element? Title = null, Element? Trailing = null);

    class InfoRow : Component<InfoRowProps>
    {
        public override Element Render()
        {
            return HStack(12,
                Props.Leading ?? Empty(),
                Props.Title is not null
                    ? Border(Props.Title).Flex(grow: 1)
                    : Empty(),
                Props.Trailing ?? Empty()
            ).Padding(8).VAlign(VerticalAlignment.Center);
        }
    }

    public override Element Render()
    {
        var (name, setName) = UseState("World");
        var (expanded, setExpanded) = UseState(false);

        return ScrollView(VStack(16,
            Heading("Slots Pattern"),
            Text("Components accept Element? props as named content areas — like React children/render props."),

            // 1. Card with all three slots
            SubHeading("1. Card — Header / Body / Footer"),
            Component<Card, CardProps>(new(
                Header: Text("User Profile").SemiBold(),
                Body: VStack(8,
                    Text("Name: Alice Johnson"),
                    Text("Email: alice@example.com"),
                    Text("Role: Software Engineer")
                ),
                Footer: HStack(8,
                    Button("Edit", () => { }),
                    Button("Delete", () => { })
                )
            )),

            // 2. Partial slots
            SubHeading("2. Partial slots — omit Footer"),
            Component<Card, CardProps>(new(
                Header: Text("Notification").SemiBold(),
                Body: Text("Build completed successfully. No footer needed.")
            )),

            // 3. Dynamic slot content
            SubHeading("3. Dynamic slot content"),
            TextField(name, setName, placeholder: "Type a name...").Width(220),
            Component<Card, CardProps>(new(
                Header: HStack(8,
                    Border(Empty()).Background(Accent).CornerRadius(12).Size(24, 24),
                    Text($"Hello, {name}!").SemiBold()
                ),
                Body: Text($"The slot content updates when you type. Name length: {name.Length} characters."),
                Footer: Button("Reset", () => setName("World"))
            )),

            // 4. InfoRow — Leading / Title / Trailing
            SubHeading("4. InfoRow — Leading / Title / Trailing"),
            Text("Another slot pattern: a row with optional leading icon, title area, and trailing action."),
            VStack(4,
                Component<InfoRow, InfoRowProps>(new(
                    Leading: Text("\uE77B").Set(t => t.FontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"]),
                    Title: Text("Inbox").SemiBold(),
                    Trailing: Text("12").Foreground(TertiaryText)
                )),
                Component<InfoRow, InfoRowProps>(new(
                    Leading: Text("\uE724").Set(t => t.FontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"]),
                    Title: Text("Sent"),
                    Trailing: Text("3").Foreground(TertiaryText)
                )),
                Component<InfoRow, InfoRowProps>(new(
                    Leading: Text("\uE74D").Set(t => t.FontFamily = (FontFamily)Application.Current.Resources["SymbolThemeFontFamily"]),
                    Title: Text("Deleted"),
                    Trailing: Text("0").Foreground(TertiaryText)
                ))
            ),

            // 5. Naming conventions
            SubHeading("5. Slot naming conventions"),
            VStack(4,
                Caption("Single default slot: params Element?[] children"),
                Caption("Named slots: Element?-typed props on a record"),
                Caption("Common names: Header, Body/Content, Footer/Actions, Leading, Trailing, Icon, Label, Title"),
                Caption("Optional slots: Element? with = null default, skip rendering when null"),
                Caption("Memo tip: static slot content (Text) → memo works. Slots with handlers → use UseCallback.").SemiBold()
            )
        ));
    }
}
