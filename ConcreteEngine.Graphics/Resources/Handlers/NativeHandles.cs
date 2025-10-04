#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal readonly record struct NativeHandle(uint Value)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty() => Value == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EqualsHandle<THandle>(THandle handle) where THandle : unmanaged, IResourceHandle, IEquatable<THandle> =>
        Value == handle.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeHandle From<THandle>(THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle> =>
        new(handle.Value);
}

internal interface IResourceHandle
{
    uint Value { get; }
}

internal readonly record struct GlTextureHandle(uint Value) : IResourceHandle;

internal readonly record struct GlShaderHandle(uint Value) : IResourceHandle;

internal readonly record struct GlMeshHandle(uint Value) : IResourceHandle;

internal readonly record struct GlVboHandle(uint Value) : IResourceHandle;

internal readonly record struct GlIboHandle(uint Value) : IResourceHandle;

internal readonly record struct GlFboHandle(uint Value) : IResourceHandle;

internal readonly record struct GlRboHandle(uint Value) : IResourceHandle;

internal readonly record struct GlUboHandle(uint Value) : IResourceHandle;