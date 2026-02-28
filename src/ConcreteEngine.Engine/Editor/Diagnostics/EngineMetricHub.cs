using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Engine.Editor.Diagnostics;

internal sealed class EngineMetricHub(SceneManager sceneManager, AssetStore assets)
{
    public bool IsConnected { get; private set; }


    private readonly FrameAccumulator _frameAccumulator = new(EngineSettings.Instance.Display.FrameRate);

    public void ConnectEditor(IMetricSystem metricSystem)
    {
        if (IsConnected) throw new InvalidOperationException(nameof(IsConnected));
        IsConnected = true;

        metricSystem.BindStore(GfxMetrics.StoreCount, AssetStore.StoreCount, WriteStoreMeta);
    }

    public void BeginFrame()
    {
        if (!IsConnected) return;
        _frameAccumulator.BeginFrame();
    }

    public void EndFrame()
    {
        if (!IsConnected || !_frameAccumulator.EndFrame(out var report)) return;
        MetricScratchpad.FrameReport = report;
    }

    public void OnDiagnosticTick()
    {
        if (!IsConnected) return;

        MetricScratchpad.GpuFrameMeta = GfxMetrics.FrameMeta;
        MetricScratchpad.FrameMeta = new FrameMeta(EngineTime.FrameId, EngineTime.Fps, EngineTime.GameAlpha);
        MetricScratchpad.SceneMeta = new SceneMeta(
            sceneManager.SceneObjectCount,
            0,
            Ecs.Game.ActiveCount,
            Ecs.Render.ActiveCount
        );
    }

    private void WriteStoreMeta(GfxStoreMeta[] gfxResult, AssetsMetaInfo[] assetResult)
    {
        GfxMetrics.DrainStoreMetrics(gfxResult.AsSpan());
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