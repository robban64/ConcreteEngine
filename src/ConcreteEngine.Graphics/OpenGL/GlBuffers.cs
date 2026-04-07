using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBuffers : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendResourceStore<GlVboHandle> _vboStore;
    private readonly BackendResourceStore<GlIboHandle> _iboStore;
    private readonly BackendResourceStore<GlUboHandle> _uboStore;

    internal GlBuffers(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _vboStore = ctx.Store.VboStore;
        _iboStore = ctx.Store.IboStore;
        _uboStore = ctx.Store.UboStore;
    }
    public GfxHandle CreateVertexBuffer(ref byte data, in CreateBufferInfo desc,
        bool nullData = false)
    {
        var handle = CreateBufferNative(ref data, in desc, nullData);
        return _vboStore.Add(new GlVboHandle(handle));
    }
    public GfxHandle CreateIndexBuffer(ref byte data, in CreateBufferInfo desc,
        bool nullData = false)
    {
        var handle = CreateBufferNative(ref data, in desc, nullData);
        return _iboStore.Add(new GlIboHandle(handle));
    }

    public GfxHandle CreateVertexBuffer(ReadOnlySpan<byte> data, in CreateBufferInfo desc,
        bool nullData = false)
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _vboStore.Add(new GlVboHandle(handle));
    }

    public GfxHandle CreateIndexBuffer(ReadOnlySpan<byte> data, in CreateBufferInfo desc,
        bool nullData = false)
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _iboStore.Add(new GlIboHandle(handle));
    }

    public GfxHandle CreateUniformBuffer(UboSlot slot, in CreateBufferInfo desc)
    {
        var handle = CreateBufferNative(ReadOnlySpan<byte>.Empty, in desc, nullData: true);
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, slot, handle);
        return _uboStore.Add(new GlUboHandle(handle));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVertexBufferData(GfxHandle vboRef, ReadOnlySpan<byte> data, nint size,
        BufferUsage usage)
    {
        _gl.NamedBufferData(_vboStore.GetHandle(vboRef), (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetIndexBufferData(GfxHandle iboRef, ReadOnlySpan<byte> data, nint size,
        BufferUsage usage)
    {
        _gl.NamedBufferData(_iboStore.GetHandle(iboRef), (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniformBufferData(GfxHandle uboRef, ReadOnlySpan<byte> data, nint size,
        BufferUsage usage)
    {
        _gl.NamedBufferData(_uboStore.GetHandle(uboRef), (nuint)size, data, usage.ToGlEnum());
    }

    public unsafe void ResizeVertexBuffer(GfxHandle vboRef, nint size, BufferUsage usage)

    {
        _gl.NamedBufferData(_vboStore.GetHandle(vboRef), (nuint)size, (void*)0, usage.ToGlEnum());
    }

    public unsafe void ResizeIndexBuffer(GfxHandle iboRef, nint size, BufferUsage usage)

    {
        _gl.NamedBufferData(_iboStore.GetHandle(iboRef), (nuint)size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ResizeUniformBuffer(GfxHandle uboRef, nint size, BufferUsage usage)

    {
        _gl.NamedBufferData(_uboStore.GetHandle(uboRef), (nuint)size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadVertexBufferData(GfxHandle vboRef, ReadOnlySpan<byte> data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(_vboStore.GetHandle(vboRef), offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadIndexBufferData(GfxHandle iboRef, ReadOnlySpan<byte> data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(_iboStore.GetHandle(iboRef), offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadUniformBufferData(GfxHandle uboRef, ref byte data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(_uboStore.GetHandle(uboRef), offset, (nuint)size, ref data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadUniformBufferData(GfxHandle uboRef, ReadOnlySpan<byte> data, nint offset,
        nint size)
    {
        _gl.NamedBufferSubData(_uboStore.GetHandle(uboRef), offset, (nuint)size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange(GfxHandle uboRef, int slot, nint offset, nint size)
    {
        var nHandle = _uboStore.GetHandle(uboRef);
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, (uint)slot, nHandle, offset, (nuint)size);
    }
    private unsafe NativeHandle CreateBufferNative(ref byte data, in CreateBufferInfo desc, bool nullData)
    {
        var flag = GlEnumUtils.ToBufferFlag(desc.Storage, desc.Access);
        var mask = desc.Storage == BufferStorage.Static ? BufferStorageMask.None : flag;

        _gl.CreateBuffers(1, out uint buffer);

        if (desc.Storage == BufferStorage.Static)
        {
            if (nullData) _gl.NamedBufferStorage(buffer, desc.Size, (void*)0, mask);
            else _gl.NamedBufferStorage(buffer, desc.Size, ref data, mask);
        }
        else
        {
            var usage = desc.Storage.ToBufferUsage();
            if (nullData) _gl.NamedBufferData(buffer, desc.Size, (void*)0, usage.ToGlEnum());
            else _gl.NamedBufferData(buffer, desc.Size, ref data, usage.ToGlEnum());
        }

        return new NativeHandle(buffer);
    }
    private unsafe NativeHandle CreateBufferNative(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData)
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