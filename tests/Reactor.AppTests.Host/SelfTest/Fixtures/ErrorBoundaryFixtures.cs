using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.AppTests.Host.SelfTest;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.SelfTest.Fixtures;

internal static class ErrorBoundaryFixtures
{
    // Component that throws during Render()
    private class ThrowingComponent : Component
    {
        public override Element Render() =>
            throw new InvalidOperationException("Component crashed");
    }

    // Component that can be toggled to throw or not
    private class ConditionalThrowComponent : Component<bool>
    {
        public override Element Render() =>
            Props ? throw new InvalidOperationException("Boom") : TextBlock("Healthy");
    }

    internal class CatchesRenderError(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                return ErrorBoundary(
                    Component<ThrowingComponent>(),
                    ex => VStack(
                        TextBlock("Error caught!"),
                        TextBlock($"Message: {ex.Message}")
                    )
                );
            });

            await Harness.Render();

            H.Check("ErrorBoundary_CatchesRenderError_FallbackShown",
                H.FindText("Error caught!") is not null);

            H.Check("ErrorBoundary_CatchesRenderError_MessageShown",
                H.FindTextContaining("Component crashed") is not null);

            H.Check("ErrorBoundary_CatchesRenderError_NoRawCrash",
                H.FindText("\u26A0 Render error") is null);
        }
    }

    internal class Recovery(Harness h) : SelfTestFixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = H.CreateHost();
            host.Mount(ctx =>
            {
                var (shouldThrow, setShouldThrow) = ctx.UseState(true);

                return VStack(
                    ErrorBoundary(
                        Component<ConditionalThrowComponent, bool>(shouldThrow),
                        ex => TextBlock("In error state")
                    ),
                    Button("Recover", () => setShouldThrow(false))
                );
            });

            await Harness.Render();

            H.Check("ErrorBoundary_Recovery_InitialError",
                H.FindText("In error state") is not null);

            H.ClickButton("Recover");
            await Harness.Render();

            H.Check("ErrorBoundary_Recovery_Recovered",
                H.FindText("Healthy") is not null);

            H.Check("ErrorBoundary_Recovery_ErrorCleared",
                H.FindText("In error state") is null);
        }
    }
}
