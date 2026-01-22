using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.CLI;

internal sealed class ConsoleComponent
{
    private struct ConsoleWindowSize
    {
        public Vector2 Position;
        public Vector2 Size;
        public Vector2 SizeConstraintMin;
        public Vector2 SizeConstraintMax;
    }

    private ConsoleWindowSize _consoleSize;

    private bool _justOpened = true;
    private bool _scrollToBottom;

    private string _input = string.Empty;

    private readonly ClipDrawer<StringLogEvent> _clipDrawer = new((i, args, ref ctx) => DrawLog(i, args, ref ctx));

    internal void ScrollToBottom() => _scrollToBottom = true;

    public void CalculateSize(int leftPanelWidth, int rightPanelWidth)
    {
        const float minW = 400f, maxWCap = 980f;
        const float minH = 160f, maxH = 240f;
        const float margin = 12f;

        var vp = ImGui.GetMainViewport();

        var centerX = vp.WorkPos.X + leftPanelWidth;
        var centerY = vp.WorkPos.Y;
        var centerW = MathF.Max(0, vp.WorkSize.X - leftPanelWidth - rightPanelWidth);
        var centerH = vp.WorkSize.Y;

        var targetW = float.Clamp(centerW * 0.80f, minW, Math.Min(maxWCap, centerW));
        var targetH = float.Clamp(centerH * 0.25f, minH, maxH);

        var posX = centerX + MathF.Max(0, (centerW - targetW) * 0.5f);
        var posY = centerY + centerH - targetH - margin;

        ref var consoleSize = ref _consoleSize;
        consoleSize.Position = new Vector2(posX, posY);
        consoleSize.Size = new Vector2(targetW, targetH);
        consoleSize.SizeConstraintMin = new Vector2(MathF.Min(minW, centerW), minH);
        consoleSize.SizeConstraintMax =
            new Vector2(MathF.Min(float.Min(maxWCap, centerW), centerW), MathF.Min(maxH, centerH));
    }

    internal void DrawConsole(ConsoleService service, FrameContext ctx)
    {
        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;


        ImGui.SetNextWindowPos(_consoleSize.Position, ImGuiCond.Always);
        ImGui.SetNextWindowSize(_consoleSize.Size, ImGuiCond.Always);
        ImGui.SetNextWindowSizeConstraints(_consoleSize.SizeConstraintMin, _consoleSize.SizeConstraintMax);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, GuiTheme.ConsoleBgColor);
        ImGui.PushStyleColor(ImGuiCol.Border, Vector4.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 6f));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 2f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);

        if (ImGui.Begin("##DevConsole"u8, flags))
            DrawInner(service, ref ctx);

        ImGui.End();

        ImGui.PopStyleVar(4);
        ImGui.PopStyleColor(2);
    }

    private void DrawInner(ConsoleService service, ref FrameContext ctx)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;
        ImGui.PushStyleColor(ImGuiCol.Text, 0x99FFFFFF);
        ImGui.SeparatorText("Console"u8);
        ImGui.PopStyleColor();

        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, GuiTheme.ConsoleInnerBgColor);

        if (!ImGui.BeginChild("##ConsoleLogRegion"u8, new Vector2(0, -inputHeight), 0, flags))
            return;

        var rowHeight = ImGui.GetFrameHeight();

        var logs = service.GetLogs();
        _clipDrawer.Draw(logs.Length, rowHeight, logs, ref ctx);

        if (_justOpened || _scrollToBottom)
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollToBottom = false;
            _justOpened = false;
        }

        ImGui.EndChild();

        DrawInput(service);

        ImGui.PopStyleColor();
    }

    private void DrawInput(ConsoleService service)
    {
        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.14f, 0.14f, 0.14f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.22f, 0.22f, 0.22f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.18f, 0.18f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.92f, 0.92f, 0.92f, 1.00f));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 6f));
        ImGui.SetNextItemWidth(-1f);

        var submitted = ImGui.InputTextWithHint("##ConsoleInput"u8, "$"u8, ref _input, 1024,
            ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);

        if (!submitted) return;

        var text = _input.Trim();
        _input = string.Empty;

        if (!string.IsNullOrEmpty(text))
            service.ExecCommand(text);

        ImGui.SetKeyboardFocusHere();
        _scrollToBottom = true;
    }


    private static void DrawLog(int i, StringLogEvent log, ref FrameContext ctx) =>
        ImGui.TextUnformatted(LogParser.Format(ref ctx.Sw, log));
}