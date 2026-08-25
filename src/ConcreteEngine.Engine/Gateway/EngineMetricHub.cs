using System.Runtime;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.Assets.Utils;
using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Graphics.Diagnostic;
using ConcreteEngine.Graphics.Gfx;

namespace ConcreteEngine.Engine.Gateway;

internal sealed class EngineMetricHub
{
    private MetricSystem? _metricSystem;

    private readonly FrameMetricAccumulator _frameMetricAccumulator =
        new((int)(EngineSettings.Current.Display.FrameRate / 4f));

    private int _frameCount;

    public void ConnectEditor(MetricSystem metricSystem)
    {
        _metricSystem = metricSystem;
        metricSystem.BindStore(GfxRegistry.StoreCount, AssetKindUtils.AssetTypeCount, WriteStoreMeta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StartCapture()
    {
        if (_metricSystem == null) return;
        _frameMetricAccumulator.BeginFrame();
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndCapture()
    {
        _frameCount++;
        if (_metricSystem == null || !_frameMetricAccumulator.EndFrame(out var frameReport)) return;

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

        var frameMeta = new FrameMeta(EngineTime.FrameId, EngineTime.FpsF, EngineTime.GameAlphaF);
        var sceneMeta = new SceneMeta(
            SceneManager.SceneStore.ActiveCount,
            0,
            0,
            RenderEcs.ActiveCount
        );

        _metricSystem.PushMeta(in frameMeta, in sceneMeta);
        _metricSystem.TickDiagnostic();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void WriteStoreMeta(GfxStoreMeta[] gfxResult, AssetsMetaInfo[] assetResult)
    {
        GfxMetrics.DrainStoreMetrics(gfxResult);

        var storeSpan = AssetManager.Assets.GetTypeStoreSpan();
        for (var i = 0; i < storeSpan.Length; i++)
            assetResult[i] = storeSpan[i].ToSnapshot();
    }
}