// Minimal Duct app that stays alive for UIA probing.
// Exposes a variety of controls with AutomationName and AutomationId.
// NOTE: No [STAThread] — DuctApp.Run handles STA automatically.

using Duct;
using Duct.Core;
using static Duct.UI;

DuctApp.Run<UiaTestRoot>("Duct UIA Test", width: 600, height: 400);

class UiaTestRoot : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        var (text, setText) = UseState("");
        var (isChecked, setChecked) = UseState(false);

        return VStack(8,
            Text("UIA Test App")
                .AutomationName("HeaderText")
                .AutomationId("HeaderTextId"),

            Text($"Count: {count}")
                .AutomationName("CountDisplay")
                .AutomationId("CountDisplayId"),

            HStack(8,
                Button("Increment", () => setCount(count + 1))
                    .AutomationName("IncrementButton")
                    .AutomationId("IncrementButtonId"),

                Button("Reset", () => setCount(0))
                    .AutomationName("ResetButton")
                    .AutomationId("ResetButtonId")
            ),

            TextField(text, v => setText(v), placeholder: "Type here...")
                .AutomationName("InputField")
                .AutomationId("InputFieldId"),

            CheckBox(isChecked, v => setChecked(v), "Toggle me")
                .AutomationName("TestCheckBox")
                .AutomationId("TestCheckBoxId"),

            Text(isChecked ? "Checked!" : "Not checked")
                .AutomationName("CheckStatus")
                .AutomationId("CheckStatusId")

        ).Margin(16);
    }
}
