using Duct;
using Duct.Core;
using static Duct.UI;

namespace DuctOutlook.Components;

internal sealed record ThreePaneSplitProps(
    Element Left,
    Element Center,
    Element Right,
    double LeftInitialWidth = 250,
    double CenterInitialWidth = 350,
    double LeftMinWidth = 180,
    double CenterMinWidth = 250
);

internal sealed class ThreePaneSplit : Component<ThreePaneSplitProps>
{
    public override Element Render()
    {
        var innerSplit = Component<SplitPanel, SplitPanelProps>(new(
            Left: Props.Center,
            Right: Props.Right,
            InitialWidth: Props.CenterInitialWidth,
            MinWidth: Props.CenterMinWidth
        ));

        return Component<SplitPanel, SplitPanelProps>(new(
            Left: Props.Left,
            Right: innerSplit,
            InitialWidth: Props.LeftInitialWidth,
            MinWidth: Props.LeftMinWidth
        ));
    }
}
