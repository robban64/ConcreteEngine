using System.Runtime.CompilerServices;
using ConcreteEngine.Editor.Data;
using ConcreteEngine.Editor.Utils;
using Hexa.NET.ImGui;

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

    public void Draw(int count, float height, TArgs args, ref SpanWriter writer)
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
