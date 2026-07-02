using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.App.CLI;
using ConcreteEngine.Editor.Core.Data;
using static ConcreteEngine.Editor.Logging.LogConsts;

namespace ConcreteEngine.Editor.Logging;

internal sealed class LogService
{
    public static readonly LogService Instance = new();

    public int NewLogs { get; private set; }
    
    private int _head;
    private int _count;

    private NativeView<byte> _logText = NativeView<byte>.MakeNull();

    private readonly LogEntry[] _logs = new LogEntry[StoredLogCap];

    private readonly Queue<LogEvent> _valueLogQueue = new(DefaultQueueCap);
    private readonly Queue<StringLogEvent> _stringLogQueue = new(DefaultQueueCap);

    public int LogCount => _count;
    public int EnqueuedLogCount => _stringLogQueue.Count + _valueLogQueue.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeView<byte> GetLogText(RangeU16 handle) => _logText.Slice(handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<LogEntry> GetLogs(int start, int length) => new(_logs, start, length);

    public void Setup()
    {
        _logText = TextBuffers.LogBuffer;
        _logText.Clear();
        for (int i = 0; i < StoredLogCap; i++)
            _logs[i] = new LogEntry(new RangeU16(i * LogStride, LogStride));
    }

    public void ResetNewLogCount() => NewLogs = 0;
    public void Enqueue(StringLogEvent evt) => _stringLogQueue.Enqueue(evt);
    public void Enqueue(in LogEvent evt) => _valueLogQueue.Enqueue(evt);

    [SkipLocalsInit]
    public unsafe void OnTick()
    {
        if (EnqueuedLogCount == 0) return;

        var buffer = stackalloc byte[256];
        var writer = new NativeSpanWriter(buffer, 256);
        
        int drainLimit = EnqueuedLogCount < 100 ? DrainPerTick : DrainPerTickHigh;
        while (drainLimit-- > 0)
        {
            bool hasString = _stringLogQueue.TryPeek(out var nextStringLog);
            bool hasValue = _valueLogQueue.TryPeek(out var nextStructLog);

            if (!hasString && !hasValue) break;

            bool pickString;
            if (hasString && hasValue)
                pickString = nextStringLog!.Timestamp <= nextStructLog.Timestamp;
            else
                pickString = hasString;

            if (pickString && _stringLogQueue.TryDequeue(out var strLog))
            {
                PushLog(writer.Append(strLog.Message).EndSpan(), strLog.Timestamp, strLog.Level, strLog.Scope);
            }
            else if (_valueLogQueue.TryDequeue(out var sLog))
            {
                var message = ValueLogParser.GetLogMessage(writer, in sLog);
                PushLog(message, sLog.Timestamp, sLog.Level, sLog.Scope);
            }

            writer.Clear();
        }

    }
    
    public void PushLog(ReadOnlySpan<byte> message, DateTime timestamp, LogLevel level = LogLevel.None,
        LogScope scope = LogScope.Unknown)
    {
        var offset = _head > 0 ? _logs[_head - 1].Handle.End + 1 : 0;

        var sw = _logText.SliceFrom(offset).Writer();
        sw.Append('[').Append(timestamp, "HH:mm:ss:ff").Append(']');
        sw.SetCursor(LogEntry.TimestampOffset);
        sw.Append(message.Truncate(LogStride - LogEntry.TimestampOffset));
        var cursor = sw.End().Length;

        ref var log = ref _logs[_head];
        log.Level = level;
        log.Scope = scope;
        log.Handle = new RangeU16(offset, cursor);

        _head = (_head + 1) % StoredLogCap;
        _count = int.Min(_count + 1, StoredLogCap);

        NewLogs++;
    }
    
    public void ClearLog()
    {
        if (_count == 0) return;

        for (var i = 0; i < _logs.Length; i++)
        {
            ref var it = ref _logs[i];
            it.Level = 0;
            it.Scope = 0;
        }

        _head = 0;
        _count = 0;
        NewLogs = 0;
    }

    public static void Log(StringLogEvent log) => Instance.Enqueue(log);
    public static void LogValue(in LogEvent log) => Instance.Enqueue(in log);
    
    [SkipLocalsInit]
    public static unsafe void PushMessage(ReadOnlySpan<char> message)
    {
        var buffer = stackalloc byte[128];
        var writer = new NativeSpanWriter(buffer, 128);
        Instance.PushLog(writer.Append(message).EndSpan(), default);
    }

}