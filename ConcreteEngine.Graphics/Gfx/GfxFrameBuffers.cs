using System.Runtime.CompilerServices;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxFrameBuffers
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;
    
    private readonly GfxFrameBuffersInvoker _invoker;

    internal GfxFrameBuffers(GfxContext context, GfxTextures gfxTextures)
    {
        _invoker = new GfxFrameBuffersInvoker(context, gfxTextures.);
        _resources = context.Stores;
        _repository = context.Repositories;
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

        var fboRef = _allocator.CreateFrameBuffer(in desc, out var attachments);
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

    private static (uint elementCount, uint elementSize, nuint size) ToElementSizeData<T>(int length)
        where T : unmanaged =>
        ((uint)length, (uint)Unsafe.SizeOf<T>(), (nuint)(length * (uint)Unsafe.SizeOf<T>()));


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

        if (desc.Attachments.ColorTexture)
        {
            var texDesc =
                new GpuTextureDescriptor((uint)size.X, (uint)size.Y, desc.TexturePreset, TextureKind.Texture2D);
            var textureId = CreateTexture(texDesc);
            var texHandle = _resources.TextureStore.GetHandle(textureId);
            _driver.FrameBuffers.AttachTexture(in fboRef.Handle, in texHandle, FrameBufferTarget.Color);
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

    private RenderBufferId CreateRenderBufferFor(in GfxHandle fbo, Vector2D<int> size,
        FrameBufferTarget target, RenderBufferMsaa msaa)
    {
        var (multisample, samples) = msaa.ToSamples();
        var meta = new RenderBufferMeta(size, target, msaa);

        var rboRef = _driver.FrameBuffers.CreateRenderBuffer(target, size, true, samples);
        _driver.FrameBuffers.AttachRenderBuffer(in fbo, in rboRef.Handle, target);

        return _resources.RboStore.Add(in meta, in rboRef);
    }

}