#region

using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Graphics.Resources;

internal readonly record struct NativeHandle(uint Value)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty() => Value == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsActive() => Value != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EqualsHandle<THandle>(THandle handle) where THandle : unmanaged, IResourceHandle, IEquatable<THandle> =>
        Value == handle.Handle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeHandle From<THandle>(THandle handle)
        where THandle : unmanaged, IResourceHandle, IEquatable<THandle> =>
        new(handle.Handle);
}

internal interface IResourceHandle
{
    uint Handle { get; }
}

internal readonly record struct GlTextureHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlShaderHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlMeshHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlVboHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlIboHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlFboHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlRboHandle(uint Handle) : IResourceHandle;

internal readonly record struct GlUboHandle(uint Handle) : IResourceHandle;