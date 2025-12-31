using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;

namespace ConcreteEngine.Editor.CLI;

public sealed class ConsoleService()
{
    private const int VisibleLogCap = 128;
    private const int StoredLogCap = 256;

    private int _head;
    private int _count;

    private readonly List<StringLogEvent> _storedLogs = new(StoredLogCap);
    private readonly StringLogEvent[] _logs = new StringLogEvent[VisibleLogCap];

    public int LogCount => _count;
    public int StoredLogCount => _storedLogs.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ReadOnlySpan<StringLogEvent> GetLogs() => _logs.AsSpan(0, _count);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Enqueue(StringLogEvent log)
    {
        EnqueueInternal(log);
        ConsoleComponent.ScrollToBottom();
    }

    private void EnqueueInternal(StringLogEvent evt)
    {
        _logs[_head] = evt;
        _head = (_head + 1) % VisibleLogCap;
        _count = Math.Min(_count + 1, VisibleLogCap);

        if (evt.IsPlain()) return;

        _storedLogs.Add(evt);
        if (_storedLogs.Count >= StoredLogCap - 1)
            _storedLogs.Clear();
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
            CommandDispatcher.InvokeCommand(new ConsoleContext(), cmd, action ?? "", arg1, arg2);
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
        int startOffset = (_head - _count + VisibleLogCap) & (VisibleLogCap - 1);
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
            .ProcessCommandEntries(new ConsoleContext(), static (ctx, meta) => ctx.LogPlain(meta.ToString()));
    }
}