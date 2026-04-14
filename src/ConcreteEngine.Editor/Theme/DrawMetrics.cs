using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Theme;

internal static unsafe class GuiMetrics
{
    public static void MetricText(
        UnsafeSpanWriter sw,
        string prefix,
        float value,
        string format = "",
        string suffix = "",
        float space = 50)
    {
        AppDraw.Text(sw.Append(prefix).End());

        if (space == 0) ImGui.SameLine();
        else ImGui.SameLine(space);
        AppDraw.Text(sw.Append(value, format).Append(suffix).End());
    }
    public static void MetricText(
        UnsafeSpanWriter sw,
        string prefix,
        Half value,
        string format = "",
        string suffix = "",
        float space = 50)
    {
        AppDraw.Text(sw.Append(prefix).End());

        if (space == 0) ImGui.SameLine();
        else ImGui.SameLine(space);
        AppDraw.Text(sw.Append(value, format).Append(suffix).End());
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
        ImGui.TextUnformatted(ctx.Sw.Append(val1, format).Append(suffix).EndPtr());

        if (!hasRef) return;

        float diff = val1 - val2;
        if (Math.Abs(diff) > 0.01f)
        {
            ImGui.SameLine(space * 2);

            var sign = diff > 0 ? "+" : string.Empty;
            ImGui.TextUnformatted(ctx.Sw.Append('(').Append(sign).Append(diff, format).Append(')').EndPtr());
        }
    }
}