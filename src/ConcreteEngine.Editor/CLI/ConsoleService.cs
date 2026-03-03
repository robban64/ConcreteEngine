using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.CLI;

internal sealed class LogItem(string message, LogScope scope, LogLevel level)
{
    public readonly string Message = message;
    public readonly LogScope Scope = scope;
    public readonly LogLevel Level = level;

    public String16Utf8 TimeString;
    public String16Utf8 ScopeString;
    public String16Utf8 LevelString;

    public void Compile(DateTime dateTime, FrameContext ctx)
    {
        TimeString = new String16Utf8(ctx.Sw.Append('[').Append(dateTime, "HH:mm:ss:fff").Append(']').EndSpan());
        ScopeString = new String16Utf8(ctx.Sw.Append('[').Append(Scope.ToLogText()).Append(']').EndSpan());
        LevelString = new String16Utf8(ctx.Sw.Append('[').Append(Level.ToLogText()).Append(']').EndSpan());
    }
}

internal sealed class ConsoleService
{
    private const int VisibleLogCap = 128;
    private const int StoredLogCap = 256;
    private const int DefaultQueueCap = 64;

    private const int DrainPerTick = 6;
    private const int DrainPerTickHigh = 12;

    private int _head;
    private int _count;

    public ConsolePanel? Console;

    private readonly StructLogParser _logParser = new();

    private readonly Queue<LogEvent> _structLogQueue = new(DefaultQueueCap);
    private readonly Queue<StringLogEvent> _stringLogQueue = new(DefaultQueueCap);

    private readonly List<StringLogEvent> _storedLogs = new(StoredLogCap);
    private readonly LogItem[] _logs = new LogItem[VisibleLogCap];

    public int LogCount => _count;
    public int StoredLogCount => _storedLogs.Count;

    internal ReadOnlySpan<LogItem> GetLogs() => _logs.AsSpan(0, _count);

    public void Enqueue(StringLogEvent evt) => _stringLogQueue.Enqueue(evt);
    public void Enqueue(in LogEvent evt) => _structLogQueue.Enqueue(evt);

    public void OnTick(FrameContext ctx)
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
                Dequeue(finalLog!, ctx);
            }
            else
            {
                _structLogQueue.TryDequeue(out var sLog);
                Dequeue(_logParser.ToStringLog(in sLog), ctx);
            }
        }
    }


    private void Dequeue(StringLogEvent evt, FrameContext ctx)
    {
        var item = new LogItem(evt.Message, evt.Scope, evt.Level);
        item.Compile(evt.Timestamp, ctx);

        _logs[_head] = item;
        _head = (_head + 1) % VisibleLogCap;
        _count = Math.Min(_count + 1, VisibleLogCap);

        if (!evt.IsPlain())
        {
            _storedLogs.Add(evt);
            if (_storedLogs.Count >= StoredLogCap - 1)
                _storedLogs.Clear();
        }

        Console?.ScrollToBottom();
    }

    internal bool ExecCommand(Span<char> line, FrameContext ctx)
    {
        if (line.IsEmpty || line.IsWhiteSpace()) return false;
        line = line.Trim();

        Dequeue(StringLogEvent.MakePlain($">> {line}"), ctx);

        var parts = line.Split(' ');
        var cmd = parts.MoveNext() ? line[parts.Current].ToString() : string.Empty;
        var action = parts.MoveNext() ? line[parts.Current].ToString() : string.Empty;
        var arg1 = parts.MoveNext() ? line[parts.Current].ToString() : string.Empty;
        var arg2 = parts.MoveNext() ? line[parts.Current].ToString() : string.Empty;

        if (cmd is "clear")
        {
            ClearLog();
            Dequeue(StringLogEvent.MakePlain("[console cleared]"), ctx);
            return true;
        }

        if (cmd is "help" || cmd is "info")
        {
            PrintCommands();
            return true;
        }

        try
        {
            CommandDispatcher.InvokeCommand(ConsoleGateway.MakeContext(), cmd, action, arg1, arg2);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            Dequeue(StringLogEvent.MakeCommandError($"Error when invoking {cmd} with error: {ex.Message}"), ctx);
            return false;
        }

        return true;
    }

/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal StringLogEvent GetActiveLog(int i)
    {
        var startOffset = (_head - _count + VisibleLogCap) & (VisibleLogCap - 1);
        var idx = (startOffset + i) & (VisibleLogCap - 1);
        return GetLogs()[idx];
    }
*/
    private void ClearLog()
    {
        _logs.AsSpan().Clear();
        _head = 0;
        _count = 0;
    }

    public static void PrintCommands()
    {
        CommandDispatcher
            .ProcessCommandEntries(ConsoleGateway.MakeContext(), static (ctx, meta) => ctx.LogCommand(meta.ToString()));
    }
}