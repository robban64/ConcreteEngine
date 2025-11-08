#region

using ConcreteEngine.Editor.Gui.Components;
using ConcreteEngine.Editor.Utils;

#endregion

namespace ConcreteEngine.Editor;

public sealed class DebugConsoleCtx
{
    private readonly Action<string?> _addStringLogDel;

    internal DebugConsoleCtx(Action<string?> addStringLogDel)
    {
        _addStringLogDel = addStringLogDel;
    }

    public void AddLog(string? msg) => _addStringLogDel(msg);
}

public static class DevConsoleService
{
    private const int MaxLogCount = 128;

    internal static readonly List<string> Log = new(MaxLogCount);

    private static DebugConsoleCtx _consoleCtx;

    static DevConsoleService()
    {
        _consoleCtx = new DebugConsoleCtx(AddLog);
    }

    public static void Draw(int leftPanelWidth, int rightPanelWidth) =>
        ConsoleComponent.DrawConsole(leftPanelWidth, rightPanelWidth);

    public static void AddLog(string? msg)
    {
        if (msg is null) return;
        Log.Add(msg);
        ConsoleComponent.ScrollToBottom();

        if (Log.Count >= MaxLogCount)
            Log.RemoveRange(Log.Count / 2, Log.Count - Log.Count / 2);
    }

    private static bool ExecCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return false;

        Log.Add($">> {commandLine}");
        var parts = commandLine.Trim().Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0];
        var action = parts.Length > 1 ? parts[1] : null;
        var arg1 = parts.Length > 2 ? parts[2] : null;
        var arg2 = parts.Length > 3 ? parts[3] : null;

        if (cmd == "clear")
        {
            Log.Clear();
            AddLog("[console cleared]");
            return true;
        }

        if (cmd == "help" || cmd == "info")
        {
            PrintCommands();
            return true;
        }

        try
        {
            CommandDispatcher.InvokeCommand(_consoleCtx, cmd, action ?? "", arg1, arg2);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            AddLog($"Error when invoking {cmd} with error: {ex.Message}");
            return false;
        }

        return true;
    }


    private static void PrintCommands()
    {
        CommandDispatcher.ProcessRegistryRecords(_consoleCtx, static (ctx, command, existsIn) =>
        {
            var console = StringUtils.BoolToYesNo(existsIn.Item1);
            var editor = StringUtils.BoolToYesNo(existsIn.Item2);

            ctx.AddLog($"{command} - Console={console} , Editor={editor}");
        });
    }
}