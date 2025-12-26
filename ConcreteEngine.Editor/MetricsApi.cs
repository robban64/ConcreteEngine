using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Editor;

internal interface IMetricProvider
{
    bool Enabled { get; }
    bool HasFetched { get; }
    void SetIntervalTicks(long intervalTicks);
    void Tick(long currentTicks);
}

internal sealed class MetricProvider<T>(Func<T> fetch, long intervalTicks) : IMetricProvider 
{
    private long _intervalTicks = intervalTicks;
    private long _lastUpdate = -1;

    public T Data;

    public bool Enabled { get; }
    public bool HasFetched => _lastUpdate > 0;

    public void SetIntervalTicks(long intervalTicks) => _intervalTicks = intervalTicks;

    public void Tick(long currentTicks)
    {
        if (currentTicks - _lastUpdate > _intervalTicks)
        {
            Data = fetch();
            _lastUpdate = currentTicks;
        }
    }
}


public static class MetricsApi
{
    private static readonly List<IMetricProvider> _allProviders = new(8);
    
    private static long _currentTick = -1;

    public static class Provider<T> 
    {
        internal static MetricProvider<T>? Record;

        public static void Register(Func<T> fetch, int intervalTicks)
        {
            if (Record != null) _allProviders.Remove(Record);            
            Record = new MetricProvider<T>(fetch,  intervalTicks);
            _allProviders.Add(Record);
        }
    }
    
    public static void Tick()
    {
        _currentTick = Stopwatch.GetTimestamp();
        foreach (var provider in _allProviders)
            provider.Tick(_currentTick);
    }


    internal static MetricProvider<PerformanceMetric>? performanceProvider;
    internal static MetricProvider<FrameMetaBundle>? FrameMetaBundle;
    internal static MetricProvider<GpuBufferMeta>? BufferMeta;
    internal static MetricProvider<SceneMeta>? SceneMeta;

    public static Action<MetricData>? FillGfxStoreMetrics;
    public static Action<MetricData>? FillAssetMetrics;

}