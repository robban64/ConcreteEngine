using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class ConsolePanel(ConsoleService consoleService)
{
    private const ImGuiWindowFlags InnerFlags =
        ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

    public static readonly uint ConsoleBgColor = new Color4(0.08f, 0.08f, 0.08f, 0.94f).ToPackedRgba();

    private static readonly uint ConsoleFrameBg = new Color4(0.14f, 0.14f, 0.14f, 1.00f).ToPackedRgba();
    private static readonly uint ConsoleFrameBgHovered = new Color4(0.22f, 0.22f, 0.22f, 1.00f).ToPackedRgba();
    private static readonly uint ConsoleFrameBgActive = new Color4(0.18f, 0.18f, 0.18f, 1.00f).ToPackedRgba();

    private static readonly uint ConsoleInnerBgColor = new Color4(0.10f, 0.10f, 0.10f, 0.75f).ToPackedRgba();

    //
    private static readonly Vector2 ConsoleFramePadding = new(8f, 6f);
    private static readonly Vector2 InnerItemSpacing = new(12f, 6f);
    private static readonly float RowHeight = GuiTheme.FontSizeDefault + GuiTheme.FramePadding.Y + 4f;

    private static FrameStepper _scrollTopBottomStepper = new(8);
    //

    private readonly ConsoleService _consoleService = consoleService;
    private NativeViewPtr<byte> _titleStrPtr;
    private NativeViewPtr<byte> _inputStrPtr;
    private ArenaBlockPtr _panelMemory;


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Allocate()
    {
        var builder = TextBuffers.PersistentArena.AllocBuilder();
        _inputStrPtr = builder.AllocSlice(64);
        _titleStrPtr = builder.AllocSlice(32);
        _panelMemory = builder.Commit();

        _titleStrPtr.Writer().Append("Console"u8).Append((char)0);
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
        ImGui.PushStyleColor(ImGuiCol.Text, Palette32.TextSecondary);
        ImGui.SeparatorText(_titleStrPtr);
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ConsoleInnerBgColor);

        // Inner
        var inputHeight = GuiLayout.GetFrameHeightWithSpacing() + 8f;
        if (ImGui.BeginChild("inner"u8, new Vector2(0, -inputHeight), 0, InnerFlags))
        {
            WindowLayout.ActiveDrawList = ImGui.GetWindowDrawList();
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

        var clipper = new ImGuiListClipper();
        clipper.Begin(_consoleService.LogCount, RowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - clipper.DisplayStart;
            var cursor = UiDrawCursor.Make(InnerItemSpacing.X, InnerItemSpacing.Y);
            var logs = _consoleService.GetLogs(start, length);
            foreach (var it in logs)
            {
                cursor.Spacing();
                if (it.Scope > LogScope.Command)
                    DrawLog(it.LogPtr, it.Level, it.Scope, ref cursor);
                else
                    DrawPlain(it.LogPtr, it.Scope, ref cursor);
            }

            cursor.Sync();
        }

        clipper.End();

        ImGui.PopStyleVar();

        if (_scrollTopBottomStepper.Tick())
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollTopBottomStepper.SetIntervalTicks(0);
        }
    }

    private static void DrawLog(byte* logPtr, LogLevel level, LogScope scope, ref UiDrawCursor cursor)
    {
        cursor.Text(logPtr, LogEntry.TimestampOffset, Palette32.TextSecondary);
        cursor.SameLine();
        cursor.Text(level.ToLogText(), StyleMap.GetLogLevelColor(level));
        cursor.SameLine();
        cursor.Text(scope.ToLogText());
        cursor.SameLine();

        var color = level == LogLevel.Error ? Palette32.RedBase : Palette32.TextPrimary;
        cursor.Text(logPtr + LogEntry.TimestampOffset, color);
    }

    private static void DrawPlain(byte* logPtr, LogScope scope, ref UiDrawCursor cursor)
    {
        cursor.Text(logPtr, LogEntry.TimestampOffset, Palette32.TextSecondary);
        if (scope == LogScope.Command)
        {
            cursor.SameLine();
            cursor.Text(">>"u8, Palette32.OrangeBase);
        }

        cursor.SameLine();
        cursor.Text(logPtr + LogEntry.TimestampOffset);
    }

    private void HandleInput()
    {
        var byteSpan = _inputStrPtr.AsSpan().SliceNullTerminate();
        if (byteSpan.IsEmpty || !UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        _consoleService.ExecCommand(chars);

        byteSpan.Clear();
        ImGui.SetKeyboardFocusHere();
        ScrollToBottom();
    }

    /*
      ImGui.TextColored(Palette.TextSecondary, logPtr);
      ImGui.SameLine();
      ImGui.TextColored(Color4.White, level.ToLogText());
      //ImGui.TextColored(StyleMap.GetLogLevelColor(level), level.ToLogText());
      ImGui.SameLine();
      ImGui.TextUnformatted(scope.ToLogText());
      ImGui.SameLine();
      if (level == LogLevel.Error)
          ImGui.TextColored(Palette.RedLight, logPtr + LogEntry.TimestampOffset);
      else
          ImGui.TextUnformatted(logPtr + LogEntry.TimestampOffset);
      */
}