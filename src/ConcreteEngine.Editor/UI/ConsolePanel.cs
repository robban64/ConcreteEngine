using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class ConsolePanel
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

    private Range32 _titleStrHandle;
    private Range32 _inputStrHandle;
    private MemoryBlockPtr _panelMemory;

    private readonly TextInput _textInput;
    private readonly ConsoleService _consoleService;

    public ConsolePanel(ConsoleService consoleService)
    {
        _consoleService = consoleService;
        _textInput = new TextInput(64, ImGuiInputTextFlags.EnterReturnsTrue)
            .WithHistory()
            .WithClearOnResult()
            .WithTransformer(true, true)
            .WithCallbackU16(HandleInput);
    }

    private NativeView<byte> TitleStr => _panelMemory.DataPtr.Slice(_titleStrHandle);
    private NativeView<byte> InputStr => _panelMemory.DataPtr.Slice(_inputStrHandle);

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void Allocate()
    {
        var builder = TextBuffers.PersistentArena.AllocBuilder();
        _titleStrHandle = builder.AllocSlice(64).AsRange32();
        _inputStrHandle = builder.AllocSlice(64).AsRange32();
        _panelMemory = builder.Commit();

        _panelMemory.DataPtr.Slice(_titleStrHandle).Writer().Append("Console"u8).Append((char)0);
        _panelMemory.DataPtr.Slice(_inputStrHandle).Clear();
    }

    internal static void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }

    internal void OnUpdateDiagnostic()
    {
        var metrics = MetricSystem.Instance;
        _panelMemory.DataPtr.Slice(_titleStrHandle)
            .Writer()
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
        ImGui.SeparatorText(TitleStr);
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ConsoleInnerBgColor);

        // Inner
        var inputHeight = GuiLayout.GetFrameHeightWithSpacing() + 8f;
        var innerWindow = ImGui.BeginChild("inner"u8, new Vector2(0, -inputHeight), 0, InnerFlags);
        if (innerWindow && _consoleService.LogCount > 0)
        {
            WindowLayout.ActiveDrawList = ImGui.GetWindowDrawList();

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, InnerItemSpacing);
            DrawVisibleLogs();
            ImGui.PopStyleVar();

            if (_scrollTopBottomStepper.Tick())
            {
                ImGui.SetScrollHereY(1.0f);
                _scrollTopBottomStepper.SetIntervalTicks(0);
            }
        }

        ImGui.EndChild();
        
        DrawInput();
    }


    private void DrawInput()
    {
        // input
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ConsoleFrameBg);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ConsoleFrameBgHovered);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ConsoleFrameBgActive);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ConsoleFramePadding);
        ImGui.SetNextItemWidth(-1f);

        _textInput.DrawHint("##input"u8, "$"u8, InputStr);

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(4);
    }


    private void DrawVisibleLogs()
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(_consoleService.LogCount, RowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - clipper.DisplayStart;
            var cursor = UiDrawCursor.Make(InnerItemSpacing.X, InnerItemSpacing.Y);
            foreach (var it in _consoleService.GetLogs(start, length))
            {
                cursor.Spacing();

                var text = _consoleService.GetLogText(it.Handle);
                if (it.Scope > LogScope.Command)
                    DrawLog(text, it.Scope, it.Level, ref cursor);
                else
                    DrawPlain(text, it.Scope, ref cursor);
            }

            cursor.Sync();
        }

        clipper.End();
    }

    private static void DrawLog(NativeView<byte> text, LogScope scope, LogLevel level, scoped ref UiDrawCursor cursor)
    {
        cursor.Text(text.Slice(0, LogEntry.TimestampOffset), Palette32.TextSecondary);
        cursor.SameLine();
        cursor.Text(TextMap.GetLogLevelText(level), StyleMap.GetLogLevelColor(level));
        cursor.SameLine();
        cursor.Text(TextMap.GetLogScopeText(scope));
        cursor.SameLine();

        var color = level == LogLevel.Error ? Palette32.RedBase : Palette32.TextPrimary;
        cursor.Text(text.SliceFrom(LogEntry.TimestampOffset), color);
    }

    private static void DrawPlain(NativeView<byte> text, LogScope scope, scoped ref UiDrawCursor cursor)
    {
        cursor.Text(text.Slice(0, LogEntry.TimestampOffset), Palette32.TextSecondary);
        if (scope == LogScope.Command)
        {
            cursor.SameLine();
            cursor.Text(">>"u8, Palette32.OrangeBase);
        }

        cursor.SameLine();
        cursor.Text(text.SliceFrom(LogEntry.TimestampOffset));
    }

    private void HandleInput(Span<char> text)
    {
        _consoleService.ExecCommand(text);
    }
}