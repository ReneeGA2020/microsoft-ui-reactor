using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor.ControlPages.BasicInput;

class TextFieldPage: Component
{
    public override Element Render()
    {
        var (text, setText) = UseState("");
        var (multiline, setMultiline) = UseState("");
        var (headerText, setHeaderText) = UseState("");

        return ScrollView(VStack(16,
            PageHeader("TextField", "A single-line or multi-line plain text input field."),

            SampleCard("Basic TextField",
                VStack(8,
                    TextField(text, v => setText(v), "Type here..."),
                    TextBlock($"Characters: {text.Length}").Foreground(Theme.SecondaryText)),
                sourceCode: @"
TextField(text, v => setText(v), ""Type here..."")
"),

            SampleCard("Multiline TextField",
                TextField(multiline, v => setMultiline(v), "Enter multiple lines...")
                    .Set(tb => { tb.AcceptsReturn = true; tb.TextWrapping = TextWrapping.Wrap; })
                    .Height(120),
                sourceCode: @"
TextField(multiline, v => setMultiline(v), ""Enter multiple lines..."")
    .Set(tb => { tb.AcceptsReturn = true; tb.TextWrapping = TextWrapping.Wrap; })
    .Height(120)
"),

            SampleCard("TextField with Header",
                TextField(headerText, v => setHeaderText(v), "user@example.com").Header("Email"),
                sourceCode: @"
TextField(headerText, v => setHeaderText(v), ""user@example.com"").Header(""Email"")
")
        ).Margin(36, 24, 36, 36));
    }
}
