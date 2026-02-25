using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static unsafe class AppDraw
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawIcon(byte* icon)
    {
        GuiTheme.PushFontIconMedium();
        ImGui.Text(icon);
        ImGui.PopFont();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextProperty(ReadOnlySpan<byte> name,  byte* value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextProperty(ReadOnlySpan<byte> name, ref byte value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(ref value);
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