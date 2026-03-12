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

internal sealed unsafe class ConsolePanel
{
    private const ImGuiWindowFlags InnerFlags =
        ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar;

    [FixedAddressValueType] private static String64Utf8 _inputUtf8;

    private static readonly Vector2 InnerItemSpacing = new(12f, 6f);

    private static FrameStepper _scrollTopBottomStepper = new(8);

    private readonly NativeViewPtr<byte> _avgViewPtr = TextBuffers.Arena.Alloc(16);

    public ConsolePanel()
    {
        _avgViewPtr.Writer().Append("[0ms]"u8);
    }

    internal static void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }

    internal void UpdateDiagnostic()
    {
        _avgViewPtr.Writer().Append('[').Append(MetricSystem.Instance.Metric.AvgMs, "F4").Append("ms"u8)
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
        ImGui.TextUnformatted(_avgViewPtr);
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

        if (ImGui.InputTextWithHint("##input"u8, "$"u8, ref _inputUtf8.GetRef(), String64Utf8.Capacity,
                ImGuiInputTextFlags.EnterReturnsTrue))
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
        var rowHeight = ImGui.GetFontSize() + FramePadding.Y + 4f;

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
        UtfText.SliceNullTerminate(_inputUtf8.AsSpan(), out var byteSpan);
        if (byteSpan.IsEmpty) return;

        var charLength = Encoding.UTF8.GetCharCount(byteSpan);
        Span<char> charBuffer = stackalloc char[charLength];
        if (!InputTextUtils.DecodeUtf8Input(byteSpan, charBuffer, out var inputStr)) return;

        service.ExecCommand(inputStr);

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