using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Panels;

internal sealed class ConsoleComponent
{
    private static String64Utf8 _inputUtf8;

    private FrameStepper _scrollTopBottomStepper = new(8);

    internal void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }


    internal void DrawConsole(ConsoleService service, in FrameContext ctx)
    {
        ImGui.Begin("cli"u8);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, GuiTheme.ConsoleInnerBgColor);

        DrawInner(service, ctx);

        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.14f, 0.14f, 0.14f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.22f, 0.22f, 0.22f, 1.00f));
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.18f, 0.18f, 0.18f, 1.00f));
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, 6f));
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.InputTextWithHint("##input"u8, "$"u8, ref _inputUtf8.GetRef(), String64Utf8.Capacity,
                ImGuiInputTextFlags.EnterReturnsTrue))
        {
            HandleInput(service);
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);
        ImGui.End();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawInner(ConsoleService service, FrameContext ctx)
    {
        const ImGuiWindowFlags flags = ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;
        ImGui.BeginChild("inner"u8, new Vector2(0, -inputHeight), 0, flags);

        var rowHeight = ImGui.GetFontSize() + GuiTheme.FramePadding.Y + 2f;

        DrawVisibleLogs(service, rowHeight, ctx);

        if (_scrollTopBottomStepper.Tick())
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollTopBottomStepper.SetIntervalTicks(0);
        }

        ImGui.EndChild();
    }

    private void HandleInput(ConsoleService service)
    {
        var len = UtfText.SliceNullTerminate(_inputUtf8.AsSpan(), out var byteSpan);
        if (len == 0) return;

        Span<char> charBuffer = stackalloc char[len];
        if (!InputTextUtils.DecodeUtf8Input(byteSpan, charBuffer, out var inputStr)) return;

        service.ExecCommand(inputStr);

        byteSpan.Clear();
        ImGui.SetKeyboardFocusHere();
        ScrollToBottom();
    }

    private static void DrawVisibleLogs(ConsoleService service, float rowHeight, FrameContext ctx)
    {
        var logs = service.GetLogs();
        if (logs.Length == 0) return;
        var clipper = new ImGuiListClipper();
        clipper.Begin(logs.Length, rowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - start;
            var slice = logs.Slice(start, length);
            foreach (var it in slice)
                LogDrawer.DrawLog(it, ctx);
        }

        clipper.End();
    }
}