using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics;

internal readonly record struct FboAttachmentHandleResult(
    ResourceRefToken<TextureId> ColorTexture,
    ResourceRefToken<TextureId> DepthTexture,
    ResourceRefToken<RenderBufferId> ColorRenderBuffer,
    ResourceRefToken<RenderBufferId> DepthRenderBuffer
);

internal sealed class GfxAllocator
{
    private readonly IGraphicsDriver _driver;

    internal GfxAllocator(IGraphicsDriver driver)
    {
        _driver = driver;
    }
    
    // Buffers
    public ResourceRefToken<MeshId> CreateEmptyMesh()
    {
        return _driver.Meshes.CreateVertexArray();
    }

    public ResourceRefToken<VertexBufferId> CreateVertexBuffer<V>(ReadOnlySpan<V> vertices, BufferUsage usage,
        uint index, nuint size)
        where V : unmanaged
    {
        return _driver.Buffers.CreateVertexBuffer(vertices, size, BufferStorage.Dynamic, BufferAccess.MapWrite);
    }

    public ResourceRefToken<IndexBufferId> CreateIndexBuffer<I>(ReadOnlySpan<I> indices, BufferUsage usage, nuint size)
        where I : unmanaged
    {
        var elementSize = Unsafe.SizeOf<I>();
        if (elementSize != 1 && elementSize != 2 && elementSize != 3)
            GraphicsException.ThrowInvalidType<I>(typeof(I).Name, "Invalid elemental size");

        return _driver.Buffers.CreateIndexBuffer(indices, size, BufferStorage.Static, BufferAccess.None);
    }

    public ResourceRefToken<UniformBufferId> CreateUniformBuffer<T>(UniformGpuSlot slot,
        UboDefaultCapacity defaultCapacity)
        where T : unmanaged, IUniformGpuData
    {
        if (!UniformBufferUtils.IsStd140Aligned<T>())
            throw GraphicsException.InvalidStd140Layout<T>();

        var size = (uint)Unsafe.SizeOf<T>();
        var stride = UniformBufferUtils.AlignUp(size, UniformBufferUtils.UboOffsetAlign);
        nuint capacity = UniformBufferUtils.GetDefaultCapacity(stride, defaultCapacity);

        return _driver.Buffers.CreateUniformBuffer<T>(slot, capacity, BufferStorage.Dynamic, BufferAccess.MapWrite);
    }

    public void SetVertexAttribute(in GfxHandle vao, IReadOnlyList<VertexAttributeDesc> attributes)
    {
        for (int i = 0; i < attributes.Count; i++)
        {
            var attrib = attributes[i];
            _driver.Meshes.SetVertexAttribute(vao, in attrib);
        }
    }

    public void SetBufferData<T>(GfxHandle buffer, ReadOnlySpan<T> data, BufferUsage usage)
        where T : unmanaged
    {
        var size = ElementToSize<T>(data.Length);
        _driver.Buffers.SetBufferData(buffer, data, size, usage);
    }

    public void UploadBufferData<T>(GfxHandle buffer, ReadOnlySpan<T> data, int offsetElements)
        where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offsetElements, data.Length);
        var offset = (nuint)(offsetElements * Unsafe.SizeOf<T>());
        var size = (nuint)(data.Length * Unsafe.SizeOf<T>());
        _driver.Buffers.UploadBufferData(buffer, data, offset, size);
    }

    public void SetUniformBufferCapacity<T>(GfxHandle buffer, nuint capacity)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(0, (int)capacity);
        _driver.Buffers.ResizeBuffer(in buffer, capacity, BufferUsage.DynamicDraw);
    }

    // Textures
    



    // FrameBuffers
    public ResourceRefToken<FrameBufferId> CreateFrameBuffer(in FrameBufferDesc desc,
        out FboAttachmentHandleResult attachments)
    {
        if (desc.Attachments.DepthTexture) GraphicsException.ThrowUnsupportedFeature("DepthTexture");
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.Y, 16);

        var (abs, ratio) = (desc.AbsoluteSize, desc.DownscaleRatio);
        var size = new Vector2D<int>((int)(abs.X * ratio.X), (int)(abs.Y * ratio.Y));

        ArgumentOutOfRangeException.ThrowIfLessThan(size.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Y, 16);

        var fboRef = _driver.FrameBuffers.CreateFrameBuffer();
        var result = new FboAttachmentHandleResult();

        if (desc.Attachments.ColorTexture)
        {
            var texDesc =
                new GpuTextureDescriptor((uint)size.X, (uint)size.Y, desc.TexturePreset, TextureKind.Texture2D);
            var textureRef = CreateTexture(ReadOnlySpan<byte>.Empty, in texDesc, out _);
            _driver.FrameBuffers.AttachTexture(fboRef.Handle, textureRef.Handle, FrameBufferTarget.Color);
            result = result with { ColorTexture = textureRef };
        }

        if (desc.Attachments.ColorRenderBuffer)
        {
            var rboRef = CreateRenderBufferFor(fboRef.Handle, size, FrameBufferTarget.Color, desc.Multisample);
            _driver.FrameBuffers.AttachRenderBuffer(fboRef.Handle, rboRef.Handle, FrameBufferTarget.Color);
            result = result with { ColorRenderBuffer = rboRef };
        }

        if (desc.Attachments.DepthStenRenderBuffer)
        {
            var rboRef = CreateRenderBufferFor(fboRef.Handle, size, FrameBufferTarget.DepthStencil, desc.Multisample);
            _driver.FrameBuffers.AttachRenderBuffer(fboRef.Handle, rboRef.Handle, FrameBufferTarget.DepthStencil);
            result = result with { DepthRenderBuffer = rboRef };
        }

        attachments = result;
        return fboRef;
    }

    private ResourceRefToken<RenderBufferId> CreateRenderBufferFor(in GfxHandle fbo, Vector2D<int> size,
        FrameBufferTarget target, RenderBufferMsaa msaa)
    {
        var samples = msaa.ToSamples();
        var rboRef = _driver.FrameBuffers.CreateRenderBuffer(target, size, samples > 0, samples);
        _driver.FrameBuffers.AttachRenderBuffer(in fbo, in rboRef.Handle, target);
        return rboRef;
    }
    
    
    public ShaderId CreateShader(string vertexSource, string fragmentSource, out ShaderMeta meta, out List<(string, int)> uniforms)
    {
        var programRef = _driver.Shaders.CreateShader(vertexSource, fragmentSource);
        _driver.Shaders.GetUniformsFromProgram(in programRef.Handle, out  uniforms, out var samples);
        meta = new ShaderMeta((uint)samples);

        var shaderId = _resources.ShaderStore.Add(in meta, programRef);
        _repository.ShaderRepository.Add(shaderId, in meta, uniforms);
        return shaderId;
    }

}