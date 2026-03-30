using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Data;

internal struct FrameContext(UnsafeSpanWriter sw)
{
    public UnsafeSpanWriter Sw = sw;
}