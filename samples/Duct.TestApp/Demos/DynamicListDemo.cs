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

class DynamicListDemo : Component
{
    public override Element Render()
    {
        var (count, setCount) = UseState(3);
        var (showIndices, setShowIndices) = UseState(true);

        return VStack(12,
            Heading("Dynamic List"),
            Text("Demonstrates conditional and list rendering"),

            HStack(8,
                Button("Remove", () => setCount(Math.Max(0, count - 1))).Disabled(count == 0),
                Text($"{count} items"),
                Button("Add", () => setCount(count + 1))
            ),

            CheckBox(showIndices, setShowIndices, label: "Show indices"),

            // Dynamic list generated from a range
            VStack(4,
                Enumerable.Range(0, count).Select(i =>
                    Border(
                        HStack(8,
                            When(showIndices, () => Text($"#{i + 1}").SemiBold()),
                            Text($"Item {i + 1}"),
                            Text($"(created dynamically)").Foreground(TertiaryText)
                        )
                    ).CornerRadius(4).Background(SubtleFill).Padding(12, 8)
                ).ToArray()
            ),

            When(count == 0, () => Text("No items. Click Add to create some.").Foreground(TertiaryText)),
            When(count >= 10, () => Text("That's a lot of items!").SemiBold())
        );
    }
}
