using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Time;

public struct AvgFrameTimer
{
    private long _startTicks;
    private long _accumulatedTicks;

    public int Ticks { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginSample() => _startTicks = Stopwatch.GetTimestamp();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int EndSample()
    {
        var end = Stopwatch.GetTimestamp();
        _accumulatedTicks += end - _startTicks;
        return ++Ticks;
    }

    public float Reset()
    {
        if (Ticks == 0)
        {
            _accumulatedTicks = 0;
            return 0f;
        }

        var avgMs = _accumulatedTicks * 1000f / (Stopwatch.Frequency * Ticks);

        _accumulatedTicks = 0;
        Ticks = 0;

        return avgMs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ResetAndPrint()
    {
        var value = Reset();
        Console.WriteLine($"{value:F5}ms");
        return value;
    }
}