using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Diagnostics.Metrics;

[StructLayout(LayoutKind.Sequential)]
public readonly struct FrameMetrics(float avgMs,float minMs,float maxMs, bool hasSpiked)
{
    public readonly float AvgMs = avgMs;
    public readonly float MinMs = minMs;
    public readonly float MaxMs = maxMs;
    public readonly bool HasSpiked = hasSpiked;
}