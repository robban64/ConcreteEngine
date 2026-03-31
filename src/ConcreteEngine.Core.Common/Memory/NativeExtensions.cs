using ConcreteEngine.Core.Common.Text;

namespace ConcreteEngine.Core.Common.Memory;

public static unsafe class NativeExtensions
{
    public static UnsafeSpanWriter Writer(this NativeViewPtr<byte> viewPtr) => new(viewPtr.Ptr, viewPtr.Length);
}
