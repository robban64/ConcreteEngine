namespace ConcreteEngine.Shared.Diagnostics;

public readonly struct PerformanceMetric(
    double avgMs,
    double minMs,
    double maxMs,
    double load,
    long allocBytes,
    double allocRateMbPerSec,
    bool hasSpiked,
    GcActivity gcActivity)
{
    public readonly int AllocatedMb = (int)(allocBytes / 1024 / 1024);
    public readonly float AvgMs = (float)avgMs;
    public readonly float MaxMs = (float)maxMs;
    public readonly float MinMs = (float)minMs;
    public readonly float Load = (float)load;
    public readonly float AllocMbPerSec = (float)allocRateMbPerSec;
    public readonly bool HasSpiked = hasSpiked;
    public readonly GcActivity GcActivity = gcActivity;

    public float Fps => 1000.0f / AvgMs;
}