using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Theme;

struct UiCursor
{
    //private static readonly uint DefaultColor = Palette.TextPrimary.ToPackedRgba();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiCursor Make(float spacingSize = 4f)
    {
        var screenPos = ImGui.GetCursorScreenPos();
        var lineHeight = ImGui.GetTextLineHeight();
        var textSize = ImGui.CalcTextSize("A"u8).X;
        return new UiCursor(ImGuiSystem.GetDrawList(), screenPos, lineHeight, textSize, spacingSize);
    }

    public ImDrawListPtr DrawList;
    public Vector2 Start;
    public Vector2 Pos;
    public float LineHeight;
    public float CharWidth;
    public float SpacingSize;

    public UiCursor(ImDrawListPtr drawList, Vector2 start, float lineHeight, float charWidth, float spacingSize)
    {
        DrawList = drawList;
        Start = start;
        Pos = start;
        LineHeight = lineHeight;
        SpacingSize = spacingSize;
        CharWidth = charWidth;
    }

    public UiCursor(float lineHeight, float charWidth)
    {
        LineHeight = lineHeight;
        CharWidth = charWidth;
    }

    public void TextColor(ref byte text, in Color4 color) => DrawList.AddText(Pos, color.ToPackedRgba(), ref text);
    public void Text(ref byte text) => DrawList.AddText(Pos, Color4.White.ToPackedRgba(), ref text);
    public void Text(ReadOnlySpan<byte> text) => DrawList.AddText(Pos, Color4.White.ToPackedRgba(), text);

    public void Spacing(float amount) => Pos.Y += amount * CharWidth;

    public void SameLine(float spacing = -1f) => Pos.X += (spacing >= 0 ? spacing : SpacingSize) ;

    public void NewLine()
    {
        Pos.X = Start.X;
        Pos.Y += LineHeight + SpacingSize;
        LineHeight = 0;
    }

    public void Advance(Vector2 size)
    {
        Pos.X += size.X + SpacingSize;
        LineHeight = MathF.Max(LineHeight, size.Y);
    }
}