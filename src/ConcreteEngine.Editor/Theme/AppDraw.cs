using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.Theme;

internal static unsafe class AppDraw
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Text(NativeView<byte> text) => ImGui.TextUnformatted(text, text + text.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextColumn(NativeView<byte> text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text, text + text.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ColumnV(NativeView<byte> text, float fontSize = GuiTheme.FontSizeDefault)
    {
        ImGui.TableNextColumn();
        var top = ImGui.GetCursorPosY();
        GuiLayout.NextAlignTextVertical(top, fontSize);
        ImGui.TextUnformatted(text, text + text.Length);
        return top;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextProperty(ReadOnlySpan<byte> name, NativeView<byte> text)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(text, text + text.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextProperty(ReadOnlySpan<byte> name, ReadOnlySpan<byte> text)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(text);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawSameLineProperty(char separator = '-')
    {
        ImGui.SameLine();
        ImGui.TextUnformatted((byte*)&separator);
        ImGui.SameLine();
    }
}