using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxFrameBuffers
{
    private readonly GfxStoreHub _resources;
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
        var samples = desc.Multisample.ToSamples();

        FboAttachmentIds attachmentIds = default;
        if (desc.Attachments.ColorTexture)
        {
            var textureId = desc.Multisample != RenderBufferMsaa.None
                ? _gfxTextures.CreateTextureMsaa(GpuTextureDescriptor.MakeFboMsaaDesc(size.X, size.Y), desc.Multisample)
                : _gfxTextures.CreateTexture2D(ReadOnlySpan<byte>.Empty,
                    GpuTextureDescriptor.MakeFboColorDesc(size.X, size.Y));

            var texRef = _resources.TextureStore.GetRef(textureId);
            _backend.AttachTexture(in fboRef, in texRef);
            attachmentIds = attachmentIds with { ColorTextureId = textureId };
        }

        if (desc.Attachments.ColorBuffer)
        {
            var rboRef = _backend.CreateAttachRenderBuffer(in fboRef, size,
                FrameBufferTarget.Color, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, in rboRef);
            attachmentIds = attachmentIds with { ColorRenderBufferId = rboId };
        }

        if (desc.Attachments.DepthStencilBuffer)
        {
            var rboRef = _backend.CreateAttachRenderBuffer(in fboRef, size,
                FrameBufferTarget.DepthStencil, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, in rboRef);
            attachmentIds = attachmentIds with { DepthRenderBufferId = rboId };
        }

        _repository.FboRepository.AddRecord(fboId, in attachmentIds, in desc);
        return fboId;
    }

    internal FrameBufferId RecreateAutoResizeFrameBuffer(FrameBufferId fboId, Vector2D<int> outputSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fboId.Value, 0, nameof(fboId));

        var layout = _repository.FboRepository.Get(fboId);
        var attachIds = layout.FboAttachmentResources;
        var oldTokenRef = _resources.FboStore.GetRefAndMeta(fboId, out var oldMeta);
        var newOutputSize = layout.AbsoluteSize == Vector2D<int>.Zero ? outputSize : layout.AbsoluteSize;
        var newSize = CalculateOutputSize(newOutputSize, layout.DownscaleRatio);

        var newMeta = FrameBufferMeta.MakeResizeCopy(in oldMeta, newSize);
        var fboRef = _backend.CreateFrameBuffer();
        _resources.FboStore.Replace(fboId, in newMeta, fboRef, out _);

        if (oldMeta.ColorTexture)
        {
            InvalidOpThrower.ThrowIfNot(attachIds.ColorTextureId.IsValid());
            var texDesc = new GpuTextureDescriptor(newSize.X, newSize.Y,
                layout.TexturePreset, TextureKind.Texture2D);

            _gfxTextures.ReplaceTexture(attachIds.ColorTextureId,
                ReadOnlySpan<byte>.Empty, in texDesc, out var texRef);

            _backend.AttachTexture(in fboRef, in texRef);
        }

        if (oldMeta.ColorBuffer)
        {
            InvalidOpThrower.ThrowIfNot(attachIds.ColorRenderBufferId.IsValid());
            var rbo = _backend.CreateAttachRenderBuffer(in fboRef, newOutputSize,
                FrameBufferTarget.Color, layout.Msaa, out var meta);
            _resources.RboStore.Add(in meta, in rbo);
            _resources.RboStore.Replace(attachIds.ColorRenderBufferId, in meta, in rbo, out _);
        }

        if (oldMeta.DepthStencilBuffer)
        {
            InvalidOpThrower.ThrowIfNot(attachIds.DepthRenderBufferId.IsValid());
            var rbo = _backend.CreateAttachRenderBuffer(in fboRef, newOutputSize,
                FrameBufferTarget.DepthStencil, layout.Msaa, out var meta);
            _resources.RboStore.Replace(attachIds.DepthRenderBufferId, in meta, in rbo, out _);
        }

        _repository.FboRepository.UpdateOutputSize(fboId, newOutputSize);
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
            var rboRef = _driver.FrameBuffers.CreateRenderBuffer(target, size, samples);
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
        var attach = desc.Attachments;
        meta = new FrameBufferMeta(size, attach.ColorTexture, attach.ColorBuffer, attach.ColorBuffer,
            attach.DepthStencilBuffer);
    }

    private static Vector2D<int> CalculateOutputSize(Vector2D<int> size, Vector2 downscaleRatio)
    {
        var (abs, ratio) = (size, downscaleRatio);
        return new Vector2D<int>((int)(abs.X * ratio.X), (int)(abs.Y * ratio.Y));
    }


    private static void EnsureCreateFrameBuffer(in FrameBufferDesc desc)
    {
        ArgumentExceptionThrower.ThrowIf(desc.Attachments.DepthTexture, nameof(desc.Attachments.DepthTexture));
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.Y, 16);
        ArgumentOutOfRangeException.ThrowIfZero(desc.DownscaleRatio.X);
        ArgumentOutOfRangeException.ThrowIfZero(desc.DownscaleRatio.Y);
    }
}