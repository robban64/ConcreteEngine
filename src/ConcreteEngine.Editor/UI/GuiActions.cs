using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;
using ZaString.Core;

namespace ConcreteEngine.Editor.UI;

internal sealed class ClipDrawer(DrawIterationDel draw)
{
    private readonly DrawIterationDel _draw = draw ?? throw new ArgumentNullException(nameof(draw));

    public void Draw(int count, int height, ref SpanWriter writer)
    {
        if (count == 0) return;

        var clipper = new ImGuiListClipper();
        clipper.Begin(count, height);
        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                _draw(i, ref writer);
        }

        clipper.End();
    }
}

internal sealed class ClipDrawer<TArgs>(DrawIterationDel<TArgs> draw)
{
    private readonly DrawIterationDel<TArgs> _draw = draw ?? throw new ArgumentNullException(nameof(draw));

    public void Draw(int count, int height, TArgs args, ref SpanWriter writer)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(count, height);
        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                _draw(i, args, ref writer);
        }

        clipper.End();
    }
}

internal static class GuiActions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ForVisible(int count, int rowHeight, ref SpanWriter writer, Action<int, SpanWriter> rowDrawer)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(count, rowHeight);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, len = clipper.DisplayEnd;
            for (var i = start; i < len; i++)
                rowDrawer(i, writer);
        }

        clipper.End();
    }
}