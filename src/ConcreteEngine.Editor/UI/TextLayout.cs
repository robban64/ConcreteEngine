using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
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

    public static readonly Vector2 DefaultVSpace = new(0, 2f);

    public static TextLayout Make(float rowHeight = 0, TextAlignMode layout = TextAlignMode.Default) =>
        new(rowHeight, layout);

    [UnscopedRef]
    public ref TextLayout WithLayout(TextAlignMode layout)
    {
        Layout = layout;
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout RowSpace()
    {
        ImGui.Dummy(DefaultVSpace);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout TitleSeparator(ReadOnlySpan<byte> text, bool padUp = true)
    {
        if (padUp) ImGui.Dummy(DefaultVSpace);
        ImGui.SeparatorText(text);
        if (!padUp) ImGui.Dummy(DefaultVSpace);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout SameLineProperty()
    {
        ImGui.SameLine();
        ImGui.TextUnformatted("-"u8);
        ImGui.SameLine();
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout Property(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(value);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout PropertyColor(in Color4 color, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextColored(color, value);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout RowStretch(ReadOnlySpan<byte> text, float width = 0)
    {
        ImGui.TableSetupColumn(text, ImGuiTableColumnFlags.WidthStretch, width);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout Row(ReadOnlySpan<byte> text, float width = 0,
        ImGuiTableColumnFlags flag = ImGuiTableColumnFlags.WidthFixed)
    {
        ImGui.TableSetupColumn(text, flag, width);
        return ref this;
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout Column(ReadOnlySpan<byte> text)
    {
        ImGui.TableNextColumn();
        ApplyStyle(text);
        ImGui.TextUnformatted(text);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout ColumnColor(in Color4 color, ReadOnlySpan<byte> text)
    {
        ImGui.TableNextColumn();
        ApplyStyle(text);
        ImGui.TextColored(color, text);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout SelectableColumn(ReadOnlySpan<byte> text, bool selected, float width, out bool result)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;
        ImGui.TableNextColumn();

        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        var textWidth = ImGui.CalcTextSize(text).X;
        var offset = (width - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        result = ImGui.Selectable(text, selected, flags, new Vector2(0, RowHeight));
        ImGui.PopStyleVar();

        return ref this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyStyle(ReadOnlySpan<byte> text)
    {
        switch (Layout)
        {
            case TextAlignMode.Default: return;
            case TextAlignMode.Center:
                GuiLayout.NextCenterAlignText(text, RowHeight);
                break;
            case TextAlignMode.Right:
                GuiLayout.NextRightAlignText(text);
                break;
            case TextAlignMode.VerticalCenter:
                GuiLayout.NextAlignTextVertical(RowHeight);
                break;
        }
    }
}