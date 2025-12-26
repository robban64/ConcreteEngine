namespace ConcreteEngine.Shared.Diagnostics;

public readonly struct PerformanceMetric(
    float avgMs,
    float minMs,
    float maxMs,
    float load,
    int allocatedMb,
    float allocRateMbPerSec,
    bool hasSpiked,
    GcActivity gcActivity)
{
    public readonly float AvgMs = avgMs;
    public readonly float MaxMs = maxMs;
    public readonly float MinMs = minMs;
    public readonly float Load = load;
    public readonly int AllocatedMb = allocatedMb;
    public readonly float AllocMbPerSec = allocRateMbPerSec;
    public readonly bool HasSpiked = hasSpiked;
    public readonly GcActivity GcActivity = gcActivity;
}