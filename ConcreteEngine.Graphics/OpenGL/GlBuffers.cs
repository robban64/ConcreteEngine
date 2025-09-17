using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlBuffers
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
    public ResourceRefToken<UniformBufferId> CreateUniformBuffer(UniformGpuSlot slot, UboDefaultCapacity capacity, uint blockSize)
    {
        _gl.CreateBuffers(1, out uint ubo);
        return _store.UniformBuffer.Add(new GlUboHandle(ubo));

        /*
        nuint capacity = UniformBufferUtils.GetDefaultCapacity(meta.Stride, defaultCapacity);
        var handle = Gl.GenBuffer();
        Gl.BindBuffer(BufferTargetARB.UniformBuffer, handle);
        Gl.BufferData(BufferTargetARB.UniformBuffer, capacity, (void*)0, BufferUsageARB.DynamicDraw);
        Gl.BindBufferBase(BufferTargetARB.UniformBuffer, meta.BindingIdx, handle);
        Gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
*/
        return _store.UniformBuffer.Add(new GlUboHandle(0));
    }
    
    public void SetVertexBufferStorage<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint size,
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
    
    //
    public void UploadVertexBufferData<T>(in GfxHandle vbo, ReadOnlySpan<T> data, nuint offset,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
    {
        _gl.NamedBufferSubData(VboHandle(in vbo).Handle, (nint)offset, data);                                    
    }

    public void UploadIndexBufferData<T>(in GfxHandle ibo, ReadOnlySpan<T> data, nuint offset,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
    {
        _gl.NamedBufferSubData(IboHandle(in ibo).Handle, (nint)offset, data);                                    
    }

    public void UploadUniformBufferData<T>(in GfxHandle ubo, ReadOnlySpan<T> data, nuint offset,
        BufferUsage usage = BufferUsage.StaticDraw) where T : unmanaged
    {
        _gl.NamedBufferSubData(UboHandle(in ubo).Handle, (nint)offset, data);                                    
    }
    
    public void BindUniformBufferSlot<T>(in GfxHandle ubo,uint bindingIndex)
    {
        _gl.BindBufferBase(BufferTargetARB.UniformBuffer, bindingIndex, UboHandle(in ubo).Handle);                                     
    }
    
    public void BindUniformBufferRange(in GfxHandle ubo, uint slot, nuint offset, nuint size)
    {
        var handle = UboHandle(in ubo).Handle;
        _gl.BindBufferRange(BufferTargetARB.UniformBuffer, slot, handle, (nint)offset, size);
    }

}