#region

using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Editor.Metrics;

public sealed class MetricData
{
    public FrameMetric FrameMetrics;
    public RenderInfoSample FrameRenderInfoSample;

    public PairSample SceneMetrics;
    public PairSample MemoryMetrics;
    public CollectionSample MaterialMetrics;

    public DebugGfxStoreMetrics[] GfxStoreMetrics { get; set; } = Array.Empty<DebugGfxStoreMetrics>();
    public DebugAssetStoreMetrics[] AssetMetrics { get; set; } = Array.Empty<DebugAssetStoreMetrics>();
}

public sealed class DebugGfxStoreMetrics(string name, string shortName, byte resourceKind)
{
    public string Name { get; } = name;
    public string ShortName { get; } = shortName;
    public byte ResourceKind { get; } = resourceKind;

    public CollectionSample GfxStoreMetrics;
    public CollectionSample BackendStoreMetrics;
    public TargetMetric SpecialMetric;
    public ValueSample SpecialSample;
}

public sealed class DebugAssetStoreMetrics(string name, string shortName, byte assetKind)
{
    public string Name { get; } = name;
    public string ShortName { get; } = shortName;
    public byte Kind { get; } = assetKind;

    public CollectionSample Sample;
}