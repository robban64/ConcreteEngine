using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Handles;

public readonly record struct NativeHandle(ulong Value)
{
    public NativeHandle(uint value) : this((ulong)value) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(NativeHandle handle) => (uint)handle.Value;

    public bool IsValid() => Value != 0;
}