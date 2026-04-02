using Duct;
using Duct.Core;
using Duct.EndToEnd.App;
using Duct.EndToEnd.App.Fixtures;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

// Parse arguments
string? fixtureName = null;
bool isTest = false;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--test") isTest = true;
    if (args[i] == "--fixture" && i + 1 < args.Length) fixtureName = args[i + 1];
}

if (!isTest || fixtureName is null)
{
    Console.Error.WriteLine("Usage: Duct.EndToEnd.App.exe --test --fixture <FixtureName>");
    Console.Error.WriteLine("Available fixtures:");
    foreach (var name in FixtureRegistry.AllFixtures)
        Console.Error.WriteLine($"  {name}");
    Environment.Exit(2);
    return;
}

// Launch WinUI app, run the fixture, exit
WinRT.ComWrappersSupport.InitializeComWrappers();
Application.Start(_ =>
{
    var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
    SynchronizationContext.SetSynchronizationContext(context);
    new DuctApplication();
    var dispatcher = DispatcherQueue.GetForCurrentThread();

    var window = new Window { Title = $"E2E: {fixtureName}" };
    window.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
    var harness = new Harness(window);

    dispatcher.TryEnqueue(async () =>
    {
        try
        {
            window.Activate();
            await Harness.Render(500); // wait for initial layout

            var fixture = FixtureRegistry.Create(fixtureName!, harness);
            if (fixture is null)
            {
                Console.WriteLine($"not ok {fixtureName} - fixture not found");
                Environment.Exit(2);
                return;
            }

            await fixture.RunAsync();
            await harness.CaptureScreenshotAsync(fixtureName!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"not ok {fixtureName}_CRASH - {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine(ex.ToString());
        }
        finally
        {
            Console.Out.Flush();
            Environment.Exit(harness.Failures > 0 ? 1 : 0);
        }
    });
});
