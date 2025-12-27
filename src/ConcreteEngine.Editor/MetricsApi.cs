using System.Diagnostics;
using ConcreteEngine.Core.Diagnostics;

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
    private static readonly List<IMetricProvider> All = new(8);

    private static long _currentTick = -1;

    public static class Store
    {
        private static GfxStoreMeta[] GfxStore = [];
        private static PairSample[] AssetStore = [];

        internal static ReadOnlySpan<GfxStoreMeta> GfxStoreSpan => GfxStore;
        internal static ReadOnlySpan<PairSample> AssetStoreSpan => AssetStore;

        public static Action TriggerFetch = null!;

        // public static readonly Action<GfxStoreMeta[]> GfxStoreCallback = OnFillGfxStore;
        // public static readonly Action<CollectionSample[]> AssetStoreCallback = OnFillAssetStore;

        public static void OnFillGfxStore(Span<GfxStoreMeta> span)
        {
            if (GfxStore.Length < span.Length) GfxStore = new GfxStoreMeta[span.Length];
            span.CopyTo(GfxStore);
        }

        public static void OnFillAssetStore(Span<PairSample> span)
        {
            if (AssetStore.Length < span.Length) AssetStore = new PairSample[span.Length];
            span.CopyTo(AssetStore);
        }
    }

    public static class Provider<T>
    {
        internal static MetricProvider<T>? Record;

        public static void Register(Func<T> fetch, int intervalTicks)
        {
            if (Record != null) All.Remove(Record);
            Record = new MetricProvider<T>(fetch, intervalTicks);
            All.Add(Record);
        }
    }

    public static void Tick()
    {
        _currentTick = Stopwatch.GetTimestamp();
        foreach (var provider in All)
            provider.Tick(_currentTick);
    }

/*
    internal static MetricProvider<PerformanceMetric>? Performance;
    internal static MetricProvider<FrameMetaBundle>? FrameMetaBundle;
    internal static MetricProvider<GpuBufferMeta>? BufferMeta;
    internal static MetricProvider<SceneMeta>? SceneMeta;

    public static Action<MetricData>? FillGfxStoreMetrics;
    public static Action<MetricData>? FillAssetMetrics;
*/
}