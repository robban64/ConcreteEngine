using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

public interface IGfxResourceAllocator
{
    MeshId CreateMesh(DrawPrimitive primitive, MeshDrawKind drawKind, DrawElementSize drawElement, out MeshMeta meta);

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

public record struct MeshDescriptor(
    DrawPrimitive Primitive,
    MeshDrawKind DrawKind,
    DrawElementSize DrawElement,
    uint DrawCount);

public record struct GfxTextureDescriptor(
    uint Width,
    uint Height,
    TexturePreset Preset,
    TextureKind Kind,
    EnginePixelFormat Format = EnginePixelFormat.Rgba,
    TextureAnisotropy Anisotropy = TextureAnisotropy.Default,
    float LodBias = 0
);

internal sealed class GfxResourceAllocator : IGfxResourceAllocator
{
    private readonly IGraphicsDriver _driver;
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    private readonly GfxResourceDisposer _disposer;


    public GfxResourceAllocator(
        IGraphicsDriver driver,
        GfxResourceManager resources,
        GfxResourceRepository repository,
        GfxResourceDisposer disposer)
    {
        _resources = resources.FrontendStoreHub;
        _driver = driver;
        _repository = repository;
        _disposer = disposer;
    }

    public MeshId CreateVertexMesh<V>(DrawPrimitive primitive, ReadOnlySpan<V> vertices, uint drawCount)
        where V : unmanaged
    {
        var meta = new MeshMeta(primitive, MeshDrawKind.Elements, DrawElementSize.Invalid, 0, drawCount);
        var vao = _driver.Meshes.CreateVertexArray().Handle;
        var meshId = _resources.MeshStore.Add(in meta, vao);

        var vboId = CreateVertexBuffer(vertices, BufferUsage.DynamicDraw, 0);
        var vbo = _resources.VboStore.GetHandle(vboId);

        var vertexSize = (uint)Unsafe.SizeOf<V>();
        _driver.Meshes.AttachVertexBuffer(in vao, in vbo, 0, 0, vertexSize);

        foreach (ref readonly var attr in attribs)
            _driver.Meshes.SetVertexAttribute(in vao, in attr);

        return meshId;
    }

    public MeshId CreateElementalMesh<V, I>(ReadOnlySpan<V> vertices, ReadOnlySpan<I> indices,
        ReadOnlySpan<VertexAttributeDescriptor> attribs, DrawPrimitive primitive,
        uint drawCount) where V : unmanaged where I : unmanaged
    {
        var meta = new MeshMeta(primitive, MeshDrawKind.Elements, DrawElementSize.Invalid, 0, drawCount);
        var vao = _driver.Meshes.CreateVertexArray().Handle;
        var meshId = _resources.MeshStore.Add(in meta, vao);

        var vboId = CreateVertexBuffer(vertices, BufferUsage.DynamicDraw, 0);
        var iboId = CreateIndexBuffer(indices, BufferUsage.StaticDraw);
        var vbo = _resources.VboStore.GetHandle(vboId);
        var ibo = _resources.IboStore.GetHandle(iboId);

        var vertexSize = (uint)Unsafe.SizeOf<V>();
        _driver.Meshes.AttachVertexBuffer(in vao, in vbo, 0, 0, vertexSize);
        _driver.Meshes.AttachIndexBuffer(in vao, in ibo);

        foreach (ref readonly var attr in attribs)
            _driver.Meshes.SetVertexAttribute(in vao, in attr);

        return meshId;
    }

    private VertexBufferId CreateVertexBuffer<V>(ReadOnlySpan<V> vertices, BufferUsage usage, uint index)
        where V : unmanaged
    {
        var elementCount = (uint)vertices.Length;
        var elementSize = (uint)Unsafe.SizeOf<V>();
        var size = elementSize * elementCount;

        var meta = new VertexBufferMeta(usage, index, elementCount, elementSize);
        var handle = _driver.Buffers.CreateVertexBuffer(vertices, size, BufferStorage.Dynamic, BufferAccess.MapWrite);
        return _resources.VboStore.Add(in meta, in handle.Handle);
    }

    private IndexBufferId CreateIndexBuffer<I>(ReadOnlySpan<I> indices, BufferUsage usage) where I : unmanaged
    {
        var elementCount = (uint)indices.Length;
        var elementSize = (uint)Unsafe.SizeOf<I>();
        var size = elementSize * elementCount;

        if (size != 1 && size != 2 && size != 3)
            GraphicsException.ThrowInvalidType<I>(typeof(I).Name, "Invalid elemental size");

        var meta = new IndexBufferMeta(usage, (uint)elementCount, elementSize);
        var handle = _driver.Buffers
            .CreateIndexBuffer(indices, size, BufferStorage.Static, BufferAccess.None);
        return _resources.IboStore.Add(meta, handle.Handle);
    }

