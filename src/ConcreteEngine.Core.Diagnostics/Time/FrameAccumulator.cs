using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Metrics;

namespace ConcreteEngine.Core.Diagnostics.Time;

public sealed class FrameAccumulator(int windowSize)
{
    private long _startTick;

    private long _accTicks;
    private long _minTicks = long.MaxValue;
    private long _maxTicks = long.MinValue;
    private int _frameCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginFrame() => _startTick = Stopwatch.GetTimestamp();

    public bool EndFrame(out FrameReport report)
    {
        var endTick = Stopwatch.GetTimestamp();
        var frameTicks = endTick - _startTick;

        _accTicks += frameTicks;
        _frameCount++;

        _minTicks = long.Min(_minTicks, frameTicks);
        _maxTicks = long.Max(_maxTicks, frameTicks);

        if (_frameCount < windowSize)
        {
            report = default;
            return false;
        }

        var avg = _accTicks / (double)_frameCount;

        var toMs = 1000.0 / Stopwatch.Frequency;
        report = new FrameReport(
            _accTicks * toMs,
            _minTicks * toMs,
            _maxTicks * toMs,
            avg * toMs
        );

        _accTicks = 0;
        _frameCount = 0;
        _minTicks = long.MaxValue;
        _maxTicks = long.MinValue;

        return true;
    }
}