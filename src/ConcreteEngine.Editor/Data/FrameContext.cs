using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Data;

internal readonly unsafe ref struct FrameContext(byte* buffer, int length)
{
    public readonly byte* Buffer = buffer;
    public readonly int Length = length;
    public UnsafeSpanWriter Sw => new (Buffer, Length);
}
