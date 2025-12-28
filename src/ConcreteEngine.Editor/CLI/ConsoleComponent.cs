using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Editor.Utils;
using ImGuiNET;

namespace ConcreteEngine.Editor.CLI;

internal static class ConsoleComponent
{
    private struct ConsoleWindowSize
    {
        public Vector2 WorkSize;
        public Vector2 Position;
        public Vector2 Size;
        public Vector2 SizeConstraintMin;
        public Vector2 SizeConstraintMax;
    }

    private static bool _justOpened = true;
    private static bool _scrollToBottom = false;

    private static string _input = string.Empty;

    private static ConsoleWindowSize _consoleSize;

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


        _consoleSize.Position = new Vector2(posX, posY);
        _consoleSize.Size = new Vector2(targetW, targetH);
        _consoleSize.SizeConstraintMin = new Vector2(MathF.Min(minW, centerW), minH);
        _consoleSize.SizeConstraintMax =
            new Vector2(MathF.Min(float.Min(maxWCap, centerW), centerW), MathF.Min(maxH, centerH));
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

        if (!VectorMath.NearlyEqual(workSize, _consoleSize.WorkSize, MetricUnits.Millimeter))
        {
            CalculateSize(vp.WorkPos, workSize, leftPanelWidth, rightPanelWidth);
            _consoleSize.WorkSize = workSize;
        }

        ImGui.SetNextWindowPos(_consoleSize.Position, ImGuiCond.Always);
        ImGui.SetNextWindowSize(_consoleSize.Size, ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(_consoleSize.SizeConstraintMin, _consoleSize.SizeConstraintMax);

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

    private static void DrawInner()
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;
        ImGui.PushStyleColor(ImGuiCol.Text, 0x99FFFFFF);
        ImGui.SeparatorText("Console");
        ImGui.PopStyleColor();

        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, GuiTheme.ConsoleInnerBgColor);

        if (ImGui.BeginChild("##ConsoleLogRegion", new Vector2(0, -inputHeight), 0, flags))
        {
            DrawLogList(ConsoleGateway.Service);
            if (_justOpened || _scrollToBottom)
            {
                ImGui.SetScrollHereY(1.0f);
                _scrollToBottom = false;
                _justOpened = false;
            }

            ImGui.EndChild();
        }

        DrawInput();

        ImGui.PopStyleColor();
    }

    private static void DrawInput()
    {
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

        if (!submitted) return;

        var text = _input.Trim();
        _input = string.Empty;

        if (!string.IsNullOrEmpty(text))
            ConsoleGateway.ExecCommand(text);

        ImGui.SetKeyboardFocusHere();
        _scrollToBottom = true;
    }

    private static readonly char[] CharBuffer = new char[512];

    private static unsafe void DrawLogList(ConsoleService service)
    {
        if (service.LogCount == 0) return;

        float rowHeight = ImGui.GetFrameHeight();
        var clipper = new ImGuiListClipper();
        ImGuiNative.ImGuiListClipper_Begin(&clipper, service.LogCount, rowHeight);

        var logs = service.GetLogs();

        while (ImGuiNative.ImGuiListClipper_Step(&clipper) != 0)
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                var log = logs[service.GetSlotIndex(i)];
                ImGui.TextUnformatted(LogParser.Format(CharBuffer, log));
            }
        }

        ImGuiNative.ImGuiListClipper_End(&clipper);
    }
}