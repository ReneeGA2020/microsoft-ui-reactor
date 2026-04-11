using Duct;
using Duct.Core;
using Duct.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class LayoutAnimationFixtures
{
    internal class OffsetAnimationSetup(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Animated Item"))
                        .LayoutAnimation()
                        .AutomationId("layout-anim-target")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "layout-anim-target");

            H.Check("LayoutAnim_TargetMounted", target is not null);

            if (target is not null)
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                H.Check("LayoutAnim_HasImplicitAnimations",
                    visual.ImplicitAnimations is not null);

                var hasOffset = false;
                if (visual.ImplicitAnimations is not null)
                {
                    try { var _ = visual.ImplicitAnimations["Offset"]; hasOffset = true; }
                    catch { hasOffset = false; }
                }
                H.Check("LayoutAnim_HasOffsetAnimation", hasOffset);
            }
        }
    }

    internal class SpringAnimationSetup(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Spring Item"))
                        .SpringLayoutAnimation(dampingRatio: 0.8f, period: 0.1f)
                        .AutomationId("spring-anim-target")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "spring-anim-target");

            H.Check("LayoutAnim_SpringTargetMounted", target is not null);

            if (target is not null)
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                H.Check("LayoutAnim_SpringHasImplicitAnimations",
                    visual.ImplicitAnimations is not null);

                var hasOffset = false;
                if (visual.ImplicitAnimations is not null)
                {
                    try { var _ = visual.ImplicitAnimations["Offset"]; hasOffset = true; }
                    catch { hasOffset = false; }
                }
                H.Check("LayoutAnim_SpringHasOffsetAnimation", hasOffset);
            }
        }
    }

    internal class SizeAnimationSetup(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Size Animated"))
                        .LayoutAnimation(new LayoutAnimationConfig { AnimateSize = true })
                        .AutomationId("size-anim-target")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "size-anim-target");

            H.Check("LayoutAnim_SizeTargetMounted", target is not null);

            if (target is not null)
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                H.Check("LayoutAnim_SizeHasImplicitAnimations",
                    visual.ImplicitAnimations is not null);

                var hasOffset = false;
                var hasSize = false;
                if (visual.ImplicitAnimations is not null)
                {
                    try { var _ = visual.ImplicitAnimations["Offset"]; hasOffset = true; }
                    catch { hasOffset = false; }
                    try { var _ = visual.ImplicitAnimations["Size"]; hasSize = true; }
                    catch { hasSize = false; }
                }
                H.Check("LayoutAnim_SizeHasOffsetAnimation", hasOffset);
                H.Check("LayoutAnim_SizeHasSizeAnimation", hasSize);
            }
        }
    }

    internal class ConnectedAnimationMountUnmount(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            // Mount a FlexPanel with items that have ConnectedAnimation keys,
            // then switch to a VStack — the unmount→mount cycle should not crash.
            var host = H.CreateHost();
            var showFlex = true;

            host.Mount(ctx =>
            {
                if (showFlex)
                {
                    return new FlexElement(new Element[]
                    {
                        Border(Text("A")).ConnectedAnimation("ca-test-a").AutomationId("ca-a"),
                        Border(Text("B")).ConnectedAnimation("ca-test-b").AutomationId("ca-b"),
                    });
                }
                else
                {
                    return VStack(
                        Border(Text("A")).ConnectedAnimation("ca-test-a").AutomationId("ca-a2"),
                        Border(Text("B")).ConnectedAnimation("ca-test-b").AutomationId("ca-b2")
                    );
                }
            });

            await Harness.Render();

            H.Check("ConnectedAnim_InitialMounted",
                H.FindText("A") is not null && H.FindText("B") is not null);

            // Toggle to trigger unmount (PrepareToAnimate) → mount (TryStart)
            showFlex = false;
            // Force re-render by remounting
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("A")).ConnectedAnimation("ca-test-a").AutomationId("ca-a2"),
                    Border(Text("B")).ConnectedAnimation("ca-test-b").AutomationId("ca-b2")
                );
            });

            await Harness.Render();

            H.Check("ConnectedAnim_AfterSwitch_Mounted",
                H.FindText("A") is not null && H.FindText("B") is not null);
        }
    }
}
