#region

using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBuffers : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOps<VertexBufferId, GlVboHandle, VertexBufferMeta, VertexBufferDef> _vboStore;
    private readonly BackendOps<IndexBufferId, GlIboHandle, IndexBufferMeta, IndexBufferDef> _iboStore;
    private readonly BackendOps<UniformBufferId, GlUboHandle, UniformBufferMeta, UniformBufferDef> _uboStore;

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
        _gl.NamedBufferData(GetVboHandle(vboRef).Value, (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetIndexBufferData(GfxRefToken<IndexBufferId> iboRef, ReadOnlySpan<byte> data, nint size,
        BufferUsage usage)
    {
        _gl.NamedBufferData(GetIboHandle(iboRef).Value, (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniformBufferData(GfxRefToken<UniformBufferId> uboRef, ReadOnlySpan<byte> data, nint size,
        BufferUsage usage)
    {
        _gl.NamedBufferData(GetUboHandle(uboRef).Value, (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ResizeVertexBuffer(GfxRefToken<VertexBufferId> vboRef, nint size, BufferUsage usage)

    {
        _gl.NamedBufferData(GetVboHandle(vboRef).Value, (nuint)size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ResizeIndexBuffer(GfxRefToken<IndexBufferId> iboRef, nint size, BufferUsage usage)

    {
        _gl.NamedBufferData(GetIboHandle(iboRef).Value, (nuint)size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ResizeUniformBuffer(GfxRefToken<UniformBufferId> uboRef, nint size, BufferUsage usage)

    {
        _gl.NamedBufferData(GetUboHandle(uboRef).Value, (nuint)size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadVertexBufferData(GfxRefToken<VertexBufferId> vboRef, ReadOnlySpan<byte> data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(GetVboHandle(vboRef).Value, offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadIndexBufferData(GfxRefToken<IndexBufferId> iboRef, ReadOnlySpan<byte> data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(GetIboHandle(iboRef).Value, offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadUniformBufferData(GfxRefToken<UniformBufferId> uboRef, ReadOnlySpan<byte> data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(GetUboHandle(uboRef).Value, offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange(GfxRefToken<UniformBufferId> uboRef, int slot, nint offset, nint size)
    {
        var nHandle = GetUboHandle(uboRef).Value;
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, (uint)slot, nHandle, offset, (nuint)size);
    }

    private unsafe NativeHandle CreateBufferNative(ReadOnlySpan<byte> data, in GfxBufferDataDesc desc, bool nullData)
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
    private GlVboHandle GetVboHandle(GfxRefToken<VertexBufferId> vboRef) => _vboStore.GetHandle(vboRef);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GlIboHandle GetIboHandle(GfxRefToken<IndexBufferId> iboRef) => _iboStore.GetHandle(iboRef);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GlUboHandle GetUboHandle(GfxRefToken<UniformBufferId> uboRef) => _uboStore.GetHandle(uboRef);
}