using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal unsafe struct FrameContext(NativeArray<byte> buffer)
{
    public UnsafeSpanWriter Sw = new(buffer.Ptr, buffer.Capacity);
}