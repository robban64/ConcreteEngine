using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Editor.Metrics;

public sealed class MetricData
{
    public PairSample SceneMetrics;
    public PairSample MemoryMetrics;
    public CollectionSample MaterialMetrics;

    public DebugGfxStoreMetrics[] GfxStoreMetrics { get; set; } = [];
    public DebugAssetStoreMetrics[] AssetMetrics { get; set; } = [];
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