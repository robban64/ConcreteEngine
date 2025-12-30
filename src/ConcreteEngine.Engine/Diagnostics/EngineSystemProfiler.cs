using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Time;

namespace ConcreteEngine.Engine.Diagnostics;

internal sealed class EngineSystemProfiler
{
    private static EngineSystemProfiler _instance = null!;
    private readonly Stopwatch _sw = new();

    private readonly ProfilerReportEntry?[] _reports;
    private int _count;

    private readonly double _targetFrameMs;
    private readonly double _spikeMultiplier;

    public EngineSystemProfiler(double spikeMultiplier = 2.0)
    {
        if (_instance != null)
            throw new InvalidOperationException("EngineSystemProfiler instance already exists.");

        _instance = this;
        _targetFrameMs = 1000.0 / EngineSettings.Instance.Display.FrameRate;
        _spikeMultiplier = spikeMultiplier;

        int len = EnumCache<TimeStepKind>.Count;
        _reports = new ProfilerReportEntry[len];
    }

    public void GetReportMetric(TimeStepKind step, out PerformanceMetric result)
    {
        var report = _reports[(int)step];
        if (report is null) result = default;
        else result = report.Result;
    }


    public void RegisterReportInterval(TimeStepKind timeStep, ActionIn<PerformanceMetric>? callback = null)
    {
        _reports[(int)timeStep] = new ProfilerReportEntry(timeStep.ToRate(), callback);
        _count++;
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

        var gcSample = CaptureGc(out var allocBytes);

        var report = new FrameReport(frameMs, _targetFrameMs, _spikeMultiplier, allocBytes);

        int len = int.Min(_count, _reports.Length);
        for (var i = 0; i < len; i++)
        {
            _reports[i]?.Accumulate(in report, gcSample);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GcSample CaptureGc(out long allocated)
    {
        allocated = GC.GetAllocatedBytesForCurrentThread();
        return new GcSample(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
    }


    private readonly struct FrameReport(double frameMs, double targetMs, double spikeMulti, long alloc)
    {
        public readonly double FrameMs = frameMs;
        public readonly double TargetMs = targetMs;
        public readonly double SpikeMulti = spikeMulti;
        public readonly long Alloc = alloc;
    }


    private sealed class ProfilerReportEntry
    {
        private double _accTimeMs;
        private double _minMs = double.MaxValue;
        private double _maxMs = double.MinValue;
        private int _frameCount;

        private long _deltaAllocBytes;
        private long _lastAllocBytes;
        private GcSample _lastGcSample;

        private readonly int _frameWindow;

        public PerformanceMetric Result;

        private readonly ActionIn<PerformanceMetric>? _callback;

        public ProfilerReportEntry(int frameWindow, ActionIn<PerformanceMetric>? callback)
        {
            _callback = callback;
            _frameWindow = frameWindow;

            _lastGcSample = CaptureGc(out _lastAllocBytes);
        }

        public void Accumulate(in FrameReport report, GcSample capturedGc)
        {
            _accTimeMs += report.FrameMs;
            _frameCount++;

            if (report.FrameMs < _minMs) _minMs = report.FrameMs;
            if (report.FrameMs > _maxMs) _maxMs = report.FrameMs;

            if (_frameCount < _frameWindow) return;

            var gcActivity = GcSample.GetActivity(capturedGc, _lastGcSample, out _);

            _deltaAllocBytes = report.Alloc - _lastAllocBytes;
            _lastAllocBytes = report.Alloc;
            _lastGcSample = capturedGc;

            var avgMs = _accTimeMs / _frameCount;
            var load = avgMs / report.TargetMs * 100.0;
            var hasSpike = _maxMs > avgMs * report.SpikeMulti;

            var windowSeconds = (float)_accTimeMs / 1000.0f;
            var allocated = report.Alloc > 0 ? (int)(report.Alloc / 1024.0f / 1024.0f) : 0;
            var allocRateMbSec = windowSeconds > 0
                ? _deltaAllocBytes / 1024.0f / 1024.0f / windowSeconds
                : 0;

            Result = new PerformanceMetric(
                avgMs: (float)avgMs,
                minMs: (float)_minMs,
                maxMs: (float)_maxMs,
                load: (float)load,
                allocatedMb: allocated,
                allocRateMbPerSec: allocRateMbSec,
                gc: capturedGc,
                hasSpiked: hasSpike,
                gcActivity: gcActivity);

            _callback?.Invoke(in Result);

            Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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