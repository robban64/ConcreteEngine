#region

using System.Numerics;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Gui.Components;

internal static class ConsoleComponent
{
    private static bool _justOpened = true;
    private static bool _scrollToBottom = false;

    private static string _input = string.Empty;

    internal static void ScrollToBottom() => _scrollToBottom = true;

    internal static void DrawConsole(int leftPanelWidth, int rightPanelWidth)
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

        const float minW = 400f, maxWCap = 980f;
        const float minH = 160f, maxH = 240f;

        var targetW = float.Clamp(centerW * 0.80f, minW, Math.Min(maxWCap, centerW));
        var targetH = float.Clamp(centerH * 0.25f, minH, maxH);

        const float margin = 12f;
        var posX = centerX + MathF.Max(0, (centerW - targetW) * 0.5f);
        var posY = centerY + centerH - targetH - margin;

        ImGui.SetNextWindowPos(new Vector2(posX, posY), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(targetW, targetH), ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(
            new Vector2(MathF.Min(minW, centerW), minH),
            new Vector2(MathF.Min(float.Min(maxWCap, centerW), centerW), MathF.Min(maxH, centerH))
        );

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.08f, 0.08f, 0.08f, 0.94f));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0, 0, 0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);

        if (ImGui.Begin("##DevConsole", flags))
        {
            DrawInner();
            ImGui.End();
        }

        ImGui.PopStyleVar(4);
        ImGui.PopStyleColor(2);
    }

    private static void DrawInner()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, 0x99FFFFFF);
        ImGui.SeparatorText("Console");
        ImGui.PopStyleColor();

        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;

        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.10f, 0.10f, 0.75f));
        if (ImGui.BeginChild(
                "ConsoleLogRegion",
                new Vector2(0, -inputHeight),
                ImGuiChildFlags.None,
                ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar)
           )
        {
            foreach (var line in DevConsoleService.Log)
                ImGui.TextUnformatted(line);

            if (_justOpened || _scrollToBottom)
            {
                ImGui.SetScrollHereY(1.0f);
                _scrollToBottom = false;
                _justOpened = false;
            }

            ImGui.EndChild();
        }


        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.14f, 0.14f, 0.14f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.22f, 0.22f, 0.22f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.18f, 0.18f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.92f, 0.92f, 0.92f, 1.00f));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 6f));

        ImGui.SetNextItemWidth(-1f);


        var submitted = ImGui.InputTextWithHint("##ConsoleInput", "$", ref _input, 1024,
            ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);

        if (submitted)
        {
            var text = _input.Trim();
            _input = string.Empty;

            if (!string.IsNullOrEmpty(text))
                DevConsoleService.AddLog(text);

            ImGui.SetKeyboardFocusHere();
            _scrollToBottom = true;
        }
    }
}