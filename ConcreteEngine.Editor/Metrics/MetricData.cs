#region

using ConcreteEngine.Shared.MetricData;

#endregion

namespace ConcreteEngine.Editor.Metrics;

public sealed class MetricData
{
    public FrameMetric<RenderInfoSample> FrameMetrics;
    public PairSample SceneMetrics;
    public PairSample MemoryMetrics;
    public BasicMetric<CollectionSample> MaterialMetrics;

    public DebugGfxStoreMetrics[] GfxStoreMetrics { get; set; } = Array.Empty<DebugGfxStoreMetrics>();
    public DebugAssetStoreMetrics[] AssetMetrics { get; set; } = Array.Empty<DebugAssetStoreMetrics>();
}

public sealed class DebugGfxStoreMetrics(string name, string shortName, byte resourceKind)
{
    public string Name { get; } = name;
    public string ShortName { get; } = shortName;
    public byte ResourceKind { get; } = resourceKind;

    public BasicMetric<CollectionSample> GfxStoreMetrics;
    public BasicMetric<CollectionSample> BackendStoreMetrics;
    public TargetMetric<ValueSample> SpecialMetric;
}

public sealed class DebugAssetStoreMetrics(string name, string shortName, byte assetKind)
{
    public string Name { get; } = name;
    public string ShortName { get; } = shortName;
    public byte Kind { get; } = assetKind;

    public BasicMetric<CollectionSample> Metrics;
}