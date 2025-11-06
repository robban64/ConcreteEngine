using ConcreteEngine.Editor.Gui;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor;

public sealed class DebugConsoleCtx(DevConsoleService devConsole)
{
    public void AddLog(string? msg) => devConsole.AddLog(msg);
    public void AddMissingArg(string arg) => devConsole.AddLog($"Argument: {arg} is null or empty");
}

public sealed class DevConsoleService
{
    private const int MaxLogCount = 128;

    private readonly DevConsoleGui _consoleGui;

    private readonly DebugConsoleCtx _ctx;

    private readonly List<string> _log = new(MaxLogCount);

    internal DevConsoleService()
    {
        _consoleGui = new DevConsoleGui(_log, ExecCommand);
        _ctx = new DebugConsoleCtx(this);
    }

    public void Draw(int leftPanelWidth, int rightPanelWidth) =>
        _consoleGui.DrawConsole(leftPanelWidth, rightPanelWidth);

    public void AddLog(string? msg)
    {
        if (msg is null) return;
        _log.Add(msg);
        _consoleGui.ScrollToBottom();

        if (_log.Count >= MaxLogCount)
            _log.RemoveRange(_log.Count / 2, _log.Count - (_log.Count / 2));
    }

    private bool ExecCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return false;

        _log.Add($">> {commandLine}");
        var parts = commandLine.Trim().Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0];
        var action = parts.Length > 1 ? parts[1] : null;
        var arg1 = parts.Length > 2 ? parts[2] : null;
        var arg2 = parts.Length > 3 ? parts[3] : null;

        if (cmd == "clear")
        {
            _log.Clear();
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
            CommandDispatcher.InvokeCommand(_ctx, cmd, action ?? "", arg1, arg2);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException)
        {
            AddLog($"Error when invoking {cmd} with error: {ex.Message}");
            return false;
        }

        return true;
    }


    private void PrintCommands()
    {
        CommandDispatcher.ProcessRegistryRecords(_ctx, static (ctx, command, existsIn) =>
        {
            var console = StringUtils.BoolToYesNo(existsIn.Item1);
            var editor = StringUtils.BoolToYesNo(existsIn.Item2);

            ctx.AddLog($"{command} - Console={console} , Editor={editor}");
        });
    }
}
