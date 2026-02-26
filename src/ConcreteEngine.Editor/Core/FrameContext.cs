using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Editor.Core;

internal unsafe struct FrameContext(NativeArray<byte> buffer)
{
    public UnsafeSpanWriter Sw = new(buffer.Ptr, buffer.Capacity);
}