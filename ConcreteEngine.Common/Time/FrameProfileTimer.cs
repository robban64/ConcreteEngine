#region

using System.Diagnostics;

#endregion

namespace ConcreteEngine.Common.Time;

public static class StaticProfileTimer
{
    public static readonly FrameProfileTimer TickTimer = new();
    public static readonly FrameProfileTimer RenderTimer = new(144, 1.0 / 144.0 * 1000);
}

public sealed class FrameProfileTimer
{
    private long _totalTicks;
    private int _samples;
    private int _frameCounter;
    private double _lastAvgMs;

    private readonly int _sampleFrames;
    private readonly double _targetFrameMs;
    private readonly Stopwatch _sw = new();

    public FrameProfileTimer(int sampleFrames = 60, double targetFrameMs = 16.6667)
    {
        _sampleFrames = sampleFrames;
        _targetFrameMs = targetFrameMs;
    }

    public void Begin() => _sw.Restart();

    public bool End()
    {
        return End(out _);
    }

    public bool EndPrint()
    {
        var res = End(out _);
        if (res) Console.WriteLine(ResultString);
        return res;
    }


    public bool End(out double meanMs)
    {
        _sw.Stop();
        _totalTicks += _sw.ElapsedTicks;
        _frameCounter++;
        _samples++;

        if (_frameCounter >= _sampleFrames)
        {
            meanMs = _totalTicks / (double)_samples * 1000.0 / Stopwatch.Frequency;

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
            var pct = _lastAvgMs / _targetFrameMs * 100.0;
            return $"{_lastAvgMs:F6} ms (~{pct:F2}% frame)";
        }
    }
}