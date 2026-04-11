using Duct;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Duct.AppTests.Host.SelfTest;

/// <summary>
/// Runs all self-test fixtures in sequence, mounts each in a DuctHost,
/// calls RunAsync(), captures TAP output, exits with 0/1.
/// </summary>
internal static class SelfTestRunner
{
    public static void RunAll()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new DuctApplication();
            var dispatcher = DispatcherQueue.GetForCurrentThread();

            var window = new Window { Title = "Duct Self-Test" };
            window.AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
            var harness = new Harness(window);

            dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    var fixtures = SelfTestFixtureRegistry.AllFixtures;
                    harness.SetupTitleBar(fixtures.Length);
                    window.Activate();
                    await Harness.Render(500); // wait for initial layout

                    Console.WriteLine($"TAP version 14");
                    Console.WriteLine($"1..{fixtures.Length}");

                    int testIndex = 0;
                    foreach (var fixtureName in fixtures)
                    {
                        testIndex++;
                        harness.UpdateProgress(testIndex, fixtureName);
                        try
                        {
                            var fixture = SelfTestFixtureRegistry.Create(fixtureName, harness);
                            if (fixture is null)
                            {
                                Console.WriteLine($"not ok {testIndex} {fixtureName} - fixture not found");
                                continue;
                            }

                            Console.WriteLine($"# Running: {fixtureName}");
                            await fixture.RunAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"not ok {testIndex} {fixtureName}_CRASH - {ex.GetType().Name}: {ex.Message}");
                            Console.Error.WriteLine(ex.ToString());
                        }
                    }

                    Console.WriteLine($"# Total failures: {harness.Failures}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bail out! {ex.GetType().Name}: {ex.Message}");
                    Console.Error.WriteLine(ex.ToString());
                }
                finally
                {
                    Console.Out.Flush();
                    Environment.Exit(harness.Failures > 0 ? 1 : 0);
                }
            });
        });
    }
}
