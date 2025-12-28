using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Editor;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Metadata;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Engine.Diagnostics;

internal static class EngineMetricHub
{
    private static EngineSystemProfiler _profiler = null!;

    private static SceneWorld _sceneWorld = null!;
    private static World _world = null!;
    private static AssetStore _assets = null!;

    public static bool PrintReport = false;
    public static bool LogReport = false;

    private static PerformanceMetric CurrentPerformanceMetric;

    public static void Attach(EngineSystemProfiler profiler, AssetStore assets, SceneWorld sceneWorld, World world)
    {
        if (_sceneWorld != null! || _profiler != null!) throw new InvalidOperationException();

        _assets = assets;
        _sceneWorld = sceneWorld;
        _world = world;
        _profiler = profiler;

        profiler.RegisterReportInterval(TimeStepKind.None, OnReport);
    }

    internal static void WireEditor()
    {
        MetricsApi.Store.TriggerFetch = static () =>
        {
            DispatchAssetStoreMetrics();
            DispatchGfxStoreMetrics();
        };

        MetricsApi.Provider<PerformanceMetric>.Register(1, GetPerformanceMetric);
        MetricsApi.Provider<FrameMetaBundle>.Register(2, GetFrameMeta);
        MetricsApi.Provider<SceneMeta>.Register(3, GetSceneMeta);
        MetricsApi.Provider<GpuBufferMeta>.Register(2, GfxMetrics.GetBufferMeta);
    }

    private static void GetPerformanceMetric(out PerformanceMetric metric) => metric = CurrentPerformanceMetric;

    private static void OnReport(in PerformanceMetric metric) => CurrentPerformanceMetric = metric;


    internal static void DispatchGfxStoreMetrics()
    {
        Span<GfxStoreMeta> span = stackalloc GfxStoreMeta[GfxMetrics.StoreCount];
        GfxMetrics.DrainStoreMetrics(span);
        MetricsApi.Store.OnFillGfxStore(span);
    }

    internal static void DispatchAssetStoreMetrics()
    {
        Span<PairSample> span = stackalloc PairSample[_assets.TypeCount];
        Span<AssetTypeMeta> meta = stackalloc AssetTypeMeta[_assets.TypeCount];
        _assets.ExtractMeta(meta);

        for (int i = 0; i < span.Length; i++)
        {
            var m = meta[i];
            span[i] = new PairSample(m.Count, m.FileCount);
        }

        MetricsApi.Store.OnFillAssetStore(span);
    }


    private static void GetFrameMeta(out FrameMetaBundle result)
    {
        result.Frame = new FrameMeta(EngineTime.FrameId, EngineTime.Fps, EngineTime.GameAlpha);
        GfxMetrics.GetFrameMeta(out result.RenderFrame);
    }

    private static void GetSceneMeta(out SceneMeta result)
    {
        result = new SceneMeta(_sceneWorld.SceneObjectCount, _world.VisibleEntityCount, Ecs.Game.ActiveCount,
            Ecs.Render.ActiveCount);
    }
/*

    private static void PrintSample(Span<char> message, in PerformanceMetric sample)
    {
        var original = Console.ForegroundColor;
        if (sample.GcActivity == GcActivity.Minor || sample.HasSpiked)
            Console.ForegroundColor = ConsoleColor.Yellow;
        else if (sample.GcActivity == GcActivity.Major)
            Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(message);
        Console.ForegroundColor = original;
    }

    private static void OnFullReport(PerformanceMetric sample)
    {
        var log = GenerateStringLog(sample);
        var level = LogLevel.Info;
        if (sample.GcActivity == GcActivity.Minor || sample.HasSpiked) level = LogLevel.Debug;
        if (sample.GcActivity == GcActivity.Major) level = LogLevel.Warn;

        Logger.LogString(LogScope.Engine, log, level);
    }


    private static void PrintShortLog(PerformanceMetric s)
    {
        Span<char> buffer = stackalloc char[128];
        var builder = ZaSpanStringBuilder.Create(buffer);

        builder.Append("Max: ").Append(s.MaxMs, "F4").Append("ms | ")
            .Append(" Avg: ").Append(s.AvgMs, "F4").Append("ms | ")
            .Append("Alloc/s: ").Append(s.AllocMbPerSec, "F4").Append("MB");

        builder.Append(s.HasSpiked ? " | [SPIKE]" : " | [Frame]");
        switch (s.GcActivity)
        {
            case GcActivity.Minor: builder.Append(" | [GC INFO]"); break;
            case GcActivity.Major: builder.Append(" | [Gc Warn]"); break;
        }
    }


    private static string GenerateStringLog(PerformanceMetric s)
    {
        Span<char> buffer = stackalloc char[128];
        var builder = ZaSpanStringBuilder.Create(buffer);

        builder
            .Append(s.AvgMs, "F4")
            .Append("ms (Min:").AppendIf(s.MinMs < 10, " ").Append(s.MinMs, "F2")
            .Append(" Max:").AppendIf(s.MaxMs < 10, " ").Append(s.MaxMs, "F2")
            .Append(") | ");

        builder.Append("Load: ").Append(s.Load, "F1").Append("% | ");
        builder.Append("Alloc/s: ").Append(s.AllocMbPerSec, "F2").Append("MB | ");

        builder.Append("Mem: ").Append(s.AllocatedMb).Append("MB");

        builder.Append(s.HasSpiked ? " | [SPIKE]" : " | [Frame]");
        switch (s.GcActivity)
        {
            case GcActivity.Minor: builder.Append(" | [GC INFO]"); break;
            case GcActivity.Major: builder.Append(" | [Gc Warn]"); break;
        }

        return builder.ToString();
    }*/
}