using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Components;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

public sealed class ConsoleCtx
{
    private readonly Action<string?> _addStringLogDel;

    internal ConsoleCtx(Action<string?> addStringLogDel)
    {
        _addStringLogDel = addStringLogDel;
    }

    public void AddLog(string? msg) => _addStringLogDel(msg);
}

public static class ConsoleService
{
    private const int MaxLogCount = 128;

    private static readonly string[] LogBuffer;
    private static int _head = 0;
    private static int _count = 0;

    private static readonly ConsoleCtx ConsoleCtx;

    static ConsoleService()
    {
        LogBuffer = new string[MaxLogCount];
        ConsoleCtx = new ConsoleCtx(SendLog);
    }

    internal static ReadOnlySpan<string> GetLogs() => LogBuffer.AsSpan(0, _count);

    public static int LogCount => _count;


    public static void Draw(int leftPanelWidth, int rightPanelWidth) =>
        ConsoleComponent.DrawConsole(leftPanelWidth, rightPanelWidth);

    public static void SendLog(string? msg)
    {
        if (msg is null) return;
        AppendLog(msg);
        ConsoleComponent.ScrollToBottom();
    }


    internal static bool ExecCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return false;

        AppendLog($">> {commandLine}");
        var parts = commandLine.Trim().Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0];
        var action = parts.Length > 1 ? parts[1] : null;
        var arg1 = parts.Length > 2 ? parts[2] : null;
        var arg2 = parts.Length > 3 ? parts[3] : null;

        if (cmd == "clear")
        {
            ClearLog();
            SendLog("[console cleared]");
            return true;
        }

        if (cmd == "help" || cmd == "info")
        {
            PrintCommands();
            return true;
        }

        try
        {
            CommandDispatcher.InvokeCommand(ConsoleCtx, cmd, action ?? "", arg1, arg2);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            SendLog($"Error when invoking {cmd} with error: {ex.Message}");
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSlotIndex(int idx)
    {
        int startOffset = (_head - _count + MaxLogCount) & (MaxLogCount - 1);
        return (startOffset + idx) & (MaxLogCount - 1);

        //int startOffset = (_head - _count + MaxLogCount) % MaxLogCount;
        //return (startOffset + idx) % MaxLogCount;
    }

    private static void AppendLog(string msg)
    {
        LogBuffer[_head] = msg;
        _head = (_head + 1) % MaxLogCount;
        _count = Math.Min(_count + 1, MaxLogCount);
    }

    private static void ClearLog()
    {
        LogBuffer.AsSpan().Clear();
        _head = 0;
        _count = 0;
    }

    private static void PrintCommands()
    {
        CommandDispatcher.ProcessRegistryRecords(ConsoleCtx, static (ctx, command, existsIn) =>
        {
            var console = StringUtils.BoolToYesNo(existsIn.Item1);
            var editor = StringUtils.BoolToYesNo(existsIn.Item2);

            ctx.AddLog($"{command} - Console={console} , Editor={editor}");
        });
    }
}