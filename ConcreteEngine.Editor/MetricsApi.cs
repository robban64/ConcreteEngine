#region

using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Editor;


public static class MetricsApi
{
    // Fetchers
    public static unsafe delegate*<out FrameMetric, out RenderInfoSample, void> PullFrameMetrics;
    
    //public static Func<FrameMetric<RenderInfoSample>>? PullFrameMetrics { get; set; }
    public static Func<PairSample>? PullSceneMetrics { get; set; }
    public static Func<CollectionSample>? PullMaterialMetrics { get; set; }
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

    public static unsafe void RefreshFrameMetrics()
    {
        if (!ActiveFrameMetrics || PullFrameMetrics == null) return;
        PullFrameMetrics(out var metric, out var sample);
        Data.FrameMetrics = metric;
        Data.FrameRenderInfoSample = sample;
        TextData.UpdateFrameMetrics(in Data.FrameMetrics, in Data.FrameRenderInfoSample);
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