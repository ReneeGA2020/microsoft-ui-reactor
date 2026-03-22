using Duct;
using Duct.Core;
using static Duct.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

DuctApp.Run<Calculator>("Calculator", width: 380, height: 620);

class Calculator : Component
{
    public override Element Render()
    {
        return Grid(["Auto", "*", "Auto"], ["*"],
            Border(Text("Hello, Duct!")).Background("orange").Grid(column: 0),
            Border(Text("Hello, Duct!")).Background("blue").Grid(column:1).VAlign(VerticalAlignment.Stretch),
            Border(Text("Hello, Duct!")).Background("green").Grid(column: 2)
        ).Background("red");
    }
}