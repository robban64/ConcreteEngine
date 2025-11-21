#region

using System.Diagnostics;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Common.Time;

public static class TimeUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetFastTimestamp() => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetTimestamp() => (DateTime.UtcNow.Ticks - 621355968000000000L) / 10_000;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasIntervalPassed(long lastTimestampMs, long durationMs) =>
        GetTimestamp() >= lastTimestampMs + durationMs;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsElapsedForFrequency(long lastTimestampMs, float frequencyHz)
    {
        float intervalMs = 1000f / frequencyHz;
        long nowMs = GetTimestamp();
        return nowMs >= lastTimestampMs + intervalMs;
    }
}