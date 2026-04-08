using CmdPerf.Shared;
using Microsoft.UI.Xaml;

namespace CmdPerf.XamlCmd;

public partial class App : Application
{
    public static CmdCliOptions Options { get; private set; } = new();

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Options = CmdCliOptions.Parse(Environment.GetCommandLineArgs());
        if (Options.Headless)
            ConsoleHelper.EnsureConsole();

        var window = new MainWindow();
        window.Activate();
    }
}
