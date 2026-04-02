using Duct;
using Duct.Core;
using Microsoft.UI.Xaml.Controls;
using static Duct.UI;

namespace Duct.EndToEnd.App.Fixtures;

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
            Props ? throw new InvalidOperationException("Boom") : Text("Healthy");
    }

    internal class CatchesRenderError(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                return ErrorBoundary(
                    Component<ThrowingComponent>(),
                    ex => VStack(
                        Text("Error caught!"),
                        Text($"Message: {ex.Message}")
                    )
                );
            });

            await Harness.Render();

            H.Check("ErrorBoundary_CatchesRenderError_FallbackShown",
                H.FindText("Error caught!") is not null);

            H.Check("ErrorBoundary_CatchesRenderError_MessageShown",
                H.FindTextContaining("Component crashed") is not null);

            H.Check("ErrorBoundary_CatchesRenderError_NoRawCrash",
                H.FindText("⚠ Render error") is null);
        }
    }

    internal class Recovery(Harness h) : FixtureBase(h)
    {
        public override async Task RunAsync()
        {
            var host = new DuctHost(H.Window);
            host.Mount(ctx =>
            {
                var (shouldThrow, setShouldThrow) = ctx.UseState(true);

                return VStack(
                    ErrorBoundary(
                        Component<ConditionalThrowComponent, bool>(shouldThrow),
                        ex => Text("In error state")
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
