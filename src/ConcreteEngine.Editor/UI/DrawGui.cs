using System.Numerics;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static class DrawGui
{
    public static void DrawRightProp(ReadOnlySpan<byte> t1, ReadOnlySpan<byte> t2)
    {
        ImGui.TextUnformatted(t1);
        ImGui.SameLine();
        ImGui.TextUnformatted(t2);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DrawSelectable(ReadOnlySpan<byte> str, bool selected, float width, float height,
        ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        var textWidth = ImGui.CalcTextSize(str).X;
        var offset = (width - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        bool result = ImGui.Selectable(str, selected, flags, new Vector2(0, height));


        ImGui.PopStyleVar();
        return result;
    }
}