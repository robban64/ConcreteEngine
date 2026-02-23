using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal static class AppDraw
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawIcon(ref byte icon)
    {
        GuiTheme.PushFontIconMedium();
        ImGui.Text(ref icon);
        ImGui.PopFont();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawTextProperty(ReadOnlySpan<byte> name, ref byte value)
    {
        ImGui.TextUnformatted(name);
        ImGui.SameLine();
        ImGui.TextUnformatted(ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawSameLineProperty()
    {
        ImGui.SameLine();
        ImGui.TextUnformatted("-"u8);
        ImGui.SameLine();
    }
}