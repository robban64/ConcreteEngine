namespace ConcreteEngine.Core.Diagnostics.Metrics;

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