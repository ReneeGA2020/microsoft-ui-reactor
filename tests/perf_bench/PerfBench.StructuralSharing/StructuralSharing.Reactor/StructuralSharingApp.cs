using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;
using static Microsoft.UI.Reactor.Factories;

namespace PerfBench.StructuralSharing.Reactor;

public class StructuralSharingApp : Component
{
    public static BenchCliOptions Opts { get; set; } = new();

    private const int PanelCount = 5;
    private const int ItemsPerPanel = 50;

    private readonly BenchTracker _tracker = new();
    private readonly Random _rng = new(42);

    private static double[][] CreateInitialPanelData()
    {
        var data = new double[PanelCount][];
        for (int p = 0; p < PanelCount; p++)
            data[p] = new double[ItemsPerPanel];
        return data;
    }

    public override Element Render()
    {
        var (panelData, updatePanelData) = UseReducer(CreateInitialPanelData());
        var (hudText, setHudText) = UseState("");

        UseEffect(() =>
        {
            _tracker.ResetGcBaseline();
            CompositionTarget.Rendering += (_, _) => _tracker.FrameRendered();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
            timer.Tick += (_, _) =>
            {
                _tracker.BeginUpdate();

                int panelIdx = _rng.Next(PanelCount);
                updatePanelData(prev =>
                {
                    var next = new double[PanelCount][];
                    for (int p = 0; p < PanelCount; p++)
                    {
                        if (p == panelIdx)
                        {
                            next[p] = new double[ItemsPerPanel];
                            for (int i = 0; i < ItemsPerPanel; i++)
                                next[p][i] = _rng.NextDouble() * 100.0;
                        }
                        else
                        {
                            next[p] = prev[p];
                        }
                    }
                    return next;
                });

                _tracker.EndUpdate();

                if (!Opts.Headless)
                    setHudText($"FPS: {_tracker.CurrentFps:F0}  Update: {_tracker.LastUpdateMs:F2}ms  Mem: {_tracker.CurrentMemoryMB}MB");
            };
            timer.Start();

            if (Opts.Headless)
            {
                var shutdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Opts.DurationSeconds) };
                shutdown.Tick += (_, _) =>
                {
                    shutdown.Stop();
                    timer.Stop();
                    _tracker.WriteReportFile("EXP3_StructuralSharing_Reactor");
                    Microsoft.UI.Xaml.Application.Current.Exit();
                };
                shutdown.Start();
            }

            return () => timer.Stop();
        });

        // Render 5 panels side by side
        var panels = new Element[PanelCount];
        for (int p = 0; p < PanelCount; p++)
        {
            var items = new Element[ItemsPerPanel + 1];
            items[0] = TextBlock($"Panel {p}").FontSize(12).Bold();
            for (int i = 0; i < ItemsPerPanel; i++)
            {
                items[i + 1] = TextBlock($"Item {i}: {panelData[p][i]:F2}").FontSize(10);
            }
            panels[p] = VStack(2, items);
        }

        return VStack(
            HStack(8, panels),
            Opts.Headless
                ? null!
                : TextBlock(hudText).Foreground("Yellow").FontSize(14)
        );
    }
}
