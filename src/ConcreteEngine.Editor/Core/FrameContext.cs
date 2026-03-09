using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Core;

internal unsafe struct FrameContext(UnsafeSpanWriter sw)
{
    public UnsafeSpanWriter Sw = sw;
}