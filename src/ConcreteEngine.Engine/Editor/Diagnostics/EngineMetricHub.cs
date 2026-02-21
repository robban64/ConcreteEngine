using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Engine.Editor.Diagnostics;

internal sealed class EngineMetricHub(SceneManager sceneManager, AssetStore assets)
{
    private readonly EngineSystemProfiler _profiler = new();

    public bool IsConnected { get; private set; }

    public void OnFrameTick() => _profiler.Tick();

    public void ConnectEditor()
    {
        if(IsConnected) throw new InvalidOperationException(nameof(IsConnected));
        IsConnected = true;
        _profiler.RegisterReportInterval(TimeStepKind.None, static (in input) =>
        {
            EngineMetricStore.PerformanceMetric = input;
        });

        EngineMetricStore.WireEditor(GetAssetStoreMetrics, GetSceneMeta);
    }

    private void GetAssetStoreMetrics(Span<AssetsMetaInfo> span)
    {
        ArgumentOutOfRangeException.ThrowIfZero(span.Length);
        for (var i = 0; i < assets.Collections.Count; i++)
            span[i] = assets.Collections[i].ToSnapshot();
    }

    private void GetSceneMeta(out SceneMeta result)
    {
        result = new SceneMeta(sceneManager.SceneObjectCount, 0, Ecs.Game.ActiveCount, Ecs.Render.ActiveCount);
    }

    private static void PrintMetrics()
    {
        ref readonly var s = ref EngineMetricStore.PerformanceMetric;

        var original = Console.ForegroundColor;
        if (s.GcActivity == GcActivity.Minor || s.HasSpiked)
            Console.ForegroundColor = ConsoleColor.Yellow;
        else if (s.GcActivity == GcActivity.Major)
            Console.ForegroundColor = ConsoleColor.Red;

        Span<char> buffer = stackalloc char[128];
        
        var sw = new SpanWriter(buffer);
        sw.Append("Max: ").Append(s.MaxMs, "F4").Append("ms | ")
            .Append(" Avg: ").Append(s.AvgMs, "F4").Append("ms | ")
            .Append("Alloc/s: ").Append(s.AllocMbPerSec, "F4").Append("MB");

        sw.Append(s.HasSpiked ? " | [SPIKE]" : " | [Frame]");
        switch (s.GcActivity)
        {
            case GcActivity.Minor: sw.Append(" | [GC INFO]"); break;
            case GcActivity.Major: sw.Append(" | [Gc Warn]"); break;
        }

        Console.WriteLine(sw.End());
        Console.ForegroundColor = original;
    }

}

internal static class EngineMetricStore
{
    public static PerformanceMetric PerformanceMetric;
    private static GpuFrameMetaBundle _gpuBundle;

    private static Action<Span<AssetsMetaInfo>> _fetchAssetMeta = null!;
    private static FuncFill<SceneMeta> _fetchSceneMeta = null!;

    internal static void WireEditor(Action<Span<AssetsMetaInfo>> fetchAssetMeta, FuncFill<SceneMeta> fetchSceneMeta)
    {
        _fetchAssetMeta = fetchAssetMeta;
        _fetchSceneMeta = fetchSceneMeta;

        GfxMetrics.OnFrameMetric = static (in input) => _gpuBundle = input;

        MetricsApi.Store.RegisterGfx(GfxMetrics.StoreCount, static span => GfxMetrics.DrainStoreMetrics(span));
        MetricsApi.Store.RegisterAsset(AssetStore.StoreCount, static span => _fetchAssetMeta(span));

        MetricsApi.Provider<PerformanceMetric>.Register(1, static (out output) => output = PerformanceMetric);
        MetricsApi.Provider<GpuFrameMetaBundle>.Register(2, static (out output) => output = _gpuBundle);

        MetricsApi.Provider<FrameMeta>.Register(1, static (out result) =>
        {
            result = new FrameMeta(EngineTime.FrameId, EngineTime.Fps, EngineTime.GameAlpha);
        });
        MetricsApi.Provider<SceneMeta>.Register(2, static (out result) => _fetchSceneMeta(out result));

        MetricsApi.FinishSetup();
    }
}