    public TextureId CreateTexture(in GfxTextureDescriptor desc)
    {
        if (desc.Kind == TextureKind.CubeMap)
            ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));

        var hasMipLevels = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        var mipLevels = hasMipLevels ? CalcMipLevels(desc.Width, desc.Height) : 0;

        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)mipLevels, false);

        var handle = _driver.Textures.CreateTexture2D(desc.Width, desc.Height, mipLevels);
        return _resources.TextureStore.Add(in meta, handle.Handle);
    }

    public void UploadTextureData(in TextureId textureId, in GpuTextureData data)
    {
        var texture = _resources.TextureStore.GetHandleAndMeta(textureId, out var meta);
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Width, data.Width, nameof(data.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Height, data.Height, nameof(data.Height));

        _driver.Textures.UploadTextureData(in texture, data);
        var newMeta = TextureMeta.CreateFromHasData(in meta, true);
        _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
    }

    public void UploadCubeMapFace(in TextureId textureId, in GpuTextureData data, int faceIdx)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(data.Width, data.Height, nameof(data.Width));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(faceIdx, 5, nameof(faceIdx));

        var texture = _resources.TextureStore.GetHandleAndMeta(textureId, out var meta);
        
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Width, data.Width, nameof(data.Width));
        ArgumentOutOfRangeException.ThrowIfNotEqual(meta.Height, data.Height, nameof(data.Height));

        _driver.Textures.UploadCubeMapFaceData(in texture, data, faceIdx);
        if (faceIdx == 5)
        {
            var newMeta = TextureMeta.CreateFromHasData(in meta, true);
            _resources.TextureStore.ReplaceMeta(textureId, in newMeta, out _);
        }
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

        _repository.FboRepository.AddRecord(fboId, new FrameBufferLayout.AttachedFboIds(fboTexId, rboDepthId, rboTexId),
            in desc);

        meta = result.Fbo.Meta;
        return fboId;
    }

    public bool RecreateFrameBuffer(FrameBufferId fboId, in Vector2D<int> outputSize)
    {
        var layout = _repository.FboRepository.Get(fboId);

        if (!layout.AutoResizeable) return false;

        var (absoluteSize, sizeRatio) = (outputSize, layout.SizeRatio);

        var size = new Vector2D<int>((int)(absoluteSize.X * sizeRatio.X), (int)(absoluteSize.Y * sizeRatio.Y));

        var attachedIds = layout.AttachedFboResources;
        var (colTexId, rboDepthId, rboTexId) = (attachedIds.FboTexId, attachedIds.RboDepthId, attachedIds.RboTexId);

        ref readonly var prevFbo = ref _resources.FboStore.GetHandleAndMeta(fboId, out var prevMeta);
        var desc = layout.GetDescriptor() with { AbsoluteSize = absoluteSize };
        var newMeta = FrameBufferMeta.CreateResizeCopy(in prevMeta, size);


        _driver.CreateFrameBuffer(in desc, out var result);
        _disposer.EnqueueRemoval(fboId, true);
        _resources.FboStore.Replace(fboId, in newMeta, result.Fbo.Handle, out var prevHandle);


        if (result.FboTex.Handle.IsValid)
            _resources.TextureStore.Replace(colTexId, result.FboTex.Meta, result.FboTex.Handle, out _);

        if (result.RboDepth.Handle.IsValid)
            _resources.RboStore.Replace(rboDepthId, result.RboDepth.Meta, result.RboDepth.Handle, out _);

        if (result.RboTex.Handle.IsValid)
            _resources.RboStore.Replace(rboTexId, result.RboTex.Meta, result.RboTex.Handle, out _);

        return true;
    }

    public ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta)
    {
        var handle = _driver.CreateShader(vertexSource, fragmentSource,
            out var uniforms, out meta);
        var shaderId = _resources.ShaderStore.Add(in meta, handle.Handle);
        _repository.ShaderRepository.Add(shaderId, in meta, uniforms);
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
        var uboId = _resources.UboStore.Add(in meta, result.Handle);
        _repository.ShaderRepository.AddUboToSlot(meta.Slot, uboId);
        return uboId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CalcMipLevels(uint width, uint height)
    {
        uint size = Math.Max(width, height);
        return (uint)Math.Floor(Math.Log2(size)) + 1;
    }
}