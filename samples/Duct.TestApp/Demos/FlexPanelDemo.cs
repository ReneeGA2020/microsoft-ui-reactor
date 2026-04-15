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

class FlexPanelDemo : Component
{
    public override Element Render()
    {
        var (direction, setDirection) = UseState(FlexDirection.Row);
        var (wrap, setWrap) = UseState(FlexWrap.NoWrap);
        var (justify, setJustify) = UseState(FlexJustify.FlexStart);
        var (alignItems, setAlignItems) = UseState(FlexAlign.Stretch);

        return ScrollView(VStack(16,
            Heading("FlexPanel"),
            Text("Interactive CSS Flexbox layout powered by the Yoga engine."),

            // Controls
            HStack(8,
                VStack(4,
                    Text("Direction").SemiBold(),
                    Button("Row", () => setDirection(FlexDirection.Row))
                        .Disabled(direction == FlexDirection.Row),
                    Button("Column", () => setDirection(FlexDirection.Column))
                        .Disabled(direction == FlexDirection.Column),
                    Button("Row Reverse", () => setDirection(FlexDirection.RowReverse))
                        .Disabled(direction == FlexDirection.RowReverse)
                ),
                VStack(4,
                    Text("Wrap").SemiBold(),
                    Button("No Wrap", () => setWrap(FlexWrap.NoWrap))
                        .Disabled(wrap == FlexWrap.NoWrap),
                    Button("Wrap", () => setWrap(FlexWrap.Wrap))
                        .Disabled(wrap == FlexWrap.Wrap)
                ),
                VStack(4,
                    Text("Justify Content").SemiBold(),
                    Button("Start", () => setJustify(FlexJustify.FlexStart))
                        .Disabled(justify == FlexJustify.FlexStart),
                    Button("Center", () => setJustify(FlexJustify.Center))
                        .Disabled(justify == FlexJustify.Center),
                    Button("Space Between", () => setJustify(FlexJustify.SpaceBetween))
                        .Disabled(justify == FlexJustify.SpaceBetween),
                    Button("Space Evenly", () => setJustify(FlexJustify.SpaceEvenly))
                        .Disabled(justify == FlexJustify.SpaceEvenly)
                ),
                VStack(4,
                    Text("Align Items").SemiBold(),
                    Button("Stretch", () => setAlignItems(FlexAlign.Stretch))
                        .Disabled(alignItems == FlexAlign.Stretch),
                    Button("Center", () => setAlignItems(FlexAlign.Center))
                        .Disabled(alignItems == FlexAlign.Center),
                    Button("Start", () => setAlignItems(FlexAlign.FlexStart))
                        .Disabled(alignItems == FlexAlign.FlexStart),
                    Button("End", () => setAlignItems(FlexAlign.FlexEnd))
                        .Disabled(alignItems == FlexAlign.FlexEnd)
                )
            ),

            Caption($"Direction={direction}  Wrap={wrap}  Justify={justify}  Align={alignItems}")
                .Foreground(TertiaryText),

            // Live flex container
            SubHeading("Live Preview"),
            Border(
                new FlexElement([
                    ColorBox("A", "#4A90D9").Flex(grow: 1).Size(80, 60),
                    ColorBox("B", "#50B86C").Flex(grow: 2).Size(80, 80),
                    ColorBox("C", "#E8834A").Flex(grow: 1).Size(80, 40),
                    ColorBox("D", "#9B59B6").Size(80, 50),
                    ColorBox("E", "#E74C3C").Flex(grow: 1).Size(80, 70),
                ])
                {
                    Direction = direction,
                    Wrap = wrap,
                    JustifyContent = justify,
                    AlignItems = alignItems,
                    ColumnGap = 8,
                    RowGap = 8,
                }
            ).Background(SubtleFill).CornerRadius(8).Padding(8).Height(300),

            // Grow ratio demo
            SubHeading("Grow Ratios"),
            Text("Items with grow 1 : 2 : 1 share available space proportionally."),
            new FlexElement([
                ColorBox("1x", "#4A90D9").Flex(grow: 1).Height(50),
                ColorBox("2x", "#50B86C").Flex(grow: 2).Height(50),
                ColorBox("1x", "#E8834A").Flex(grow: 1).Height(50),
            ])
            {
                Direction = FlexDirection.Row,
                ColumnGap = 8,
            },

            // Wrap demo
            SubHeading("Wrap"),
            Text("Eight items with wrap enabled flow onto multiple lines."),
            new FlexElement(
                Enumerable.Range(1, 8).Select(i =>
                    ColorBox($"{i}", i % 2 == 0 ? "#4A90D9" : "#E8834A")
                        .Size(120, 50).Flex(grow: 1)
                ).ToArray()
            )
            {
                Direction = FlexDirection.Row,
                Wrap = FlexWrap.Wrap,
                ColumnGap = 8,
                RowGap = 8,
            }
        ));
    }

    static Element ColorBox(string label, string color) =>
        Border(
            Text(label).SemiBold().HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center)
        ).Background(color).CornerRadius(4).Padding(8);
}
