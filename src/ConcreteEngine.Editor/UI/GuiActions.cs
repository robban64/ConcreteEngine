using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;
using ZaString.Core;

namespace ConcreteEngine.Editor.UI;

internal static class GuiActions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ForVisible(int count, int rowHeight, Action<int> rowDrawer)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(count, rowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, len = clipper.DisplayEnd;
            for (var i = start; i < len; i++)
                rowDrawer(i);
        }

        clipper.End();
    }
}