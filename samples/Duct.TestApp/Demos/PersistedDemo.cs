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

class PersistedDemo : Component
{
    public override Element Render()
    {
        // Persisted state — survives tab switches
        var (pName, setPName) = UsePersisted("demo.p.name", "");
        var (pEmail, setPEmail) = UsePersisted("demo.p.email", "");
        var (pColor, setPColor) = UsePersisted("demo.p.color", "Blue");

        // Regular state — lost on tab switch
        var (rName, setRName) = UseState("");
        var (rEmail, setREmail) = UseState("");
        var (rColor, setRColor) = UseState("Blue");

        string[] colors = ["Blue", "Red", "Green", "Purple"];

        Element FormColumn(string heading, string bg,
            string name, Action<string> setName,
            string email, Action<string> setEmail,
            string color, Action<string> setColor)
        {
            return VStack(12,
                SubHeading(heading),
                VStack(4, Text("Name"), TextField(name, setName, placeholder: "Enter name").Width(220)),
                VStack(4, Text("Email"), TextField(email, setEmail, placeholder: "you@example.com").Width(220)),
                VStack(4,
                    Text("Color"),
                    HStack(4, colors.Select(c =>
                        Button(c, () => setColor(c)).Disabled(color == c)
                    ).ToArray())
                ),
                Border(VStack(4,
                    Text("Current values:").SemiBold(),
                    Text($"Name: {(string.IsNullOrEmpty(name) ? "(empty)" : name)}"),
                    Text($"Email: {(string.IsNullOrEmpty(email) ? "(empty)" : email)}"),
                    Text($"Color: {color}")
                )).Padding(12).CornerRadius(6).Background(bg)
            );
        }

        return ScrollView(VStack(16,
            Heading("Persisted State"),
            Text("UsePersisted keeps values across unmount/remount (tab switches)."),

            HStack(32,
                FormColumn("UsePersisted (survives)", "#e8f5e9",
                    pName, setPName, pEmail, setPEmail, pColor, setPColor),
                FormColumn("UseState (lost on switch)", "#ffebee",
                    rName, setRName, rEmail, setREmail, rColor, setRColor)
            ),

            Border(
                Text("Fill in both sides, switch to another tab, then come back. Left persists, right resets.")
            ).Padding(12).CornerRadius(6).Background("#fff3e0")
        ));
    }
}
