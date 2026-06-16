using System.Runtime;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class EngineMetricHub
{
    private MetricSystem? _metricSystem;

    private readonly FrameAccumulator _frameAccumulator = new((int)(EngineSettings.Current.Display.FrameRate / 4f));

    private int _frameCount;

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

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnDiagnosticTick()
    {
        if (_metricSystem == null) return;

        var frameMeta = new FrameMeta(EngineTime.FrameId, EngineTime.Fps, EngineTime.GameAlpha);
        var sceneMeta = new SceneMeta(
            SceneManager.SceneStore.ActiveCount,
            0,
            Ecs.Game.ActiveCount,
            Ecs.Render.ActiveCount
        );

        _metricSystem.PushMeta(in frameMeta, in sceneMeta, in GfxMetrics.FrameMeta);
        _metricSystem.TickDiagnostic();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WriteStoreMeta(GfxStoreMeta[] gfxResult, AssetsMetaInfo[] assetResult)
    {
        GfxMetrics.DrainStoreMetrics(gfxResult);

        var storeSpan = AssetStore.Instance.GetTypeStoreSpan();
        for (var i = 0; i < storeSpan.Length; i++)
            assetResult[i] = storeSpan[i].ToSnapshot();
    }
}