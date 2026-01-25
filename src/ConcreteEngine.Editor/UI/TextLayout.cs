using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    public ref TextLayout TitleSeparator(ref byte text, bool padUp = true, bool padDown = false)
    {
        if (padUp) ImGui.Dummy(DefaultVSpace);
        ImGui.SeparatorText(ref text);
        if (padDown) ImGui.Dummy(DefaultVSpace);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout TitleSeparator(ReadOnlySpan<byte> text, bool padUp = true, bool padDown = false)
    {
        return ref TitleSeparator(ref MemoryMarshal.GetReference(text), padUp, padDown);
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
    public ref TextLayout Property(ReadOnlySpan<byte> name, ref byte value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(ref value);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout Property(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        return ref Property(name, ref MemoryMarshal.GetReference(value));
    }


    [UnscopedRef]
    public ref TextLayout PropertyColor(in Color4 color, ReadOnlySpan<byte> name, ref byte value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextColored(color, ref value);
        return ref this;
    }


    [UnscopedRef]
    public ref TextLayout PropertyColor(in Color4 color, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        return ref PropertyColor(in color, name, ref MemoryMarshal.GetReference(value));
    }


    [UnscopedRef]
    public ref TextLayout RowStretch(ref byte text, float width = 0)
    {
        ImGui.TableSetupColumn(ref text, ImGuiTableColumnFlags.WidthStretch, width);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout RowStretch(ReadOnlySpan<byte> text, float width = 0)
    {
        ImGui.TableSetupColumn(text, ImGuiTableColumnFlags.WidthStretch, width);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout Row(ref byte text, float width = 0,
        ImGuiTableColumnFlags flag = ImGuiTableColumnFlags.WidthFixed)
    {
        ImGui.TableSetupColumn(ref text, flag, width);
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
    public ref TextLayout Column(ref byte text)
    {
        ImGui.TableNextColumn();
        ApplyStyle(ref text);
        ImGui.TextUnformatted(ref text);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout Column(ReadOnlySpan<byte> text)
    {
        return ref Column(ref MemoryMarshal.GetReference(text));
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout ColumnColor(in Color4 color, ref byte text)
    {
        ImGui.TableNextColumn();
        ApplyStyle(ref text);
        ImGui.TextColored(color, ref text);
        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout ColumnColor(in Color4 color, ReadOnlySpan<byte> text)
    {
        return ref ColumnColor(in color, ref MemoryMarshal.GetReference(text));
    }


    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout SelectableColumn(ref byte text, bool selected, float width, out bool result)
    {
        const ImGuiSelectableFlags flags = ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowDoubleClick;
        ImGui.TableNextColumn();

        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.0f, 0.5f));

        var textWidth = ImGui.CalcTextSize(ref text).X;
        var offset = (width - textWidth) * 0.5f;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        result = ImGui.Selectable(ref text, selected, flags, new Vector2(0, RowHeight));
        ImGui.PopStyleVar();

        return ref this;
    }

    [UnscopedRef, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TextLayout SelectableColumn(ReadOnlySpan<byte> text, bool selected, float width, out bool result)
    {
        return ref SelectableColumn(ref MemoryMarshal.GetReference(text), selected, width, out result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ApplyStyle(ref byte text)
    {
        switch (Layout)
        {
            case TextAlignMode.Default: return;
            case TextAlignMode.Center:
                GuiLayout.NextCenterAlignText(ref text, RowHeight);
                break;
            case TextAlignMode.Right:
                GuiLayout.NextRightAlignText(ref text);
                break;
            case TextAlignMode.VerticalCenter:
                GuiLayout.NextAlignTextVertical(RowHeight);
                break;
        }
    }
}