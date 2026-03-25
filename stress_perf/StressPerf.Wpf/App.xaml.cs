using System.Windows;
using StressPerf.Shared;

namespace StressPerf.Wpf;

public partial class App : Application
{
    public static CliOptions Options { get; set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Options = CliOptions.Parse(e.Args);

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }
}
