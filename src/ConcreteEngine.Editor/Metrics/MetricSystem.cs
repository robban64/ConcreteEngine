using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Editor.Metrics;

public interface IMetricSystem
{
    bool Enabled { get; set; }
    void BindStore(int gfxStoreCount, int assetStoreCount, Action<GfxStoreMeta[], AssetsMetaInfo[]> refreshStore);

    void PushReport(int frameCount, in FrameReport frameReport, in RuntimeReport runtimeReport);
    void PushMeta(in FrameMeta frameMeta, in SceneMeta sceneMeta, in GpuFrameMeta gpuFrameMeta);
}

internal sealed class MetricSystem : IMetricSystem
{
    private const int SamplesPerWindowSlow = 8;
    private const int SamplesPerWindowFast = 4;

    public static readonly MetricSystem Instance = new();

    public FrameMetric Metric;
    public GpuFrameMeta GpuFrameMeta;
    public FrameMeta FrameMeta;
    public SceneMeta SceneMeta;

    public StoreMetrics? Stores { get; private set; }

    public bool Enabled { get; set; } = true;

    public bool FastMode
    {
        get;
        set
        {
            field = value;
            _currentSampleIndex = value ? SamplesPerWindowFast : SamplesPerWindowSlow;
        }
    }

    public double SpikeMultiplier { get; set; } = 2.0;
    public bool IsWarmup => _totalTicks < 40;

    private long _totalTicks;
    private long _startAllocatedBytes;
    private int _samplesPerWindow = SamplesPerWindowSlow;
    private int _currentSampleIndex = SamplesPerWindowSlow;

    private FrameReportAggregator _aggregator;

    private MetricSystem()
    {
        _aggregator.Reset();
    }

    public void BindStore(int gfxStoreCount, int assetStoreCount, Action<GfxStoreMeta[], AssetsMetaInfo[]> refreshStore)
    {
        Stores = new StoreMetrics(gfxStoreCount, assetStoreCount, refreshStore);
    }

    public void PushReport(int frameCount, in FrameReport frameReport, in RuntimeReport runtimeReport)
    {
        if (!Enabled) return;

        if (_aggregator.WindowTotalFrames == 0)
            _startAllocatedBytes = runtimeReport.Allocated;

        _aggregator.AggregateTime(frameCount, in frameReport);

        if (++_currentSampleIndex < _samplesPerWindow) return;

        float finalAvgMs = (float)(_aggregator.WindowTotalMs / _aggregator.WindowTotalFrames);
        float windowSec = (float)(_aggregator.WindowTotalMs / 1000.0);

        int compiledILKb = runtimeReport.CompiledILBytes > 0 ? (int)(runtimeReport.CompiledILBytes / 1024f) : 0;
        int allocMb = runtimeReport.Allocated > 0 ? (int)(runtimeReport.Allocated / 1024f / 1024f) : 0;

        long allocDelta = runtimeReport.Allocated - _startAllocatedBytes;
        float allocRateMbSec = (allocDelta / 1024f / 1024f) / windowSec;
        var activity = GcSample.GetActivity(runtimeReport.Gc, Metric.Gc);

        Metric = new FrameMetric(
            avgMs: finalAvgMs,
            maxMs: (float)_aggregator.WindowMaxMs,
            minMs: (float)_aggregator.WindowMinMs,
            compiledILKb: compiledILKb,
            allocatedMb: allocMb,
            allocMbPerSec: allocRateMbSec,
            gc: runtimeReport.Gc,
            gcActivity: activity
        );

        _aggregator.Reset();
        _currentSampleIndex = 0;
        _startAllocatedBytes = runtimeReport.Allocated;
    }

    public void PushMeta(in FrameMeta frameMeta, in SceneMeta sceneMeta, in GpuFrameMeta gpuFrameMeta)
    {
        if (!Enabled) return;
        FrameMeta = frameMeta;
        SceneMeta = sceneMeta;
        GpuFrameMeta = gpuFrameMeta;
    }

    public void TickDiagnostic()
    {
        if (!Enabled) return;
        _totalTicks++;

        /*
         *         _lastReport = MetricScratchpad.FrameReport;

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
         */
    }

    private struct FrameReportAggregator()
    {
        public double WindowTotalMs;
        public double WindowMaxMs = double.MinValue;
        public double WindowMinMs = double.MaxValue;
        public long WindowTotalFrames;

        public void AggregateTime(int frameCount, in FrameReport frameReport)
        {
            WindowTotalFrames += frameCount;
            WindowTotalMs += frameReport.AccTimeMs;

            WindowMaxMs = Math.Max(WindowMaxMs, frameReport.MaxMs);
            WindowMinMs = Math.Min(WindowMinMs, frameReport.MinMs);
        }

        public void Reset()
        {
            WindowTotalMs = 0;
            WindowTotalFrames = 0;
            WindowMaxMs = double.MinValue;
            WindowMinMs = double.MaxValue;
        }
    }
}