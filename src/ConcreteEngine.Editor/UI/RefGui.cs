using Hexa.NET.ImGui;
using ZaString.Core;

namespace ConcreteEngine.Editor.UI;

internal static class RefGui
{
    public static void DrawIdRow(ref ZaUtf8SpanWriter za)
    {
        ImGui.TableNextRow();
        ImGui.PushID(za.AsSpan());
        za.Clear();
    }
    
    public static void DrawColumn(ref ZaUtf8SpanWriter za)
    {
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(za.AsSpan());
        za.Clear();
    }
}