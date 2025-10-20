using System.Numerics;
using ImGuiNET;

namespace Tools.DebugInterface;

public class DebugConsoleUi
{
    private readonly List<string> _log = new();
    private string _input = string.Empty;
    private bool _scrollToBottom = false;
    private bool _justOpened = true;

    public void AddLog(string? msg)
    {
        if (msg is null) return;
        _log.Add(msg);
        _scrollToBottom = true;
    }

    private void ExecCommand(string commandLine)
    {
        _log.Add($"> {commandLine}");
        var cmd = commandLine.Trim().ToLowerInvariant();

        if (cmd == "clear" || cmd == "clear()")
        {
            _log.Clear();
            _log.Add("[console cleared]");
        }
        else if (!string.IsNullOrWhiteSpace(cmd))
        {
            _log.Add($"Unknown command: {commandLine}");
        }

        _scrollToBottom = true;
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

        const float minW = 400f, maxW = 800f;
        const float minH = 180f, maxH = 420f;

        var targetH = Math.Clamp(centerH * 0.28f, minH, maxH);
        var targetW = MathF.Min(MathF.Max(centerW, minW), maxW);
        targetW = MathF.Min(targetW, centerW);

        var margin = 12f;
        var posX = centerX + MathF.Max(0, (centerW - targetW) * 0.5f);
        var posY = centerY + centerH - targetH - margin;

        ImGui.SetNextWindowPos(new Vector2(posX, posY), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(targetW, targetH), ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(MathF.Min(minW, centerW), minH),
            new Vector2(MathF.Min(maxW, centerW), MathF.Min(maxH, centerH))
        );


        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.08f, 0.94f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8f, 6f));

        if (!ImGui.Begin("Console", flags))
        {
            ImGui.End();
            return;
        }

        ImGui.TextUnformatted("Console");
        ImGui.Separator();

        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 6f; // offset

        ImGui.BeginChild(
            "ConsoleLogRegion",
            new Vector2(0, -inputHeight),
            ImGuiChildFlags.None,
            ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar);


        foreach (var line in _log)
            ImGui.TextUnformatted(line);

        if (_justOpened || _scrollToBottom)
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollToBottom = false;
            _justOpened = false;
        }

        ImGui.EndChild();
        ImGui.Separator();

        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.15f, 0.15f, 0.15f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.25f, 0.25f, 0.25f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.20f, 0.20f, 0.20f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.90f, 0.90f, 0.90f, 1.00f));
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.InputText("##ConsoleInput", ref _input, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            var submitted = _input;
            _input = string.Empty;

            if (!string.IsNullOrWhiteSpace(submitted))
                ExecCommand(submitted);

            ImGui.SetKeyboardFocusHere();
            _scrollToBottom = true;
        }

        ImGui.PopStyleColor(4);
        ImGui.SameLine();


        ImGui.End();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);
    }
}