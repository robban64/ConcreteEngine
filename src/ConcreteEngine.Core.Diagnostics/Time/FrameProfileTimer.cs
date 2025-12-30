using System.Diagnostics;

namespace ConcreteEngine.Core.Diagnostics.Time;

public sealed class FrameProfileTimer(int sampleFrames = 144, double targetFrameMs = 1f / 144f * 1000)
{
    private long _totalTicks;
    private int _samples;
    private int _frameCounter;
    private double _lastAvgMs;

    private readonly Stopwatch _sw = new();

    public void Begin() => _sw.Restart();

    public bool End()
    {
        return End(out _);
    }

    public bool EndPrint(string? prefix = null)
    {
        var res = End(out _);
        if (res)
        {
            if (prefix is not null) Console.WriteLine($"{prefix}: {ResultString}");
            else Console.WriteLine(ResultString);
        }

        return res;
    }


    public bool End(out double meanMs)
    {
        _sw.Stop();
        _totalTicks += _sw.ElapsedTicks;
        _frameCounter++;
        _samples++;

        if (_frameCounter >= sampleFrames)
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
            var pct = _lastAvgMs / targetFrameMs * 100.0;
            return $"{_lastAvgMs:F6} ms (~{pct:F2}% frame)";
        }
    }
}