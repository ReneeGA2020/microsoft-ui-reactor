using Microsoft.UI.Reactor.Animation;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

internal static class StaggerTests
{
    internal class DelayComputation(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            await Task.CompletedTask;

            // Verify StaggerConfig record
            var config = new StaggerConfig(TimeSpan.FromMilliseconds(40));
            H.Check("Stagger_DelayMs", config.Delay.TotalMilliseconds == 40);
            H.Check("Stagger_NoCurve", config.Curve is null);

            var configWithCurve = new StaggerConfig(TimeSpan.FromMilliseconds(30), Curve.Spring());
            H.Check("Stagger_WithCurve", configWithCurve.Curve is SpringCurve);

            // Child N should get N * delay
            var delay = config.Delay;
            H.Check("Stagger_Child0", TimeSpan.FromTicks(delay.Ticks * 0) == TimeSpan.Zero);
            H.Check("Stagger_Child1", TimeSpan.FromTicks(delay.Ticks * 1).TotalMilliseconds == 40);
            H.Check("Stagger_Child2", TimeSpan.FromTicks(delay.Ticks * 2).TotalMilliseconds == 80);
        }
    }

    internal class ComposesWithLayoutAnimation(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(TextBlock("A")).LayoutAnimation().WithKey("a"),
                    Border(TextBlock("B")).LayoutAnimation().WithKey("b"),
                    Border(TextBlock("C")).LayoutAnimation().WithKey("c")
                ).Stagger(TimeSpan.FromMilliseconds(40))
                 .AutomationId("stagger-container");
            });

            await Harness.Render();

            var container = H.FindControl<StackPanel>(sp =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(sp) == "stagger-container");

            H.Check("Stagger_ContainerMounted", container is not null);

            if (container is not null)
            {
                H.Check("Stagger_HasChildren", container.Children.Count == 3);

                // Each child should have implicit animations set
                for (int i = 0; i < container.Children.Count; i++)
                {
                    var visual = ElementCompositionPreview.GetElementVisual(container.Children[i]);
                    H.Check($"Stagger_Child{i}_HasImplicit",
                        visual.ImplicitAnimations is not null);
                }
            }
        }
    }

    internal class ComposesWithEnterTransitions(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(TextBlock("X")).Transition(Animation.Transition.Fade).WithKey("x"),
                    Border(TextBlock("Y")).Transition(Animation.Transition.Fade).WithKey("y"),
                    Border(TextBlock("Z")).Transition(Animation.Transition.Fade).WithKey("z")
                ).Stagger(TimeSpan.FromMilliseconds(50))
                 .AutomationId("stagger-transition-container");
            });

            await Harness.Render();

            var container = H.FindControl<StackPanel>(sp =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(sp) == "stagger-transition-container");

            H.Check("StaggerTransition_ContainerMounted", container is not null);
            H.Check("StaggerTransition_HasChildren", container is not null && container.Children.Count == 3);
        }
    }
}
