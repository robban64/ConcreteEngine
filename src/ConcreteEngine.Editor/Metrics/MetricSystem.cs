using System.Diagnostics;
using System.Runtime;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Editor.Metrics;

public interface IMetricSystem
{
    bool Enabled { get; set; }
    void BindStore(int gfxStoreCount, int assetStoreCount, Action<GfxStoreMeta[], AssetsMetaInfo[]> refreshStore);
}

internal sealed class MetricSystem : IMetricSystem
{
    public static readonly MetricSystem Instance = new();

    public FrameMetric FrameMetric;
    public RuntimeMetric RuntimeMetric;

    public GcSample GcSample = MetricUtils.CollectGcSample();

    public GpuFrameMeta GpuFrameMeta;
    public FrameMeta FrameMeta;
    public SceneMeta SceneMeta;

    private FrameReport _lastReport;
    private long _lastILBytesCompiled;
    private long _totalTicks;
    private int _spikeTimer;

    public StoreMetrics? Stores { get; private set; }

    public bool Enabled { get; set; }
    public double SpikeMultiplier { get; set; } = 2.0;

    public bool IsWarmup => _totalTicks < 40;

    private MetricSystem() { }

    public void BindStore(int gfxStoreCount, int assetStoreCount, Action<GfxStoreMeta[], AssetsMetaInfo[]> refreshStore)
    {
        Stores = new StoreMetrics(gfxStoreCount, assetStoreCount, refreshStore);
    }

    public void TickDiagnostic()
    {
        if (!Enabled) return;

        _totalTicks++;

        _lastReport = MetricScratchpad.FrameReport;

        var currentSpike = _lastReport.MaxMs > _lastReport.AvgMs * SpikeMultiplier;
        if (currentSpike) _spikeTimer = 4;
        else if (_spikeTimer > 0) _spikeTimer--;

        var windowSeconds = (float)_lastReport.AccTimeMs / 1000.0f;

        CollectRuntimeMetrics(windowSeconds, out RuntimeMetric);

        FrameMetric = new FrameMetric(
            (float)_lastReport.AccTimeMs,
            (float)_lastReport.MinMs,
            (float)_lastReport.MaxMs,
            _spikeTimer > 0);

        FrameMeta = MetricScratchpad.FrameMeta;
        SceneMeta = MetricScratchpad.SceneMeta;
        GpuFrameMeta = MetricScratchpad.GpuFrameMeta;
    }


    private void CollectRuntimeMetrics(float windowSeconds, out RuntimeMetric runtime)
    {
        var gcSample = MetricUtils.CollectGcSample();
        var gcActivity = GcSample.GetActivity(in gcSample, in GcSample, out var allocDelta);
        GcSample = gcSample;

        var allocatedMb = gcSample.Allocated > 0 ? (int)(gcSample.Allocated / 1024.0f / 1024.0f) : 0;
        var allocRateMbSec = windowSeconds > 0 ? allocDelta / 1024.0f / 1024.0f / windowSeconds : 0;

        var ilBytes = JitInfo.GetCompiledILBytes();
        var ilBytesDelta = ilBytes - _lastILBytesCompiled;
        _lastILBytesCompiled = ilBytes;

        var ilKiloBytes = (int)(ilBytes / 1024.0f);
        var ilRateKbSec = windowSeconds > 0 ? ilBytesDelta / 1024.0f / windowSeconds : 0;

        runtime = new RuntimeMetric(ilKiloBytes, ilRateKbSec, allocatedMb, allocRateMbSec, gcActivity);
    }
}