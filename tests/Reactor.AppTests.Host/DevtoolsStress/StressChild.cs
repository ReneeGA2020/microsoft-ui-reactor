using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Microsoft.UI.Reactor.AppTests.Host.DevtoolsStress;

/// <summary>
/// Minimal Reactor component hosted by the E2E stress child process. Kept
/// deliberately trivial so each iteration's cold start is dominated by
/// framework/devtools init cost, not render cost.
/// </summary>
internal sealed class StressChild : Component
{
    public override Element Render() => VStack(
        TextBlock($"stress child — pid {Environment.ProcessId}"),
        TextBlock("devtools MCP up; parent will validate and terminate")
    );
}
