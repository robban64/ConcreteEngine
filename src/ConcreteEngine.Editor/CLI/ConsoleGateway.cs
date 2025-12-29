using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Data;

namespace ConcreteEngine.Editor.CLI;

internal sealed record ConsoleCommandMeta(string Name, string Description, bool IsNoOp);

internal sealed class ConsoleCommandEntry
{
    public required ConsoleCommandMeta Meta { get; init; }
    public required ConsoleCommandDel Handler { get; init; }
}

public ref struct ConsoleContext
{
    public void LogStruct(in LogEvent log) => ConsoleGateway.LogStruct(log);
    public void Log(StringLogEvent log) => ConsoleGateway.Log(log);
    public void LogPlain(string log) => ConsoleGateway.LogPlain(log);
}

public static class ConsoleGateway
{
    internal static readonly ConsoleService Service = new();

    private const int MaxLogQueueSize = 512;
    private const int DefaultQueueCap = 64;

    private const int DrainPerTick = 6;
    private const int DrainPerTickHigh = 12;

    private static readonly Queue<LogEvent> StructLogQueue = new(DefaultQueueCap);
    private static readonly Queue<StringLogEvent> StringLogQueue = new(DefaultQueueCap);
    private static readonly StructLogParser LogParser = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogStruct(in LogEvent log) => StructLogQueue.Enqueue(log);
    
    public static void Log(StringLogEvent? log)
    {
        ArgumentNullException.ThrowIfNull(log);
        StringLogQueue.Enqueue(log);
    }

    public static void LogPlain(string? log)
    {
        ArgumentException.ThrowIfNullOrEmpty(log);
        StringLogQueue.Enqueue(StringLogEvent.MakePlain(log));
    }

    public static void OnTick()
    {
        var count = StringLogQueue.Count + StructLogQueue.Count;
        if (count == 0) return;

        int drainLimit = count < 100 ? DrainPerTick : DrainPerTickHigh;

        while (drainLimit-- > 0)
        {
            bool hasString = StringLogQueue.TryPeek(out var nextStringLog);
            bool hasStruct = StructLogQueue.TryPeek(out var nextStructLog);

            if (!hasString && !hasStruct) break;

            bool pickString;
            if (hasString && hasStruct)
                pickString = nextStringLog!.Timestamp <= nextStructLog.Timestamp;
            else
                pickString = hasString;

            if (pickString)
            {
                StringLogQueue.TryDequeue(out var finalLog);
                Service.Enqueue(finalLog!);
            }
            else
            {
                StructLogQueue.TryDequeue(out var sLog);
                Service.Enqueue(LogParser.ToStringLog(in sLog));
            }
        }
    }

    internal static void ExecCommand(string cmd) => Service.ExecCommand(cmd);
}