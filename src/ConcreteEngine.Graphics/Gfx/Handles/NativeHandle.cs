using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Gfx;

public readonly record struct NativeHandle(ulong Value)
{
    public NativeHandle(uint value) : this((ulong)value) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(NativeHandle handle) => (uint)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value != 0;
}
public readonly record struct NativeHandle<T>(ulong Value) where T : IResourceMeta
{
    public NativeHandle(uint value) : this((ulong)value) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(NativeHandle<T> handle) => (uint)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeHandle(NativeHandle<T> handle) => new (handle.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value != 0;
}