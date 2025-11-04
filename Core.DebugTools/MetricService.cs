using Core.DebugTools.Data;

namespace Core.DebugTools;

public sealed class MetricService
{
    public MetricData Data { get; } = new();
    public MetricReport TextData { get; } = new();

    public bool ActiveSceneMetrics { get; set; } = true;
    public bool ActiveFrameMetrics { get; set; } = true;
    public bool ActiveStoreMetrics { get; set; } = true;
    public bool ActiveMemoryMetrics { get; set; } = true;

    public void RefreshSceneMetrics()
    {
        if(!ActiveSceneMetrics) return;
        Data.SceneMetrics = RouteTable.PullSceneMetrics?.Invoke() ?? default;
        TextData.UpdateSceneMetrics(in Data.SceneMetrics);
    }

    public void RefreshFrameMetrics()
    {
        if(!ActiveFrameMetrics) return;
        Data.FrameMetrics = RouteTable.PullFrameMetrics?.Invoke() ?? default;
        TextData.UpdateFrameMetrics(in Data.FrameMetrics);
    }

    public void RefreshStoreMetrics()
    {
        if(!ActiveStoreMetrics) return;
        Data.MaterialMetrics = RouteTable.PullMaterialMetrics?.Invoke() ?? default;
        TextData.UpdateMaterialMetrics(in Data.MaterialMetrics);

        RouteTable.FillAssetMetrics?.Invoke(Data);
        RouteTable.FillGfxStoreMetrics?.Invoke(Data);
        TextData.UpdateAssetMetrics(Data.AssetMetrics);
        TextData.UpdateGfxStoreMetrics(Data.GfxStoreMetrics);
    }

    public void RefreshMemoryMetrics()
    {
        if(!ActiveMemoryMetrics) return;
        Data.MemoryMetrics = RouteTable.PullMemoryMetrics?.Invoke() ?? default;
        TextData.UpdateMemoryMetrics(Data.MemoryMetrics);
    }

}