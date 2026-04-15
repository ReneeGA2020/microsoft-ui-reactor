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

class CounterDemo : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        var (step, setStep) = UseState(1);

        return VStack(12,
            Heading("Counter"),
            SubHeading($"Current count: {count}"),

            HStack(8,
                Button($"- {step}", () => setCount(count - step)),
                Button("Reset", () => setCount(0)).Disabled(count == 0),
                Button($"+ {step}", () => setCount(count + step))
            ),

            HStack(8,
                Text("Step size:"),
                Slider(step, 1, 10, v => setStep((int)v)).Width(200),
                Text($"{step}")
            ),

            // Conditional rendering — shows different messages based on count
            count switch
            {
                0 => Text("Try clicking the buttons!").Foreground(TertiaryText),
                > 0 and < 10 => Text("Going up..."),
                >= 10 and < 50 => Text("Getting bigger!").SemiBold(),
                >= 50 => Text("That's a LOT!").SemiBold(),
                < 0 and > -10 => Text("Going negative..."),
                _ => Text("Way down there!").SemiBold()
            }
        );
    }
}
