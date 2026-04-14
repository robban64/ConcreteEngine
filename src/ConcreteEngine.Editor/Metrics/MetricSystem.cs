using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Engine.Assets.Data;
using ConcreteEngine.Graphics.Diagnostic;

namespace ConcreteEngine.Editor.Metrics;

public sealed class MetricSystem
{
    private const int SamplesPerWindowSlow = 8;
    private const int SamplesPerWindowFast = 4;



    internal static readonly MetricSystem Instance = new();
    internal StoreMetrics? Stores { get; private set; }
    private readonly FrameReportAggregator _aggregator;

    public bool Enabled { get; set; } = true;
    private int _currentSampleIndex = SamplesPerWindowSlow;
    private long _totalTicks;
    private long _startAllocatedBytes;

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

    internal ref FrameMetric Metric => ref DataStore.Metric;
    internal ref GpuFrameMeta GpuFrameMeta => ref DataStore.GpuFrameMeta;
    internal ref FrameMeta FrameMeta => ref DataStore.FrameMeta;
    internal ref SceneMeta SceneMeta => ref DataStore.SceneMeta;

    private MetricSystem()
    {
        _aggregator = new FrameReportAggregator();
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

        if (++_currentSampleIndex < SamplesPerWindowSlow) return;

        float finalAvgMs = (float)(_aggregator.WindowTotalMs / _aggregator.WindowTotalFrames);
        float windowSec = (float)(_aggregator.WindowTotalMs / 1000.0);

        int compiledILKb = runtimeReport.CompiledILBytes > 0 ? (int)(runtimeReport.CompiledILBytes / 1024f) : 0;
        int allocMb = runtimeReport.Allocated > 0 ? (int)(runtimeReport.Allocated / 1024f / 1024f) : 0;

        long allocDelta = runtimeReport.Allocated - _startAllocatedBytes;
        float allocRateMbSec = allocDelta / 1024f / 1024f / windowSec;
        var activity = GcSample.GetActivity(runtimeReport.Gc, Metric.Gc);

        Metric = new FrameMetric(
            avgMs: (Half)finalAvgMs,
            maxMs: (Half)_aggregator.WindowMaxMs,
            minMs: (Half)_aggregator.WindowMinMs,
            allocMbPerSec: (Half)allocRateMbSec,
            allocatedMb: (ushort)allocMb,
            compiledILKb: (ushort)compiledILKb,
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TickDiagnostic()
    {
        if (!Enabled) return;
        _totalTicks++;
    }

    private static class DataStore
    {
        public static FrameMetric Metric;
        public static GpuFrameMeta GpuFrameMeta;
        public static FrameMeta FrameMeta;
        public static SceneMeta SceneMeta;
    }

    private sealed class FrameReportAggregator()
    {
        public double WindowTotalMs;
        public double WindowMaxMs = double.MinValue;
        public double WindowMinMs = double.MaxValue;
        public long WindowTotalFrames;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AggregateTime(int frameCount, in FrameReport frameReport)
        {
            WindowTotalFrames += frameCount;
            WindowTotalMs += frameReport.AccTimeMs;

            WindowMaxMs = double.Max(WindowMaxMs, frameReport.MaxMs);
            WindowMinMs = double.Min(WindowMinMs, frameReport.MinMs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            WindowTotalMs = 0;
            WindowTotalFrames = 0;
            WindowMaxMs = double.MinValue;
            WindowMinMs = double.MaxValue;
        }
    }
}