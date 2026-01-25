using System.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.CLI;

internal sealed class ConsoleComponent
{
    private const ImGuiWindowFlags WindowFlags =
        ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

    private struct ConsoleWindowSize
    {
        public Vector2 Position;
        public Vector2 Size;
        public Vector2 SizeConstraintMin;
        public Vector2 SizeConstraintMax;
    }

    private ConsoleWindowSize _consoleSize;

    private FrameStepper _scrollTopBottomStepper = new(8);

    private readonly ClipDrawer<StringLogEvent> _clipDrawer =
        new(static (i, log, in ctx) => LogDrawer.DrawLog(i, log, in ctx));


    internal void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }


    public void CalculateSize(int leftPanelWidth, int rightPanelWidth)
    {
        const float minW = 400f, maxWCap = 980f;
        const float minH = 240f, maxH = 300f;
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

    internal void DrawConsole(ConsoleService service, in FrameContext ctx)
    {
        {
            ref readonly var layout = ref _consoleSize;
            ImGui.SetNextWindowPos(layout.Position);
            ImGui.SetNextWindowSize(layout.Size);
            ImGui.SetNextWindowSizeConstraints(layout.SizeConstraintMin, layout.SizeConstraintMax);

            ImGui.PushStyleColor(ImGuiCol.WindowBg, GuiTheme.ConsoleBgColor);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12f, 6f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 2f);
        }

        if (ImGui.Begin("##cli"u8, WindowFlags))
            DrawInner(service, in ctx);

        ImGui.End();

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();
    }

    private void DrawInner(ConsoleService service, in FrameContext ctx)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;
        ImGui.PushStyleColor(ImGuiCol.Text, 0x99FFFFFF);
        ImGui.SeparatorText("Console"u8);
        ImGui.PopStyleColor();

        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;
        ImGui.PushStyleColor(ImGuiCol.ChildBg, GuiTheme.ConsoleInnerBgColor);

        if (!ImGui.BeginChild("##inner"u8, new Vector2(0, -inputHeight), 0, flags))
        {
            ImGui.EndChild();
            return;
        }

        var rowHeight = ImGui.GetFontSize() + GuiTheme.FramePadding.Y + 2f;
        var logs = service.GetLogs();
        _clipDrawer.Draw(logs.Length, rowHeight, logs, in ctx);

        if (_scrollTopBottomStepper.Tick())
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollTopBottomStepper.SetIntervalTicks(0);
        }

        ImGui.EndChild();


        DrawInput(service);
        ImGui.PopStyleColor();
    }

    private void DrawInput(ConsoleService service)
    {
        if (!DrawStyledInput()) return;

        var input = DataStore.InputCliBuffer128.AsSpan();
        var len = StrUtils.SliceNullTerminate(input, out var byteSpan);
        if (len == 0) return;

        Span<char> charBuffer = stackalloc char[len];
        if (!StrUtils.DecodeUtf8Input(byteSpan, charBuffer, out var inputStr)) return;

        service.ExecCommand(inputStr);

        byteSpan.Clear();
        ImGui.SetKeyboardFocusHere();
        ScrollToBottom();
        return;

        static unsafe bool DrawStyledInput()
        {
            const ImGuiInputTextFlags flags = ImGuiInputTextFlags.EnterReturnsTrue;
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.14f, 0.14f, 0.14f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.22f, 0.22f, 0.22f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.18f, 0.18f, 1.00f));
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 6f));
            ImGui.SetNextItemWidth(-1f);

            var submitted = ImGui.InputTextWithHint("##input"u8, "$"u8, DataStore.InputCliBuffer128.Ptr, 64, flags);

            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
            return submitted;
        }
    }
}