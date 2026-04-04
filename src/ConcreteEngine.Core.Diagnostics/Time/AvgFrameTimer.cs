using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public float ResetAndPrint(string? prefix = null)
    {
        var value = Reset();
        
        var len = prefix?.Length ?? 0;
        var sw = new SpanWriter(stackalloc char[16 + len]);
        if(prefix != null) sw.Append(prefix).Append(": ");
        sw.Append(value, "F5").Append("ms");
        Console.WriteLine(sw.End());
        return value;
    }
}