namespace Duct.EndToEnd.App.Fixtures;

/// <summary>
/// Base class for all E2E test fixtures. Each fixture mounts UI, runs checks, and reports TAP results.
/// </summary>
internal abstract class FixtureBase
{
    protected Harness H { get; }

    protected FixtureBase(Harness harness) => H = harness;

    public abstract Task RunAsync();
}
