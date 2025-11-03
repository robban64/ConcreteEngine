using Core.DebugTools.Components;

namespace Core.DebugTools;

public sealed class DebugConsoleCtx(DevConsoleService devConsole)
{
    public void AddLog(string? msg) => devConsole.AddLog(msg);
    public void AddMissingArg(string arg) => devConsole.AddLog($"Argument: {arg} is null or empty");
}

public sealed class DevConsoleService
{
    private readonly DevConsoleGui _consoleGui;

    private readonly DebugConsoleCtx _ctx;
    
    private readonly List<string> _log = new(64);

    internal DevConsoleService()
    {
        _consoleGui = new DevConsoleGui(ExecCommand);
        _ctx = new DebugConsoleCtx(this);
    }
    
    public void Draw()
    {
        _consoleGui.DrawConsole(_log,200, 200);
    }
    
    public bool ExecCommand(string commandLine)
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
            _log.Add("[console cleared]");
            return true;
        }

        if (!RouteTable.InvokeCommand(_ctx, cmd, arg1, arg2))
        {
            _log.Add($"Unknown command: {cmd}");
            return false;
        }

        return true;
    }

    
    public void AddLog(string? msg)
    {
        if (msg is null) return;
        _log.Add(msg);
        _consoleGui.ScrollToBottom();
    }

}