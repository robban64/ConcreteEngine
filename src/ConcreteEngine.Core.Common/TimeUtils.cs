using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common;

public static class TimeUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetElapsedMilliseconds(long startTimestamp, long endTimestamp)
    {
        return (endTimestamp - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetElapsedMillisecondsSince(long startTimestamp)
    {
        return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }
}