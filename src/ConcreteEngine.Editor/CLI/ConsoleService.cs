using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;

namespace ConcreteEngine.Editor.CLI;

internal readonly struct LogEntry
{
    public const int DateLength = 15;

    public readonly byte[] Message;
    public readonly LogScope Scope;
    public readonly LogLevel Level;

    public LogEntry(UnsafeSpanWriter sw, string message, DateTime timestamp, LogScope scope, LogLevel level)
    {
        Scope = scope;
        Level = level;

        var len = DateLength + Encoding.UTF8.GetByteCount(message) + 1;
        Message = new byte[len];

        var dateSpan = sw.Append('[').Append(timestamp, "HH:mm:ss:fff").Append(']').EndSpan();
        dateSpan.CopyTo(Message.AsSpan(0, DateLength));

        var msgSpan = Message.AsSpan(DateLength);
        int written = Encoding.UTF8.GetBytes(message, msgSpan);
        msgSpan[written] = 0;
    }

}

internal sealed class ConsoleService
{
    private const int StoredLogCap = 128;

    private const int DefaultQueueCap = 64;

    private const int DrainPerTick = 6;
    private const int DrainPerTickHigh = 12;

    private int _head;
    private int _count;

    public int LogCount => _count;

    private readonly LogEntry[] _logs = new LogEntry[StoredLogCap];

    private readonly Queue<LogEvent> _structLogQueue = new(DefaultQueueCap);
    private readonly Queue<StringLogEvent> _stringLogQueue = new(DefaultQueueCap);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<LogEntry> GetLogs() => _logs.AsSpan(0, _count);

    public void Enqueue(StringLogEvent evt) => _stringLogQueue.Enqueue(evt);
    public void Enqueue(in LogEvent evt) => _structLogQueue.Enqueue(evt);

    public void OnTick()
    {
        var count = _stringLogQueue.Count + _structLogQueue.Count;
        if (count == 0) return;

        int drainLimit = count < 100 ? DrainPerTick : DrainPerTickHigh;

        var writer = TextBuffers.GetWriter();
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
                _stringLogQueue.TryDequeue(out var strLog);
                var entry = new LogEntry(writer, strLog!.Message, strLog.Timestamp, strLog.Scope, strLog.Level);
                Dequeue(in entry);
            }
            else
            {
                _structLogQueue.TryDequeue(out var sLog);
                var message = StructLogParser.GetLogMessage(in sLog);
                var entry = new LogEntry(writer, message, sLog.Timestamp, sLog.Scope, sLog.Level);
                Dequeue(in entry);
            }
        }
    }


    private void Dequeue(in LogEntry log)
    {
        _logs[_head] = log;
        _head = (_head + 1) % StoredLogCap;
        _count = Math.Min(_count + 1, StoredLogCap);

        ConsolePanel.ScrollToBottom();
    }

    private void PushPlain(string message)
    {
        Dequeue(new LogEntry(TextBuffers.GetWriter(), message, default, LogScope.Unknown, LogLevel.None));
    }

    internal bool ExecCommand(Span<char> line)
    {
        if (line.IsEmpty || line.IsWhiteSpace()) return false;
        line = line.Trim();

        PushPlain($">> {line}");

        var parts = line.Split(' ');
        var cmd = parts.MoveNext() ? line[parts.Current].ToString() : string.Empty;
        var action = parts.MoveNext() ? line[parts.Current].ToString() : string.Empty;
        var arg1 = parts.MoveNext() ? line[parts.Current].ToString() : string.Empty;
        var arg2 = parts.MoveNext() ? line[parts.Current].ToString() : string.Empty;

        if (cmd is "clear")
        {
            ClearLog();
            PushPlain("[console cleared]");
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
            PushPlain($"Error when invoking {cmd} with error: {ex.Message}");
            return false;
        }

        return true;
    }

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