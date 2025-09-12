using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Utils;

namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceAllocator
{
    MeshId CreateMesh(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement, out MeshMeta meta);

    VertexBufferId CreateVertexBuffer(BufferUsage usage, uint elementSize, uint index,
        out VertexBufferMeta meta);

    IndexBufferId CreateIndexBuffer(BufferUsage usage, uint elementSize, out IndexBufferMeta meta);

    TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta);
    TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta);

    FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta);
    ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta);

    UniformBufferId CreateUniformBuffer<T>(UniformGpuSlot slot, UboDefaultCapacity defaultCapacity,
        out UniformBufferMeta meta)
        where T : unmanaged, IUniformGpuData;
}

internal sealed class GfxResourceAllocator : IGfxResourceAllocator
{
    private readonly IGraphicsDriver _driver;
    private readonly GfxResourceManager _stores;
    private readonly GfxResourceRegistry _registry;
    

    public GfxResourceAllocator(
        IGraphicsDriver driver,
        GfxResourceManager stores,
        GfxResourceRegistry registry)
    {
        _stores = stores;
        _driver = driver;
        _registry = registry;
    }

    public MeshId CreateMesh(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement,
        out MeshMeta meta)
    {
        var handle = _driver.CreateVertexArray(primitive, drawKind, drawElement, out  meta);
        var meshId = _stores.MeshStore.Add(in meta, handle);
        _registry.MeshRegistry.RegisterEmptyMesh(meshId);
        return meshId;
    }

    public VertexBufferId CreateVertexBuffer(BufferUsage usage, uint elementSize, uint index, out VertexBufferMeta meta)
    {
        var handle = _driver.CreateVertexBuffer(usage, elementSize, index, out meta);
        var vboId = _stores.VboStore.Add(new VertexBufferMeta(usage, index, 0, elementSize), handle);
        return vboId;
    }

    public IndexBufferId CreateIndexBuffer(BufferUsage usage, uint elementSize, out IndexBufferMeta meta)
    {
        var handle = _driver.CreateIndexBuffer(usage, elementSize, out meta);
        return _stores.IboStore.Add(new IndexBufferMeta(usage, 0, elementSize), handle);
    }

    public TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta)
    {
        var handle = _driver.CreateTexture2D(data, in desc, out meta);
        return _stores.TextureStore.Add(in meta, handle);
    }

    public TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta)
    {
        var handle = _driver.CreateCubeMap(data, in desc, out meta);
        return _stores.TextureStore.Add(in meta, handle);
    }

    public FrameBufferId CreateFramebuffer(in FrameBufferDesc desc, out FrameBufferMeta meta)
    {
        _driver.CreateFramebuffer(in desc, out var result);
        var fboId = _stores.FboStore.Add(result.Fbo.Meta, result.Fbo.Handle);
        var fboTexId = result.FboTex != default ?  _stores.TextureStore.Add(result.FboTex.Meta, result.FboTex.Handle) : default;
        var rboDepthId = result.RboDepth != default ? _stores.RboStore.Add(result.RboDepth.Meta, result.RboDepth.Handle) : default;
        var rboTexId = result.RboTex != default ? _stores.RboStore.Add(result.RboTex.Meta, result.RboTex.Handle) : default;
        
        _registry.FboRegistry.Register(new FrameBufferLayout
        {
            FboId = fboId,
            FboTexId = fboTexId,
            RboTexId = rboTexId,
            RboDepthId = rboDepthId,
        });
        
        meta = result.Fbo.Meta;
        return fboId;
    }

    public ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta)
    {
        var handle = _driver.CreateShader(vertexSource, fragmentSource,
            out var uniformTable, out meta);
        var shaderId = _stores.ShaderStore.Add(in meta, in handle);
        _registry.ShaderRegistry.Add(shaderId, uniformTable);
        return shaderId;
    }

    public UniformBufferId CreateUniformBuffer<T>(UniformGpuSlot slot, UboDefaultCapacity defaultCapacity,
        out UniformBufferMeta meta) where T : unmanaged, IUniformGpuData
    {
        if (slot == UniformGpuSlot.None)
            throw new GraphicsException($"UBO - {nameof(slot)} has value None");

        if (!UniformBufferUtils.IsStd140Aligned<T>())
            throw GraphicsException.InvalidStd140Layout<T>();

        uint size = (uint)Unsafe.SizeOf<T>();
        //nuint stride = UniformBufferUtils.AlignUp(size, UniformBufferUtils.UboOffsetAlign);
        //nuint capacity = UniformBufferUtils.GetDefaultCapacity(stride, defaultCapacity);

        var result = _driver.CreateUniformBuffer(slot, defaultCapacity, size, out meta);
        var uboId = _stores.UboStore.Add(in meta, result);
        _registry.ShaderRegistry.AddUboToSlot(meta.Slot, uboId);
        return uboId;
    }
}