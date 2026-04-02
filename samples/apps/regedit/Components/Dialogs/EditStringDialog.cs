using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using static Duct.UI;

namespace DuctRegedit.Components.Dialogs;

internal sealed record EditStringDialogProps(
    bool IsOpen,
    string Title,
    string ValueName,
    string ValueData,
    Action<string> OnValueDataChanged,
    Action OnSave,
    Action OnCancel
);

internal sealed class EditStringDialog : Component<EditStringDialogProps>
{
    public override Element Render()
    {
        return ContentDialog(
            Props.Title,
            VStack(12,
                VStack(4,
                    Text(Strings.ValueName),
                    TextField(Props.ValueName, _ => { })
                        .ReadOnly()
                ),
                VStack(4,
                    Text(Strings.ValueData),
                    TextField(Props.ValueData, Props.OnValueDataChanged)
                )
            ).Width(400),
            Strings.OK
        ) with
        {
            IsOpen = Props.IsOpen,
            SecondaryButtonText = Strings.Cancel,
            DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Primary,
            OnClosed = _ => Props.OnCancel(),
        };
    }
}
