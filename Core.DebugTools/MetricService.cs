using Core.DebugTools.Data;

namespace Core.DebugTools;

public sealed class MetricService
{
    public MetricData Data { get; } = new();
    public MetricReport TextData { get; } = new();

    public void RefreshSceneMetrics()
    {
        Data.SceneMetrics = RouteTable.PullSceneMetrics?.Invoke() ?? default;
        TextData.UpdateSceneMetrics(in Data.SceneMetrics);
    }

    public void RefreshFrameMetrics()
    {
        Data.FrameMetrics = RouteTable.PullFrameMetrics?.Invoke() ?? default;
        TextData.UpdateFrameMetrics(in Data.FrameMetrics);
    }

    public void RefreshStoreMetrics()
    {
        
        Data.MaterialMetrics = RouteTable.PullMaterialMetrics?.Invoke() ?? default;
        TextData.UpdateMaterialMetrics(in Data.MaterialMetrics);

        RouteTable.FillAssetMetrics?.Invoke(Data);
        RouteTable.FillGfxStoreMetrics?.Invoke(Data);
        TextData.UpdateAssetMetrics(Data.AssetMetrics);
        TextData.UpdateGfxStoreMetrics(Data.GfxStoreMetrics);
    }

    public void RefreshMemoryMetrics()
    {
        Data.MemoryMetrics = RouteTable.PullMemoryMetrics?.Invoke() ?? default;
        TextData.UpdateMemoryMetrics(Data.MemoryMetrics);
    }

}