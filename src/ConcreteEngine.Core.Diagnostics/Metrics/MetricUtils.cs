namespace ConcreteEngine.Core.Diagnostics.Metrics;

public static class MetricUtils
{
    public static GcSample CollectGcSample()
    {
        return new GcSample(GC.GetAllocatedBytesForCurrentThread(), GC.CollectionCount(0), GC.CollectionCount(1),
            GC.CollectionCount(2));
    }
}