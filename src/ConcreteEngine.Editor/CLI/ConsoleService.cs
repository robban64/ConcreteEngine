using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics;

namespace ConcreteEngine.Editor.CLI;

internal sealed class ConsoleService(ConsoleContext context)
{
    private const int LogCap = 128;

    private int _head = 0;
    private int _count = 0;

    private readonly List<StringLogEvent> _allLogs = new(LogCap);
    private readonly StringLogEvent[] _logs = new StringLogEvent[LogCap];

    public int LogCount => _count;
    public int StoredLogCount => _allLogs.Count;

    public ReadOnlySpan<StringLogEvent> GetLogs() => _logs.AsSpan(0, _count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string log)
    {
        AppendInternal(StringLogEvent.MakePlain(log));
        ConsoleComponent.ScrollToBottom();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(StringLogEvent log)
    {
        AppendInternal(log);
        ConsoleComponent.ScrollToBottom();
    }

    public void AppendMany(ReadOnlySpan<StringLogEvent> logs)
    {
        foreach (var log in logs) AppendInternal(log);
        ConsoleComponent.ScrollToBottom();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendInternal(StringLogEvent evt)
    {
        _logs[_head] = evt;
        _head = (_head + 1) % LogCap;
        _count = Math.Min(_count + 1, LogCap);

        if (evt.IsPlain()) return;
        _allLogs.Add(evt);
        if (_allLogs.Count > 512)
        {
            _allLogs.RemoveRange(256, _allLogs.Count - 256);
        }
    }


    public bool ExecCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return false;

        Append($">> {commandLine}");
        var parts = commandLine.Trim().Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0];

        if (cmd == "clear")
        {
            ClearLog();
            Append("[console cleared]");
            return true;
        }

        if (cmd == "help" || cmd == "info")
        {
            PrintCommands(context);
            return true;
        }

        var action = parts.Length > 1 ? parts[1] : null;
        var arg1 = parts.Length > 2 ? parts[2] : null;
        var arg2 = parts.Length > 3 ? parts[3] : null;

        try
        {
            CommandDispatcher.InvokeCommand(context, cmd, action ?? "", arg1, arg2);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            Append($"Error when invoking {cmd} with error: {ex.Message}");
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetSlotIndex(int idx)
    {
        int startOffset = (_head - _count + LogCap) & (LogCap - 1);
        return (startOffset + idx) & (LogCap - 1);
    }

    private void ClearLog()
    {
        _logs.AsSpan().Clear();
        _head = 0;
        _count = 0;
    }

    private static void PrintCommands(ConsoleContext ctx)
    {
        CommandDispatcher.ProcessCommandEntries(ctx, static (ctx, meta) => ctx.AddLog(meta.ToString()));
    }
}