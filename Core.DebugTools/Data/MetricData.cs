using ConcreteEngine.Common.Diagnostics;

namespace Core.DebugTools.Data;

public sealed class MetricData
{
    public FrameMetric<RenderInfoSample> FrameMetrics;
    public PairSample SceneMetrics;
    public PairSample MemoryMetrics;
    public StoreMetric<CollectionSample> MaterialMetrics;

    public DebugGfxStoreMetrics[] GfxStoreMetrics { get; set; } = Array.Empty<DebugGfxStoreMetrics>();
    public DebugAssetStoreMetrics[] AssetMetrics { get; set; } = Array.Empty<DebugAssetStoreMetrics>();
}

public sealed class DebugGfxStoreMetrics(string name, string shortName, byte resourceKind)
{
    public string Name { get; } = name;
    public string ShortName { get; } = shortName;
    public byte ResourceKind { get; } = resourceKind;

    public StoreMetric<CollectionSample> GfxStoreMetrics;
    public StoreMetric<CollectionSample> BackendStoreMetrics;
    public GfxResourceMetric<ValueSample> SpecialMetric;
}

public sealed class DebugAssetStoreMetrics(string name, string shortName, byte assetKind)
{
    public string Name { get; } = name;
    public string ShortName { get; } = shortName;
    public byte Kind { get; } = assetKind;

    public StoreMetric<CollectionSample> Metrics;
}