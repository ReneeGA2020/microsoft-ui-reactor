using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace DuctRegedit.Components.Dialogs;

internal sealed record EditMultiStringDialogProps(
    bool IsOpen,
    string ValueName,
    string ValueData,
    Action<string> OnValueDataChanged,
    Action OnSave,
    Action OnCancel
);

internal sealed class EditMultiStringDialog : Component<EditMultiStringDialogProps>
{
    public override Element Render()
    {
        return ContentDialog(
            Strings.EditMultiStringTitle,
            VStack(12,
                VStack(4,
                    Text(Strings.ValueName),
                    TextField(Props.ValueName, _ => { })
                        .ReadOnly()
                ),
                VStack(4,
                    Text(Strings.ValueData),
                    TextField(Props.ValueData, Props.OnValueDataChanged)
                        .Set(tb =>
                        {
                            tb.AcceptsReturn = true;
                            tb.TextWrapping = TextWrapping.Wrap;
                        })
                        .Height(200)
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
