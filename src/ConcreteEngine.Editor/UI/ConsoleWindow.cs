using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Editor.CLI;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Lib;
using ConcreteEngine.Editor.Lib.Widgets;
using ConcreteEngine.Editor.Metrics;
using ConcreteEngine.Editor.Theme;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed unsafe class ConsoleWindow : EditorWindow
{
    private const ImGuiWindowFlags InnerFlags =
        ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

    //
    private static readonly Vector2 InputFramePad = new(8f, 6f);
    private static readonly Vector2 ItemSpacing = new(12f, 6f);

    private static readonly float InputHeight = GuiTheme.FontSizeDefault + InputFramePad.Y * 2 + ItemSpacing.Y;
    private static readonly float RowHeight = GuiTheme.FontSizeDefault + ItemSpacing.Y;
    private static FrameStepper _scrollTopBottomStepper = new(8);

    //    
    private readonly TextInput _textInput;

    private NativeString _title;
    private NativeString _inputString;


    public override ReadOnlySpan<byte> Id => WindowRoot.ConsoleWindowId;

    public ConsoleWindow(StateManager state) : base(state)
    {
        _textInput = new TextInput("cli", 64, ImGuiInputTextFlags.EnterReturnsTrue) { Hint = "$" }
            .WithHistory()
            .WithClearOnResult()
            .WithTransformer(true, true)
            .WithCallbackU16(static (text) => ConsoleGateway.Service.ExecCommand(text));
    }


    protected override void OnCreate()
    {
        _title = StringArena.AllocateString(64);
        _inputString = StringArena.AllocateString(64);
        _textInput.SetTextBuffer(_inputString);
    }

    public static void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }

    public override void OnUpdateDiagnostic()
    {
        var metrics = MetricSystem.Instance;
        //ImGui.GetIO().Framerate
        _title.NewWrite.Append("Console"u8).PadRight(4)
            .Append('[').Append(metrics.Metric.AvgMs, "F4").Append("ms"u8).Append(']')
            .PadRight(4)
            .Append('[').Append(metrics.Metric.AllocMbPerSec, "F4").Append("MB/s"u8).Append(']')
            .End();
    }


    protected override void OnDraw()
    {
        // header
        ImGui.PushStyleColor(ImGuiCol.Text, Palette32.TextSecondary);
        ImGui.SeparatorText(_title);
        ImGui.PopStyleColor();

        // log
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ItemSpacing);
        var innerWindow = ImGui.BeginChild("logs"u8, new Vector2(0, -InputHeight), ImGuiChildFlags.None, InnerFlags);
        if (innerWindow && ConsoleGateway.Service.LogCount > 0)
        {
            var clipper = new ImGuiListClipper();
            clipper.Begin(ConsoleGateway.Service.LogCount, RowHeight);
            while (clipper.Step())
            {
                int start = clipper.DisplayStart, length = clipper.DisplayEnd - clipper.DisplayStart;
                DrawVisibleLogs(ConsoleGateway.Service, start, length);
            }

            if (_scrollTopBottomStepper.Tick())
            {
                ImGui.SetScrollHereY(1.0f);
                _scrollTopBottomStepper.SetIntervalTicks(0);
            }
        }

        ImGui.EndChild();
        ImGui.PopStyleVar(1);

        // input
        ImGui.PushStyleColor(ImGuiCol.FrameBg, Palette32.SurfaceDark);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, InputFramePad);
        ImGui.SetNextItemWidth(-1f);

        _textInput.Draw();

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(1);
    }

    private static void DrawVisibleLogs(ConsoleService service, int start, int length)
    {
        var cursor = UiDrawCursor.Make(ItemSpacing);
        var logs = service.GetLogs(start, length);
        for (var i = 0; i < logs.Length; i++)
        {
            var it = logs[i];
            if (i > 0) cursor.NewLine();

            var text = service.GetLogText(it.Handle);
            if (it.Scope > LogScope.Command)
                DrawLog(text, it.Scope, it.Level, ref cursor);
            else
                DrawPlain(text, it.Scope, ref cursor);
        }

        cursor.Sync();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DrawLog(NativeView<byte> text, LogScope scope, LogLevel level, scoped ref UiDrawCursor cursor)
    {
        cursor.Text(text.Slice(0, LogEntry.TimestampOffset), Palette32.TextSecondary);
        cursor.SameLine();
        cursor.Text(level.ToLogText(), StyleMap.GetLogLevelColor(level));
        cursor.SameLine();
        cursor.Text(scope.ToLogText());
        cursor.SameLine();

        var color = level == LogLevel.Error ? Palette32.RedBase : Palette32.TextPrimary;
        cursor.Text(text.SliceFrom(LogEntry.TimestampOffset), color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
}