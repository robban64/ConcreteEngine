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
/*
    private readonly NativeArray<byte> _drawBuffer;
    private readonly NativeView<Color4> _colorPtr;
    private readonly NativeView<byte> _levelPtr;
    private readonly NativeView<byte> _scopePtr;
*/

    private FrameStepper _scrollTopBottomStepper = new(8);
    private readonly NativeView<byte> _avgView = TextBuffers.Arena.Alloc(16);
    public ConsolePanel()
    {
        _avgView.Writer().Append("[0ms]"u8);
        /* _drawBuffer = NativeArray.Allocate<byte>(512);

         var colorLength = EnumCache<LogLevel>.Count * Unsafe.SizeOf<Color4>();
         var logLevelLength = EnumCache<LogLevel>.Count * 16;
         var logScopeLength = EnumCache<LogScope>.Count * 16;

         _colorPtr = _drawBuffer.Slice(0, colorLength).Reinterpret<Color4>();
         _levelPtr = _drawBuffer.Slice(colorLength, logLevelLength);
         _scopePtr = _drawBuffer.Slice(colorLength + logLevelLength, logScopeLength);

         _colorPtr[(int)LogLevel.None] = Color4.White;
         _colorPtr[(int)LogLevel.Trace] = Palette.GrayLight;
         _colorPtr[(int)LogLevel.Debug] = Palette.BlueLight;
         _colorPtr[(int)LogLevel.Info] = Palette.GreenBase;
         _colorPtr[(int)LogLevel.Warn] = Palette.OrangeBase;
         _colorPtr[(int)LogLevel.Error] = Palette.RedBase;
         _colorPtr[(int)LogLevel.Critical] = Palette.RedLight;


         var sw = _levelPtr.Writer();
         for (int i = 0; i < EnumCache<LogLevel>.Count; i++)
         {
             var name = EnumCache<LogLevel>.Names[i];
             sw.SetCursor(i * 16);
             sw.Append('[').Append(name).Append(']').Append((char)0);
         }

         sw = _scopePtr.Writer();
         for (int i = 0; i < EnumCache<LogScope>.Count; i++)
         {
             var name = EnumCache<LogScope>.Names[i];
             sw.SetCursor(i * 16);
             sw.Append('[').Append(name).Append(']').Append((char)0);
         }
         */
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ScrollToBottom()
    {
        if (_scrollTopBottomStepper.IntervalTicks > 0) return;
        _scrollTopBottomStepper.SetIntervalTicks(4);
    }

    internal void UpdateDiagnostic()
    {
        _avgView.Writer().Append('[').Append(MetricSystem.Instance.Metric.AvgMs, "F4").Append("ms"u8)
            .Append(']').End();
    }

    internal void DrawConsole(ConsoleService service, FrameContext ctx)
    {
        if (!ImGui.Begin("cli"u8))
        {
            ImGui.End();
            return;
        }

        // header
        ImGui.PushStyleColor(ImGuiCol.Text, 0x99FFFFFF);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(_avgView);
        ImGui.SameLine();
        ImGui.SeparatorText("Console"u8);
        ImGui.PopStyleColor();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ConsoleInnerBgColor);

        // Inner
        var inputHeight = ImGui.GetFrameHeightWithSpacing() + 8f;
        ImGui.BeginChild("inner"u8, new Vector2(0, -inputHeight), 0, InnerFlags);

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, InnerItemSpacing);
        if (service.LogCount > 0)
        {
            var rowHeight = ImGui.GetFontSize() + FramePadding.Y + 4f;
            DrawVisibleLogs(service, ctx, rowHeight);
        }
        ImGui.PopStyleVar();

        if (_scrollTopBottomStepper.Tick())
        {
            ImGui.SetScrollHereY(1.0f);
            _scrollTopBottomStepper.SetIntervalTicks(0);
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

    private void DrawVisibleLogs(ConsoleService service, FrameContext ctx, float rowHeight)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(service.LogCount, rowHeight);
        while (clipper.Step())
        {
            var sw = ctx.Sw;
            int start = clipper.DisplayStart, end = clipper.DisplayEnd - clipper.DisplayStart;
            var logs = service.GetLogs().Slice(start, end);
            foreach (ref var it in logs)
                DrawLog(ref it, sw);
        }

        clipper.End();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private  void DrawLog(ref LogEntry log, UnsafeSpanWriter sw)
    {
        ImGui.TextColored(Palette.TextSecondary, sw.Write(ref log.Timestamp.GetRef()));

        ImGui.SameLine();

        if (log.Scope != LogScope.Command)
        {
            ImGui.TextColored(StyleMap.GetLogLevelColor(log.Level), log.Level.ToLogText());
            ImGui.SameLine();
            ImGui.TextUnformatted(log.Scope.ToLogText());
        }
        else
        {
            ImGui.TextColored(Palette.OrangeBase, "$"u8);
        }

        ImGui.SameLine();

        if (log.Level == LogLevel.Error)
            ImGui.TextColored(Palette.RedLight, sw.Write(log.Message));
        else
            ImGui.TextUnformatted(sw.Write(log.Message));
    }
}