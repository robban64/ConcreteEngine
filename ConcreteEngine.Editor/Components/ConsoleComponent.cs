#region

using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Common.Numerics.Maths;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

#endregion

namespace ConcreteEngine.Editor.Components;

internal static class ConsoleComponent
{
    private static bool _justOpened = true;
    private static bool _scrollToBottom = false;

    private static string _input = string.Empty;

    private static Vector2 _workSize = Vector2.Zero;

    private static Vector2 _position = Vector2.Zero;
    private static Vector2 _size = Vector2.Zero;
    private static Vector2 _sizeConstraintMin = Vector2.Zero;
    private static Vector2 _sizeConstraintMax = Vector2.Zero;

    private static Vector2 _consoleTextSize = Vector2.Zero;


    internal static void ScrollToBottom() => _scrollToBottom = true;

    private static void CalculateSize(Vector2 workPos, Vector2 workSize, int leftPanelWidth, int rightPanelWidth)
    {
        const float minW = 400f, maxWCap = 980f;
        const float minH = 160f, maxH = 240f;
        const float margin = 12f;

        var centerX = workPos.X + leftPanelWidth;
        var centerY = workPos.Y;
        var centerW = MathF.Max(0, workSize.X - leftPanelWidth - rightPanelWidth);
        var centerH = workSize.Y;

        var targetW = float.Clamp(centerW * 0.80f, minW, Math.Min(maxWCap, centerW));
        var targetH = float.Clamp(centerH * 0.25f, minH, maxH);

        var posX = centerX + MathF.Max(0, (centerW - targetW) * 0.5f);
        var posY = centerY + centerH - targetH - margin;


        _position = new Vector2(posX, posY);
        _size = new Vector2(targetW, targetH);
        _sizeConstraintMin = new Vector2(MathF.Min(minW, centerW), minH);
        _sizeConstraintMax = new Vector2(MathF.Min(float.Min(maxWCap, centerW), centerW), MathF.Min(maxH, centerH));
    }

    internal static void DrawConsole(int leftPanelWidth, int rightPanelWidth)
    {
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;


        var vp = ImGui.GetMainViewport();
        var workSize = vp.WorkSize;

        if (!VectorMath.NearlyEqual(workSize, _workSize, MetricUnits.Millimeter))
        {
            CalculateSize(vp.WorkPos, workSize, leftPanelWidth, rightPanelWidth);
            _workSize = workSize;
        }

        ImGui.SetNextWindowPos(_position, ImGuiCond.Always);
        ImGui.SetNextWindowSize(_size, ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(_sizeConstraintMin, _sizeConstraintMax);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, GuiTheme.ConsoleBgColor);
        ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
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

    private static unsafe void DrawInner()
    {
        ImGui.PushStyleColor(ImGuiCol.Text, 0x99FFFFFF);
        ImGui.SeparatorText("Console");
        ImGui.PopStyleColor();

        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, GuiTheme.ConsoleInnerBgColor);

        if (ImGui.BeginChild("##ConsoleLogRegion", new Vector2(0, -inputHeight), ImGuiChildFlags.None,
                ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar))
        {
            float rowHeight = ImGui.GetFrameHeight();
            var clipper = new ImGuiListClipper();
            var logs = ConsoleService.GetLogs();
            ImGuiNative.ImGuiListClipper_Begin(&clipper, ConsoleService.LogCount, rowHeight);
            while (ImGuiNative.ImGuiListClipper_Step(&clipper) != 0)
            {
                for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    ImGui.TextUnformatted(logs[ConsoleService.GetSlotIndex(i)]);
            }

            ImGuiNative.ImGuiListClipper_End(&clipper);

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
                ConsoleService.ExecCommand(text);

            ImGui.SetKeyboardFocusHere();
            _scrollToBottom = true;
        }
    }
}