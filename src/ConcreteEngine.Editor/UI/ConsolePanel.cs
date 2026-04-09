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

    private NativeView<byte> _titleStr;
    private NativeView<byte> _inputStr;
    private ArenaBlockPtr _panelMemory;


    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Allocate()
    {
        var builder = TextBuffers.PersistentArena.AllocBuilder();
        _inputStr = builder.AllocSlice(64);
        _titleStr = builder.AllocSlice(32);
        _panelMemory = builder.Commit();

        _titleStr.Writer().Append("Console"u8).Append((char)0);
    }

    internal static void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }

    internal void OnUpdateDiagnostic()
    {
        var metrics = MetricSystem.Instance;
        _titleStr.Writer()
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
        ImGui.SeparatorText(_titleStr);
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

        if (ImGui.InputTextWithHint("##input"u8, "$"u8, _inputStr, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            HandleInput();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);
    }

    private void DrawVisibleLogs()
    {
        if (consoleService.LogCount == 0) return;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, InnerItemSpacing);

        var clipper = new ImGuiListClipper();
        clipper.Begin(consoleService.LogCount, RowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - clipper.DisplayStart;
            var cursor = UiDrawCursor.Make(InnerItemSpacing.X, InnerItemSpacing.Y);
            foreach (var it in consoleService.GetLogs(start, length))
            {
                cursor.Spacing();
                if (it.Scope > LogScope.Command)
                    DrawLog(it, ref cursor);
                else
                    DrawPlain(it, ref cursor);
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

    private  void DrawLog(LogEntry log, ref UiDrawCursor cursor)
    {
        var logText = consoleService.GetLogText(log.Handle);
        cursor.Text(logText.Slice(0, LogEntry.TimestampOffset), Palette32.TextSecondary);
        cursor.SameLine();
        cursor.Text(log.Level.ToLogText(), StyleMap.GetLogLevelColor(log.Level));
        cursor.SameLine();
        cursor.Text(log.Scope.ToLogText());
        cursor.SameLine();

        var color = log.Level == LogLevel.Error ? Palette32.RedBase : Palette32.TextPrimary;
        cursor.Text(logText.SliceFrom(LogEntry.TimestampOffset), color);
    }

    private void DrawPlain(LogEntry log, ref UiDrawCursor cursor)
    {
        var logText = consoleService.GetLogText(log.Handle);
        cursor.Text(logText.Slice(0, LogEntry.TimestampOffset), Palette32.TextSecondary);
        if (log.Scope == LogScope.Command)
        {
            cursor.SameLine();
            cursor.Text(">>"u8, Palette32.OrangeBase);
        }

        cursor.SameLine();
        cursor.Text(logText.SliceFrom(LogEntry.TimestampOffset));
    }

    private void HandleInput()
    {
        var byteSpan = _inputStr.AsSpan().SliceNullTerminate();
        if (byteSpan.IsEmpty || !UtfText.IsAscii(byteSpan)) return;

        Span<char> chars = stackalloc char[byteSpan.Length];
        Encoding.UTF8.GetChars(byteSpan, chars);

        consoleService.ExecCommand(chars);

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