using Core.DebugTools.Gui;

namespace Core.DebugTools;

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
        
        if(_log.Count >= MaxLogCount)
            _log.RemoveRange(_log.Count / 2, _log.Count - (_log.Count / 2));

    }

    internal bool ExecuteInternalCommand(string cmd, string? arg1, string? arg2 = null)
    {
        if (RouteTable.InvokeCommand(_ctx, cmd, arg1, arg2)) return true;
        AddLog($"Unknown command: {cmd}");
        return false;
    }

    private bool ExecCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return false;

        _log.Add($">> {commandLine}");
        var parts = commandLine.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        var cmd = parts[0];
        var arg1 = parts.Length > 1 ? parts[1] : null;
        var arg2 = parts.Length > 2 ? parts[2] : null;

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

        if (!RouteTable.InvokeCommand(_ctx, cmd, arg1, arg2))
        {
            AddLog($"Unknown command: {cmd}");
            return false;
        }

        return true;
    }

    private void PrintCommands()
    {
        foreach (var cmd in RouteTable.RegisterCommands)
            AddLog(cmd);
    }
}