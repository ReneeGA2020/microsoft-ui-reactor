namespace Duct.AppTests.Host.SelfTest;

/// <summary>
/// Base class for all self-test fixtures. Each fixture mounts UI, runs checks, and reports TAP results.
/// </summary>
internal abstract class SelfTestFixtureBase
{
    protected Harness H { get; }

    protected SelfTestFixtureBase(Harness harness) => H = harness;

    public abstract Task RunAsync();
}
