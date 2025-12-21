using System.Diagnostics;

namespace ConcreteEngine.Common.Time;
public sealed class DurationProfileTimer
{
    private readonly long _windowIntervalTicks;
    
    private long _accumulatedTicks;
    private long _sampleCount;
    private long _windowStartTimestamp;
    private long _currentOpStartTimestamp;
    
    private double _lastMeanMs;

    public DurationProfileTimer(TimeSpan sampleInterval)
    {
        _windowIntervalTicks = (long)(sampleInterval.TotalSeconds * Stopwatch.Frequency);
        _windowStartTimestamp = Stopwatch.GetTimestamp();
    }

    public void Begin()
    {
        _currentOpStartTimestamp = Stopwatch.GetTimestamp();
    }

    public bool End(out double meanMs)
    {
        long now = Stopwatch.GetTimestamp();
        _accumulatedTicks += (now - _currentOpStartTimestamp);
        _sampleCount++;

        if (now - _windowStartTimestamp >= _windowIntervalTicks)
        {
            _lastMeanMs = (_accumulatedTicks / (double)_sampleCount) 
                / Stopwatch.Frequency * 1000.0;

            meanMs = _lastMeanMs;

            // Reset for next window
            _accumulatedTicks = 0;
            _sampleCount = 0;
            _windowStartTimestamp = now;
            return true;
        }

        meanMs = 0;
        return false;
    }
    
    public bool EndPrint()
    {
        var res = End(out _);
        if (res) Console.WriteLine(ResultString);
        return res;
    }

    public string ResultString => $"{_lastMeanMs:F6} ms (Avg over window)";
}