using Duct.Animation;
using Duct.AppTests.Host.SelfTest;
using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class AnimateModifierTests
{
    internal class ImplicitAnimationsCreated(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Animated"))
                        .Animate(Curve.Spring(0.65f))
                        .AutomationId("animate-target")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "animate-target");

            H.Check("Animate_TargetMounted", target is not null);

            if (target is not null)
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                H.Check("Animate_HasImplicitAnimations",
                    visual.ImplicitAnimations is not null);

                if (visual.ImplicitAnimations is not null)
                {
                    var hasOpacity = false;
                    var hasScale = false;
                    var hasRotation = false;
                    try { var _ = visual.ImplicitAnimations["Opacity"]; hasOpacity = true; } catch { }
                    try { var _ = visual.ImplicitAnimations["Scale"]; hasScale = true; } catch { }
                    try { var _ = visual.ImplicitAnimations["RotationAngle"]; hasRotation = true; } catch { }

                    H.Check("Animate_HasOpacity", hasOpacity);
                    H.Check("Animate_HasScale", hasScale);
                    H.Check("Animate_HasRotation", hasRotation);
                }
            }
        }
    }

    internal class TargetedProperties(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Targeted"))
                        .Animate(Curve.Ease(200), AnimateProperty.Opacity | AnimateProperty.Scale)
                        .AutomationId("animate-targeted")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "animate-targeted");

            H.Check("AnimateTargeted_Mounted", target is not null);

            if (target is not null)
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                H.Check("AnimateTargeted_HasImplicit", visual.ImplicitAnimations is not null);

                if (visual.ImplicitAnimations is not null)
                {
                    var hasOpacity = false;
                    var hasScale = false;
                    var hasRotation = false;
                    try { var _ = visual.ImplicitAnimations["Opacity"]; hasOpacity = true; } catch { }
                    try { var _ = visual.ImplicitAnimations["Scale"]; hasScale = true; } catch { }
                    try { var _ = visual.ImplicitAnimations["RotationAngle"]; hasRotation = true; } catch { }

                    H.Check("AnimateTargeted_HasOpacity", hasOpacity);
                    H.Check("AnimateTargeted_HasScale", hasScale);
                    H.Check("AnimateTargeted_NoRotation", !hasRotation);
                }
            }
        }
    }

    internal class MergesWithLayoutAnimation(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Merged"))
                        .LayoutAnimation()
                        .Animate(Curve.Spring(0.8f))
                        .AutomationId("animate-merged")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "animate-merged");

            H.Check("AnimateMerge_Mounted", target is not null);

            if (target is not null)
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                H.Check("AnimateMerge_HasImplicit", visual.ImplicitAnimations is not null);

                if (visual.ImplicitAnimations is not null)
                {
                    // Should have both layout (Offset) and property (Opacity, Scale, Rotation)
                    var hasOffset = false;
                    var hasOpacity = false;
                    try { var _ = visual.ImplicitAnimations["Offset"]; hasOffset = true; } catch { }
                    try { var _ = visual.ImplicitAnimations["Opacity"]; hasOpacity = true; } catch { }

                    H.Check("AnimateMerge_HasOffset", hasOffset);
                    H.Check("AnimateMerge_HasOpacity", hasOpacity);
                }
            }
        }
    }
}
