namespace ConcreteEngine.Engine.Diagnostics;

internal readonly struct FrameMetricSample(
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
    private readonly Half _avgMs = (Half)avgMs;
    private readonly Half _maxMs = (Half)maxMs;
    private readonly Half _minMs = (Half)minMs;
    private readonly Half _load = (Half)load;
    private readonly Half _allocMbPerSec = (Half)allocRateMbPerSec;
    public readonly bool HasSpiked = hasSpiked;
    public readonly GcActivity GcActivity = gcActivity;

    public float AvgMs => (float)_avgMs;
    public float MaxMs => (float)_maxMs;
    public float MinMs => (float)_minMs;
    public float Load => (float)_load;

    public float AllocMbPerSec => (float)_allocMbPerSec;


    public float Fps => 1000.0f / AvgMs;
}