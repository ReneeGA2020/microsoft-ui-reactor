using System.Windows;
using CmdPerf.Shared;

namespace CmdPerf.Wpf;

public partial class App : Application
{
    public static CmdCliOptions Options { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Options = CmdCliOptions.Parse(Environment.GetCommandLineArgs());

        if (Options.Headless)
        {
            ConsoleHelper.EnsureConsole();
        }
    }
}
