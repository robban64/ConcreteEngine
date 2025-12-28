using System.Diagnostics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

public static class MetricsApi
{
    private static readonly List<MetricProvider> All = new(8);

    private static long _currentTick = -1;

    internal static PerformanceSession? Session;

    public static class Provider<T> where T : unmanaged
    {
        internal static MetricProvider<T>? Record;

        public static void Register(int intervalTicks, FuncFill<T> fetch)
        {
            if (Record != null) All.Remove(Record);
            Record = new MetricProvider<T>(intervalTicks, fetch);
            All.Add(Record);

            if (Record is MetricProvider<PerformanceMetric> performanceProvider)
            {
                Session = new PerformanceSession(performanceProvider);
            }
        }
    }

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

    public static void Tick()
    {
        _currentTick = Stopwatch.GetTimestamp();

        Session?.Update();

        foreach (var provider in All)
            provider.Update(_currentTick);

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