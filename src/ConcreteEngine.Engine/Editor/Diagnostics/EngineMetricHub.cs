using System.Runtime;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Engine.Editor.Diagnostics;

internal sealed class EngineMetricHub(SceneManager sceneManager, AssetStore assets)
{
    private MetricSystem? _metricSystem;

    private readonly FrameAccumulator _frameAccumulator = new((int)(EngineSettings.Instance.Display.FrameRate / 4f));

    private int _frameCount = 0;

    public void ConnectEditor(MetricSystem metricSystem)
    {
        _metricSystem = metricSystem;
        metricSystem.BindStore(GfxMetrics.StoreCount, AssetStore.StoreCount, WriteStoreMeta);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartCapture()
    {
        if (_metricSystem == null) return;
        _frameAccumulator.BeginFrame();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndCapture()
    {
        _frameCount++;
        if (_metricSystem == null || !_frameAccumulator.EndFrame(out var frameReport)) return;

        var gcSample = new GcSample(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
        var runtimeReport = new RuntimeReport(
            JitInfo.GetCompiledILBytes(),
            GC.GetAllocatedBytesForCurrentThread(),
            gcSample
        );

        _metricSystem.PushReport(_frameCount, in frameReport, in runtimeReport);
        _frameCount = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnDiagnosticTick()
    {
        if (_metricSystem == null) return;

        var frameMeta = new FrameMeta(EngineTime.FrameId, EngineTime.Fps, EngineTime.GameAlpha);
        var sceneMeta = new SceneMeta(
            sceneManager.SceneObjectCount,
            0,
            Ecs.Game.ActiveCount,
            Ecs.Render.ActiveCount
        );

        _metricSystem.PushMeta(in frameMeta, in sceneMeta, in GfxMetrics.FrameMeta);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WriteStoreMeta(GfxStoreMeta[] gfxResult, AssetsMetaInfo[] assetResult)
    {
        GfxMetrics.DrainStoreMetrics(gfxResult);
        for (var i = 0; i < assets.Collections.Count; i++)
            assetResult[i] = assets.Collections[i].ToSnapshot();
    }


/*
    private static void PrintMetrics()
    {
        ref readonly var s = ref MetricScratchpad.Performance;

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
*/
}