using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Extensions;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.CLI;

internal static class LogDrawer
{
    public static void DrawLog(int i, StringLogEvent log, in FrameContext ctx)
    {
        var ts = log.Timestamp;
        var level = log.Level;
        var scope = log.Scope;
        var msg = log.Message;

        var sw = ctx.Writer;
        ImGui.TextColored(level.ToColor(), sw.Start("["u8).Append(level.ToLogText()).Append("]"u8).End());

        ImGui.SameLine(42);
        ImGui.TextColored(Palette.GrayLight, sw.Start("["u8).Append(ts.Hour).Append(":"u8).Append(ts.Minute)
            .Append(":"u8).Append(ts.Second).Append(":"u8).Append(ts.Millisecond).Append("] "u8).End());

        ImGui.SameLine();
        ImGui.TextColored(Palette.CyanLight, scope.ToLogText());

        ImGui.SameLine();
        ImGui.TextColored(Palette.WhiteSilver, sw.Start(" - ").Append(msg).End());
    }

    private static Color4 ToColor(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => Palette.GrayLight,
            LogLevel.Debug => Palette.BlueLight,
            LogLevel.Info => Palette.GreenBase,
            LogLevel.Warn => Palette.OrangeBase,
            LogLevel.Error => Palette.RedBase,
            LogLevel.Critical => Palette.RedLight,
            _ => Color4.White
        };
    }
}