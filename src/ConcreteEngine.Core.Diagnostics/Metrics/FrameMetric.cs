namespace ConcreteEngine.Core.Diagnostics.Metrics;

public readonly struct FrameReport(double accTimeMs, double minMs, double maxMs)
{
    public readonly double AccTimeMs = accTimeMs;
    public readonly double MinMs = minMs;
    public readonly double MaxMs = maxMs;
}

public readonly struct RuntimeReport(long compiledILBytes, long allocated, GcSample gc)
{
    public readonly long CompiledILBytes = compiledILBytes;
    public readonly long Allocated = allocated;
    public readonly GcSample Gc = gc;
}

public readonly struct FrameMetric(
    Half avgMs,
    Half minMs,
    Half maxMs,
    Half allocMbPerSec,
    ushort compiledILKb,
    ushort allocatedMb,
    GcSample gc,
    GcActivity gcActivity)
{
    public readonly Half AvgMs = avgMs;
    public readonly Half MinMs = minMs;
    public readonly Half MaxMs = maxMs;
    public readonly Half AllocMbPerSec = allocMbPerSec;
    public readonly ushort AllocatedMb = allocatedMb;
    public readonly ushort CompiledILKb = compiledILKb;
    public readonly GcSample Gc = gc;
    public readonly GcActivity GcActivity = gcActivity;
}