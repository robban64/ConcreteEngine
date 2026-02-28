using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Diagnostics.Time;

public struct AvgFrameTimer
{
    private long _startTicks;
    private long _accumulatedTicks;
    private int _count;

    public int Count => _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BeginSample() => _startTicks = Stopwatch.GetTimestamp();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void EndSample()
    {
        var end = Stopwatch.GetTimestamp();
        _accumulatedTicks += (end - _startTicks);
        _count++;
    }

    public float Reset()
    {
        if (_count == 0)
        {
            _accumulatedTicks = 0;
            return 0f;
        }

        var avgMs = _accumulatedTicks * 1000f / (Stopwatch.Frequency * _count);

        _accumulatedTicks = 0;
        _count = 0;

        return avgMs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetAndPrint()
    {
        Console.WriteLine($"{Reset():F5}ms");
    }
}