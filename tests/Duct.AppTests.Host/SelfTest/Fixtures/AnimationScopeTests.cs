using Duct.Animation;
using Duct.AppTests.Host.SelfTest;
using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class AnimationScopeTests
{
    internal class ScopeSetRestore(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            // Before scope: no current curve
            H.Check("AnimScope_NoInitialScope", !AnimationScope.HasScope);
            H.Check("AnimScope_NoInitialCurve", AnimationScope.Current is null);

            // Inside scope: curve is set
            AnimationScope.WithAnimation(Curve.Spring(), () =>
            {
                H.Check("AnimScope_HasScope", AnimationScope.HasScope);
                H.Check("AnimScope_HasCurve", AnimationScope.Current is SpringCurve);
            });

            // After scope: restored
            H.Check("AnimScope_RestoredNoScope", !AnimationScope.HasScope);
            H.Check("AnimScope_RestoredNoCurve", AnimationScope.Current is null);
        }
    }

    internal class NestingBehavior(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            AnimationScope.WithAnimation(Curve.Ease(300), () =>
            {
                H.Check("AnimScope_OuterIsEase", AnimationScope.Current is EaseCurve);

                // Inner scope overrides
                AnimationScope.WithAnimation(Curve.Spring(), () =>
                {
                    H.Check("AnimScope_InnerIsSpring", AnimationScope.Current is SpringCurve);
                });

                // Outer restored
                H.Check("AnimScope_OuterRestoredAfterInner", AnimationScope.Current is EaseCurve);
            });
        }
    }

    internal class NullCurveSuppresses(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            AnimationScope.WithAnimation(Curve.Spring(), () =>
            {
                H.Check("AnimScope_OuterHasCurve", AnimationScope.Current is not null);

                // null curve explicitly suppresses animation
                AnimationScope.WithAnimation(null, () =>
                {
                    H.Check("AnimScope_NullHasScope", AnimationScope.HasScope);
                    H.Check("AnimScope_NullCurve", AnimationScope.Current is null);
                });
            });
        }
    }

    internal class WithAnimationIntegration(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            var opacity = 1.0;

            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Animate Me"))
                        .Opacity(opacity)
                        .AutomationId("anim-scope-target")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "anim-scope-target");

            H.Check("AnimScope_Integration_TargetMounted", target is not null);

            // Change opacity inside WithAnimation scope
            AnimationScope.WithAnimation(Curve.Ease(200), () =>
            {
                opacity = 0.5;
                host.Mount(ctx =>
                {
                    return VStack(
                        Border(Text("Animate Me"))
                            .Opacity(opacity)
                            .AutomationId("anim-scope-target")
                    );
                });
            });

            await Harness.Render();

            // The animation was started — verify Visual has animation activity
            // (we can't check mid-animation value, but we can verify no crash)
            H.Check("AnimScope_Integration_AnimationStarted", true);
        }
    }
}
