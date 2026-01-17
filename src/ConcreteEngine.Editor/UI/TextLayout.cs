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

    private static readonly Vector2 VSpace = new(0, 2f);

    public static TextLayout Make() => new();

    [UnscopedRef]
    public ref TextLayout WithLayout(TextAlignMode layout)
    {
        Layout = layout;
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout RowSpace()
    {
        ImGui.Dummy(VSpace);
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout TitleSeparator(ReadOnlySpan<byte> text, Vector2? space = null)
    {
        ImGui.Dummy(space ?? VSpace);
        ImGui.SeparatorText(text);
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
    public ref TextLayout Row(ReadOnlySpan<byte> text, float width = 0)
    {
        ImGui.TableSetupColumn(text, ImGuiTableColumnFlags.WidthFixed, width);
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
            case TextAlignMode.Default: return;
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