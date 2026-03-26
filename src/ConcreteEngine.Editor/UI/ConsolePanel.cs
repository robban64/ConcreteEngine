using System.Numerics;
using System.Runtime.CompilerServices;
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

internal sealed unsafe class ConsolePanel(ConsoleService consoleService)
{
    private const ImGuiWindowFlags InnerFlags =
        ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

    private static readonly Vector2 InnerItemSpacing = new(12f, 6f);
    private static FrameStepper _scrollTopBottomStepper = new(8);

    private readonly ConsoleService _consoleService = consoleService;

    private NativeViewPtr<byte> _titleStrPtr;
    private NativeViewPtr<byte> _inputStrPtr;

    private ArenaBlock* _panelMemory;

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Allocate()
    {
        _panelMemory = TextBuffers.PersistentArena.Alloc(64 + 32);
        _inputStrPtr = _panelMemory->AllocSlice(64);
        _titleStrPtr = _panelMemory->AllocSlice(32);
        _titleStrPtr.Writer().Append("Console"u8);

    }

    internal static void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }

    internal void OnUpdateDiagnostic()
    {
        var metrics = MetricSystem.Instance;
        _titleStrPtr.Writer()
            .Append("Console"u8).PadRight(4)
            .Append('[').Append(metrics.Metric.AvgMs, "F4").Append("ms"u8).Append(']')
            .PadRight(4)
            .Append('[').Append(metrics.Metric.AllocMbPerSec, "F4").Append("MB/s"u8).Append(']')
            .End();
    }

    internal void Draw()
    {
        // header
        ImGui.PushStyleColor(ImGuiCol.Text, Palette.TextSecondary);
        ImGui.SeparatorText(_titleStrPtr);
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ConsoleInnerBgColor);

        // Inner
        var inputHeight = GuiLayout.GetFrameHeightWithSpacing() + 8f;
        if (ImGui.BeginChild("inner"u8, new Vector2(0, -inputHeight), 0, InnerFlags))
        {
            DrawVisibleLogs();
        }

        ImGui.EndChild();

        // input
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ConsoleFrameBg);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ConsoleFrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ConsoleFrameBgActive);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ConsoleFramePadding);
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.InputTextWithHint("##input"u8, "$"u8, _inputStrPtr, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            HandleInput();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);
    }

    private void DrawVisibleLogs()
    {
        if (_consoleService.LogCount == 0) return;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, InnerItemSpacing);
        var rowHeight = FontSizeDefault + FramePadding.Y + 4f;

        var clipper = new ImGuiListClipper();
        clipper.Begin(_consoleService.LogCount, rowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - clipper.DisplayStart;
            var logs = _consoleService.GetLogs(start, length);
            foreach (var it in logs)
            {
                switch (it.Scope)
                {
                    case LogScope.Unknown: DrawPlain(it.LogPtr); break;
                    case LogScope.Command: DrawCommand(it.LogPtr); break;
                    default: DrawLog(it.LogPtr, it.Level, it.Scope); break;
                }
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

    private void HandleInput()
    {
        UtfText.SliceNullTerminate(_inputStrPtr.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty || !UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        _consoleService.ExecCommand(chars);

        byteSpan.Clear();
        ImGui.SetKeyboardFocusHere();
        ScrollToBottom();
    }

    private static void DrawLog(byte* logPtr, LogLevel level, LogScope scope)
    {
        ImGui.TextColored(Palette.TextSecondary, logPtr);
        ImGui.SameLine();
        ImGui.TextColored(StyleMap.GetLogLevelColor(level), level.ToLogText());
        ImGui.SameLine();
        ImGui.TextUnformatted(scope.ToLogText());
        ImGui.SameLine();
        if (level == LogLevel.Error)
            ImGui.TextColored(Palette.RedLight, logPtr + LogEntry.TimestampOffset);
        else
            ImGui.TextUnformatted(logPtr + LogEntry.TimestampOffset);
    }

    private static void DrawCommand(byte* logPtr)
    {
        ImGui.TextColored(Palette.TextSecondary, logPtr);
        ImGui.SameLine();
        ImGui.TextColored(Palette.OrangeBase, ">>"u8);
        ImGui.SameLine();
        ImGui.TextUnformatted(logPtr + LogEntry.TimestampOffset);
    }
    private static void DrawPlain(byte* logPtr)
    {
        ImGui.TextColored(Palette.TextSecondary, logPtr);
        ImGui.SameLine();
        ImGui.TextUnformatted(logPtr + LogEntry.TimestampOffset);
    }

}