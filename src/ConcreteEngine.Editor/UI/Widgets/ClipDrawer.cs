using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI.Widgets;

internal sealed class ClipDrawer(ClipDrawDel clipDraw)
{
    private readonly ClipDrawDel _clipDraw = clipDraw ?? throw new ArgumentNullException(nameof(clipDraw));

    public void Draw(int count, float height, FrameContext ctx)
    {
        if (count <= 0) return;

        var clipper = new ImGuiListClipper();
        clipper.Begin(count, height);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, end = clipper.DisplayEnd;
            for (var i = start; i < end; i++)
                _clipDraw(i,  ctx);
        }

        clipper.End();
    }
}

internal sealed class ClipDrawer<T>(ClipDrawDel<T> clipDraw)
{
    private readonly ClipDrawDel<T> _clipDraw = clipDraw ?? throw new ArgumentNullException(nameof(clipDraw));

    public void Draw(int count, float height, ReadOnlySpan<T> span, FrameContext ctx)
    {
        if (count <= 0) return;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, span.Length);

        var clipper = new ImGuiListClipper();
        clipper.Begin(count, height);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - start;
            var idx = start;
            var slice = span.Slice(start, length);
            foreach (var it in slice)
                _clipDraw(idx++, it,  ctx);
        }

        clipper.End();
    }
}