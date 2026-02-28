using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Diagnostics.Metrics;

public readonly struct FrameReport(double accTimeMs, double minMs, double maxMs, double avgMs)
{
    public readonly double AccTimeMs = accTimeMs;
    public readonly double MinMs = minMs;
    public readonly double MaxMs = maxMs;
    public readonly double AvgMs = avgMs;
}

public readonly struct FrameMetric(float avgMs,float minMs,float maxMs, bool hasSpiked)
{
    public readonly float AvgMs = avgMs;
    public readonly float MinMs = minMs;
    public readonly float MaxMs = maxMs;
    public readonly bool HasSpiked = hasSpiked;
}