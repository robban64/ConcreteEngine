using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Editor.CLI;

internal sealed class ConsoleService
{
    private const int VisibleLogCap = 128;
    private const int StoredLogCap = 256;
    private const int DefaultQueueCap = 64;

    private const int DrainPerTick = 6;
    private const int DrainPerTickHigh = 12;

    private int _head;
    private int _count;

    private readonly StructLogParser _logParser = new();

    private readonly Queue<LogEvent> _structLogQueue = new(DefaultQueueCap);
    private readonly Queue<StringLogEvent> _stringLogQueue = new(DefaultQueueCap);

    private readonly List<StringLogEvent> _storedLogs = new(StoredLogCap);
    private readonly StringLogEvent[] _logs = new StringLogEvent[VisibleLogCap];

    public int LogCount => _count;
    public int StoredLogCount => _storedLogs.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<StringLogEvent> GetLogs() => _logs.AsSpan(0, _count);

    public void Enqueue(StringLogEvent evt) => _stringLogQueue.Enqueue(evt);
    public void Enqueue(in LogEvent evt) => _structLogQueue.Enqueue(evt);

    public void OnTick()
    {
        var count = _stringLogQueue.Count + _structLogQueue.Count;
        if (count == 0) return;

        int drainLimit = count < 100 ? DrainPerTick : DrainPerTickHigh;

        while (drainLimit-- > 0)
        {
            bool hasString = _stringLogQueue.TryPeek(out var nextStringLog);
            bool hasStruct = _structLogQueue.TryPeek(out var nextStructLog);

            if (!hasString && !hasStruct) break;

            bool pickString;
            if (hasString && hasStruct)
                pickString = nextStringLog!.Timestamp <= nextStructLog.Timestamp;
            else
                pickString = hasString;

            if (pickString)
            {
                _stringLogQueue.TryDequeue(out var finalLog);
                Dequeue(finalLog!);
            }
            else
            {
                _structLogQueue.TryDequeue(out var sLog);
                Dequeue(_logParser.ToStringLog(in sLog));
            }
        }
    }


    private void Dequeue(StringLogEvent evt)
    {
        _logs[_head] = evt;
        _head = (_head + 1) % VisibleLogCap;
        _count = Math.Min(_count + 1, VisibleLogCap);

        if (!evt.IsPlain())
        {
            _storedLogs.Add(evt);
            if (_storedLogs.Count >= StoredLogCap - 1)
                _storedLogs.Clear();
        }

        ConsoleComponent.ScrollToBottom();
    }

    internal bool ExecCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return false;

        Enqueue(StringLogEvent.MakePlain($">> {commandLine}"));
        var parts = commandLine.Trim().Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0];

        if (cmd == "clear")
        {
            ClearLog();
            Enqueue(StringLogEvent.MakePlain("[console cleared]"));
            return true;
        }

        if (cmd == "help" || cmd == "info")
        {
            PrintCommands();
            return true;
        }

        var action = parts.Length > 1 ? parts[1] : null;
        var arg1 = parts.Length > 2 ? parts[2] : null;
        var arg2 = parts.Length > 3 ? parts[3] : null;

        try
        {
            CommandDispatcher.InvokeCommand(ConsoleGateway.MakeContext(), cmd, action ?? "", arg1, arg2);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            Enqueue(StringLogEvent.MakePlain($"Error when invoking {cmd} with error: {ex.Message}"));
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetSlotIndex(int idx)
    {
        var startOffset = (_head - _count + VisibleLogCap) & (VisibleLogCap - 1);
        return (startOffset + idx) & (VisibleLogCap - 1);
    }

    private void ClearLog()
    {
        _logs.AsSpan().Clear();
        _head = 0;
        _count = 0;
    }

    private static void PrintCommands()
    {
        CommandDispatcher
            .ProcessCommandEntries(ConsoleGateway.MakeContext(), static (ctx, meta) => ctx.LogPlain(meta.ToString()));
    }
}