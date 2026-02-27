using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Theme.GuiTheme;

namespace ConcreteEngine.Editor.Panels;

internal sealed class ConsolePanel
{
    private const ImGuiWindowFlags InnerFlags =
        ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

    private static String64Utf8 _inputUtf8;

    private FrameStepper _scrollTopBottomStepper = new(8);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }


    internal void DrawConsole(ConsoleService service, in FrameContext ctx)
    {
        ImGui.Begin("cli"u8);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ConsoleInnerBgColor);
        // Inner
        {
            var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;
            ImGui.BeginChild("inner"u8, new Vector2(0, -inputHeight), 0, InnerFlags);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ItemSpacing with { X = 12f });
            var rowHeight = ImGui.GetFontSize() + FramePadding.Y + 4f;

            DrawVisibleLogs(service, rowHeight, in ctx);
            ImGui.PopStyleVar();
            if (_scrollTopBottomStepper.Tick())
            {
                ImGui.SetScrollHereY(1.0f);
                _scrollTopBottomStepper.SetIntervalTicks(0);
            }

            ImGui.EndChild();
        }


        ImGui.PushStyleColor(ImGuiCol.FrameBg, ConsoleFrameBg);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ConsoleFrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ConsoleFrameBgActive);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ConsoleFramePadding);
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.InputTextWithHint("##input"u8, "$"u8, ref _inputUtf8.GetRef(), String64Utf8.Capacity,
                ImGuiInputTextFlags.EnterReturnsTrue))
        {
            HandleInput(service, ctx);
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);
        ImGui.End();
    }


    private void HandleInput(ConsoleService service, in FrameContext ctx)
    {
        UtfText.SliceNullTerminate(_inputUtf8.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;

        var charLength = Encoding.UTF8.GetCharCount(byteSpan);
        Span<char> charBuffer = stackalloc char[charLength];
        if (!InputTextUtils.DecodeUtf8Input(byteSpan, charBuffer, out var inputStr)) return;

        service.ExecCommand(inputStr, ctx);

        byteSpan.Clear();
        ImGui.SetKeyboardFocusHere();
        ScrollToBottom();
    }

    private static void DrawVisibleLogs(ConsoleService service, float rowHeight, in FrameContext ctx)
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