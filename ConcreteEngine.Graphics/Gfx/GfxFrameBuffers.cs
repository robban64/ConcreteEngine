using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxFrameBuffers
{
    private readonly FrontendStoreHub _resources;
    private readonly GfxResourceRepository _repository;

    private readonly GfxTextures _gfxTextures;

    private readonly GfxFrameBuffersBackend _backend;

    internal GfxFrameBuffers(GfxContextInternal context, GfxTextures gfxTextures)
    {
        _backend = new GfxFrameBuffersBackend(context);
        _gfxTextures = gfxTextures;
        _resources = context.Stores;
        _repository = context.Repositories;
    }

    public FrameBufferId CreateFrameBuffer(in FrameBufferDesc desc)
    {
        EnsureCreateFrameBuffer(in desc);
        GetMetaAndSizeFromDesc(in desc, out var fboMeta, out var size);

        var fboRef = _backend.CreateFrameBuffer();
        var fboId = _resources.FboStore.Add(in fboMeta, fboRef);

        FboAttachmentIds attachmentIds = default;
        if (desc.Attachments.ColorTexture)
        {
            var texDesc = new GpuTextureDescriptor(size.X, size.Y,
                desc.TexturePreset, TextureKind.Texture2D);

            var textureId = _gfxTextures.CreateTexture(ReadOnlySpan<byte>.Empty, in texDesc);
            var texRef = _resources.TextureStore.GetRef(textureId);
            _backend.AttachTexture(in fboRef, in texRef);
            attachmentIds = attachmentIds with { ColorTextureId = textureId };
        }

        if (desc.Attachments.ColorRenderBuffer)
        {
            var rboRef = _backend.CreateAttachRenderBuffer(in fboRef, size,
                FrameBufferTarget.Color, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, in rboRef);
            attachmentIds = attachmentIds with { ColorRenderBufferId = rboId };
        }

        if (desc.Attachments.DepthStenRenderBuffer)
        {
            var rboRef = _backend.CreateAttachRenderBuffer(in fboRef, size,
                FrameBufferTarget.DepthStencil, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, in rboRef);
            attachmentIds = attachmentIds with { DepthRenderBufferId = rboId };
        }

        _repository.FboRepository.AddRecord(fboId, in attachmentIds, in desc);
        return fboId;
    }

    internal FrameBufferId RecreateFrameBuffer(FrameBufferId fboId, in FrameBufferDesc desc)
    {
        EnsureCreateFrameBuffer(in desc);
        GetMetaAndSizeFromDesc(in desc, out var fboMeta, out var size);
        var layout = _repository.FboRepository.Get(fboId);
        var attachIds = layout.FboAttachmentResources;


        var fboRef = _backend.CreateFrameBuffer();
        _resources.FboStore.Replace(fboId, in fboMeta, fboRef, out _);

        if (desc.Attachments.ColorTexture)
        {
            InvalidOpThrower.ThrowIfNot(attachIds.ColorTextureId.IsValid());
            var texDesc = new GpuTextureDescriptor(size.X, size.Y,
                desc.TexturePreset, TextureKind.Texture2D);

            _gfxTextures.ReplaceTexture(attachIds.ColorTextureId, 
                ReadOnlySpan<byte>.Empty, in texDesc, out var texRef);
            
            _backend.AttachTexture(in fboRef, in texRef);
        }

        if (desc.Attachments.ColorRenderBuffer)
        {
            InvalidOpThrower.ThrowIfNot(attachIds.ColorRenderBufferId.IsValid());
            var rbo = _backend.CreateAttachRenderBuffer(in fboRef, size,
                FrameBufferTarget.Color, desc.Multisample, out var meta);
             _resources.RboStore.Add(in meta, in rbo);
            _resources.RboStore.Replace(attachIds.ColorRenderBufferId, in meta, in rbo, out _);
        }

        if (desc.Attachments.DepthStenRenderBuffer)
        {
            InvalidOpThrower.ThrowIfNot(attachIds.DepthRenderBufferId.IsValid());
            var rbo = _backend.CreateAttachRenderBuffer(in fboRef, size,
                FrameBufferTarget.DepthStencil, desc.Multisample, out var meta);
            _resources.RboStore.Replace(attachIds.DepthRenderBufferId, in meta, in rbo, out _);
        }

        _repository.FboRepository.UpdateRecord(fboId, in desc);
        return fboId;
    }

    private sealed class GfxFrameBuffersBackend
    {
        private readonly IGraphicsDriver _driver;

        internal GfxFrameBuffersBackend(GfxContextInternal context)
        {
            _driver = context.Driver;
        }

        public GfxRefToken<FrameBufferId> CreateFrameBuffer()
        {
            return _driver.FrameBuffers.CreateFrameBuffer();
        }

        public void AttachTexture(in GfxRefToken<FrameBufferId> fbo, in GfxRefToken<TextureId> tex)
        {
            _driver.FrameBuffers.AttachTexture(fbo.Handle, tex.Handle, FrameBufferTarget.Color);
        }

        public GfxRefToken<RenderBufferId> CreateAttachRenderBuffer(in GfxRefToken<FrameBufferId> fbo,
            Vector2D<int> size,
            FrameBufferTarget target, RenderBufferMsaa msaa, out RenderBufferMeta meta)
        {
            var samples = msaa.ToSamples();
            var rboRef = _driver.FrameBuffers.CreateRenderBuffer(target, size, samples > 0, samples);
            _driver.FrameBuffers.AttachRenderBuffer(in fbo.Handle, in rboRef.Handle, target);
            meta = new RenderBufferMeta(size, target, msaa);
            return rboRef;
        }
    }


    private static void GetMetaAndSizeFromDesc(in FrameBufferDesc desc, out FrameBufferMeta meta,
        out Vector2D<int> size)
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