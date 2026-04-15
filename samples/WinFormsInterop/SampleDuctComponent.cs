using Duct;
using Duct.Core;
using static Duct.UI;
using XamlAlignment = Microsoft.UI.Xaml.HorizontalAlignment;

namespace WinFormsInterop.Sample;

/// <summary>
/// A Duct component displayed inside a WinForms window via XAML Island.
/// Demonstrates Duct's declarative UI, hooks-based state, and WinUI rendering.
/// </summary>
class SampleDuctComponent : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(0);
        var (name, setName) = UseState("World");
        var (sliderValue, setSliderValue) = UseState(50.0);

        return VStack(
            // ── Header ─────────────────────────────────────
            Text("Duct Component (via XAML Island)")
                .FontSize(22)
                .FontWeight(Microsoft.UI.Text.FontWeights.Bold)
                .Margin(0, 0, 0, 4),

            Text("This Duct/WinUI component is rendered inside a XAML Island hosted in a WinForms window.")
                .Opacity(0.6)
                .Margin(0, 0, 0, 20),

            // ── Counter ────────────────────────────────────
            HStack(
                Button("-", () => setCount(count - 1)),
                Text($"  {count}  ")
                    .FontSize(20)
                    .VAlign(Microsoft.UI.Xaml.VerticalAlignment.Center),
                Button("+", () => setCount(count + 1))
            ).Margin(0, 0, 0, 16),

            // ── Text input ─────────────────────────────────
            HStack(
                Text("Name: ")
                    .VAlign(Microsoft.UI.Xaml.VerticalAlignment.Center),
                TextField(name, setName)
                    .Width(200)
            ).Margin(0, 0, 0, 8),

            Text($"Hello, {name}! (count={count})")
                .FontSize(16)
                .Margin(0, 0, 0, 16),

            // ── Slider ─────────────────────────────────────
            Slider(sliderValue, onChanged: setSliderValue)
                .Width(300)
                .Margin(0, 0, 0, 4),

            Text($"Slider: {sliderValue:F0}%")
                .FontSize(12)
                .Opacity(0.5)
                .Margin(0, 0, 0, 20),

            // ── Visual proof of WinUI rendering ────────────
            Text("WinUI CornerRadius + accent colors:")
                .FontSize(11)
                .Opacity(0.4)
                .Margin(0, 0, 0, 6),

            HStack(
                Enumerable.Range(0, 5).Select(i =>
                    Border(
                        Text($"{i + 1}")
                            .HAlign(XamlAlignment.Center)
                            .VAlign(Microsoft.UI.Xaml.VerticalAlignment.Center)
                    )
                    .CornerRadius(8)
                    .Background(i == count % 5 ? "#0078D4" : "#404040")
                    .Width(50).Height(50)
                    .Margin(4)
                ).ToArray()
            ),

            Text("These rounded boxes use WinUI rendering — not possible in WinForms.")
                .FontSize(11)
                .Opacity(0.35)
                .Margin(0, 8, 0, 0)
        ).Padding(24);
    }
}
