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

    ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta);

    UniformBufferId CreateUniformBuffer<T>(UniformGpuSlot slot, UboDefaultCapacity defaultCapacity,
        out UniformBufferMeta meta)
        where T : unmanaged, IUniformGpuData;
}

internal sealed class GfxResourceAllocator : IGfxResourceAllocator
{
    private readonly IGraphicsDriver _driver;
    private readonly GfxResourceManager _resources;
    private readonly GfxResourceRegistry _registry;
    private readonly GfxResourceDisposer _disposer;

    private BackendDriverDispatcher Dispatcher => _resources.GetDispatcher();


    public GfxResourceAllocator(
        IGraphicsDriver driver,
        GfxResourceManager resources,
        GfxResourceRegistry registry,
        GfxResourceDisposer disposer)
    {
        _resources = resources;
        _driver = driver;
        _registry = registry;
        _disposer = disposer;
    }

    public MeshId CreateMesh(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementType drawElement,
        out MeshMeta meta)
    {
        var handle = _driver.CreateVertexArray(primitive, drawKind, drawElement, out meta);
        var meshId = _resources.MeshStore.Add(in meta, handle);
        return meshId;
    }

    public VertexBufferId CreateVertexBuffer(BufferUsage usage, uint elementSize, uint index, out VertexBufferMeta meta)
    {
        var handle = _driver.CreateVertexBuffer(usage, elementSize, index, out meta);
        return _resources.VboStore.Add(new VertexBufferMeta(usage, index, 0, elementSize), handle);
    }

    public IndexBufferId CreateIndexBuffer(BufferUsage usage, uint elementSize, out IndexBufferMeta meta)
    {
        var handle = _driver.CreateIndexBuffer(usage, elementSize, out meta);
        return _resources.IboStore.Add(new IndexBufferMeta(usage, 0, elementSize), handle);
    }

    public TextureId CreateTexture2D(GpuTextureData data, in GpuTextureDescriptor desc, out TextureMeta meta)
    {
        var handle = _driver.CreateTexture2D(data, in desc, out meta);
        return _resources.TextureStore.Add(in meta, handle);
    }

    public TextureId CreateCubeMap(GpuCubeMapData data, in GpuCubeMapDescriptor desc, out TextureMeta meta)
    {
        var handle = _driver.CreateCubeMap(data, in desc, out meta);
        return _resources.TextureStore.Add(in meta, handle);
    }

    public FrameBufferId CreateFrameBuffer(in FrameBufferDesc desc, out FrameBufferMeta meta)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.Y, 16);

        _driver.CreateFrameBuffer(in desc, out var result);
        var fboId = _resources.FboStore.Add(result.Fbo.Meta, result.Fbo.Handle);
        var fboTexId = result.FboTex.Handle.IsValid
            ? _resources.TextureStore.Add(result.FboTex.Meta, result.FboTex.Handle)
            : default;
        var rboDepthId = result.RboDepth.Handle.IsValid 
            ? _resources.RboStore.Add(result.RboDepth.Meta, result.RboDepth.Handle)
            : default;
        var rboTexId = result.RboTex.Handle.IsValid
            ? _resources.RboStore.Add(result.RboTex.Meta, result.RboTex.Handle)
            : default;

        _registry.FboRepository.AddRecord(fboId, new FrameBufferLayout.AttachedFboIds(fboTexId, rboDepthId, rboTexId),
            in desc);

        meta = result.Fbo.Meta;
        return fboId;
    }

    public bool RecreateFrameBuffer(FrameBufferId fboId, in Vector2D<int> outputSize)
    {
        var layout = _registry.FboRepository.Get(fboId);
        
        if(!layout.AutoResizeable) return false;
        

        var (absoluteSize,sizeRatio) = (outputSize, layout.SizeRatio);

        var size = new Vector2D<int>((int)(absoluteSize.X * sizeRatio.X), (int)(absoluteSize.Y * sizeRatio.Y));

        var attachedIds = layout.AttachedFboResources;
        var (colTexId, rboDepthId, rboTexId) = (attachedIds.FboTexId, attachedIds.RboDepthId, attachedIds.RboTexId);

        ref readonly var prevFbo = ref _resources.FboStore.GetHandleAndMeta(fboId, out var prevMeta);

        GfxHandle prevTex = default;
        GfxHandle prevRboDepth = default;
        GfxHandle prevRboTex = default;

        if (colTexId.Id > 0)
            prevTex =  _resources.TextureStore.GetHandle(colTexId);

        if (rboTexId.Id > 0)
            prevRboTex =  _resources.RboStore.GetHandle(rboTexId);

        if (rboDepthId.Id > 0)
            prevRboDepth =  _resources.RboStore.GetHandle(rboDepthId);
        
        _disposer.EnqueueRemoval(fboId, true);



        var desc = layout.GetDescriptor() with { AbsoluteSize = absoluteSize };

        _driver.ReplaceFrameBuffer(in desc, in prevFbo, in prevTex, in prevRboDepth, in prevRboTex, out var result);

        var newMeta = FrameBufferMeta.CreateResizeCopy(in prevMeta, size);
        _resources.FboStore.Replace(fboId, result.Fbo.Meta, result.Fbo.Handle, out _);

        if (result.FboTex.Handle.IsValid)
            _resources.TextureStore.Replace(colTexId, result.FboTex.Meta, result.FboTex.Handle, out _);

        if (result.RboDepth.Handle.IsValid)
            _resources.RboStore.Replace(rboDepthId, result.RboDepth.Meta, result.RboDepth.Handle, out _);;

        if (result.RboTex.Handle.IsValid)
            _resources.RboStore.Replace(rboTexId, result.RboTex.Meta, result.RboTex.Handle, out _);
        return true;
        //
    }

    public ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta)
    {
        var handle = _driver.CreateShader(vertexSource, fragmentSource,
            out var uniforms, out meta);
        var shaderId = _resources.ShaderStore.Add(in meta, in handle);
        _registry.ShaderRepository.Add(shaderId, in meta, uniforms);
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
        var uboId = _resources.UboStore.Add(in meta, result);
        _registry.ShaderRepository.AddUboToSlot(meta.Slot, uboId);
        return uboId;
    }
}