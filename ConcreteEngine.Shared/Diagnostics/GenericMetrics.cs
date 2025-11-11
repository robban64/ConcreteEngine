namespace ConcreteEngine.Shared.Diagnostics;

public readonly struct BasicMetric<TSample>(in TSample sample, MetricHeader header) where TSample : unmanaged
{
    public readonly TSample Sample  = sample;
    public readonly MetricHeader Header  = header;

}

public readonly struct TargetMetric<TSample>(int targetId, in TSample sample, MetricHeader header)
    where TSample : unmanaged
{
    public readonly int TargetId  = targetId;
    public readonly TSample Sample  = sample;
    public readonly MetricHeader Header  = header;

}

public readonly struct TimeTargetMetric<TSample>(
    int targetId,
    long timeStamp,
    in TSample sample,
    MetricHeader header)
    where TSample : unmanaged
{
    public readonly int TargetId  = targetId;
    public readonly long TimeStamp  = timeStamp;
    public readonly TSample Sample  = sample;
    public readonly MetricHeader Header  = header;


}

public readonly struct FrameMetric<TSample>(long frameId, long timeStamp, in TSample sample, MetricHeader header)
    where TSample : unmanaged
{
    public readonly long FrameId  = frameId;
    public readonly long TimeStamp  = timeStamp;
    public readonly TSample Sample  = sample;
    public readonly MetricHeader Header  = header;

}