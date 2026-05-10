using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed unsafe class GlBuffers 
{
    private readonly GL _gl;
    private readonly BackendResourceStore<GlHandle> _vboStore;
    private readonly BackendResourceStore<GlHandle> _iboStore;
    private readonly BackendResourceStore<GlHandle> _uboStore;

    internal GlBuffers(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _vboStore = ctx.Store.VboStore;
        _iboStore = ctx.Store.IboStore;
        _uboStore = ctx.Store.UboStore;
    }

    public GfxHandle CreateVertexBuffer(byte* data, in CreateBufferInfo desc, bool nullData = false)
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _vboStore.Add(new GlHandle(handle));
    }

    public GfxHandle CreateIndexBuffer(byte* data, in CreateBufferInfo desc, bool nullData = false)
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _iboStore.Add(new GlHandle(handle));
    }

    public GfxHandle CreateVertexBuffer(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData = false)
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _vboStore.Add(new GlHandle(handle));
    }

    public GfxHandle CreateIndexBuffer(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData = false)
    {
        var handle = CreateBufferNative(data, in desc, nullData);
        return _iboStore.Add(new GlHandle(handle));
    }

    public GfxHandle CreateUniformBuffer(UboSlot slot, in CreateBufferInfo desc)
    {
        var handle = CreateBufferNative(ReadOnlySpan<byte>.Empty, in desc, nullData: true);
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, slot, handle);
        return _uboStore.Add(new GlHandle(handle));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVertexBufferData(GfxHandle vboRef, ReadOnlySpan<byte> data, uint size, BufferUsage usage)
    {
        _gl.NamedBufferData(_vboStore.GetHandle(vboRef), size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetIndexBufferData(GfxHandle iboRef, ReadOnlySpan<byte> data, uint size, BufferUsage usage)
    {
        _gl.NamedBufferData(_iboStore.GetHandle(iboRef), size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUniformBufferData(GfxHandle uboRef, ReadOnlySpan<byte> data, uint size, BufferUsage usage)
    {
        _gl.NamedBufferData(_uboStore.GetHandle(uboRef), size, data, usage.ToGlEnum());
    }

    public void ResizeVertexBuffer(GfxHandle vboRef, uint size, BufferUsage usage)

    {
        _gl.NamedBufferData(_vboStore.GetHandle(vboRef), size, (void*)0, usage.ToGlEnum());
    }

    public void ResizeIndexBuffer(GfxHandle iboRef, uint size, BufferUsage usage)

    {
        _gl.NamedBufferData(_iboStore.GetHandle(iboRef), size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResizeUniformBuffer(GfxHandle uboRef, uint size, BufferUsage usage)

    {
        _gl.NamedBufferData(_uboStore.GetHandle(uboRef), size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadVertexBufferData(GfxHandle vboRef, ReadOnlySpan<byte> data, uint offset, uint size)
    {
        _gl.NamedBufferSubData(_vboStore.GetHandle(vboRef), (nint)offset, size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadIndexBufferData(GfxHandle iboRef, ReadOnlySpan<byte> data, uint offset, uint size)
    {
        _gl.NamedBufferSubData(_iboStore.GetHandle(iboRef), (nint)offset, size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadUniformBufferData(GfxHandle uboRef, byte* data, uint offset, uint size)
    {
        _gl.NamedBufferSubData(_uboStore.GetHandle(uboRef), (nint)offset, size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange(GfxHandle uboRef, uint slot, uint offset, uint size)
    {
        var nHandle = _uboStore.GetHandle(uboRef);
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, slot, nHandle, (nint)offset, size);
    }

    private NativeHandle CreateBufferNative(byte* data, in CreateBufferInfo desc, bool nullData)
    {
        var flag = GlEnumUtils.ToBufferFlag(desc.Storage, desc.Access);
        var mask = desc.Storage == BufferStorage.Static ? BufferStorageMask.None : flag;

        _gl.CreateBuffers(1, out uint buffer);

        if (desc.Storage == BufferStorage.Static)
        {
            if (nullData) _gl.NamedBufferStorage(buffer, desc.Size, (void*)0, mask);
            else _gl.NamedBufferStorage(buffer, desc.Size, data, mask);
        }
        else
        {
            var usage = desc.Storage.ToBufferUsage();
            if (nullData) _gl.NamedBufferData(buffer, desc.Size, (void*)0, usage.ToGlEnum());
            else _gl.NamedBufferData(buffer, desc.Size, data, usage.ToGlEnum());
        }

        return new NativeHandle(buffer);
    }

    private NativeHandle CreateBufferNative(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData)
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