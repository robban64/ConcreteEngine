using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.UI;

internal static class RefGui
{
    public static void TextUnformatted(ref ZaUtf8SpanWriter za)
    {
        ImGui.TextUnformatted(za.AsSpan());
        za.Clear();
    }

    public static void SeparatorTextId(ReadOnlySpan<byte> prop, int id, ref ZaUtf8SpanWriter za)
    {
        ImGui.SeparatorText(za.Append(prop).Append(" ["u8).Append(id).AppendEnd("]"u8).AsSpan());
        za.Clear();
    }

    
    public static void NextRowPushId(ref ZaUtf8SpanWriter za, ImGuiTableRowFlags flags = ImGuiTableRowFlags.None, float rowHeight = 0)
    {
        ImGui.PushID(za.AsSpan());
        ImGui.TableNextRow(flags,rowHeight);
        za.Clear();
    }
    
    public static void DrawColumn(ref ZaUtf8SpanWriter za)
    {
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(za.AsSpan());
        za.Clear();
    }
    
    public static void DrawRightProp(ReadOnlySpan<byte> t1, ref ZaUtf8SpanWriter za)
    {
        ImGui.TextUnformatted(t1);
        ImGui.SameLine();
        ImGui.TextUnformatted(za.AsSpan());
        za.Clear();
    }

    public static void DrawRightPropColor(in Color4 color, ReadOnlySpan<byte> t1, ref ZaUtf8SpanWriter za)
    {
        ImGui.TextUnformatted(t1);
        ImGui.SameLine();
        ImGui.TextColored(color, za.AsSpan());
        za.Clear();
    }
}