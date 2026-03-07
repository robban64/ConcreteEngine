using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.CLI;

internal static unsafe class LogDrawer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLog(LogItem log, FrameContext ctx)
    {
        ImGui.TextColored(Palette.TextSecondary, ctx.Sw.Write(ref log.TimeString.GetRef()));

        ImGui.SameLine();

        if (log.Scope != LogScope.Command)
        {
            ImGui.TextColored(StyleMap.GetLogLevelColor(log.Level), ctx.Sw.Write(ref log.LevelString.GetRef()));
            ImGui.SameLine();
            ImGui.TextUnformatted(ctx.Sw.Write(ref log.ScopeString.GetRef()));
        }
        else
        {
            ImGui.TextColored(Palette.OrangeBase, "$"u8);
        }


        ImGui.SameLine();

        if (log.Level == LogLevel.Error)
            ImGui.TextColored(Palette.RedLight, ctx.Sw.Write(log.Message));
        else
            ImGui.TextUnformatted(ctx.Sw.Write(log.Message));
    }
}