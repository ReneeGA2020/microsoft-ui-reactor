using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using static WinUIGalleryReactor.SamplePageHost;

namespace WinUIGalleryReactor.ControlPages.StatusAndInfo;

class InfoBarPage : Component
{
    public override Element Render()
    {
        var (showClosable, setShowClosable) = UseState(true);

        return ScrollView(
            VStack(16,
                PageHeader("InfoBar",
                    "A dismissible bar for displaying essential app-level messages."),

                SampleCard("Severity Levels",
                    VStack(8,
                        InfoBar("Informational", "This is an informational message.") with
                        {
                            Severity = InfoBarSeverity.Informational,
                            IsClosable = false,
                        },
                        InfoBar("Success", "Operation completed successfully.") with
                        {
                            Severity = InfoBarSeverity.Success,
                            IsClosable = false,
                        },
                        InfoBar("Warning", "Please review before proceeding.") with
                        {
                            Severity = InfoBarSeverity.Warning,
                            IsClosable = false,
                        },
                        InfoBar("Error", "Something went wrong.") with
                        {
                            Severity = InfoBarSeverity.Error,
                            IsClosable = false,
                        }
                    ),
                    @"InfoBar(""Success"", ""Completed!"") with {
    Severity = InfoBarSeverity.Success,
    IsClosable = false,
}"),

                SampleCard("Closable InfoBar",
                    VStack(8,
                        showClosable
                            ? InfoBar("Closable", "Click the close button to dismiss.").Closable()
                            : TextBlock("InfoBar was closed.").Foreground(Theme.SecondaryText),
                        Button("Reset", () => setShowClosable(true))
                    ),
                    @"InfoBar(""Closable"", ""Click close to dismiss."").Closable()")
            ).Margin(36, 24, 36, 36)
        );
    }
}
