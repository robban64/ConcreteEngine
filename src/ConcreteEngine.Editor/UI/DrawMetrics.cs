using ConcreteEngine.Core.Common.Text;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static class GuiMetrics
{
    public static void MetricText(
        UnsafeSpanWriter sw,
        string prefix,
        float value,
        string format = "",
        string suffix = "",
        int space = 50)
    {
        ImGui.TextUnformatted(ref sw.Write(prefix));
        ImGui.SameLine(space);
        ImGui.TextUnformatted(ref sw.Start(value, format).Append(suffix).End());
    }

    public static void MetricHistory(
        UnsafeSpanWriter sw,
        string prefix,
        float val1,
        float val2,
        bool hasRef,
        string format = "",
        string suffix = "",
        int space = 50)
    {
        ImGui.TextUnformatted(ref sw.Write(prefix));
        ImGui.SameLine(space);
        ImGui.TextUnformatted(ref sw.Start(val1, format).Append(suffix).End());

        if (!hasRef) return;

        float diff = val1 - val2;
        if (Math.Abs(diff) > 0.01f)
        {
            ImGui.SameLine(space * 2);

            var sign = diff > 0 ? "+"u8 : ReadOnlySpan<byte>.Empty;
            ImGui.TextUnformatted(ref sw.Start("("u8).Append(sign).Append(diff, format).Append(")"u8).End());
        }
    }
}