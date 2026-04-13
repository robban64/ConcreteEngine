using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using ConcreteEngine.Editor.Theme;

namespace ConcreteEngine.Editor.Data;

internal struct FrameContext(UnsafeSpanWriter sw)
{
    public UnsafeSpanWriter Sw = sw;
}

internal readonly ref struct WindowContext(ref UiDrawCursor draw)
{
    public readonly ref UiDrawCursor Draw = ref draw;
    public UnsafeSpanWriter Sw => TextBuffers.GetWriter();
}