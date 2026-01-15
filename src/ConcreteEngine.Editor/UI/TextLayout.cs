using System.Diagnostics.CodeAnalysis;
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


    [UnscopedRef]
    public ref TextLayout WithLayout(TextAlignMode layout)
    {
        Layout = layout;
        return ref this;
    }

    [UnscopedRef]
    public ref TextLayout DrawPropertySeparator()
    {
        ImGui.SameLine();
        ImGui.TextUnformatted("-"u8);
        ImGui.SameLine();
        return ref this;
    }
    
    [UnscopedRef]
    public ref TextLayout DrawProperty(ReadOnlySpan<byte> name,  ReadOnlySpan<byte> value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(value);
        return ref this;
    }


    [UnscopedRef]
    public ref TextLayout DrawColumn(ReadOnlySpan<byte> text)
    {
        ImGui.TableNextColumn();
        ApplyStyle(text);
        ImGui.TextUnformatted(text);
        return ref this;
    }
    
    [UnscopedRef]
    public ref TextLayout DrawColumnColor(ReadOnlySpan<byte> text, in Color4 color)
    {
        ImGui.TableNextColumn();
        ApplyStyle(text);
        ImGui.TextColored(color, text);
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