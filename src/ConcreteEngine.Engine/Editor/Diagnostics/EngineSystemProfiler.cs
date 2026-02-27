using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Metrics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Time;

namespace ConcreteEngine.Engine.Editor.Diagnostics;

internal sealed class EngineSystemProfiler
{
    private static EngineSystemProfiler _instance = null!;
    private readonly Stopwatch _sw = new();

    private readonly ProfilerReportEntry _perfProfiler;

    public EngineSystemProfiler()
    {
        if (_instance != null)
            throw new InvalidOperationException("EngineSystemProfiler instance already exists.");

        _instance = this;

        _perfProfiler = new ProfilerReportEntry(EngineSettings.Instance.Display.FrameRate);
    }

    public void Tick()
    {
        return;
        if (!_sw.IsRunning)
        {
            _sw.Start();
            return;
        }

        var frameMs = _sw.Elapsed.TotalMilliseconds;
        _sw.Restart();

        if (_perfProfiler.Accumulate(frameMs, out var perfMetric))
        {
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static GcSample CaptureGc()
    {
        return new GcSample(GC.GetAllocatedBytesForCurrentThread(), GC.CollectionCount(0), GC.CollectionCount(1),
            GC.CollectionCount(2));
    }

    private sealed class ProfilerReportEntry
    {
        private readonly int _frameWindow;

        private readonly double _targetFrameMs;
        private readonly double _spikeMultiplier;

        private int _frameCount;

        private double _accTimeMs;
        private double _minMs = double.MaxValue;
        private double _maxMs = double.MinValue;

        private long _deltaAllocBytes;
        private GcSample _lastGcSample;


        public ProfilerReportEntry(int frameWindow, double spikeMultiplier = 2.0)
        {
            _frameWindow = frameWindow;

            _targetFrameMs = 1000.0 / EngineSettings.Instance.Display.FrameRate;
            _spikeMultiplier = spikeMultiplier;


            _lastGcSample = CaptureGc();
        }

        public bool Accumulate(double frameMs, out FrameMetrics metrics)
        {
            _accTimeMs += frameMs;
            _frameCount++;

            if (frameMs < _minMs) _minMs = frameMs;
            if (frameMs > _maxMs) _maxMs = frameMs;

            if (_frameCount < _frameWindow)
            {
                Unsafe.SkipInit(out metrics);
                return false;
            }

            var gcSample = CaptureGc();
            var gcActivity = GcSample.GetActivity(in gcSample, in _lastGcSample, out _deltaAllocBytes);
            _lastGcSample = gcSample;

            var avgMs = _accTimeMs / _frameCount;
            var load = avgMs / _targetFrameMs * 100.0;
            var hasSpike = _maxMs > avgMs * _spikeMultiplier;


            var windowSeconds = (float)_accTimeMs / 1000.0f;
            var allocated = gcSample.Allocated > 0 ? (int)(gcSample.Allocated / 1024.0f / 1024.0f) : 0;
            var allocRateMbSec = windowSeconds > 0
                ? _deltaAllocBytes / 1024.0f / 1024.0f / windowSeconds
                : 0;

            metrics = new FrameMetrics(
                avgMs: (float)avgMs,
                minMs: (float)_minMs,
                maxMs: (float)_maxMs,hasSpike);

            Reset();
            return true;
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