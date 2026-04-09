namespace Duct;

/// <summary>
/// Snapshot of render loop performance, updated every ~1 second by <see cref="DuctHost"/>.
/// Always available: FPS, frame timing, render count.
/// DEBUG builds add per-reconcile element counters from the last render pass.
/// </summary>
public struct RenderStats
{
    /// <summary>Renders per second over the last measurement window.</summary>
    public double Fps;

    /// <summary>Number of renders in the last measurement window (~1 second).</summary>
    public int RendersInWindow;

    /// <summary>Total renders since the DuctHost was created.</summary>
    public long TotalRenders;

    /// <summary>Average tree-build time (ms) over the last window.</summary>
    public double AvgTreeBuildMs;

    /// <summary>Average reconcile time (ms) over the last window.</summary>
    public double AvgReconcileMs;

    /// <summary>Average effects flush time (ms) over the last window.</summary>
    public double AvgEffectsMs;

    /// <summary>Average total frame time (ms) over the last window (tree + reconcile + effects).</summary>
    public double AvgTotalMs;

#if DEBUG
    /// <summary>Elements diffed in the last reconcile pass. DEBUG only.</summary>
    public int LastDiffed;

    /// <summary>Elements skipped (memo/ShallowEquals) in the last reconcile pass. DEBUG only.</summary>
    public int LastSkipped;

    /// <summary>New UIElements created (mounted) in the last reconcile pass. DEBUG only.</summary>
    public int LastCreated;

    /// <summary>UIElements modified (property updates) in the last reconcile pass. DEBUG only.</summary>
    public int LastModified;
#endif
}
