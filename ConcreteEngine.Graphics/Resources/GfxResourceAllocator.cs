using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Resources;

public interface IGfxResourceAllocator
{
    MeshId CreateMesh(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement, out MeshMeta meta);

    VertexBufferId CreateVertexBuffer(BufferUsage usage, uint elementSize, uint index,
        out VertexBufferMeta meta);

    IndexBufferId CreateIndexBuffer(BufferUsage usage, uint elementSize, out IndexBufferMeta meta);

    TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta);
    TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta);

    FrameBufferId CreateFrameBuffer(in FrameBufferDesc desc, out FrameBufferMeta meta);
    void RecreateFrameBuffer(FrameBufferId fboId, in Vector2D<int> outputSize, out FrameBufferMeta meta);

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
    private readonly GfxResourceDisposer _disposer;


    public GfxResourceAllocator(
        IGraphicsDriver driver,
        GfxResourceManager stores,
        GfxResourceRegistry registry, 
        GfxResourceDisposer disposer)
    {
        _stores = stores;
        _driver = driver;
        _registry = registry;
        _disposer = disposer;
    }

    public MeshId CreateMesh(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement,
        out MeshMeta meta)
    {
        var handle = _driver.CreateVertexArray(primitive, drawKind, drawElement, out meta);
        var meshId = _stores.MeshStore.Add(in meta, handle);
        _registry.MeshRegistry.RegisterEmptyMesh(meshId);
        return meshId;
    }

    public VertexBufferId CreateVertexBuffer(BufferUsage usage, uint elementSize, uint index, out VertexBufferMeta meta)
    {
        var handle = _driver.CreateVertexBuffer(usage, elementSize, index, out meta);
        return _stores.VboStore.Add(new VertexBufferMeta(usage, index, 0, elementSize), handle);
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

    public FrameBufferId CreateFrameBuffer(in FrameBufferDesc desc, out FrameBufferMeta meta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.Y, 16);

        _driver.CreateFramebuffer(in desc, out var result);
        var fboId = _stores.FboStore.Add(result.Fbo.Meta, result.Fbo.Handle);
        var fboTexId = result.FboTex.Handle.Slot > 0
            ? _stores.TextureStore.Add(result.FboTex.Meta, result.FboTex.Handle)
            : default;
        var rboDepthId = result.RboDepth.Handle.Slot > 0
            ? _stores.RboStore.Add(result.RboDepth.Meta, result.RboDepth.Handle)
            : default;
        var rboTexId = result.RboTex.Handle.Slot > 0
            ? _stores.RboStore.Add(result.RboTex.Meta, result.RboTex.Handle)
            : default;

        _registry.FboRegistry.Register(new FrameBufferLayout(
            FboId: fboId,
            AttachedFboResources: new FrameBufferLayout.AttachedFboIds(
                FboTexId: fboTexId,
                RboDepthId: rboDepthId,
                RboTexId: rboTexId
            ),
            CreateDescriptor: in desc
        ));

        meta = result.Fbo.Meta;
        return fboId;
    }

    public void RecreateFrameBuffer(FrameBufferId fboId, in Vector2D<int> outputSize, out FrameBufferMeta meta)
    {
        ref readonly var prevMeta = ref _stores.FboStore.GetMeta(fboId);
        var layout = _registry.FboRegistry.Get(fboId);
        var size = new Vector2D<int>((int)(outputSize.X * prevMeta.SizeRatio.X),
            (int)(outputSize.Y * prevMeta.SizeRatio.Y));

        var attachedIds = layout.AttachedFboResources;
        var (colTexId, rboTexId, rboDepthId) = (attachedIds.FboTexId, attachedIds.RboTexId, attachedIds.RboDepthId);

        TextureMeta colTexMeta = default;
        RenderBufferMeta rboTexMeta = default, rboDepthMeta = default;

        if (colTexId.Id > 0)
        {
            ref readonly var prevTexMeta = ref _stores.TextureStore.GetMeta(colTexId);
            colTexMeta = new TextureMeta(size.X, size.Y, prevTexMeta.Format);
        }

        if (rboTexId.Id > 0)
        {
            ref readonly var prevRboMeta = ref _stores.RboStore.GetMeta(rboTexId);
            rboTexMeta = new RenderBufferMeta(prevRboMeta.Kind, size, prevRboMeta.Multisample);
        }

        if (rboDepthId.Id > 0)
        {
            ref readonly var prevRboMeta = ref _stores.RboStore.GetMeta(rboDepthId);
            rboDepthMeta = new RenderBufferMeta(prevRboMeta.Kind, size, prevRboMeta.Multisample);
        }

        var fboMeta = new FrameBufferMeta(prevMeta.TexturePreset, prevMeta.SizeRatio, outputSize,
            prevMeta.DepthStencilBuffer, prevMeta.Msaa, prevMeta.Samples);
        _disposer.EnqueueRemoval(fboId, true);
        
        meta = fboMeta;
        _stores.FboStore.Replace(fboId, in meta, )
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