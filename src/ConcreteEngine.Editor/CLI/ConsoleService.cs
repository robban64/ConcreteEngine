using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;

namespace ConcreteEngine.Editor.CLI;

internal struct LogEntry(RangeU16 handle)
{
    public const byte TimestampOffset = 15;
    public RangeU16 Handle = handle;
    public LogScope Scope;
    public LogLevel Level;
}

internal sealed class ConsoleService
{
    public const int LogStride = 128 + 16;
    public const int StoredLogCap = 128;

    private const int DefaultQueueCap = 64;

    private const int DrainPerTick = 6;
    private const int DrainPerTickHigh = 12;

    private int _head;
    private int _count;
    
    private NativeView<byte> _logText = NativeView<byte>.MakeNull();
    
    private readonly LogEntry[] _logs = new LogEntry[StoredLogCap];
    private readonly Queue<LogEvent> _structLogQueue = new(DefaultQueueCap);
    private readonly Queue<StringLogEvent> _stringLogQueue = new(DefaultQueueCap);

    public int LogCount => _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> GetLogText(RangeU16 handle) => _logText.Slice(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<LogEntry> GetLogs(int start, int length) => _logs.AsSpan(start, length);

    public void Setup()
    {
        _logText = TextBuffers.LogBuffer;
        _logText.Clear();
        for (int i = 0; i < StoredLogCap; i++)
            _logs[i] = new LogEntry(new RangeU16(i * LogStride, LogStride));
    }

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
                PushLog(writer.Append(strLog!.Message).EndSpan(), strLog.Timestamp, strLog.Level, strLog.Scope);
            }
            else
            {
                _structLogQueue.TryDequeue(out var sLog);
                var message = StructLogParser.GetLogMessage(writer, in sLog);
                PushLog(message, sLog.Timestamp, sLog.Level, sLog.Scope);
            }

            writer.Clear();
        }
    }

    private void PushLog(ReadOnlySpan<byte> message, DateTime timestamp, LogLevel level = LogLevel.None,
        LogScope scope = LogScope.Unknown)
    {
        ref var log = ref _logs[_head];
        log.Level = level;
        log.Scope = scope;

        var offset = _head > 0 ? _logs[_head - 1].Handle.End + 1 : 0;

        var sw = new UnsafeSpanWriter(_logText.SliceFrom(offset));
        sw.Append('[').Append(timestamp, "HH:mm:ss:fff").Append(']').End();
        sw.SetCursor(LogEntry.TimestampOffset);
        var text2 = sw.Append(message).End();

        log.Handle = new RangeU16(offset, text2.Length);

        _head = (_head + 1) % StoredLogCap;
        _count = Math.Min(_count + 1, StoredLogCap);

        ConsolePanel.ScrollToBottom();
    }

    private void PushPlain(string message) => PushLog(TextBuffers.GetWriter().Append(message).EndSpan(), default);

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
        if (_count == 0) return;

        foreach (ref var it in _logs.AsSpan(0, _count))
        {
            it.Level = 0;
            it.Scope = 0;
        }

        _head = 0;
        _count = 0;
    }

    public static void PrintCommands()
    {
        CommandDispatcher
            .ProcessCommandEntries(ConsoleGateway.MakeContext(), static (ctx, meta) => ctx.LogCommand(meta.ToString()));
    }
}