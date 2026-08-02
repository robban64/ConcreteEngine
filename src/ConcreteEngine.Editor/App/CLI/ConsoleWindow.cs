using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.App.Shared;
using ConcreteEngine.Editor.App.Theme;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Field;
using ConcreteEngine.Editor.Logging;
using ConcreteEngine.Editor.Metrics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.CLI;

internal sealed unsafe class ConsoleWindow : EditorWindow
{
    private const ImGuiWindowFlags InnerFlags =
        ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

    //
    private static readonly Vector2 InputFramePad = new(8f, 6f);
    private static readonly float InputHeight = GuiTheme.FontSizeDefault + InputFramePad.Y * 2 + GuiTheme.ItemSpacing.Y;
    private static readonly float RowHeight = GuiTheme.FontSizeDefault + GuiTheme.ItemSpacing.Y;
    private FrameStepper _scrollTopBottomStepper = new(8);

    //    
    private readonly TextInput _textInput;

    private NativeString _title;

    public override ReadOnlySpan<byte> Id => WindowRoot.ConsoleWindowId;

    public ConsoleWindow(StateManager state) : base(state)
    {
        _textInput = new TextInput("cli", 64, ConsoleSystem.ExecuteCommand)
            {
                Hint = "$", Trim = true, Lowercase = true, ClearAfter = true
            }
            .WithHistory()
            .ToggleFlag(ImGuiInputTextFlags.CharsNoBlank, false)
            .ToggleFlag(ImGuiInputTextFlags.EnterReturnsTrue, true);
    }


    protected override void OnCreate()
    {
        _title = StringArena.AllocateString(64);
    }

    public override void OnUpdateDiagnostic()
    {
        if (LogService.Instance.NewLogs > 0)
            _scrollTopBottomStepper.SetIntervalTicks(4);

        var sw = _title.OverWriter;
        sw.Append("CLI: "u8);
        sw.AppendAscii('[').Append(ImGuiSystem.Io.Framerate, "F0").AppendAscii('H', 'Z', ']');
        sw.PadRight(4);

        sw.AppendAscii('[').Append(MetricSystem.Instance.Metric.AvgMs, "F4").AppendAscii('m', 's', ']');
        sw.PadRight(4);

        sw.Append("GC: "u8);
        sw.AppendAscii('[').Append(MetricSystem.Instance.Metric.AllocMbPerSec, "F4").Append("MB/s"u8).AppendAscii(']');
        sw.PadRight(4);

        sw.Append("Native: "u8);
        sw.AppendAscii('[').Append(NativeArray.AllocSizeInMb, "F2").AppendAscii('M', 'B', ']');

        var length = sw.End().Length;
        _title.SetLength(length);
    }


    protected override void OnDraw()
    {
        // header
        ImGui.PushStyleColor(ImGuiCol.Text, Palette32.TextSecondary);
        ImGui.SeparatorText(_title);
        ImGui.PopStyleColor();

        // log
        var innerWindow = ImGui.BeginChild("logs"u8, new Vector2(0, -InputHeight), ImGuiChildFlags.None, InnerFlags);
        if (innerWindow && LogService.Instance.LogCount > 0)
            DrawLogInnerWindow();

        ImGui.EndChild();

        // input
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Palette32.SurfaceDark);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, InputFramePad);
        ImGui.SetNextItemWidth(-1f);

        _textInput.Draw();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(1);
    }

    private void DrawLogInnerWindow()
    {
        var logService = LogService.Instance;

        foreach (var range in AppDraw.Clipper(logService.LogCount, RowHeight, out _))
        {
            var cursor = UiDrawCursor.Make();
            var logs = logService.GetLogs(range.Offset, range.Length);
            for (var i = 0; i < logs.Length; i++)
            {
                var it = logs[i];
                if (i > 0) cursor.NewLine();

                var text = logService.GetLogText(it.Handle);
                if (it.Scope > LogScope.Command)
                    DrawLog(text, it.Scope, it.Level, ref cursor);
                else
                    cursor.Text(text.SliceFrom(LogEntry.TimestampOffset));
            }

            cursor.Sync();
        }

        if (_scrollTopBottomStepper.Tick())
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollTopBottomStepper.SetIntervalTicks(0);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawLog(NativeView<byte> text, LogScope scope, LogLevel level, scoped ref UiDrawCursor cursor)
    {
        cursor.Text(text.Slice(0, LogEntry.TimestampOffset));
        cursor.SameLine();
        cursor.Text(level.ToLogText(), StyleMap.GetLogLevelColor(level));
        cursor.SameLine();
        cursor.Text(scope.ToLogText());
        cursor.SameLine();

        var color = level == LogLevel.Error ? Palette32.RedBase : Palette32.TextPrimary;
        cursor.Text(text.SliceFrom(LogEntry.TimestampOffset), color);
    }
}