#region

using ConcreteEngine.Common.Diagnostics;
using ConcreteEngine.Editor.Metrics;

#endregion

namespace ConcreteEngine.Editor;

public static class MetricsApi
{
    // Fetchers
    public static Func<FrameMetric<RenderInfoSample>>? PullFrameMetrics { get; set; }
    public static Func<PairSample>? PullSceneMetrics { get; set; }
    public static Func<StoreMetric<CollectionSample>>? PullMaterialMetrics { get; set; }
    public static Func<PairSample>? PullMemoryMetrics { get; set; }
    public static Action<MetricData>? FillGfxStoreMetrics { get; set; }
    public static Action<MetricData>? FillAssetMetrics { get; set; }


    // State
    public static MetricData Data { get; } = new();
    public static MetricReport TextData { get; } = new();

    public static bool ActiveSceneMetrics { get; private set; } = true;
    public static bool ActiveFrameMetrics { get; private set; } = true;
    public static bool ActiveStoreMetrics { get; private set; } = true;
    public static bool ActiveMemoryMetrics { get; private set; } = true;

    public static void ToggleMetrics(bool value)
    {
        ActiveSceneMetrics = value;
        ActiveFrameMetrics = value;
        ActiveStoreMetrics = value;
        ActiveMemoryMetrics = value;
    }

    public static void RefreshSceneMetrics()
    {
        if (!ActiveSceneMetrics) return;
        Data.SceneMetrics = PullSceneMetrics?.Invoke() ?? default;
        TextData.UpdateSceneMetrics(in Data.SceneMetrics);
    }

    public static void RefreshFrameMetrics()
    {
        if (!ActiveFrameMetrics) return;
        Data.FrameMetrics = PullFrameMetrics?.Invoke() ?? default;
        TextData.UpdateFrameMetrics(in Data.FrameMetrics);
    }

    public static void RefreshAssetMetrics()
    {
        if (!ActiveStoreMetrics) return;
        Data.MaterialMetrics = PullMaterialMetrics?.Invoke() ?? default;
        TextData.UpdateMaterialMetrics(in Data.MaterialMetrics);

        FillAssetMetrics?.Invoke(Data);
        TextData.UpdateAssetMetrics(Data.AssetMetrics);
    }

    public static void RefreshGfxResourceMetrics()
    {
        if (!ActiveStoreMetrics) return;
        FillGfxStoreMetrics?.Invoke(Data);
        TextData.UpdateGfxStoreMetrics(Data.GfxStoreMetrics);
    }

    public static void RefreshMemoryMetrics()
    {
        if (!ActiveMemoryMetrics) return;
        Data.MemoryMetrics = PullMemoryMetrics?.Invoke() ?? default;
        TextData.UpdateMemoryMetrics(Data.MemoryMetrics);
    }
}