using System.Runtime.CompilerServices;
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

    private GlVboHandle VboHandle(in GfxHandle handle) => _store.VertexBuffer.Get(in handle);
    private GlIboHandle IboHandle(in GfxHandle handle) => _store.IndexBuffer.Get(in handle);
    private GlUboHandle UboHandle(in GfxHandle handle) => _store.UniformBuffer.Get(in handle);

    private NativeHandle CreateBufferNative<T>
        (ReadOnlySpan<T> data, nuint size, BufferStorage storage, BufferAccess access) where T : unmanaged
    {
        var flag = GlEnumUtils.ToBufferFlag(storage, access);
        _gl.CreateBuffers(1, out uint buffer);
        if (storage == BufferStorage.Static)
            _gl.NamedBufferStorage(buffer, size, data, 0u);
        else
            _gl.NamedBufferStorage(buffer, size, data, flag);


        return new NativeHandle(buffer);
    }


    public ResourceRefToken<VertexBufferId> CreateVertexBuffer<T>(ReadOnlySpan<T> data, nuint size, BufferStorage storage, BufferAccess access)
        where T : unmanaged
    {
        var handle = CreateBufferNative(data, size, storage, access);
        return _store.VertexBuffer.Add(new GlVboHandle(handle.Value));
    }

    public ResourceRefToken<IndexBufferId> CreateIndexBuffer<T>( ReadOnlySpan<T> data, nuint size,BufferStorage storage, BufferAccess access)
        where T : unmanaged
    {
        var handle = CreateBufferNative(data, size, storage, access);
        return _store.IndexBuffer.Add(new GlIboHandle(handle.Value));
    }

    public ResourceRefToken<UniformBufferId> CreateUniformBuffer<T>(UniformGpuSlot slot, 
        ReadOnlySpan<T> data, nuint size, BufferStorage storage, BufferAccess access)
        where T : unmanaged, IUniformGpuData
    {
        var handle = CreateBufferNative(data, size, storage, access);
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)slot, handle.Value);
        return _store.UniformBuffer.Add(new GlUboHandle(handle.Value));
    }
    

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadVertexBufferData<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint offset) where T : unmanaged
    {
        _gl.NamedBufferSubData(VboHandle(in vbo).Handle, (nint)offset, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadIndexBufferData<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint offset) where T : unmanaged
    {
        _gl.NamedBufferSubData(IboHandle(in ibo).Handle, (nint)offset, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadUniformBufferData<T>(in GfxHandle ubo, ReadOnlySpan<T> data, nuint offset) where T : unmanaged
    {
        _gl.NamedBufferSubData(UboHandle(in ubo).Handle, (nint)offset, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void BindUniformBufferRange(in GfxHandle ubo, uint slot, nuint offset, nuint size)
    {
        var handle = UboHandle(in ubo).Handle;
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, slot, handle, (nint)offset, size);
    }
    
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