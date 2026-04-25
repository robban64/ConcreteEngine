using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Theme;

public unsafe struct UiDrawCursor
{
   
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UiDrawCursor Make(float itemSpacingX = -1f, float lineSpacingY = -1f)
    {
        var itemSpacing = new Vector2(
            itemSpacingX >= 0f ? itemSpacingX : GuiTheme.ItemSpacing.X,
            lineSpacingY >= 0f ? lineSpacingY : GuiTheme.ItemSpacing.Y
        );
        return new UiDrawCursor(ImGui.GetWindowDrawList(), ImGui.GetCursorScreenPos(), itemSpacing);
    }

    public readonly ImDrawList* DrawList;
    public Vector2 Start;
    public Vector2 Cursor;
    public Vector2 ItemSpacing;
    public float LineHeight;
    public float MaxRight;

    public UiDrawCursor(ImDrawListPtr drawList, Vector2 start, Vector2 itemSpacing)
    {
        DrawList = drawList;
        Start = start;
        Cursor = start;
        LineHeight = 0;
        ItemSpacing = itemSpacing;
        MaxRight = start.X;
    }

    public void RestoreCursor()
    {
        var start = Start = ImGui.GetCursorScreenPos();
        Cursor = start;
        LineHeight = 0;
        MaxRight = start.X;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Text(byte* text, uint color = Palette32.TextPrimary)
    {
        DrawList->AddText(Cursor, color, text);
        Advance(ImGui.CalcTextSize(text));
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Text(NativeView<byte> text, uint color = Palette32.TextPrimary)
    {
        DrawList->AddText(Cursor, color, text.Ptr, text.Ptr + text.Length);
        Advance(ImGui.CalcTextSize(text.Ptr, text.Ptr + text.Length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Text(ReadOnlySpan<byte> text, uint color = Palette32.TextPrimary)
    {
        ref var textRef = ref MemoryMarshal.GetReference(text);
        DrawList->AddText(Cursor, color, ref textRef, ref Unsafe.Add(ref textRef, text.Length));
        Advance(ImGui.CalcTextSize(ref textRef));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Sync()
    {
        var localY = Start.Y + LineHeight - ImGui.GetCursorScreenPos().Y + ImGui.GetScrollY();
        ImGui.SetCursorPosY(localY);
        ImGui.Dummy(default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SameLine(float spacing = -1f) => Cursor.X += spacing >= 0 ? spacing : ItemSpacing.X;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NewLine()
    {
        Cursor.X = Start.X;
        Cursor.Y += LineHeight + ItemSpacing.Y;
        Start = Cursor;
        LineHeight = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Spacing(float height = -1f)
    {
        float h = height < 0f ? ItemSpacing.Y : height;
        if (LineHeight < h) LineHeight = h;
        NewLine();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Gap(float width = -1f) => Cursor.X += width < 0f ? ItemSpacing.X : width;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Indent(float amount)
    {
        Start.X += amount;
        Cursor.X = MathF.Max(Cursor.X, Start.X);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Advance(Vector2 size)
    {
        Cursor.X += size.X;
        LineHeight = MathF.Max(LineHeight, size.Y);
        if (Cursor.X > MaxRight) MaxRight = Cursor.X;
    }
}