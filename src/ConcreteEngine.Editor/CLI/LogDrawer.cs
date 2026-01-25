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
        var sw = ctx.Writer;

        if (scope != LogScope.Command)
        {
            ImGui.TextColored(level.ToColor(), ref sw.Start("["u8).Append(level.ToLogText()).Append("]"u8).End());
            ImGui.SameLine(42);
        }

        ImGui.TextColored(Palette.TextSecondary, ref sw.Start("["u8).Append(ts.Hour).Append(":"u8).Append(ts.Minute)
            .Append(":"u8).Append(ts.Second).Append(":"u8).Append(ts.Millisecond).Append("] "u8).End());

        ImGui.SameLine();
        ImGui.TextColored(scope.ToLogColor(), scope.ToLogText());

        ImGui.SameLine();
        if (level == LogLevel.Error)
            ImGui.TextColored(Palette.RedLight, ref sw.Write(log.Message));
        else
            ImGui.TextUnformatted(ref sw.Write(log.Message));
    }
}