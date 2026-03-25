using Microsoft.UI.Xaml;

namespace MiniXamlTest;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Title = "Calculator";
        _window.AppWindow.Resize(new Windows.Graphics.SizeInt32(380, 620));
        _window.Activate();
    }
}
