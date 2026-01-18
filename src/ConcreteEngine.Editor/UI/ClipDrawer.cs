using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed class ClipDrawer(DrawIterationDel draw)
{
    private readonly DrawIterationDel _draw = draw ?? throw new ArgumentNullException(nameof(draw));

    public void Draw(int count, int height, ref FrameContext ctx)
    {
        if (count == 0) return;
        var clipper = new ImGuiListClipper();
        clipper.Begin(count, height);
        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                _draw(i, ref ctx);
        }
        clipper.End();
    }
}

internal sealed class ClipDrawer<TArgs>(DrawIterationDel<TArgs> draw)
{
    private readonly DrawIterationDel<TArgs> _draw = draw ?? throw new ArgumentNullException(nameof(draw));

    public void Draw(int count, float height, TArgs args, ref FrameContext ctx)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(count, height);
        while (clipper.Step())
        {
            for (int i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                _draw(i, args, ref ctx);
        }

        clipper.End();
    }
}
