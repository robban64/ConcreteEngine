using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

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

    public ResourceRefToken<VertexBufferId> CreateVertexBuffer<T>(ReadOnlySpan<T> data, nuint size,
        BufferStorage storage, BufferAccess access, bool nullData = false)
        where T : unmanaged
    {
        var handle = CreateBufferNative(data, size, storage, access, nullData);
        return _store.VertexBuffer.Add(new GlVboHandle(handle.Value));
    }

    public ResourceRefToken<IndexBufferId> CreateIndexBuffer<T>(ReadOnlySpan<T> data, nuint size, BufferStorage storage,
        BufferAccess access, bool nullData = false)
        where T : unmanaged
    {
        var handle = CreateBufferNative(data, size, storage, access, nullData);
        return _store.IndexBuffer.Add(new GlIboHandle(handle.Value));
    }

    public ResourceRefToken<UniformBufferId> CreateUniformBuffer<T>(UniformGpuSlot slot, nuint size,
        BufferStorage storage, BufferAccess access)
        where T : unmanaged, IUniformGpuData
    {
        var handle = CreateBufferNative(ReadOnlySpan<T>.Empty, size, storage, access, nullData: true);
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)slot, handle.Value);
        return _store.UniformBuffer.Add(new GlUboHandle(handle.Value));
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBufferData<T>(in GfxHandle handle, ReadOnlySpan<T> data, nuint size, BufferUsage usage)
        where T : unmanaged
    {
        _gl.NamedBufferData(GetNativeHandle(handle).Value, size, data, usage.ToGlEnum());
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void ResizeBuffer(in GfxHandle handle, nuint size, BufferUsage usage) 
    {
        _gl.NamedBufferData(GetNativeHandle(handle).Value, size, (void*)0, usage.ToGlEnum());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void UploadBufferData<T>(in GfxHandle handle, T data, nuint offset, nuint size)
        where T : unmanaged
    {
        var nHandle = GetNativeHandle(handle).Value;
        fixed (T* p = &Unsafe.AsRef(in data))
            _gl.NamedBufferSubData(nHandle, (nint)offset, size, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadBufferData<T>(in GfxHandle handle, ReadOnlySpan<T> data, nuint offset, nuint size)
        where T : unmanaged
    {
        _gl.NamedBufferSubData(GetNativeHandle(handle).Value, (nint)offset, size, data);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindBufferRange(in GfxHandle handle, uint slot, nuint offset, nuint size)
    {
        var nHandle = GetNativeHandle(handle).Value;
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, slot, nHandle, (nint)offset, size);
    }

    private unsafe NativeHandle CreateBufferNative<T>(ReadOnlySpan<T> data, nuint size, BufferStorage storage,
        BufferAccess access, bool nullData) where T : unmanaged
    {
        var flag = GlEnumUtils.ToBufferFlag(storage, access);
        var mask = storage == BufferStorage.Static ? BufferStorageMask.None : flag;

        _gl.CreateBuffers(1, out uint buffer);

        if (nullData) _gl.NamedBufferStorage(buffer, size, (void*)0, mask);
        else _gl.NamedBufferStorage(buffer, size, data, mask);

        return new NativeHandle(buffer);
    }
    
    private NativeHandle GetNativeHandle(in GfxHandle handle)
    {
        return handle.Kind switch
        {
            ResourceKind.VertexBuffer => NativeHandle.From(_store.VertexBuffer.Get(handle)),
            ResourceKind.IndexBuffer => NativeHandle.From(_store.IndexBuffer.Get(handle)),
            ResourceKind.UniformBuffer => NativeHandle.From(_store.UniformBuffer.Get(handle)),
            _ => GraphicsException.ThrowInvalidAction<NativeHandle>(nameof(handle.Kind))
        };
    }
    

    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadVertexBufferData<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint offset) where T : unmanaged =>
        _gl.NamedBufferSubData(VboHandle(in vbo).Handle, (nint)offset, data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadIndexBufferData<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint offset) where T : unmanaged =>
        _gl.NamedBufferSubData(IboHandle(in ibo).Handle, (nint)offset, data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void UploadUniformBufferData<T>(in GfxHandle ubo, T data, nuint offset, nuint size)
        where T : unmanaged
    {
        fixed (T* p = &Unsafe.AsRef(in data))
        {
            _gl.NamedBufferSubData(UboHandle(in ubo).Handle, (nint)offset, size, data);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange(in GfxHandle ubo, uint slot, nuint offset, nuint size)
    {
        var handle = UboHandle(in ubo).Handle;
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, slot, handle, (nint)offset, size);
    }

    public void SetVertexBufferData<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint size, BufferUsage usage)
        where T : unmanaged
    {
        _gl.NamedBufferStorage(VboHandle(in vbo).Handle, size, data, BufferStorageMask.DynamicStorageBit);
    }

    public void SetIndexBufferData<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint size, BufferUsage usage)
        where T : unmanaged
    {
        _gl.NamedBufferStorage(IboHandle(in ibo).Handle, size, data, BufferStorageMask.DynamicStorageBit);
    }

    public void SetUniformBufferData<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint size, BufferUsage usage,
        bool nullData = false)
        where T : unmanaged
    {
        _gl.NamedBufferData(UboHandle(in ubo).Handle, size, data, BufferStorageMask.DynamicStorageBit);
    }
*/
    /*
     *   public void SetVertexBufferStorage<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint size,
             BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
         {
             _gl.NamedBufferStorage(VboHandle(in vbo).Handle, size, data, BufferStorageMask.DynamicStorageBit);
         }

         public void SetIndexBufferStorage<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint size,
             BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
         {
             _gl.NamedBufferStorage(IboHandle(in ibo).Handle, size, data, BufferStorageMask.DynamicStorageBit);
         }

         public void SetUniformBufferStorage<T>(in GfxHandle ubo, ReadOnlySpan<T> data, nuint size,
             BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
         {
             _gl.NamedBufferStorage(UboHandle(in ubo).Handle, size, data, BufferStorageMask.DynamicStorageBit);
         }
     */

    /*

    public ResourceRefToken<VertexBufferId> CreateVertexBuffer(BufferUsage usage, uint elementSize, uint bindingIndex)
    {
        _gl.CreateBuffers(1, out uint vbo);
        return _store.VertexBuffer.Add(new GlVboHandle(vbo));
    }

    public ResourceRefToken<IndexBufferId> CreateIndexBuffer(BufferUsage usage, uint elementSize)
    {
        _gl.CreateBuffers(1, out uint ibo);
        return _store.IndexBuffer.Add(new GlIboHandle(ibo));
    }

    public ResourceRefToken<UniformBufferId> CreateUniformBuffer(UniformGpuSlot slot, UboDefaultCapacity capacity,
        uint blockSize)
    {
        _gl.CreateBuffers(1, out uint ubo);
        return _store.UniformBuffer.Add(new GlUboHandle(ubo));
    }*/


    /*
         public unsafe void SetUniformBufferSize(UniformGpuSlot slot, nuint capacity) =>
           _gl.BufferData(BufferTargetARB.UniformBuffer, capacity, (void*)0, BufferUsageARB.DynamicDraw);


              public void BindUniformBufferSlot<T>(in GfxHandle ubo, uint bindingIndex)
       {
           _gl.BindBufferBase(BufferTargetARB.UniformBuffer, bindingIndex, UboHandle(in ubo).Handle);
       }
nuint capacity = UniformBufferUtils.GetDefaultCapacity(meta.Stride, defaultCapacity);
var handle = Gl.GenBuffer();
Gl.BindBuffer(BufferTargetARB.UniformBuffer, handle);
Gl.BufferData(BufferTargetARB.UniformBuffer, capacity, (void*)0, BufferUsageARB.DynamicDraw);
Gl.BindBufferBase(BufferTargetARB.UniformBuffer, meta.BindingIdx, handle);
Gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
        return _store.UniformBuffer.Add(new GlUboHandle(0));
*/
}