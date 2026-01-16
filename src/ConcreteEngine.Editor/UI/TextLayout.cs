using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

public enum TextAlignMode : byte
{
    Default,
    Center,
    Right,
    VerticalCenter,
}

internal struct TextLayout(float rowHeight = 0, TextAlignMode layout = TextAlignMode.Default)
{
    public float RowHeight = rowHeight;
    public TextAlignMode Layout = layout;
    public ImGuiTableRowFlags RowFlags = ImGuiTableRowFlags.None;

    public static TextLayout Make() => new();

    [UnscopedRef]
    public ref TextLayout WithLayout(TextAlignMode layout)
    {
        Layout = layout;
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout TitleWithId(ref SpanWriter sw, ReadOnlySpan<byte> subject, int id)
    {
        ImGui.SeparatorText(sw.Start(subject).Append(" ["u8).Append(id).Append("]"u8).End());
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout RowSpace()
    {
        ImGui.Dummy(new Vector2(0, 2));
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout PropertySeparator()
    {
        ImGui.SameLine();
        ImGui.TextUnformatted("-"u8);
        ImGui.SameLine();
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout DrawProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(value);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout NextColumn(ReadOnlySpan<byte> text)
    {
        ImGui.TableNextColumn();
        ApplyStyle(text);
        ImGui.TextUnformatted(text);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout NextColumnColor(in Color4 color, ReadOnlySpan<byte> text)
    {
        ImGui.TableNextColumn();
        ApplyStyle(text);
        ImGui.TextColored(color, text);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout SelectableColumn(ReadOnlySpan<byte> text, bool selected, float width, out bool result)
    {
        ImGui.TableNextColumn();
        result = DrawGui.DrawSelectable(text, selected, RowHeight, width);
        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyStyle(ReadOnlySpan<byte> text)
    {
        switch (Layout)
        {
            case TextAlignMode.Center:
                GuiLayout.NextCenterAlignText(text, RowHeight);
                break;
            case TextAlignMode.Right:
                GuiLayout.NextRightAlignText(text);
                break;
            case TextAlignMode.VerticalCenter:
                GuiLayout.NextAlignTextVertical(text, RowHeight);
                break;
        }
    }
}