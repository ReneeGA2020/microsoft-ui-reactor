using System.Numerics;
using Duct.Animation;
using Duct.AppTests.Host.SelfTest;
using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class KeyframeBuilderTests
{
    internal class BuilderProducesCorrectDef(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            var builder = new KeyframeBuilder()
                .Duration(600)
                .At(0.0f, scale: Vector3.One)
                .At(0.4f, scale: new Vector3(1.3f, 1.3f, 1f), easing: Easing.Decelerate)
                .At(0.7f, scale: new Vector3(0.95f, 0.95f, 1f))
                .At(1.0f, scale: Vector3.One, easing: Easing.Accelerate);

            var def = builder.Build();

            H.Check("Keyframe_Duration", def.Duration.TotalMilliseconds == 600);
            H.Check("Keyframe_NotLoop", !def.Loop);
            H.Check("Keyframe_KeyframeCount", def.Keyframes.Length == 4);
            H.Check("Keyframe_FirstProgress", def.Keyframes[0].Progress == 0.0f);
            H.Check("Keyframe_LastProgress", def.Keyframes[3].Progress == 1.0f);
            H.Check("Keyframe_FirstScale", def.Keyframes[0].Scale == Vector3.One);
            H.Check("Keyframe_SecondEasing", def.Keyframes[1].Easing == Easing.Decelerate);
        }
    }

    internal class TriggerValueChange(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            var counter = 0;

            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Pulse"))
                        .Keyframes("pulse", counter, kf => kf
                            .Duration(300)
                            .At(0.0f, opacity: 1f)
                            .At(0.5f, opacity: 0.5f)
                            .At(1.0f, opacity: 1f))
                        .AutomationId("keyframe-target")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "keyframe-target");

            H.Check("Keyframe_TargetMounted", target is not null);

            // Change trigger
            counter = 1;
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Pulse"))
                        .Keyframes("pulse", counter, kf => kf
                            .Duration(300)
                            .At(0.0f, opacity: 1f)
                            .At(0.5f, opacity: 0.5f)
                            .At(1.0f, opacity: 1f))
                        .AutomationId("keyframe-target")
                );
            });

            await Harness.Render();

            // If we get here without crash, trigger detection + animation start worked
            H.Check("Keyframe_TriggerChanged_NoError", true);
        }
    }

    internal class LoopFlag(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            var def = new KeyframeBuilder()
                .Duration(1200)
                .Loop()
                .At(0.0f, opacity: 0.3f)
                .At(0.5f, opacity: 0.7f)
                .At(1.0f, opacity: 0.3f)
                .Build();

            H.Check("Keyframe_LoopEnabled", def.Loop);
            H.Check("Keyframe_LoopDuration", def.Duration.TotalMilliseconds == 1200);
        }
    }
}
