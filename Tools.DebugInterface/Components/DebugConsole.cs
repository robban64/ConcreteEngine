using System.Numerics;
using ImGuiNET;

namespace Tools.DebugInterface.Components;

public sealed class DebugConsoleCtx(DebugConsole debugConsole)
{
    public void AddLog(string? msg) => debugConsole.AddLog(msg);
}

public class DebugConsole
{
    private bool _justOpened = true;
    private bool _scrollToBottom = false;

    private string _input = string.Empty;

    private readonly DebugConsoleCtx _ctx;
    private readonly List<string> _log = new(64);
    private readonly Dictionary<string, Delegate> _commands = new(4);

    public DebugConsole()
    {
        _ctx = new DebugConsoleCtx(this);
    }


    public void RegisterCommand(string command, Func<string?, string?, string> commandHandler) =>
        _commands[command] = commandHandler;

    public void RegisterCommand(string command, Func<string> commandHandler) =>
        _commands[command] = commandHandler;

    public void RegisterCommand(string command, Action<DebugConsoleCtx, string?, string?> commandHandler) =>
        _commands[command] = commandHandler;

    public void RegisterCommand(string command, Action<DebugConsoleCtx> commandHandler) =>
        _commands[command] = commandHandler;

    
    public void AddLog(string? msg)
    {
        if (msg is null) return;
        _log.Add(msg);
        _scrollToBottom = true;
    }

    public bool ExecCommand(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return false;

        _scrollToBottom = true;
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
        
        if (!_commands.TryGetValue(cmd, out var commandHandler))
        {
            _log.Add($"Unknown command: {cmd}");
            return false;
        }
        
        switch (commandHandler)
        {
            case Func<string?, string?, string> funcArg:
                _log.Add(funcArg(arg1, arg2));
                break;
            case Func<string> funcNoArg:
                _log.Add(funcNoArg());
                break;
            case Action<DebugConsoleCtx, string?, string?> actionArg:
                actionArg(_ctx, arg1, arg2);
                break;
            case Action<DebugConsoleCtx> actionNoArg:
                actionNoArg(_ctx);
                break;
            default:
                _log.Add($"Invalid invoke handler: {commandHandler.GetType().Name}");
                return false;
        }

        return true;
    }

    public void DrawConsole(int leftPanelWidth, int rightPanelWidth)
    {
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        var vp = ImGui.GetMainViewport();

        var centerX = vp.WorkPos.X + leftPanelWidth;
        var centerY = vp.WorkPos.Y;
        var centerW = MathF.Max(0, vp.WorkSize.X - leftPanelWidth - rightPanelWidth);
        var centerH = vp.WorkSize.Y;

        const float minW = 600f, maxWCap = 980f;
        const float minH = 200f, maxH = 520f;

        var targetW = Math.Clamp(centerW * 0.80f, minW, Math.Min(maxWCap, centerW));
        var targetH = Math.Clamp(centerH * 0.30f, minH, maxH);

        const float margin = 12f;
        var posX = centerX + MathF.Max(0, (centerW - targetW) * 0.5f);
        var posY = centerY + centerH - targetH - margin;

        ImGui.SetNextWindowPos(new Vector2(posX, posY), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(targetW, targetH), ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(MathF.Min(minW, centerW), minH),
            new Vector2(MathF.Min(Math.Min(maxWCap, centerW), centerW), MathF.Min(maxH, centerH))
        );

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.08f, 0.94f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);

        if (!ImGui.Begin("Console", flags))
        {
            ImGui.End();
            ImGui.PopStyleVar(3);
            ImGui.PopStyleVar(); // WindowPadding
            ImGui.PopStyleColor(2);
            return;
        }

        {
            var dl = ImGui.GetWindowDrawList();
            var winPos = ImGui.GetWindowPos();
            var crMin = ImGui.GetWindowContentRegionMin();
            var crMax = ImGui.GetWindowContentRegionMax();

            var x1 = winPos.X + crMin.X;
            var x2 = winPos.X + crMax.X;
            var y = winPos.Y + crMin.Y + 1f;

            var lineCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.40f, 0.41f, 0.43f, 0.55f));
            dl.AddLine(new Vector2(x1, y), new Vector2(x2, y), lineCol, 1.0f);

            ImGui.Dummy(new Vector2(0, 6f));
        }

        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;

        ImGui.BeginChild(
            "ConsoleLogRegion",
            new Vector2(0, -inputHeight),
            ImGuiChildFlags.None,
            ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar
        );

        foreach (var line in _log)
            ImGui.TextUnformatted(line);

        if (_justOpened || _scrollToBottom)
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollToBottom = false;
            _justOpened = false;
        }

        ImGui.EndChild();

        ImGui.Dummy(new Vector2(0, 5f));

        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.14f, 0.14f, 0.14f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.22f, 0.22f, 0.22f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.18f, 0.18f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.92f, 0.92f, 0.92f, 1.00f));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 6f)); // add height
        ImGui.SetNextItemWidth(-1f);

        var submitted = ImGui.InputTextWithHint("##ConsoleInput", "$", ref _input, 1024,
            ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.PopStyleColor(4);

        if (submitted)
        {
            var text = _input.Trim();
            _input = string.Empty;

            if (!string.IsNullOrEmpty(text))
                ExecCommand(text);

            ImGui.SetKeyboardFocusHere();
            _scrollToBottom = true;
        }

        ImGui.End();

        ImGui.PopStyleVar(3); // FrameRounding, ChildRounding, WindowRounding
        ImGui.PopStyleVar(); // WindowPadding
        ImGui.PopStyleColor(2);
    }
}