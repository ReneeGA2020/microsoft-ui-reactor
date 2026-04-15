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

class ConditionalDemo : Component
{
    enum ViewMode { Simple, Detailed, Custom }

    public override Element Render()
    {
        var (showAdvanced, setShowAdvanced) = UseState(false);
        var (enableFeatureA, setFeatureA) = UseState(false);
        var (enableFeatureB, setFeatureB) = UseState(false);
        var (viewMode, setViewMode) = UseReducer(ViewMode.Simple);
        var (itemCount, setItemCount) = UseState(3);

        return ScrollView(VStack(16,
            Heading("Conditional UI"),
            Text("Every piece of UI below is driven by plain C# expressions."),
            Text("Check the boxes and watch entire sub-trees appear and disappear."),

            // 1. Simple if/else via checkbox
            SubHeading("1. Checkbox toggles a sub-tree"),
            CheckBox(showAdvanced, setShowAdvanced, label: "Show advanced options"),

            // This is just a C# ternary — when false, the whole VStack is gone
            showAdvanced
                ? Border(
                    VStack(8,
                        Text("Advanced Settings").SemiBold(),
                        CheckBox(enableFeatureA, setFeatureA, label: "Enable Feature A"),
                        CheckBox(enableFeatureB, setFeatureB, label: "Enable Feature B"),

                        // Nested conditionals — each feature shows its own config
                        enableFeatureA
                            ? Border(
                                VStack(4,
                                    Text("Feature A Configuration").SemiBold(),
                                    Text("This sub-tree only exists when Feature A is checked."),
                                    Slider(50, 0, 100).Width(200)
                                )
                              ).CornerRadius(4).Background(SubtleFill).Padding(12)
                            : null,

                        enableFeatureB
                            ? Border(
                                VStack(4,
                                    Text("Feature B Configuration").SemiBold(),
                                    Text("This sub-tree only exists when Feature B is checked."),
                                    ToggleSwitch(false, null, onContent: "On", offContent: "Off")
                                )
                              ).CornerRadius(4).Background(SubtleFill).Padding(12)
                            : null
                    )
                  ).CornerRadius(8).Background(SubtleFill).Padding(16)
                : Text("Check the box above to reveal advanced options.").Foreground(TertiaryText),

            // 2. Switch expression -> completely different sub-trees
            SubHeading("2. Switch expression picks a sub-tree"),
            HStack(8,
                Button("Simple", () => setViewMode(_ => ViewMode.Simple))
                    .Disabled(viewMode == ViewMode.Simple),
                Button("Detailed", () => setViewMode(_ => ViewMode.Detailed))
                    .Disabled(viewMode == ViewMode.Detailed),
                Button("Custom", () => setViewMode(_ => ViewMode.Custom))
                    .Disabled(viewMode == ViewMode.Custom)
            ),

            // Each branch renders a COMPLETELY different control tree
            viewMode switch
            {
                ViewMode.Simple => VStack(4,
                    Text("Simple view — just a summary."),
                    Text($"{itemCount} items in the list.")
                ),

                ViewMode.Detailed => VStack(4,
                    Text("Detailed view — shows every item:").SemiBold(),
                    ForEach(
                        Enumerable.Range(1, itemCount),
                        i => HStack(4,
                            Text($"Item {i}").Width(80),
                            Progress(i * 100.0 / itemCount).Width(150)
                        )
                    )
                ),

                ViewMode.Custom => VStack(8,
                    Text("Custom view — configure the list:").SemiBold(),
                    HStack(8,
                        Text("Item count:"),
                        Slider(itemCount, 1, 10, v => setItemCount((int)v)).Width(200),
                        Text($"{itemCount}")
                    ),
                    ForEach(
                        Enumerable.Range(1, itemCount),
                        i => Border(
                            Text($"Custom item {i}")
                        ).CornerRadius(4).Background(SubtleFill).Padding(8, 4)
                    )
                ),

                _ => Empty()
            },

            // 3. Inline computed UI
            SubHeading("3. Computed UI from expressions"),
            Text("The UI below is generated by a C# expression — no templates needed:"),

            VStack(4,
                // A simple computed summary based on current state
                Text($"Advanced: {(showAdvanced ? "ON" : "OFF")}, " +
                     $"Features: {(enableFeatureA ? "A" : "")}{(enableFeatureB ? "B" : "")}{(!enableFeatureA && !enableFeatureB ? "none" : "")}, " +
                     $"View: {viewMode}")
                    .Foreground(SecondaryText),

                // Conditional warning
                When(showAdvanced && enableFeatureA && enableFeatureB,
                    () => Border(
                        Text("Warning: Both features enabled simultaneously may cause conflicts.")
                    ).CornerRadius(4).Background(Theme.Ref("SystemFillColorCautionBackgroundBrush")).Padding(12)
                )
            )
        ));
    }
}
