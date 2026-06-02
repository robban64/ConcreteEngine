using System.Runtime.InteropServices;

namespace ConcreteEngine.Engine.Assets.Utils;

internal readonly unsafe ref struct ScopedNativeMemory : IDisposable
{
    public readonly byte* Ptr;
    public readonly int Length;

    public ScopedNativeMemory(byte* ptr, int length)
    {
        ArgumentNullException.ThrowIfNull(ptr);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        Ptr = ptr;
        Length = length;
    }

    public Span<byte> AsSpan() => new(Ptr, Length);

    public void Dispose()
    {
        if(Ptr == null) throw new InvalidOperationException("Pointer is null");
        NativeMemory.Free(Ptr);
    }
}