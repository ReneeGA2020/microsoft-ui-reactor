using Duct;
using Duct.Core;
using static Duct.UI;
using Microsoft.UI.Xaml;

DuctApp.Run<App>("TodoApp", width: 800, height: 600);

class App : Component
{
    public override Element Render()
    {
        return Text("Hello, World!").FontSize(24).Margin(20);
    }
}