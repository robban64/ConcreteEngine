using System.Diagnostics;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

internal abstract class MetricProvider(long intervalTicks)
{
    protected long IntervalTicks = intervalTicks;
    protected long LastUpdate = -1;

    public bool HasData => LastUpdate > 0;

    public bool Enabled { get; protected set; }
    public abstract void Toggle(bool enabled);
    public abstract void SetIntervalTicks(long intervalTicks);
    public abstract void Tick(long currentTicks);
}

internal sealed class MetricProvider<T>(Func<T> fetch, long intervalTicks) : MetricProvider(intervalTicks) where T : unmanaged
{
    public T Data;

    public override void Toggle(bool enabled)
    {
        if (Enabled == enabled) return;
        Enabled = enabled;
        Data = default;
        LastUpdate = -1;
    }

    public override void SetIntervalTicks(long intervalTicks)
    {
        if (IntervalTicks == intervalTicks) return;
        IntervalTicks = intervalTicks;
        LastUpdate = -1;
    }

    public override void Tick(long currentTicks)
    {
        if (currentTicks - LastUpdate > IntervalTicks)
        {
            Data = fetch();
            LastUpdate = currentTicks;
        }
    }
}

public static class MetricsApi
{
    private static readonly List<MetricProvider> All = new(8);

    private static long _currentTick = -1;


    public static class Store
    {
        private static readonly GfxStoreMeta[] GfxStoreMetas = new GfxStoreMeta[8];
        private static readonly string[] GfxSpecialMetas = new string[8];

        internal static ReadOnlySpan<GfxStoreMeta> GfxStoreSpan => GfxStoreMetas;
        internal static ReadOnlySpan<string> GfxSpecialMetaSpan => GfxSpecialMetas;


        private static PairSample[] _assetStore = [];
        internal static ReadOnlySpan<PairSample> AssetStoreSpan => _assetStore;


        public static Action TriggerFetch = null!;

        public static void OnFillGfxStore(Span<GfxStoreMeta> span)
        {
            span.CopyTo(GfxStoreMetas);
            for (var i = 0; i < span.Length; i++)
                GfxSpecialMetas[i] = MetricsFormatter.FormatGfxStoreMeta(in span[i]);
        }

        public static void OnFillAssetStore(Span<PairSample> span)
        {
            if (_assetStore.Length < span.Length) _assetStore = new PairSample[span.Length];
            span.CopyTo(_assetStore);
        }
    }

    public static class Provider<T> where T : unmanaged
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