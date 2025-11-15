namespace ConcreteEngine.Shared.Diagnostics;

public readonly struct TargetMetric(int targetId, MetricHeader header)
{
    public readonly int TargetId = targetId;
    public readonly MetricHeader Header = header;
}

public readonly struct TimeTargetMetric(
    int targetId,
    long timeStamp,
    MetricHeader header)
{
    public readonly long TimeStamp = timeStamp;
    public readonly int TargetId = targetId;
    public readonly MetricHeader Header = header;
}

public readonly struct FrameMetric(long frameId, long timeStamp, MetricHeader header)
{
    public readonly long FrameId = frameId;
    public readonly long TimeStamp = timeStamp;
    public readonly MetricHeader Header = header;
}