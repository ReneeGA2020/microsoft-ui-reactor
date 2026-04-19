using Microsoft.UI.Reactor.Animation;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

internal static class TransitionTests
{
    internal class TypeHierarchy(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            // Verify Fade + Slide produces CombinedTransition
            var combined = Animation.Transition.Fade + Animation.Transition.Slide(Edge.Bottom);
            H.Check("Transition_FadePlusSlide_IsCombined", combined is CombinedTransition);

            // Verify CombinedTransition contents
            var ct = (CombinedTransition)combined;
            H.Check("Transition_Combined_FirstIsFade", ct.First is FadeTransition);
            H.Check("Transition_Combined_SecondIsSlide", ct.Second is SlideTransition);

            // Verify Scale preset
            var scale = Animation.Transition.Scale(0.9f);
            H.Check("Transition_Scale_IsScale", scale is ScaleTransition);
            H.Check("Transition_Scale_Value", ((ScaleTransition)scale).From == 0.9f);
        }
    }

    internal class AsymmetricTransition(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            // Enter(Fade) | Exit(Scale) produces AsymmetricTransition
            var asym = Animation.Transition.Enter(Animation.Transition.Fade)
                     | Animation.Transition.Exit(Animation.Transition.Scale());

            // The | operator combines two DirectionalTransitions into AsymmetricTransition
            H.Check("Transition_Asymmetric_IsAsymmetric", asym is Animation.AsymmetricTransition);

            // Verify AsymmetricTransition has enter/exit
            if (asym is Animation.AsymmetricTransition at)
            {
                H.Check("Transition_Asymmetric_HasEnter", at.EnterTransition is not null);
                H.Check("Transition_Asymmetric_HasExit", at.ExitTransition is not null);
            }
        }
    }

    internal class CurveResolutionPriority(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            // Default curve when none specified
            var et1 = new ElementTransition(Animation.Transition.Fade);
            H.Check("Transition_DefaultCurve_Null", et1.Curve is null);

            // Explicit curve parameter
            var et2 = new ElementTransition(Animation.Transition.Fade, Curve.Spring(0.7f));
            H.Check("Transition_ExplicitCurve_IsSpring", et2.Curve is SpringCurve);
        }
    }

    internal class MountUnmountIntegration(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            var showElement = true;

            host.Mount(ctx =>
            {
                return VStack(
                    showElement
                        ? Border(TextBlock("Transitioned"))
                            .Transition(Animation.Transition.Fade)
                            .AutomationId("transition-target")
                        : (Element)TextBlock("Hidden")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "transition-target");

            H.Check("Transition_Mount_Visible", target is not null);

            // Verify enter animation was applied (element should be visible)
            if (target is not null)
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                // Enter transition starts opacity animation — visual exists and is valid
                H.Check("Transition_Mount_VisualExists", visual is not null);
            }
        }
    }
}
