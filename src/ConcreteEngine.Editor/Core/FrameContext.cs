using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Utils;

namespace ConcreteEngine.Editor.Core;

internal unsafe struct FrameContext(UnsafeSpanWriter sw)
{
    public UnsafeSpanWriter Sw = sw;
}