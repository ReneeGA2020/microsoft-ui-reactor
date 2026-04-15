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

class FormDemo : Component
{
    public override Element Render()
    {
        var (name, setName) = UseState("");
        var (email, setEmail) = UseState("");
        var (agreeToTerms, setAgree) = UseState(false);
        var (darkMode, setDarkMode) = UseState(false);
        var (fontSize, setFontSize) = UseState(14.0);
        var (submitted, setSubmitted) = UseState(false);

        if (submitted)
        {
            return VStack(12,
                Heading("Form Submitted!"),
                Text($"Name: {name}"),
                Text($"Email: {email}"),
                Text($"Dark mode: {(darkMode ? "Yes" : "No")}"),
                Text($"Font size: {fontSize:F0}px"),
                Button("Back", () => setSubmitted(false))
            );
        }

        var isValid = !string.IsNullOrWhiteSpace(name)
            && !string.IsNullOrWhiteSpace(email)
            && agreeToTerms;

        return VStack(16,
            Heading("Registration Form"),

            VStack(8,
                Text("Name"),
                TextField(name, setName, placeholder: "Enter your name").Width(300)
            ),

            VStack(8,
                Text("Email"),
                TextField(email, setEmail, placeholder: "you@example.com").Width(300)
            ),

            ToggleSwitch(darkMode, setDarkMode, onContent: "Dark", offContent: "Light"),

            HStack(8,
                Text("Font size:"),
                Slider(fontSize, 10, 30, setFontSize).Width(200),
                Text($"{fontSize:F0}px")
            ),

            CheckBox(agreeToTerms, setAgree, label: "I agree to the terms"),

            When(!isValid, () =>
                Text("Please fill all fields and agree to terms").Foreground(TertiaryText)),

            Button("Submit", () => setSubmitted(true)).Disabled(!isValid)
        );
    }
}
