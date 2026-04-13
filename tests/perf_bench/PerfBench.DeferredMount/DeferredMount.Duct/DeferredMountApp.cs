using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;
using static Duct.UI;

namespace PerfBench.DeferredMount.Duct;

public class DeferredMountApp : Component
{
    public static BenchCliOptions Opts { get; set; } = new();

    private const int TabCount = 5;
    private const int ItemsPerTab = 200;

    private readonly BenchTracker _tracker = new();

    public override Element Render()
    {
        var (activeTab, setActiveTab) = UseState(0);
        var (hudText, setHudText) = UseState("");

        UseEffect(() =>
        {
            _tracker.ResetGcBaseline();
            _tracker.BeginMount();

            CompositionTarget.Rendering += (_, _) =>
            {
                _tracker.FrameRendered();
                if (!Opts.Headless)
                    setHudText($"FPS: {_tracker.CurrentFps:F0}  Block: {_tracker.LongestFrameBlockMs:F1}ms  Mem: {_tracker.CurrentMemoryMB}MB");
            };

            _tracker.EndMount();

            if (Opts.Headless)
            {
                int switchIndex = 1;
                var switchTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                switchTimer.Tick += (_, _) =>
                {
                    switchTimer.Stop();
                    var tabTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                    tabTimer.Tick += (_, _) =>
                    {
                        if (switchIndex < TabCount)
                        {
                            _tracker.BeginUpdate();
                            setActiveTab(switchIndex);
                            _tracker.EndUpdate();
                            switchIndex++;
                        }
                    };
                    tabTimer.Start();
                };
                switchTimer.Start();

                var shutdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Opts.DurationSeconds) };
                shutdown.Tick += (_, _) =>
                {
                    shutdown.Stop();
                    _tracker.WriteReportFile("EXP9_DeferredMount_Duct");
                    Microsoft.UI.Xaml.Application.Current.Exit();
                };
                shutdown.Start();
            }
        });

        // Tab buttons
        var tabButtons = new Element[TabCount];
        for (int t = 0; t < TabCount; t++)
        {
            int tabIndex = t;
            tabButtons[t] = Button($"Tab {t}", () =>
            {
                _tracker.BeginUpdate();
                setActiveTab(tabIndex);
                _tracker.EndUpdate();
            });
        }

        // Active tab content: 200 items
        var tabItems = new Element[ItemsPerTab];
        for (int i = 0; i < ItemsPerTab; i++)
            tabItems[i] = Text($"Tab {activeTab} - Item {i}").FontSize(10);

        var children = new List<Element?>
        {
            HStack(tabButtons),
            ScrollView(VStack(tabItems))
        };

        if (!Opts.Headless)
            children.Add(Text(hudText).Foreground("Yellow").FontSize(14));

        return VStack(children.ToArray());
    }
}
