namespace ConcreteEngine.Shared.Diagnostics;

public readonly record struct BasicMetric<TSample>(in TSample Sample, MetricHeader Header) where TSample : unmanaged;

public readonly record struct TargetMetric<TSample>(int TargetId, in TSample Sample, MetricHeader Header)
    where TSample : unmanaged;

public readonly record struct TimeTargetMetric<TSample>(
    int TargetId,
    long TimeStamp,
    in TSample Sample,
    MetricHeader Header)
    where TSample : unmanaged;

public readonly record struct FrameMetric<TSample>(long FrameId, long TimeStamp, in TSample Sample, MetricHeader Header)
    where TSample : unmanaged;