using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Gfx;

public readonly record struct NativeHandle
{
    public readonly ulong Value;
    
    public NativeHandle(ulong value)
    {
        Value = value;
    }
    public NativeHandle(uint value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(NativeHandle handle) => (uint)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value != 0;

}

public readonly record struct NativeHandle<T> where T : IResourceMeta
{
    public readonly ulong Value;

    public NativeHandle(ulong value)
    {
        Value = value;
    }
    public NativeHandle(uint value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(NativeHandle<T> handle) => (uint)handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeHandle(NativeHandle<T> handle) => new(handle.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValid() => Value != 0;
}