using System.Diagnostics;
using ConcreteEngine.Common;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Diagnostics;

internal sealed class EngineSystemProfiler
{
    private const int MaxReports = 8;

    private static EngineSystemProfiler _instance = null!;
    private readonly Stopwatch _sw = new();

    private readonly List<ProfilerReportEntry> _reports = new(4);

    private readonly double _targetFrameMs;
    private readonly double _spikeMultiplier;

    public EngineSystemProfiler(double targetFps, double spikeMultiplier = 2.0)
    {
        if (_instance != null)
            throw new InvalidOperationException("EngineSystemProfiler instance already exists.");

        _instance = this;
        _targetFrameMs = 1000.0 / targetFps;
        _spikeMultiplier = spikeMultiplier;
    }

    public void RegisterReportInterval(int frames, Action<PerformanceMetric> callback)
    {
        InvalidOpThrower.ThrowIf(_reports.Count > MaxReports);
        _reports.Add(new ProfilerReportEntry(frames, callback));
    }

    public void Tick()
    {
        if (!_sw.IsRunning)
        {
            _sw.Start();
            return;
        }

        var frameMs = _sw.Elapsed.TotalMilliseconds;
        _sw.Restart();

        var allocBytes = GC.GetAllocatedBytesForCurrentThread();
        var gcSample = GcSample.Capture();

        var report = new FrameReport(frameMs, _targetFrameMs, _spikeMultiplier, allocBytes, gcSample);
        foreach (var entry in _reports)
        {
            entry.Accumulate(in report);
        }
    }

    private readonly struct GcSample(int gen0, int gen1, int gen2)
    {
        public readonly short Gen0 = (short)gen0;
        public readonly short Gen1 = (short)gen1;
        public readonly short Gen2 = (short)gen2;

        public static GcSample Capture() => new(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));

        public static GcActivity GetActivity(in GcSample current, in GcSample last, out int delta)
        {
            int d0 = current.Gen0 - last.Gen0, d1 = current.Gen1 - last.Gen1, d2 = current.Gen2 - last.Gen2;
            delta = d0 + d1 + d2;
            if (d1 > 0 || d2 > 0) return GcActivity.Major;
            if (d0 > 0) return GcActivity.Minor;
            return GcActivity.None;
        }
    }

    private readonly struct FrameReport(double frameMs, double targetMs, double spikeMulti, long alloc, GcSample sample)
    {
        public readonly double FrameMs = frameMs;
        public readonly double TargetMs = targetMs;
        public readonly double SpikeMulti = spikeMulti;
        public readonly long Alloc = alloc;
        public readonly GcSample Gc = sample;
    }


    private sealed class ProfilerReportEntry(int frameWindow, Action<PerformanceMetric> callback)
    {
        public readonly int FrameWindow = frameWindow;

        private double _accTimeMs;
        private double _minMs = double.MaxValue;
        private double _maxMs = double.MinValue;
        private int _frameCount;

        private long _deltaAllocBytes;

        private long _lastAllocBytes = GC.GetAllocatedBytesForCurrentThread();
        private GcSample _lastGcSample = GcSample.Capture();

        public void Accumulate(in FrameReport report)
        {
            _accTimeMs += report.FrameMs;
            _frameCount++;

            if (report.FrameMs < _minMs) _minMs = report.FrameMs;
            if (report.FrameMs > _maxMs) _maxMs = report.FrameMs;

            if (_frameCount < FrameWindow) return;

            var avgMs = _accTimeMs / _frameCount;
            var load = avgMs / report.TargetMs * 100.0;
            var hasSpike = _maxMs > avgMs * report.SpikeMulti;

            _deltaAllocBytes = report.Alloc - _lastAllocBytes;
            var windowSeconds = _accTimeMs / 1000.0;
            var allocRateMbSec = windowSeconds > 0
                ? _deltaAllocBytes / 1024.0 / 1024.0 / windowSeconds
                : 0;

            _lastAllocBytes = report.Alloc;

            var gcActivity = GcSample.GetActivity(report.Gc, _lastGcSample, out var gcDelta);

            _lastGcSample = report.Gc;

            var sample = new PerformanceMetric(
                avgMs: avgMs,
                minMs: _minMs,
                maxMs: _maxMs,
                load: load,
                allocBytes: _deltaAllocBytes,
                allocRateMbPerSec: allocRateMbSec,
                hasSpiked: hasSpike,
                gcActivity: gcActivity);

            callback(sample);

            Reset();
        }

        private void Reset()
        {
            _accTimeMs = 0;
            _frameCount = 0;
            _minMs = double.MaxValue;
            _maxMs = double.MinValue;
            _deltaAllocBytes = 0;
        }
    }
}