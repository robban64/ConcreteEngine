namespace ConcreteEngine.Core.Diagnostics.Metrics;

public readonly struct FrameReport(double accTimeMs, double minMs, double maxMs, double avgMs)
{
    public readonly double AccTimeMs = accTimeMs;
    public readonly double MinMs = minMs;
    public readonly double MaxMs = maxMs;
    public readonly double AvgMs = avgMs;
}

public readonly struct RuntimeReport(long compiledILBytes, long allocated, GcSample gc)
{
    public readonly long CompiledILBytes = compiledILBytes;
    public readonly long Allocated = allocated;
    public readonly GcSample Gc = gc;
}

public readonly struct FrameMetric(float avgMs, float minMs, float maxMs, int compiledILKb, int allocatedMb, float allocMbPerSec, GcSample gc, GcActivity gcActivity)
{
    public readonly float AvgMs = avgMs;
    public readonly float MinMs = minMs;
    public readonly float MaxMs = maxMs;
    public readonly int CompiledILKb = compiledILKb;
    public readonly int AllocatedMb = allocatedMb;
    public readonly float AllocMbPerSec = allocMbPerSec;
    public readonly GcSample Gc = gc;
    public readonly GcActivity GcActivity = gcActivity;

}