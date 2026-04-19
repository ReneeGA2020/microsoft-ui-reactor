using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.Fixtures;

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
            Props ? throw new InvalidOperationException("Boom") : TextBlock("Healthy").AutomationId("HealthyText");
    }

    // Static: error boundary catches render error and shows fallback
    internal static Element CatchesRenderError(RenderContext ctx) =>
        ErrorBoundary(
            Component<ThrowingComponent>(),
            ex => VStack(
                TextBlock("Error caught!").AutomationId("ErrorCaught"),
                TextBlock($"Message: {ex.Message}").AutomationId("ErrorMessage")
            )
        );

    // Interactive: recover from error state
    internal class RecoveryComponent : Component
    {
        public override Element Render()
        {
            var (shouldThrow, setShouldThrow) = UseState(true);

            return VStack(
                ErrorBoundary(
                    Component<ConditionalThrowComponent, bool>(shouldThrow),
                    ex => TextBlock("In error state").AutomationId("ErrorState")
                ),
                Button("Recover", () => setShouldThrow(false)).AutomationId("RecoverBtn")
            );
        }
    }

    internal static Element Recovery(RenderContext ctx) =>
        Component<RecoveryComponent>();
}
