namespace Core.DebugTools.Data;

public sealed class DebugDataContainer
{
    public DebugFrameMetrics FrameMetrics;
    public DebugSceneMetrics SceneMetrics;
    public DebugMaterialMetrics MaterialMetrics;
    public DebugMemoryMetrics MemoryMetrics;

    public List<DebugGfxStoreMetricRecord> GfxStoreMetrics { get; } = new(8);
    public List<DebugAssetStoreMetricRecord> AssetMetrics { get; } = new(8);


}

