#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using ConcreteEngine.Graphics.Primitives;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBuffers : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;

    internal GlBuffers(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _store = ctx.Store;
    }

    public GfxRefToken<VertexBufferId> CreateVertexBuffer<T>(ReadOnlySpan<T> data, in GfxBufferDataDesc desc,
        bool nullData = false) where T : unmanaged
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _store.VertexBuffer.Add(new GlVboHandle(handle.Value));
    }

    public GfxRefToken<IndexBufferId> CreateIndexBuffer<T>(ReadOnlySpan<T> data, in GfxBufferDataDesc desc,
        bool nullData = false) where T : unmanaged
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _store.IndexBuffer.Add(new GlIboHandle(handle.Value));
    }

    public GfxRefToken<UniformBufferId> CreateUniformBuffer<T>(UniformGpuSlot slot, in GfxBufferDataDesc desc)
        where T : unmanaged, IUniformGpuData
    {
        var handle = CreateBufferNative(ReadOnlySpan<T>.Empty, in desc, nullData: true);
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)slot, handle.Value);
        return _store.UniformBuffer.Add(new GlUboHandle(handle.Value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBufferDataRaw<TId>(GfxRefToken<TId> refToken, ReadOnlySpan<byte> data, nint size, BufferUsage usage)
        where TId : unmanaged, IResourceId
    {
        _gl.NamedBufferData(ToNativeHandle(refToken.Handle).Value, (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBufferData<TId, T>(GfxRefToken<TId> refToken, ReadOnlySpan<T> data, nint size, BufferUsage usage)
        where TId : unmanaged, IResourceId where T : unmanaged
    {
        _gl.NamedBufferData(ToNativeHandle(refToken.Handle).Value, (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ResizeBuffer<TId>(GfxRefToken<TId> refToken, nint size, BufferUsage usage)
        where TId : unmanaged, IResourceId

    {
        _gl.NamedBufferData(ToNativeHandle(refToken.Handle).Value, (nuint)size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void UploadBufferData<T, TId>(GfxRefToken<TId> refToken, T data, nint offset, nint size)
        where TId : unmanaged, IResourceId where T : unmanaged
    {
        var nHandle = ToNativeHandle(refToken.Handle).Value;
        fixed (T* p = &Unsafe.AsRef(in data))
            _gl.NamedBufferSubData(nHandle, offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadBufferData<T, TId>(GfxRefToken<TId> refToken, ReadOnlySpan<T> data, nint offset, nint size)
        where TId : unmanaged, IResourceId where T : unmanaged
    {
        _gl.NamedBufferSubData(ToNativeHandle(refToken.Handle).Value, offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindBufferRange<TId>(GfxRefToken<TId> refToken, int slot, nint offset, nint size)
        where TId : unmanaged, IResourceId

    {
        var nHandle = ToNativeHandle(refToken.Handle).Value;
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, (uint)slot, nHandle, offset, (nuint)size);
    }

    private unsafe NativeHandle CreateBufferNative<T>(ReadOnlySpan<T> data, in GfxBufferDataDesc desc, bool nullData)
        where T : unmanaged
    {
        var flag = GlEnumUtils.ToBufferFlag(desc.Storage, desc.Access);
        var mask = desc.Storage == BufferStorage.Static ? BufferStorageMask.None : flag;

        _gl.CreateBuffers(1, out uint buffer);

        if (desc.Storage == BufferStorage.Static)
        {
            if (nullData || data.IsEmpty) _gl.NamedBufferStorage(buffer, (nuint)desc.Size, (void*)0, mask);
            else _gl.NamedBufferStorage(buffer, (nuint)desc.Size, data, mask);
        }
        else
        {
            var usage = desc.Storage.ToBufferUsage();
            if (nullData || data.IsEmpty) _gl.NamedBufferData(buffer, (nuint)desc.Size, (void*)0, usage.ToGlEnum());
            else _gl.NamedBufferData(buffer, (nuint)desc.Size, data, usage.ToGlEnum());
        }


        return new NativeHandle(buffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GlVboHandle GetVboHandle(GfxRefToken<VertexBufferId> vboRef) => _store.VertexBuffer.GetRef(vboRef);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GlIboHandle GetIboHandle(GfxRefToken<IndexBufferId> iboRef) => _store.IndexBuffer.GetRef(iboRef);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GlUboHandle GetUboHandle(GfxRefToken<UniformBufferId> uboRef) => _store.UniformBuffer.GetRef(uboRef);


    private NativeHandle ToNativeHandle(GfxHandle handle)
    {
        return handle.Kind switch
        {
            ResourceKind.VertexBuffer => NativeHandle.From(_store.VertexBuffer.Get(handle)),
            ResourceKind.IndexBuffer => NativeHandle.From(_store.IndexBuffer.Get(handle)),
            ResourceKind.UniformBuffer => NativeHandle.From(_store.UniformBuffer.Get(handle)),
            _ => GraphicsException.ThrowInvalidAction<NativeHandle>(nameof(handle.Kind))
        };
    }
}