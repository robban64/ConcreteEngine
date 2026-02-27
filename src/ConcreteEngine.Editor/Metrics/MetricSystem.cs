using System.Diagnostics;
using System.Runtime;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Editor.Metrics;

public interface IMetricSystem
{
    int FrameRate { get; set; }
    bool Enabled { get; set; }
    void BindStore(int gfxStoreCount, int assetStoreCount, Action<GfxStoreMeta[], AssetsMetaInfo[]> refreshStore);
}

internal sealed class MetricSystem : IMetricSystem
{
    public static readonly MetricSystem Instance = new();

    private static class LocalStore
    {
        public static long LastILBytesCompiled = 0;
        public static FrameMetrics LatestFrameMetric;
    }

    public FrameMetrics FrameMetric;
    public RuntimeMetrics RuntimeMetric;

    public GcSample GcSample = MetricUtils.CollectGcSample();

    public GpuFrameMetaBundle GpuFrameMeta;
    public FrameMeta FrameMeta;
    public SceneMeta SceneMeta;

    public StoreMetrics? Stores;

    private readonly Stopwatch _sw = new();
    private readonly MetricAccumulator _accumulator = new(144);

    private long _totalTicks = 0;

    //  public readonly PerformanceSession PerfSession = new();
    public bool Enabled { get; set; }
    public int FrameRate { get; set; } = 144;

    public bool IsWarmup => _totalTicks > 40;

    private MetricSystem() { }


    public void BindStore(int gfxStoreCount, int assetStoreCount, Action<GfxStoreMeta[], AssetsMetaInfo[]> refreshStore)
    {
        Stores = new StoreMetrics(gfxStoreCount, assetStoreCount, refreshStore);
    }

    public void AdvanceFrame()
    {
        const double spikeMultiplier = 2;
        
        if(!Enabled) return;

        var frameMs = _sw.Elapsed.TotalMilliseconds;
        _sw.Restart();

        if (_accumulator.Accumulate(frameMs, spikeMultiplier, out var metrics))
            LocalStore.LatestFrameMetric = metrics;
    }

    public void TickDiagnostic()
    {
        if(!Enabled) return;

        _totalTicks++;
        
        var windowSeconds = (float)_accumulator.CurrentAccTimeMs / 1000.0f;

        if (_totalTicks % 2 == 0)
        {
            CollectRuntimeMetrics(windowSeconds, out RuntimeMetric);
            FrameMetric = LocalStore.LatestFrameMetric;
        }
        else
        {
            FrameMeta = MetricScratchpad.FrameMeta;
            SceneMeta = MetricScratchpad.SceneMeta;
            GpuFrameMeta = MetricScratchpad.GpuFrameMeta;
        }
    }


    public void CollectRuntimeMetrics(float windowSeconds, out RuntimeMetrics runtime)
    {
        var gcSample = MetricUtils.CollectGcSample();
        var gcActivity = GcSample.GetActivity(in gcSample, in GcSample, out var allocDelta);
        GcSample = gcSample;

        var allocatedMb = gcSample.Allocated > 0 ? (int)(gcSample.Allocated / 1024.0f / 1024.0f) : 0;
        var allocRateMbSec = windowSeconds > 0 ? allocDelta / 1024.0f / 1024.0f / windowSeconds : 0;

        var ilBytes = JitInfo.GetCompiledILBytes();
        var ilBytesDelta = ilBytes - LocalStore.LastILBytesCompiled;
        LocalStore.LastILBytesCompiled = ilBytes;

        var ilKiloBytes = (int)(ilBytes / 1024.0f);
        var ilRateKbSec = windowSeconds > 0 ? ilBytesDelta / 1024.0f / windowSeconds : 0;

        runtime = new RuntimeMetrics(ilKiloBytes, ilRateKbSec, allocatedMb, allocRateMbSec, gcActivity);
    }
}