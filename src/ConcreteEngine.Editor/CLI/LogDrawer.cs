using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.CLI;

internal static class LogDrawer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static  void DrawLog(StringLogEvent log,  FrameContext ctx)
    {
        var ts = log.Timestamp;
        var level = log.Level;
        var scope = log.Scope;

        ImGui.TextColored(Palette.TextSecondary,  ref ctx.Sw.Start("["u8).Append(ts.Hour).Append(":"u8).Append(ts.Minute)
            .Append(":"u8).Append(ts.Second).Append(":"u8).Append(ts.Millisecond).Append("] "u8).End());
        
        ImGui.SameLine(84);

        if (scope != LogScope.Command)
        {
            ImGui.TextColored(StyleMap.GetLogLevelColor(level), ref ctx.Sw.Start("["u8).Append(level.ToLogText()).Append("]"u8).End());
            ImGui.SameLine(84+52);
            ImGui.TextUnformatted(ref ctx.Sw.Write(scope.ToLogText()));
        }
        else
        {
            ImGui.TextColored(Palette.OrangeBase,"$"u8);
        }

        
        ImGui.SameLine();
        
        if (level == LogLevel.Error)
            ImGui.TextColored(Palette.RedLight, ref ctx.Sw.Write(log.Message));
        else
            ImGui.TextUnformatted(ref ctx.Sw.Write(log.Message));
    }
}