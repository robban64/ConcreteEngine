using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Diagnostics.Metrics;



[StructLayout(LayoutKind.Sequential)]
public readonly struct PerformanceMetric(
    float avgMs,
    float minMs,
    float maxMs,
    float load,
    int allocatedMb,
    float allocRateMbPerSec,
    GcSample gc,
    bool hasSpiked,
    GcActivity gcActivity)
{
    public readonly float AvgMs = avgMs;
    public readonly float MaxMs = maxMs;
    public readonly float MinMs = minMs;
    public readonly float Load = load;
    public readonly int AllocatedMb = allocatedMb;
    public readonly float AllocMbPerSec = allocRateMbPerSec;
    public readonly GcSample Gc = gc;
    public readonly GcActivity GcActivity = gcActivity;
    public readonly bool HasSpiked = hasSpiked;
}

[StructLayout(LayoutKind.Sequential)]
public record struct PerformanceSnapshot(
    float AvgMs,
    float MinMs,
    float MaxMs,
    float Load,
    int AllocatedMb,
    float MaxAllocRate)
{
    public static PerformanceSnapshot FromMetric(in PerformanceMetric m) =>
        new(m.AvgMs, m.MinMs, m.MaxMs, m.Load, m.AllocatedMb, m.AllocMbPerSec);
}