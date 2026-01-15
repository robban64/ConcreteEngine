using System.Buffers.Text;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Definitions;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;
using ZaString.Extensions;

namespace ConcreteEngine.Editor.UI;

internal static class DrawContext
{
    private static readonly byte[] Buffer = new byte[128];

    public static FrameContext GetCtx(float dt, ModeState mode) => new(GetWriter(), dt, mode);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanWriter GetWriter() => new(Buffer.AsSpan());

    //


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SeparatorTextId(ref SpanWriter sw, ReadOnlySpan<byte> prop, int id)
    {
        ImGui.SeparatorText(sw.Start(prop).Append(" ["u8).Append(id).Append("]"u8).End());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NextColumn(ReadOnlySpan<byte> text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NextColumnColor(ReadOnlySpan<byte> text, in Color4 c)
    {
        ImGui.TableNextColumn();
        ImGui.TextColored(c, text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawRightProp(ReadOnlySpan<byte> t1, ReadOnlySpan<byte> t2)
    {
        ImGui.TextUnformatted(t1);
        ImGui.SameLine();
        ImGui.TextUnformatted(t2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawRightPropColor(ReadOnlySpan<byte> t1, ReadOnlySpan<byte> t2, in Color4 color)
    {
        ImGui.TextUnformatted(t1);
        ImGui.SameLine();
        ImGui.TextColored(color, t2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTooltip(ReadOnlySpan<byte> text, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        if (!ImGui.IsItemHovered(flags)) return;
        if (ImGui.BeginTooltip())
        {
            ImGui.TextUnformatted(text);
            ImGui.EndTooltip();
        }
    }

    public static bool DrawSelectable(ReadOnlySpan<byte> str, bool selected, int width, int height,
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