using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using static Duct.UI;

namespace DuctHostControlDemo;

/// <summary>
/// A Duct component that demonstrates state, effects, and interactive controls —
/// all hosted inside a DuctHostControl in a vanilla WinUI XAML window.
/// </summary>
public class CounterDemo : Component
{
    public override Element Render()
    {
        // threadSafe: true because the timer fires on a thread pool thread
        var (count, setCount) = UseState(0, threadSafe: true);
        var (step, setStep) = UseState(1.0);
        var (auto, setAuto) = UseState(false);

        // Auto-increment effect: starts/stops a timer based on the toggle
        UseEffect(() =>
        {
            if (!auto) return () => { };

            var currentStep = (int)step;
            var timer = new Timer(_ => setCount(count + currentStep), null, 500, 500);
            return () => timer.Dispose();
        }, auto, step);

        return VStack(12,
            Text("Counter").FontSize(20).Bold().Margin(16, 16, 16, 0),

            Text($"{count}")
                .FontSize(48).Bold()
                .HAlign(HorizontalAlignment.Center)
                .Margin(0, 8),

            HStack(8,
                Button("-", () => setCount(count - (int)step)).Width(60),
                Button("Reset", () => setCount(0)),
                Button("+", () => setCount(count + (int)step)).Width(60)
            ).HAlign(HorizontalAlignment.Center),

            Slider(step, min: 1, max: 10, onChanged: v => setStep(v))
                .Margin(16, 8),

            Text($"Step size: {(int)step}").HAlign(HorizontalAlignment.Center),

            ToggleSwitch(auto, onChanged: setAuto,
                onContent: "Auto ON", offContent: "Auto OFF",
                header: "Auto-increment")
                .Margin(16, 8, 16, 16)
        );
    }
}
