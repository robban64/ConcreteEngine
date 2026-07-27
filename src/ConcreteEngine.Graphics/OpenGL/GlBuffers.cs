using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Gfx;
using Silk.NET.OpenGL;
using static ConcreteEngine.Graphics.OpenGL.GlDriver;

namespace ConcreteEngine.Graphics.OpenGL;

internal static unsafe class GlBuffers
{
    public static NativeHandle CreateVertexBuffer(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData = false)
    {
        return CreateBufferNative(data, in desc, nullData);
    }

    public static NativeHandle CreateIndexBuffer(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData = false)
    {
        return CreateBufferNative(data, in desc, nullData);
    }

    public static NativeHandle CreateUniformBuffer(byte slot, in CreateBufferInfo desc)
    {
        var handle = CreateBufferNative(ReadOnlySpan<byte>.Empty, in desc, nullData: true);
        Gl.BindBufferBase(BufferTargetARB.UniformBuffer, slot, handle);
        return handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetBufferData(NativeHandle uboHandle, ReadOnlySpan<byte> data, int size, BufferUsage usage)
    {
        Gl.NamedBufferData(uboHandle, (nuint)size, data, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UploadBufferData(NativeHandle handle, ReadOnlySpan<byte> data, uint offset, uint size)
    {
        Gl.NamedBufferSubData(handle, (nint)offset, size, data);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UploadBufferData(NativeHandle handle, byte* data, int offset, int size)
    {
        Gl.NamedBufferSubData(handle, offset, (nuint)size, data);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResizeBuffer(NativeHandle handle, int size, BufferUsage usage)

    {
        Gl.NamedBufferData(handle, (nuint)size, (void*)0, usage.ToGlEnum());
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BindUniformBufferRange(NativeHandle uboHandle, uint slot, int offset, int size)
    {
        Gl.BindBufferRange(BufferTargetARB.UniformBuffer, slot, uboHandle, offset, (nuint)size);
    }

    private static NativeHandle CreateBufferNative(ReadOnlySpan<byte> data, in CreateBufferInfo desc, bool nullData)
    {
        var flag = GlEnumUtils.ToBufferFlag(desc.Storage, desc.Access);
        var mask = desc.Storage == BufferStorage.Static ? BufferStorageMask.None : flag;

        Gl.CreateBuffers(1, out uint buffer);

        if (desc.Storage == BufferStorage.Static)
        {
            if (nullData || data.IsEmpty) Gl.NamedBufferStorage(buffer, desc.Size, (void*)0, mask);
            else Gl.NamedBufferStorage(buffer, desc.Size, data, mask);
        }
        else
        {
            var usage = desc.Storage.ToBufferUsage();
            if (nullData || data.IsEmpty) Gl.NamedBufferData(buffer, desc.Size, (void*)0, usage.ToGlEnum());
            else Gl.NamedBufferData(buffer, desc.Size, data, usage.ToGlEnum());
        }

        return new NativeHandle(buffer);
    }
}