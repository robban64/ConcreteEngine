#region

using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Store;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Editor;

public static class MetricsApi
{
    public static Func<PairSample>? PullSceneMetrics;
    public static Func<CollectionSample>? PullMaterialMetrics;
    public static Func<PairSample>? PullMemoryMetrics;
    public static Action<MetricData>? FillGfxStoreMetrics;
    public static Action<MetricData>? FillAssetMetrics;


    // State
    public static readonly MetricData Data = new();
    public static readonly MetricReport TextData = new();

    private static bool _activeSceneMetrics = true;
    private static bool _activeFrameMetrics = true;
    private static bool _activeStoreMetrics = true;
    private static bool _activeMemoryMetrics = true;

    public static void ToggleMetrics(bool value)
    {
        _activeSceneMetrics = value;
        _activeFrameMetrics = value;
        _activeStoreMetrics = value;
        _activeMemoryMetrics = value;
    }

    public static void RefreshSceneMetrics()
    {
        if (!_activeSceneMetrics) return;
        Data.SceneMetrics = PullSceneMetrics?.Invoke() ?? default;
        TextData.UpdateSceneMetrics(in Data.SceneMetrics);
    }

    public static void RefreshFrameMetrics()
    {
        if (!_activeFrameMetrics) return;
        TextData.UpdateFrameMetrics(in EditorDataStore.MetricState.FrameMetrics,
            in EditorDataStore.MetricState.FrameSample);
    }

    public static void RefreshAssetMetrics()
    {
        if (!_activeStoreMetrics) return;
        Data.MaterialMetrics = PullMaterialMetrics?.Invoke() ?? default;
        TextData.UpdateMaterialMetrics(in Data.MaterialMetrics);

        FillAssetMetrics?.Invoke(Data);
        TextData.UpdateAssetMetrics(Data.AssetMetrics);
    }

    public static void RefreshGfxResourceMetrics()
    {
        if (!_activeStoreMetrics) return;
        FillGfxStoreMetrics?.Invoke(Data);
        TextData.UpdateGfxStoreMetrics(Data.GfxStoreMetrics);
    }

    public static void RefreshMemoryMetrics()
    {
        if (!_activeMemoryMetrics) return;
        Data.MemoryMetrics = PullMemoryMetrics?.Invoke() ?? default;
        TextData.UpdateMemoryMetrics(Data.MemoryMetrics);
    }
}