using Duct;
using Duct.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using PerfBench.Shared;
using static Duct.UI;

namespace PerfBench.TimeSlice.Duct;

public class TimeSliceApp : Component
{
    public static BenchCliOptions Opts { get; set; } = new();

    private const int ItemCount = 2000;
    private const double BallSize = 30;

    private readonly BenchTracker _tracker = new();

    public override Element Render()
    {
        var (mounted, setMounted) = UseState(false);
        var (hudText, setHudText) = UseState("");
        var ballX = UseRef(50.0);
        var ballY = UseRef(50.0);
        var ballDx = UseRef(3.0);
        var ballDy = UseRef(2.0);
        var (ballLeft, setBallLeft) = UseState(50.0);
        var (ballTop, setBallTop) = UseState(50.0);

        UseEffect(() =>
        {
            _tracker.ResetGcBaseline();

            CompositionTarget.Rendering += (_, _) =>
            {
                _tracker.FrameRendered();

                // Update bouncing ball position
                ballX.Current += ballDx.Current;
                ballY.Current += ballDy.Current;

                if (ballX.Current <= 0 || ballX.Current >= 400 - BallSize) ballDx.Current = -ballDx.Current;
                if (ballY.Current <= 0 || ballY.Current >= 200 - BallSize) ballDy.Current = -ballDy.Current;

                ballX.Current = Math.Clamp(ballX.Current, 0, 400 - BallSize);
                ballY.Current = Math.Clamp(ballY.Current, 0, 200 - BallSize);

                setBallLeft(ballX.Current);
                setBallTop(ballY.Current);

                if (!Opts.Headless)
                    setHudText($"FPS: {_tracker.CurrentFps:F0}  Block: {_tracker.LongestFrameBlockMs:F1}ms  Drops: {_tracker.AnimationDrops}  Mem: {_tracker.CurrentMemoryMB}MB");
            };

            // Trigger mount
            _tracker.BeginMount();
            setMounted(true);
            _tracker.EndMount();

            if (Opts.Headless)
            {
                var shutdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Opts.DurationSeconds) };
                shutdown.Tick += (_, _) =>
                {
                    shutdown.Stop();
                    _tracker.WriteReportFile("EXP7_TimeSlice_Duct");
                    Microsoft.UI.Xaml.Application.Current.Exit();
                };
                shutdown.Start();
            }
        });

        // Build child elements
        var children = new List<Element?>();

        // Canvas with bouncing ball - matches Direct/Bound variants
        var ball = Ellipse()
            .Width(BallSize).Height(BallSize)
            .Fill(new SolidColorBrush(Microsoft.UI.Colors.OrangeRed))
            .Canvas(left: ballLeft, top: ballTop);

        var canvasEl = Canvas(ball);
        // Set canvas background and height via record init
        canvasEl = canvasEl with
        {
            Background = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray),
            Height = 200
        };
        children.Add(canvasEl);

        // Mount 2000 items when mounted
        if (mounted)
        {
            for (int i = 0; i < ItemCount; i++)
            {
                children.Add(Text($"Item {i}: mounted").FontSize(10));
            }
        }

        // HUD
        if (!Opts.Headless)
            children.Add(Text(hudText).Foreground("Yellow").FontSize(14));

        return ScrollView(VStack(children.ToArray()));
    }
}
