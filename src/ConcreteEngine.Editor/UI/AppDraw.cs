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
}