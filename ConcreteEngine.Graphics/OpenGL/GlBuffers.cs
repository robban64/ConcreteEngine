using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBuffers : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendResourceStore<VertexBufferId, GlVboHandle> _vboStore;
    private readonly BackendResourceStore<IndexBufferId, GlIboHandle> _iboStore;
    private readonly BackendResourceStore<UniformBufferId, GlUboHandle> _uboStore;

    internal GlBuffers(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _vboStore = ctx.Store.VertexBuffer;
        _iboStore = ctx.Store.IndexBuffer;
        _uboStore = ctx.Store.UniformBuffer;
    }

    public GfxRefToken<VertexBufferId> CreateVertexBuffer(ReadOnlySpan<byte> data, in GfxBufferDataDesc desc,
        bool nullData = false)
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _vboStore.Add(new GlVboHandle(handle.Value));
    }

    public GfxRefToken<IndexBufferId> CreateIndexBuffer(ReadOnlySpan<byte> data, in GfxBufferDataDesc desc,
        bool nullData = false)
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _iboStore.Add(new GlIboHandle(handle.Value));
    }

    public GfxRefToken<UniformBufferId> CreateUniformBuffer(UboSlot slot, in GfxBufferDataDesc desc)
    {
        var handle = CreateBufferNative(ReadOnlySpan<byte>.Empty, in desc, nullData: true);
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, slot, handle.Value);
        return _uboStore.Add(new GlUboHandle(handle.Value));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVertexBufferData(GfxRefToken<VertexBufferId> vboRef, ReadOnlySpan<byte> data, nint size,
        BufferUsage usage)
    {
        _gl.NamedBufferData(_vboStore.GetHandle(vboRef), (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetIndexBufferData(GfxRefToken<IndexBufferId> iboRef, ReadOnlySpan<byte> data, nint size,
        BufferUsage usage)
    {
        _gl.NamedBufferData(_iboStore.GetHandle(iboRef), (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniformBufferData(GfxRefToken<UniformBufferId> uboRef, ReadOnlySpan<byte> data, nint size,
        BufferUsage usage)
    {
        _gl.NamedBufferData(_uboStore.GetHandle(uboRef), (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ResizeVertexBuffer(GfxRefToken<VertexBufferId> vboRef, nint size, BufferUsage usage)

    {
        _gl.NamedBufferData(_vboStore.GetHandle(vboRef), (nuint)size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ResizeIndexBuffer(GfxRefToken<IndexBufferId> iboRef, nint size, BufferUsage usage)

    {
        _gl.NamedBufferData(_iboStore.GetHandle(iboRef), (nuint)size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ResizeUniformBuffer(GfxRefToken<UniformBufferId> uboRef, nint size, BufferUsage usage)

    {
        _gl.NamedBufferData(_uboStore.GetHandle(uboRef), (nuint)size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadVertexBufferData(GfxRefToken<VertexBufferId> vboRef, ReadOnlySpan<byte> data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(_vboStore.GetHandle(vboRef), offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadIndexBufferData(GfxRefToken<IndexBufferId> iboRef, ReadOnlySpan<byte> data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(_iboStore.GetHandle(iboRef), offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadUniformBufferData(GfxRefToken<UniformBufferId> uboRef, ReadOnlySpan<byte> data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(_uboStore.GetHandle(uboRef), offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange(GfxRefToken<UniformBufferId> uboRef, int slot, nint offset, nint size)
    {
        var nHandle = _uboStore.GetHandle(uboRef);
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, (uint)slot, nHandle, offset, (nuint)size);
    }

    private unsafe NativeHandle CreateBufferNative(ReadOnlySpan<byte> data, in GfxBufferDataDesc desc, bool nullData)
    {
        var flag = GlEnumUtils.ToBufferFlag(desc.Storage, desc.Access);
        var mask = desc.Storage == BufferStorage.Static ? BufferStorageMask.None : flag;

        _gl.CreateBuffers(1, out uint buffer);

        if (desc.Storage == BufferStorage.Static)
        {
            if (nullData || data.IsEmpty) _gl.NamedBufferStorage(buffer, desc.Size, (void*)0, mask);
            else _gl.NamedBufferStorage(buffer, desc.Size, data, mask);
        }
        else
        {
            var usage = desc.Storage.ToBufferUsage();
            if (nullData || data.IsEmpty) _gl.NamedBufferData(buffer, desc.Size, (void*)0, usage.ToGlEnum());
            else _gl.NamedBufferData(buffer, desc.Size, data, usage.ToGlEnum());
        }

        return new NativeHandle(buffer);
    }
}