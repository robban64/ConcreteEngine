using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;
using ZaString.Core;

namespace ConcreteEngine.Editor.UI;

internal static class DrawContextExtensions
{
    extension(DrawContext it)
    {
        public DrawContext TextUnformatted(ref ZaUtf8SpanWriter za)
        {
            ImGui.TextUnformatted(za.AsSpan());
            za.Clear();
            return it;
        }


        public DrawContext TextColored(ref ZaUtf8SpanWriter za, in Color4 color)
        {
            ImGui.TextColored(color, za.AsSpan());
            za.Clear();
            return it;
        }

        public DrawContext SeparatorTextId(ref ZaUtf8SpanWriter za, ReadOnlySpan<byte> prop, int id)
        {
            it.SeparatorTextId(prop, id);
            za.Clear();
            return it;
        }

        public DrawContext NextColumnColor(ref ZaUtf8SpanWriter za, in Color4 color)
        {
            it.NextColumnColor(za.AsSpan(), in color);
            za.Clear();
            return it;
        }

        public DrawContext NextColumn(ref ZaUtf8SpanWriter za)
        {
            it.NextColumn(za.AsSpan());
            za.Clear();
            return it;
        }

        public DrawContext DrawRightProp(ref ZaUtf8SpanWriter za, ReadOnlySpan<byte> t1)
        {
            it.DrawRightProp(t1, za.AsSpan());
            za.Clear();
            return it;
        }

        public DrawContext DrawRightPropColor(ref ZaUtf8SpanWriter za, in Color4 color, ReadOnlySpan<byte> t1)
        {
            it.DrawRightPropColor(in color, t1, za.AsSpan());
            za.Clear();
            return it;
        }

        public void DrawTooltip(ref ZaUtf8SpanWriter za, ImGuiHoveredFlags flags = ImGuiHoveredFlags.DelayNormal)
        {
            it.DrawTooltip(za.AsSpan(), flags);
            za.Clear();
        }

        public bool DrawSelectable(ref ZaUtf8SpanWriter za, bool selected, int width, int height,
            ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

            var str = za.AsSpan();
            var textWidth = ImGui.CalcTextSize(str).X;
            var offset = (width - textWidth) * 0.5f;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            bool result = ImGui.Selectable(str, selected, flags, new Vector2(0, height));
            za.Clear();

            ImGui.PopStyleVar();
            return result;
        }
    }
}