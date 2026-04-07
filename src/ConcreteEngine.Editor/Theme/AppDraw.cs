using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Theme;

internal static unsafe class AppDraw
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(NativeViewPtr<byte> text) => ImGui.TextUnformatted(text, text + text.Length);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawIcon(byte* icon)
    {
        GuiTheme.PushFontIconMedium();
        ImGui.Text(icon);
        ImGui.PopFont();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Column(byte* text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ColumnV(byte* text, float fontSize = GuiTheme.FontSizeDefault)
    {
        ImGui.TableNextColumn();
        var top = ImGui.GetCursorPosY();
        GuiLayout.NextAlignTextVertical(top, fontSize);
        ImGui.TextUnformatted(text);
        return top;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ColumnVTop(byte* text, float top, float rowHeight)
    {
        ImGui.TableNextColumn();
        GuiLayout.NextAlignTextVerticalTop(top, rowHeight);
        ImGui.TextUnformatted(text);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextProperty(ReadOnlySpan<byte> name, byte* value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawSameLineProperty()
    {
        ImGui.SameLine();
        ImGui.TextUnformatted("-"u8);
        ImGui.SameLine();
    }
}