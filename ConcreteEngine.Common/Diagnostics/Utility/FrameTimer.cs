#region

using System.Diagnostics;

#endregion

namespace ConcreteEngine.Common.Diagnostics.Utility;

public sealed class FrameTimer
{
    private const double TargetFrameMs = 16.6667; // 60 FPS 

    private long _totalTicks;
    private int _samples;
    private int _frameCounter;
    private double _lastAvgMs;

    private readonly int _sampleFrames;
    private readonly Stopwatch _sw = new();

    public FrameTimer(int sampleFrames = 60) => _sampleFrames = sampleFrames;
    public void Begin() => _sw.Restart();

    public bool End()
    {
        return End(out _);
    }

    public bool End(out double meanMs)
    {
        _sw.Stop();
        _totalTicks += _sw.ElapsedTicks;
        _frameCounter++;
        _samples++;

        if (_frameCounter >= _sampleFrames)
        {
            meanMs = (_totalTicks / (double)_samples) * 1000.0 / Stopwatch.Frequency;

            _lastAvgMs = meanMs;

            _frameCounter = 0;
            _totalTicks = 0;
            _samples = 0;

            return true;
        }

        meanMs = 0;
        return false;
    }

    public string ResultString
    {
        get
        {
            if (_lastAvgMs <= 0) return "Waiting for data...";
            var pct = (_lastAvgMs / TargetFrameMs) * 100.0;
            return $"{_lastAvgMs:F3} ms (~{pct:F2}% frame)";
        }
    }
}