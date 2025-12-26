using ConcreteEngine.Common;
using ConcreteEngine.Editor;
using ConcreteEngine.Renderer.State;
using ConcreteEngine.Shared.Diagnostics;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Engine.Diagnostics;

internal static class EngineMetricHub
{
    private static readonly List<PerformanceMetric> FrameSamples = new(8);

    private static PerformanceMetric _performanceMetric;
    private static FrameMetaBundle _frameMeta;
    private static SceneMeta _sceneMeta;
    private static GpuBufferMeta _bufferMeta;

    public static bool PrintReport = false;
    public static bool LogReport = false;


    internal static void WireEditor()
    {
        MetricsApi.Provider<PerformanceMetric>.Register(() => 1337, 1337);
    }

    public static void Attach(EngineSystemProfiler profiler)
    {
        InvalidOpThrower.ThrowIf(FrameSamples.Count > 0);
        profiler.RegisterReportInterval(144 * 2, OnReport);
    }

    private static void OnReport(PerformanceMetric sample)
    {
        _performanceMetric = sample;
        if (FrameSamples.Count >= 7) FrameSamples.Clear();
        if (PrintReport) PrintShortLog(sample);
        if (LogReport) FrameSamples.Add(sample);
    }

    private static void PrintSample(Span<char> message, in PerformanceMetric sample)
    {
        var original = Console.ForegroundColor;
        if (sample.GcActivity == GcActivity.Minor || sample.HasSpiked)
            Console.ForegroundColor = ConsoleColor.Yellow;
        else if (sample.GcActivity == GcActivity.Major)
            Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(message);
        Console.ForegroundColor = original;
    }

    private static void OnFullReport(PerformanceMetric sample)
    {
        var log = GenerateStringLog(sample);
        var level = LogLevel.Info;
        if (sample.GcActivity == GcActivity.Minor || sample.HasSpiked) level = LogLevel.Debug;
        if (sample.GcActivity == GcActivity.Major) level = LogLevel.Warn;

        Logger.LogString(LogScope.Engine, log, level);
    }


    private static void PrintShortLog(PerformanceMetric s)
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
    }


    private static string GenerateStringLog(PerformanceMetric s)
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