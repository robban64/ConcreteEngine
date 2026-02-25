using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.UI;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.CLI;

internal static unsafe class LogDrawer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLog(LogItem log,  FrameContext ctx)
    {
        ref var data = ref log.Data;
        ImGui.TextColored(Palette.TextSecondary, ref data.Time.GetRef());

        ImGui.SameLine(84);

        if (log.Scope != LogScope.Command)
        {
            ImGui.TextColored(StyleMap.GetLogLevelColor(log.Level), ref data.Level.GetRef());
            ImGui.SameLine(84 + 52);
            ImGui.TextUnformatted(ref data.Scope.GetRef());
        }
        else
        {
            ImGui.TextColored(Palette.OrangeBase, "$"u8);
        }


        ImGui.SameLine();

        if (log.Level == LogLevel.Error)
            ImGui.TextColored(Palette.RedLight, ctx.Write(log.Message));
        else
            ImGui.TextUnformatted(ctx.Write(log.Message));
    }
}