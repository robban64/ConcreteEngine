using System.Diagnostics;

namespace ConcreteEngine.Core.Diagnostics.Time;

public sealed class DurationProfileTimer(TimeSpan sampleInterval, string name = "")
{
    public static readonly DurationProfileTimer Default = new(TimeSpan.FromSeconds(1));

    private readonly long _windowIntervalTicks = (long)(sampleInterval.TotalSeconds * Stopwatch.Frequency);

    private long _accumulatedTicks;
    private long _sampleCount;
    private long _windowStartTimestamp = Stopwatch.GetTimestamp();
    private long _currentOpStartTimestamp;

    private double _lastMeanMs;

    public void Begin()
    {
        _currentOpStartTimestamp = Stopwatch.GetTimestamp();
    }

    public bool End(out double meanMs)
    {
        long now = Stopwatch.GetTimestamp();
        _accumulatedTicks += now - _currentOpStartTimestamp;
        _sampleCount++;

        if (now - _windowStartTimestamp >= _windowIntervalTicks)
        {
            _lastMeanMs = _accumulatedTicks / (double)_sampleCount
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

    public bool EndPrintSimple()
    {
        if (End(out _))
        {
            double microseconds = _lastMeanMs * 1000.0;
            int displayValue = (int)Math.Round(microseconds);

            var n = string.IsNullOrEmpty(name) ? "" : $"{name}: ";
            Console.WriteLine($"{n}{displayValue} µs (crude avg)");
            return true;
        }

        return false;
    }

    public bool EndPrint()
    {
        var res = End(out _);
        if (res) Console.WriteLine(ResultString);
        return res;
    }

    public string ResultString
    {
        get
        {
            var n = string.IsNullOrEmpty(name) ? "" : $"{name}: ";
            return $"{n}{_lastMeanMs:F6} ms (Avg over window)";
        }
    }
}