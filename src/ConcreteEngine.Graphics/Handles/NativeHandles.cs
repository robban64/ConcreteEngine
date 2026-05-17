using System.Runtime.CompilerServices;

namespace ConcreteEngine.Graphics.Handles;

internal interface IGraphicsHandle
{
    uint Value { get; }
}

internal readonly record struct GlHandle(uint Value) : IGraphicsHandle
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(GlHandle handle) => handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator GlHandle(BkHandle handle) => new(handle.Handle);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeHandle(GlHandle handle) => new(handle.Value);
}

public readonly record struct NativeHandle(nint Value)
{
    public NativeHandle(uint value) : this((nint)value) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator nint(NativeHandle handle) => handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(NativeHandle handle) => (uint)handle.Value;
}