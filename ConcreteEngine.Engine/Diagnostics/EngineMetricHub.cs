using ConcreteEngine.Common;
using ConcreteEngine.Shared.Diagnostics;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Engine.Diagnostics;

internal static class EngineMetricHub
{
    private static readonly List<FrameMetricSample> FrameSamples = new(256);

    public static void Attach(EngineSystemProfiler profiler)
    {
        InvalidOpThrower.ThrowIf(FrameSamples.Count > 0);
        //profiler.RegisterReportInterval(144*2, OnReport);
        //profiler.RegisterReportInterval(144 * 4, OnFullReport);
    }

    private static void OnReport(FrameMetricSample sample)
    {
        if (FrameSamples.Count >= 255) FrameSamples.Clear();
        FrameSamples.Add(sample);
        PrintShortLog(sample);
    }

    private static void OnFullReport(FrameMetricSample sample)
    {
        var log = GenerateStringLog(sample);
        Logger.LogString(LogScope.Engine, log, LogLevel.Info);
    }


    private static void PrintShortLog(FrameMetricSample s)
    {
        Span<char> buffer = stackalloc char[128];
        var builder = ZaSpanStringBuilder.Create(buffer);

        builder.Append("Max: ").Append(s.MaxMs, "F4").Append("ms | ")
            .Append(" Avg: ").Append(s.AvgMs, "F4").Append("ms | ")
            .Append("Alloc/s: ").Append(s.AllocMbPerSec, "F4").Append("MB");

        builder.Append(s.HasSpiked ? " | [SPIKE]" : " | [Frame]");
        switch (s.GcActivity)
        {
            case GcActivity.Minor: builder.Append(" | [GC INFO]"); break;
            case GcActivity.Major: builder.Append(" | [Gc Warn]"); break;
        }

        var original = Console.ForegroundColor;
        if (s.GcActivity == GcActivity.Minor || s.HasSpiked)
            Console.ForegroundColor = ConsoleColor.Yellow;
        else if (s.GcActivity == GcActivity.Major || (s.GcActivity == GcActivity.Minor && s.HasSpiked))
            Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(builder.AsSpan());
        Console.ForegroundColor = original;
    }


    private static string GenerateStringLog(FrameMetricSample s)
    {
        Span<char> buffer = stackalloc char[128];
        var builder = ZaSpanStringBuilder.Create(buffer);

        builder
            .Append(s.AvgMs, "F4")
            .Append("ms (Min:").AppendIf(s.MinMs < 10, " ").Append(s.MinMs, "F2")
            .Append(" Max:").AppendIf(s.MaxMs < 10, " ").Append(s.MaxMs, "F2")
            .Append(") | ");

        builder.Append("Load: ").Append(s.Load, "F1").Append("% | ");
        builder.Append("Alloc/s: ").Append(s.AllocMbPerSec, "F2").Append("MB | ");

        builder.Append("Mem: ").Append(s.AllocatedMb).Append("MB");

        builder.Append(s.HasSpiked ? " | [SPIKE]" : " | [Frame]");
        switch (s.GcActivity)
        {
            case GcActivity.Minor: builder.Append(" | [GC INFO]"); break;
            case GcActivity.Major: builder.Append(" | [Gc Warn]"); break;
        }

        return builder.ToString();
    }
}