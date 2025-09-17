using System.Numerics;
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

internal sealed class GfxResourceAllocator
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

    public MeshId CreateMesh(DrawPrimitive primitive,  uint drawCount)
    {
        
    }

    public MeshId CreateVertexMesh<V>(DrawPrimitive primitive, ReadOnlySpan<V> vertices, uint drawCount)
        where V : unmanaged
    {
        var meta = new MeshMeta(primitive, MeshDrawKind.Elements, DrawElementSize.Invalid, 0, drawCount);
        var vao = _driver.Meshes.CreateVertexArray();
        var meshId = _resources.MeshStore.Add(in meta, vao);

        var vboId = CreateVertexBuffer(vertices, BufferUsage.DynamicDraw, 0);
        var vbo = _resources.VboStore.GetHandle(vboId);

        var vertexSize = (uint)Unsafe.SizeOf<V>();
        _driver.Meshes.AttachVertexBuffer(in vao.Handle, in vbo, 0, 0, vertexSize);

        // foreach (ref readonly var attr in attribs)
        //    _driver.Meshes.SetVertexAttribute(in vao.Handle, in attr);

        return meshId;
    }

    public MeshId CreateElementalMesh<V, I>(ReadOnlySpan<V> vertices, ReadOnlySpan<I> indices,
        ReadOnlySpan<VertexAttributeDescriptor> attribs, DrawPrimitive primitive,
        uint drawCount) where V : unmanaged where I : unmanaged
    {
        var meta = new MeshMeta(primitive, MeshDrawKind.Elements, DrawElementSize.Invalid, 0, drawCount);
        var vaoRef = _driver.Meshes.CreateVertexArray();
        var meshId = _resources.MeshStore.Add(in meta, vaoRef);

        var vboId = CreateVertexBuffer(vertices, BufferUsage.DynamicDraw, 0);
        var iboId = CreateIndexBuffer(indices, BufferUsage.StaticDraw);
        var vbo = _resources.VboStore.GetHandle(vboId);
        var ibo = _resources.IboStore.GetHandle(iboId);

        var vertexSize = (uint)Unsafe.SizeOf<V>();
        _driver.Meshes.AttachVertexBuffer(in vaoRef.Handle, in vbo, 0, 0, vertexSize);
        _driver.Meshes.AttachIndexBuffer(in vaoRef.Handle, in ibo);

        foreach (ref readonly var attr in attribs)
            _driver.Meshes.SetVertexAttribute(in vaoRef.Handle, in attr);

        return meshId;
    }

    private VertexBufferId CreateVertexBuffer<V>(ReadOnlySpan<V> vertices, BufferUsage usage, uint index)
        where V : unmanaged
    {
        var elementCount = (uint)vertices.Length;
        var elementSize = (uint)Unsafe.SizeOf<V>();
        var size = elementSize * elementCount;

        var meta = new VertexBufferMeta(usage, index, elementCount, elementSize);
        var vboRef = _driver.Buffers.CreateVertexBuffer(vertices, size, BufferStorage.Dynamic, BufferAccess.MapWrite);
        return _resources.VboStore.Add(in meta, in vboRef);
    }

    private IndexBufferId CreateIndexBuffer<I>(ReadOnlySpan<I> indices, BufferUsage usage) where I : unmanaged
    {
        var elementCount = (uint)indices.Length;
        var elementSize = (uint)Unsafe.SizeOf<I>();
        var size = elementSize * elementCount;

        if (size != 1 && size != 2 && size != 3)
            GraphicsException.ThrowInvalidType<I>(typeof(I).Name, "Invalid elemental size");

        var meta = new IndexBufferMeta(usage, (uint)elementCount, elementSize);
        var iboRef = _driver.Buffers
            .CreateIndexBuffer(indices, size, BufferStorage.Static, BufferAccess.None);
        return _resources.IboStore.Add(meta, iboRef);
    }

    public UniformBufferId CreateUniformBuffer<T>(UniformGpuSlot slot, UboDefaultCapacity defaultCapacity)
        where T : unmanaged, IUniformGpuData
    {
        if (!UniformBufferUtils.IsStd140Aligned<T>())
            throw GraphicsException.InvalidStd140Layout<T>();

        var size = (uint)Unsafe.SizeOf<T>();
        var meta = new UniformBufferMeta(slot, size);
        nuint capacity = UniformBufferUtils.GetDefaultCapacity(meta.Stride, defaultCapacity);

        var result =
            _driver.Buffers.CreateUniformBuffer<T>(slot, capacity, BufferStorage.Dynamic, BufferAccess.MapWrite);
        
        var uboId = _resources.UboStore.Add(in meta, result);
        _repository.ShaderRepository.AddUboToSlot(meta.Slot, uboId);
        return uboId;
    }
    
    public void SetVertexAttribute(MeshId meshId, ReadOnlySpan<VertexAttributeDescriptor> attributes)
    {
        var vao = _resources.MeshStore.GetHandleAndMeta(meshId, out var meta);
        var meshLayout = _repository.MeshRepository.Get(meshId);
        var vboIds = meshLayout.GetVertexBufferIds();
/*
        VertexBufferId prevVboId = default;
        for (int i = 0; i < attributes.Length; i++)
        {
            ref readonly var attrib = ref attributes[i];
            if (attrib.VboBinding > vboIds.Length)
                throw GraphicsException.InvalidState(
                    $"Attrib vbo index {attrib.VboBinding} is greater than vbo count {vboIds.Length}");

            var vboId = vboIds[(int)attrib.VboBinding];
            if (prevVboId != vboId)
                BindVertexBuffer(vboId);

            _driver.SetVertexAttribute(vao, (uint)i, attrib);
            prevVboId = vboId;
        }
*/
        var newMeta = MeshMeta.CreateCopy(in meta, (uint)attributes.Length, meta.DrawCount);
        _resources.MeshStore.ReplaceMeta(meshId, in newMeta, out _);
    }

    public void SetVertexBuffer<T>(VertexBufferId vboId, ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
    {
        var vbo = _resources.VboStore.GetHandleAndMeta(vboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.ElementSize > 0)
            GraphicsException.ThrowInvalidBufferData<VertexBufferId>(nameof(vboId), "Buffer is static");

        var elementCount = data.Length;
        var elementSize = Unsafe.SizeOf<T>();
        nuint size = (nuint)(elementSize * elementCount);

        _driver.Buffers.SetVertexBuffer(default, vbo, data, size, usage);

        var newMeta = new VertexBufferMeta(meta.Usage, meta.BindingIdx, (uint)elementCount, (uint)elementSize);
        _resources.VboStore.ReplaceMeta(vboId, in newMeta, out _);
    }

    public void SetIndexBuffer<T>(IndexBufferId iboId, ReadOnlySpan<T> data, BufferUsage usage) where T : unmanaged
    {
        var ibo = _resources.IboStore.GetHandleAndMeta(iboId, out var meta);

        if (meta.Usage == BufferUsage.StaticDraw && meta.ElementCount * meta.ElementSize > 0)
            GraphicsException.ThrowInvalidBufferData<IndexBufferId>(nameof(iboId),"Buffer is static");

        var elementCount = data.Length;
        var elementSize = Unsafe.SizeOf<T>();
        nuint size = (nuint)(elementSize * elementCount);

        _driver.Buffers.SetIndexBuffer(default, ibo, data, size, usage);

        var newMeta = new IndexBufferMeta(meta.Usage, (uint)elementCount, (uint)elementSize);
        _resources.IboStore.ReplaceMeta(iboId, in newMeta, out _);
    }
    
    
    public void SetUniformBufferSize(UniformGpuSlot slot, nuint capacityBytes)
    {
        var ubo = _repository.ShaderRepository.GetUboId(slot);
        var handle = _resources.UboStore.GetHandle(ubo);
        _driver.Buffers.SetUniformBufferSize(slot, capacityBytes);
    }

    public void UploadVertexBuffer<T>(VertexBufferId vboId, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        var handle = _resources.VboStore.GetHandle(vboId);
        var byteOffset = (nuint)(offsetElements * Unsafe.SizeOf<T>());
        _driver.Buffers.UploadVertexBufferData(handle, data, byteOffset);
    }

    public void UploadIndexBuffer<T>(IndexBufferId iboId, ReadOnlySpan<T> data, int offsetElements) where T : unmanaged
    {
        var handle = _resources.IboStore.GetHandle(iboId);
        var byteOffset = (nuint)(offsetElements * Unsafe.SizeOf<T>());
        _driver.Buffers.UploadIndexBufferData(handle, data, byteOffset);
    }


    public void UploadUniformGpuData<T>(UniformGpuSlot slot, in T data, nuint offsetBytes = 0)
        where T : unmanaged, IUniformGpuData
    {
        var ubo = _repository.ShaderRepository.GetUboId(slot);
        var handle = _resources.UboStore.GetHandleAndMeta(ubo, out var meta);
        _driver.Buffers.UploadUniformBufferData<T>(handle, data, meta.Stride, offsetBytes);
    }

    public void BindUniformBufferRange(UniformGpuSlot slot, nuint offset, nuint size)
    {
        var ubo = _repository.ShaderRepository.GetUboId(slot);
        var handle = _resources.UboStore.GetHandle(ubo);
        _driver.Buffers.BindUniformBufferRange(handle, (uint)slot, offset, size);
    }
    
    // TEXTURE
    public TextureId CreateTexture(in GpuTextureDescriptor desc)
    {
        if (desc.Kind == TextureKind.CubeMap)
            ArgumentOutOfRangeException.ThrowIfNotEqual(desc.Width, desc.Height, nameof(desc.Width));

        var hasMipLevels = desc.Preset is TexturePreset.LinearMipmapClamp or TexturePreset.LinearMipmapRepeat;
        var mipLevels = hasMipLevels ? CalcMipLevels(desc.Width, desc.Height) : 0;

        var meta = new TextureMeta(desc.Width, desc.Height, desc.Preset, desc.Kind, desc.Anisotropy, desc.Format,
            (byte)mipLevels, false);

        var texRef = _driver.Textures.CreateTexture2D(desc.Width, desc.Height, mipLevels);
        return _resources.TextureStore.Add(in meta, texRef);
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


    public FrameBufferId CreateFrameBuffer(in FrameBufferDesc desc)
    {
        if (desc.Attachments.DepthTexture) GraphicsException.ThrowUnsupportedFeature("DepthTexture");
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.Y, 16);

        var (abs, ratio) = (desc.AbsoluteSize, desc.DownscaleRatio);
        var size = new Vector2D<int>((int)(abs.X * ratio.X), (int)(abs.Y * ratio.Y));

        ArgumentOutOfRangeException.ThrowIfLessThan(size.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Y, 16);

        var fboMeta = new FrameBufferMeta(size, true, false, true);

        var fboRef = _driver.FrameBuffers.CreateFrameBuffer();
        var fboId = _resources.FboStore.Add(in fboMeta, fboRef);

        FboAttachmentIds attachmentIds = default;

        if (desc.Attachments.ColorTexture)
        {
            var texDesc =
                new GpuTextureDescriptor((uint)size.X, (uint)size.Y, desc.TexturePreset, TextureKind.Texture2D);
            var textureId = CreateTexture(texDesc);
            var texHandle = _resources.TextureStore.GetHandle(textureId);
            _driver.FrameBuffers.AttachTexture(in fboRef.Handle, in texHandle, FrameBufferTarget.Color);
            attachmentIds = attachmentIds with { ColorTextureId = textureId };
        }

        if (desc.Attachments.ColorRenderBuffer)
        {
            var rboId = CreateRenderBufferFor(in fboRef.Handle, size, FrameBufferTarget.Color, desc.Multisample);
            var rboHandle = _resources.RboStore.GetHandle(rboId);
            _driver.FrameBuffers.AttachRenderBuffer(in fboRef.Handle, in rboHandle, FrameBufferTarget.Color);
        }

        if (desc.Attachments.DepthStenRenderBuffer)
        {
            var rboId = CreateRenderBufferFor(in fboRef.Handle, size, FrameBufferTarget.DepthStencil, desc.Multisample);
            var rboHandle = _resources.RboStore.GetHandle(rboId);
            _driver.FrameBuffers.AttachRenderBuffer(in fboRef.Handle, in rboHandle, FrameBufferTarget.DepthStencil);
        }

        _repository.FboRepository.AddRecord(fboId, in attachmentIds, in desc);
        return fboId;
    }

/*
    public bool RecreateFrameBuffer(FrameBufferId fboId, in Vector2D<int> outputSize)
    {
        var layout = _repository.FboRepository.Get(fboId);

        if (!layout.AutoResizeable) return false;

        var (absoluteSize, sizeRatio) = (outputSize, SizeRatio: layout.DownscaleRatio);
        var size = new Vector2D<int>((int)(absoluteSize.X * sizeRatio.X), (int)(absoluteSize.Y * sizeRatio.Y));

        var attachedIds = layout.FboAttachmentResources;
        var (texColId, texDepthId, rboDepthId, rboTexId) = attachedIds;

        ref readonly var prevFbo = ref _resources.FboStore.GetHandleAndMeta(fboId, out var prevMeta);
        var desc = layout.GetDescriptor() with { AbsoluteSize = absoluteSize };
        var newMeta = FrameBufferMeta.CreateResizeCopy(in prevMeta, size);


        _driver.FrameBuffers.CreateFrameBuffer(in desc, out var result);
        _disposer.EnqueueRemoval(fboId, true);
        _resources.FboStore.Replace(fboId, in newMeta, result.Fbo.Handle, out var prevHandle);


        if (result.FboTex.Handle.IsValid)
            _resources.TextureStore.Replace(texColId, result.FboTex.Meta, result.FboTex.Handle, out _);

        if (result.RboDepth.Handle.IsValid)
            _resources.RboStore.Replace(rboDepthId, result.RboDepth.Meta, result.RboDepth.Handle, out _);

        if (result.RboTex.Handle.IsValid)
            _resources.RboStore.Replace(rboTexId, result.RboTex.Meta, result.RboTex.Handle, out _);

        return true;
    }
*/
    private RenderBufferId CreateRenderBufferFor(in GfxHandle fbo, Vector2D<int> size,
        FrameBufferTarget target, RenderBufferMsaa msaa)
    {
        var (multisample, samples) = msaa.ToSamples();
        var meta = new RenderBufferMeta(size, target, msaa);

        var rboRef = _driver.FrameBuffers.CreateRenderBuffer(target, size, true, samples);
        _driver.FrameBuffers.AttachRenderBuffer(in fbo, in rboRef.Handle, target);

        return _resources.RboStore.Add(in meta, in rboRef);
    }

    public ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta)
    {
        var programRef = _driver.Shaders.CreateShader(vertexSource, fragmentSource);
        _driver.Shaders.GetUniformsFromProgram(in programRef.Handle, out var uniforms, out var samples);
        meta = new ShaderMeta((uint)samples);

        var shaderId = _resources.ShaderStore.Add(in meta, programRef);
        _repository.ShaderRepository.Add(shaderId, in meta, uniforms);
        return shaderId;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint CalcMipLevels(uint width, uint height)
    {
        uint size = Math.Max(width, height);
        return (uint)Math.Floor(Math.Log2(size)) + 1;
    }

    /*
    public FrameBufferId CreateFrameBufferMsaa(Vector2D<int> size, RenderBufferMsaa msaa)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((byte)msaa, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(size.X, 16, nameof(size.X));
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Y, 16, nameof(size.Y));

        var fboRef = _driver.FrameBuffers.CreateFrameBuffer();
        var fboMeta = new FrameBufferMeta(size, true, false, true);
        var fboId = _resources.FboStore.Add(in fboMeta, fboRef);

        var rboColor = CreateFboRenderBuffer(in fboRef.Handle, size, FrameBufferTarget.Color, msaa);
        var rboDepth = CreateFboRenderBuffer(in fboRef.Handle, size, FrameBufferTarget.DepthStencil, msaa);

        _driver.FrameBuffers.SetDrawBuffers(in fboRef.Handle, FrameBufferTarget.Color);
        _driver.FrameBuffers.SetDrawBuffers(in fboRef.Handle, FrameBufferTarget.DepthStencil);

        var ids = new FboAttachmentIds(default, default, rboColor, rboDepth);
        var d = new FrameBufferAttachmentDesc(false, false, true, true);
        _repository.FboRepository.AddRecord(fboId, in ids, new FrameBufferDesc(Vector2.One, size, d));
        return fboId;
    }
*/
}