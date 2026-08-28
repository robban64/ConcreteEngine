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
using ConcreteEngine.Editor.Lib.Inputs;
using ConcreteEngine.Editor.Logging;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.App.CLI;


internal sealed unsafe class ConsoleWindow : EditorWindow
{
    private const ImGuiWindowFlags InnerFlags =
        ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

    private const float InputFramePadHeight = 6f;
    private const float InputBaseHeight = GuiTheme.FontSizeDefault + InputFramePadHeight * 2;
    
    private static float InputHeight => InputBaseHeight + GuiTheme.ItemSpacing.Y;
    private static float RowHeight => GuiTheme.FontSizeDefault + GuiTheme.ItemSpacing.Y;
    
    //    
    private readonly TextInput _textInput;

    private readonly NativeString _title;
    private FrameStepper _scrollTopBottomStepper = new(8);

    public override ReadOnlySpan<byte> Id => WindowRoot.ConsoleWindowId;

    public ConsoleWindow(StateManager state) : base(state)
    {
        _title = StringArena.AllocateString(80);
        _textInput = new TextInput("cli", 64, ConsoleSystem.ExecuteCommand)
            {
                Hint = "$", Trim = true, Lowercase = true, ClearAfter = true
            }
            .WithHistory()
            .ToggleFlag(ImGuiInputTextFlags.CharsNoBlank, false)
            .ToggleFlag(ImGuiInputTextFlags.EnterReturnsTrue, true);
    }

    public override void OnUpdateDiagnostic()
    {
        if (LogService.Instance.NewLogs > 0)
            _scrollTopBottomStepper.SetIntervalTicks(4);

        var sw = _title.GetWriter();
        sw.Append("CLI: "u8);
        sw.AppendAscii('[').Append(ImGuiSystem.Io.Framerate, "F0").AppendAscii(']');
        sw.PadRight(4);

        sw.AppendAscii('[').Append(MetricSystem.Instance.Metric.AvgMs, "F4").AppendAscii('m', 's', ']');
        sw.PadRight(4);

        var allocMbPerSec = MetricSystem.Instance.Metric.AllocMbPerSec;
        sw.Append("GC: "u8);
        sw.AppendAscii('[').Append(allocMbPerSec, "F4").Append("MB/s"u8).AppendAscii(']');
        sw.PadRight(4);

        sw.Append("Native: "u8);
        sw.AppendAscii('[').Append(NativeArray.AllocSizeInMb, "F2").AppendAscii('M', 'B', ']');
        sw.EndNativeString();
    }


    protected override void OnDraw()
    {
        // header
        ImGui.PushStyleColor(ImGuiCol.Text, Palette32.TextSecondary);
        ImGui.SeparatorText(_title);
        ImGui.PopStyleColor();

        // log
        var innerWindow = ImGui.BeginChild("logs"u8, new Vector2(0, -InputHeight), ImGuiChildFlags.None, InnerFlags);
        var logCount = LogService.Instance.LogCount;
        if (innerWindow && logCount > 0)
        {
            DrawLogInnerWindow(logCount);

            if (_scrollTopBottomStepper.Tick())
            {
                ImGui.SetScrollHereY(1.0f);
                _scrollTopBottomStepper.SetIntervalTicks(0);
            }
        }

        ImGui.EndChild();

        // input
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Palette32.SurfaceDark);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8f, InputFramePadHeight));
        ImGui.SetNextItemWidth(-1f);

        _textInput.Draw();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(1);
    }


    private static void DrawLogInnerWindow(int count)
    {
        foreach (var range in AppDraw.Clipper(count, RowHeight, out _))
        {
            var cursor = UiDrawCursor.Make();
            var logs = LogService.Instance.GetLogs(range.Offset, range.Length);
            for (var i = 0; i < logs.Length; i++)
            {
                var it = logs[i];
                if (i > 0) cursor.NewLine();

                var text = LogService.Instance.GetLogText(it.Handle);
                DrawLog(text, it.Scope, it.Level, ref cursor);
            }

            cursor.Sync();
        }
    }


    private static void DrawLog(NativeView<byte> text, LogScope scope, LogLevel level, scoped ref UiDrawCursor cursor)
    {
        if (scope == LogScope.Command)
        {
            cursor.Text(text.SliceFrom(LogEntry.TimestampOffset));
            return;
        }

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