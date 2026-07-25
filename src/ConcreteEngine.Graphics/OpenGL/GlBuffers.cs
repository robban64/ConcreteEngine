using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed unsafe class GlBuffers
{
    private readonly GL _gl = GlBackendDriver.Gl;

    public GfxHandle CreateVertexBuffer(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData = false)
    {
        return CreateBufferNative(data, in desc, nullData);
    }

    public GfxHandle CreateIndexBuffer(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData = false)
    {
        return CreateBufferNative(data, in desc, nullData);
    }

    public GfxHandle CreateUniformBuffer(UboSlot slot, in CreateBufferInfo desc)
    {
        var handle = CreateBufferNative(ReadOnlySpan<byte>.Empty, in desc, nullData: true);
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, slot, handle);
        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBufferData(GfxHandle uboHandle, ReadOnlySpan<byte> data, int size, BufferUsage usage)
    {
        _gl.NamedBufferData(uboHandle, (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadBufferData(GfxHandle handle, ReadOnlySpan<byte> data, uint offset, uint size)
    {
        _gl.NamedBufferSubData(handle, (nint)offset, size, data);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadBufferData(GfxHandle handle, byte* data, int offset, int size)
    {
        _gl.NamedBufferSubData(handle, offset, (nuint)size, data);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResizeBuffer(GfxHandle handle, int size, BufferUsage usage)

    {
        _gl.NamedBufferData(handle, (nuint)size, (void*)0, usage.ToGlEnum());
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange(GfxHandle uboHandle, uint slot, int offset, int size)
    {
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, slot, uboHandle, offset, (nuint)size);
    }

    private GfxHandle CreateBufferNative(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData)
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

        return new GfxHandle(buffer);
    }
}