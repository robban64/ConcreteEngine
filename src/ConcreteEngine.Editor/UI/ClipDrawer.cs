using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Data;
using Hexa.NET.ImGui;

namespace ConcreteEngine.Editor.UI;

internal sealed class ClipDrawer<T>(ClipDrawDel<T> clipDraw)
{
    private readonly ClipDrawDel<T> _clipDraw = clipDraw ?? throw new ArgumentNullException(nameof(clipDraw));

    public void Draw(int count, float height, ReadOnlySpan<T> span, ref FrameContext ctx)
    {
        if (count <= 0) return;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, span.Length);

        var idx = 0;
        var clipper = new ImGuiListClipper();
        clipper.Begin(count, height);
        while (clipper.Step())
        {
            int start = clipper.DisplayStart, length = clipper.DisplayEnd - start;
            var slice = span.Slice(start, length);
            foreach (var it in slice)
                _clipDraw(idx++, it, ref ctx);
        }

        clipper.End();
    }
}