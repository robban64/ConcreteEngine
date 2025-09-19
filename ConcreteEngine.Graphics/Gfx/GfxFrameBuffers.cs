using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
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

    private readonly GfxTextures _gfxTextures;

    private readonly IGraphicsDriver _driver;

    internal GfxFrameBuffers(GfxContext context, GfxTextures gfxTextures)
    {
        _invoker = new GfxFrameBuffersInvoker(context, gfxTextures.Invoker);
        _driver = context.Driver;
        _gfxTextures = gfxTextures;
        _resources = context.Stores;
        _repository = context.Repositories;
    }



    public FrameBufferId CreateFrameBuffer(in FrameBufferDesc desc)
    {
        EnsureCreateFrameBuffer(in desc);
        GetMetaAndSizeFromDesc(in desc, out var fboMeta, out var size);
        
        var fboRef = CreateFrameBufferBackend();
        var fboId = _resources.FboStore.Add(in fboMeta, fboRef);

        FboAttachmentIds attachmentIds = default;
        if (desc.Attachments.ColorTexture)
        {
            var texDesc = new GpuTextureDescriptor((uint)size.X, (uint)size.Y,
                desc.TexturePreset, TextureKind.Texture2D);

            var textureId = _gfxTextures.CreateTexture(ReadOnlySpan<byte>.Empty, in texDesc);
            AttachTextureBackend(in fboRef, textureId);
            attachmentIds = attachmentIds with { ColorTextureId = textureId };
        }

        if (desc.Attachments.ColorRenderBuffer)
        {
            var rbo = CreateAttachRenderBufferBackend(in fboRef, size,
                FrameBufferTarget.Color, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, in rbo);
            attachmentIds = attachmentIds with { ColorRenderBufferId = rboId };
        }

        if (desc.Attachments.DepthStenRenderBuffer)
        {
            var rbo = CreateAttachRenderBufferBackend(in fboRef, size,
                FrameBufferTarget.DepthStencil, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, in rbo);
            attachmentIds = attachmentIds with { DepthRenderBufferId = rboId };
        }

        _repository.FboRepository.AddRecord(fboId, in attachmentIds, in desc);
        return fboId;
    }

    public FrameBufferId RecreateFrameBuffer(FrameBufferId fboId, in FrameBufferDesc desc)
    {
        EnsureCreateFrameBuffer(in desc);
        GetMetaAndSizeFromDesc(in desc, out var fboMeta, out var size);
        var layout = _repository.FboRepository.Get(fboId);
        var attachIds = layout.FboAttachmentResources;


        var fboRef = CreateFrameBufferBackend();
        _resources.FboStore.Replace(fboId, in fboMeta, fboRef, out _);

        if (desc.Attachments.ColorTexture)
        {
            var texDesc = new GpuTextureDescriptor((uint)size.X, (uint)size.Y,
                desc.TexturePreset, TextureKind.Texture2D);

            var texId = _gfxTextures.ReplaceTexture(attachIds.ColorTextureId, ReadOnlySpan<byte>.Empty, in texDesc);
            AttachTextureBackend(in fboRef, texId);
        }

        if (desc.Attachments.ColorRenderBuffer)
        {
            InvalidOpThrower.ThrowIfNot(attachIds.ColorRenderBufferId.IsValid());
            var rbo = CreateAttachRenderBufferBackend(in fboRef, size,
                FrameBufferTarget.Color, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, in rbo);
            _resources.RboStore.Replace(attachIds.ColorRenderBufferId, in meta, in rbo, out _);
        }

        if (desc.Attachments.DepthStenRenderBuffer)
        {
            InvalidOpThrower.ThrowIfNot(attachIds.DepthRenderBufferId.IsValid());
            var rbo = CreateAttachRenderBufferBackend(in fboRef, size,
                FrameBufferTarget.DepthStencil, desc.Multisample, out var meta);
            _resources.RboStore.Replace(attachIds.DepthRenderBufferId, in meta, in rbo, out _);
        }

        _repository.FboRepository.UpdateRecord(fboId, in desc);
        return fboId;
    }

    private GfxRefToken<FrameBufferId> CreateFrameBufferBackend()
    {
        return _driver.FrameBuffers.CreateFrameBuffer();
    }

    private void AttachTextureBackend(in GfxRefToken<FrameBufferId> fbo,
        TextureId textureId)
    {
        var tex = _resources.TextureStore.GetHandle(textureId);
        _driver.FrameBuffers.AttachTexture(fbo.Handle, tex, FrameBufferTarget.Color);
    }

    private GfxRefToken<RenderBufferId> CreateAttachRenderBufferBackend(in GfxRefToken<FrameBufferId> fbo,
        Vector2D<int> size,
        FrameBufferTarget target, RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var samples = msaa.ToSamples();
        var rboRef = _driver.FrameBuffers.CreateRenderBuffer(target, size, samples > 0, samples);
        _driver.FrameBuffers.AttachRenderBuffer(in fbo.Handle, in rboRef.Handle, target);
        meta = new RenderBufferMeta(size, target, msaa);
        return rboRef;
    }
    
    private static void GetMetaAndSizeFromDesc(in FrameBufferDesc desc, out FrameBufferMeta meta, out Vector2D<int> size)
    {
        var (abs, ratio) = (desc.AbsoluteSize, desc.DownscaleRatio);
        size = new Vector2D<int>((int)(abs.X * ratio.X), (int)(abs.Y * ratio.Y));
        meta = new FrameBufferMeta(size, true, false, true);
    }
    private static void EnsureCreateFrameBuffer(in FrameBufferDesc desc)
    {
        InvalidOpThrower.ThrowIf(desc.Attachments.DepthTexture, nameof(desc.Attachments.DepthTexture));
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.Y, 16);
        ArgumentOutOfRangeException.ThrowIfZero(desc.DownscaleRatio.X);
        ArgumentOutOfRangeException.ThrowIfZero(desc.DownscaleRatio.Y);
    }
}