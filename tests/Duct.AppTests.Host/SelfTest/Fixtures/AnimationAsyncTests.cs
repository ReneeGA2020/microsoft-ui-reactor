using Duct.Animation;
using Duct.AppTests.Host.SelfTest;
using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using static Duct.UI;

namespace Duct.AppTests.Host.SelfTest.Fixtures;

internal static class AnimationAsyncTests
{
    internal class TaskCompletes(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Async Target"))
                        .AutomationId("async-target")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "async-target");

            H.Check("AsyncAnim_TargetMounted", target is not null);

            // Ensure compositor provider has a reference
            if (target is not null)
                CompositorProvider.EnsureCompositor(target);

            // WithAnimationAsync should return a task
            var task = AnimationScope.WithAnimationAsync(Curve.Ease(50), () =>
            {
                // No actual property changes — batch should complete immediately
            });

            H.Check("AsyncAnim_TaskNotNull", task is not null);

            // Wait for completion (with timeout)
            var completed = await Task.WhenAny(task, Task.Delay(2000));
            H.Check("AsyncAnim_TaskCompleted", completed == task);
        }
    }

    internal class SequentialAwait(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Seq Target"))
                        .AutomationId("seq-target")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "seq-target");

            if (target is not null)
                CompositorProvider.EnsureCompositor(target);

            // Sequential await — second should start after first
            var order = new List<int>();

            var task1 = AnimationScope.WithAnimationAsync(Curve.Ease(10), () =>
            {
                order.Add(1);
            });

            await task1;
            order.Add(2);

            var task2 = AnimationScope.WithAnimationAsync(Curve.Ease(10), () =>
            {
                order.Add(3);
            });

            await task2;
            order.Add(4);

            H.Check("AsyncAnim_SequentialOrder",
                order.Count == 4 && order[0] == 1 && order[1] == 2 && order[2] == 3 && order[3] == 4);
        }
    }

    internal class ParallelWhenAll(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return VStack(
                    Border(Text("Parallel Target"))
                        .AutomationId("parallel-target")
                );
            });

            await Harness.Render();

            var target = H.FindControl<Border>(b =>
                Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(b) == "parallel-target");

            if (target is not null)
                CompositorProvider.EnsureCompositor(target);

            // Parallel with Task.WhenAll
            var t1 = AnimationScope.WithAnimationAsync(Curve.Ease(10), () => { });
            var t2 = AnimationScope.WithAnimationAsync(Curve.Ease(10), () => { });

            var allTask = Task.WhenAll(t1, t2);
            var completed = await Task.WhenAny(allTask, Task.Delay(2000));
            H.Check("AsyncAnim_ParallelCompleted", completed == allTask);
        }
    }
}
