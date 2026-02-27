using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static unsafe class GuiMetrics
{
    public static void MetricText(
        FrameContext ctx,
        string prefix,
        float value,
        string format = "",
        string suffix = "",
        int space = 50)
    {
        ImGui.TextUnformatted(ctx.Sw.Write(prefix));
        ImGui.SameLine(space);
        ImGui.TextUnformatted(ref ctx.Sw.Start(value, format).Append(suffix).End());
    }

    public static void MetricHistory(
        FrameContext ctx,
        string prefix,
        float val1,
        float val2,
        bool hasRef,
        string format = "",
        string suffix = "",
        int space = 50)
    {
        ImGui.TextUnformatted(ctx.Sw.Write(prefix));
        ImGui.SameLine(space);
        ImGui.TextUnformatted(ref ctx.Sw.Start(val1, format).Append(suffix).End());

        if (!hasRef) return;

        float diff = val1 - val2;
        if (Math.Abs(diff) > 0.01f)
        {
            ImGui.SameLine(space * 2);

            var sign = diff > 0 ? "+" : string.Empty;
            ImGui.TextUnformatted(ref ctx.Sw.Start('(').Append(sign).Append(diff, format).Append(')').End());
        }
    }
}