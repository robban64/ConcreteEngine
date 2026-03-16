using System.Numerics;
using System.Text;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using static ConcreteEngine.Editor.Theme.GuiTheme;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class ConsolePanel
{
    private const ImGuiWindowFlags InnerFlags =
        ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

    private static readonly Vector2 InnerItemSpacing = new(12f, 6f);

    private static FrameStepper _scrollTopBottomStepper = new(8);

    private readonly ArenaBlock _panelMemory;
    private readonly NativeViewPtr<byte> _avgStrPtr;
    private readonly NativeViewPtr<byte> _inputStrPtr;

    public ConsolePanel()
    {
        _panelMemory = TextBuffers.PersistentArena.Alloc(64 + 16);
        _inputStrPtr = _panelMemory.AllocSlice(64);
        _avgStrPtr = _panelMemory.AllocSlice(16);
        _avgStrPtr.Writer().Append("[0ms]"u8);
        
    }
    

    internal static void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }

    internal void UpdateDiagnostic()
    {
        _avgStrPtr.Writer().Append('[').Append(MetricSystem.Instance.Metric.AvgMs, "F4").Append("ms"u8)
            .Append(']').End();
    }

    internal void DrawConsole(ConsoleService service)
    {
        if (!ImGui.Begin("cli"u8))
        {
            ImGui.End();
            return;
        }

        // header
        ImGui.PushStyleColor(ImGuiCol.Text, 0x99FFFFFF);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(_avgStrPtr);
        ImGui.SameLine();
        ImGui.SeparatorText("Console"u8);
        ImGui.PopStyleColor();
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ConsoleInnerBgColor);

        // Inner
        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;
        if (ImGui.BeginChild("inner"u8, new Vector2(0, -inputHeight), 0, InnerFlags))
        {
            DrawVisibleLogs(service);
        }

        ImGui.EndChild();

        // input
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ConsoleFrameBg);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ConsoleFrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ConsoleFrameBgActive);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ConsoleFramePadding);
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.InputTextWithHint("##input"u8, "$"u8, _inputStrPtr, 64, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            HandleInput(service);
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);

        ImGui.End();
    }


    private static void DrawVisibleLogs(ConsoleService service)
    {
        if (service.LogCount == 0) return;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, InnerItemSpacing);
        var rowHeight = FontSizeDefault + FramePadding.Y + 4f;

        var clipper = new ImGuiListClipper();
        clipper.Begin(service.LogCount, rowHeight);
        while (clipper.Step())
        {
            var sw = TextBuffers.GetWriter();
            int start = clipper.DisplayStart, end = clipper.DisplayEnd - clipper.DisplayStart;
            var logs = service.GetLogs().Slice(start, end);
            foreach (ref readonly var it in logs)
            {
                DrawLog(sw.Write(it.Message), it.Level, it.Scope);
            }
        }

        clipper.End();

        ImGui.PopStyleVar();

        if (_scrollTopBottomStepper.Tick())
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollTopBottomStepper.SetIntervalTicks(0);
        }
    }

    private void HandleInput(ConsoleService service)
    {
        UtfText.SliceNullTerminate(_inputStrPtr.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty || !UtfText.IsAscii(byteSpan)) return ;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        service.ExecCommand(chars);

        byteSpan.Clear();
        ImGui.SetKeyboardFocusHere();
        ScrollToBottom();
    }

    private static void DrawLog(byte* message, LogLevel level, LogScope scope)
    {
        ImGui.TextColored(Palette.TextSecondary, message);

        ImGui.SameLine();

        if (scope != LogScope.Command)
        {
            ImGui.TextColored(StyleMap.GetLogLevelColor(level), level.ToLogText());
            ImGui.SameLine();
            ImGui.TextUnformatted(scope.ToLogText());
        }
        else
        {
            ImGui.TextColored(Palette.OrangeBase, "$"u8);
        }

        ImGui.SameLine();

        if (level == LogLevel.Error)
            ImGui.TextColored(Palette.RedLight, message + LogEntry.DateLength);
        else
            ImGui.TextUnformatted(message + LogEntry.DateLength);
    }
}