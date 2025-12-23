using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Data;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxFrameBuffers
{
    private readonly GfxResourceDisposer _disposer;

    private readonly GfxResourceStore<FrameBufferId, FrameBufferMeta> _fboStore;
    private readonly GfxResourceStore<RenderBufferId, RenderBufferMeta> _rboStore;
    private readonly GfxResourceStore<TextureId, TextureMeta> _textureStore;

    private readonly GfxTextures _gfxTextures;
    private readonly GlFrameBuffers _driver;


    internal GfxFrameBuffers(GfxContextInternal context, GfxTextures gfxTextures)
    {
        _fboStore = context.Resources.GfxStoreHub.FboStore;
        _rboStore = context.Resources.GfxStoreHub.RboStore;
        _textureStore = context.Resources.GfxStoreHub.TextureStore;

        _disposer = context.Disposer;
        _driver = context.Driver.FrameBuffers;
        _gfxTextures = gfxTextures;
    }

    public FrameBufferId CreateFrameBuffer(in GfxFrameBufferDescriptor desc)
    {
        EnsureCreateFrameBuffer(in desc);
        var size = desc.Size;
        var fboRef = _driver.CreateFrameBuffer();

        var isMultisample = desc.Multisample != RenderBufferMsaa.None;

        FboAttachmentIds attachments = default;
        if (desc.ColorTexture is { } colTex)
        {
            var texKind = !isMultisample ? TextureKind.Texture2D : TextureKind.Multisample2D;
            var texDesc = new GfxTextureDescriptor(size.Width, size.Height,
                texKind, colTex.PixelFormat,
                1, desc.Multisample);

            var texProps = new GfxTextureProperties(
                0f, colTex.TexturePreset, TextureAnisotropy.Off,
                DepthMode.Unset, colTex.ColorBorder
            );

            var textureId = _gfxTextures.BuildEmptyTexture(texDesc, texProps);
            var texRef = _textureStore.GetRefHandle(textureId);
            AttachTexture(fboRef, texRef, FrameBufferAttachmentSlot.Color);
            attachments = attachments with { ColorTextureId = textureId };
        }

        if (desc.DepthTexture is { } depTex)
        {
            var texDesc = new GfxTextureDescriptor(size.Width, size.Height, TextureKind.Texture2D,
                TexturePixelFormat.Depth, 1);
            var texProps = new GfxTextureProperties(0f, depTex.TexturePreset, TextureAnisotropy.Off,
                depTex.CompareTextureFunc, depTex.BorderColor);

            var textureId = _gfxTextures.BuildEmptyTexture(texDesc, texProps);
            var texRef = _textureStore.GetRefHandle(textureId);
            AttachTexture(fboRef, texRef, FrameBufferAttachmentSlot.Depth);
            attachments = attachments with { DepthTextureId = textureId };
        }

        if (desc.ColorBuffer)
        {
            var rboId = CreateAttachRenderBuffer(fboRef, size,
                FrameBufferAttachmentSlot.Color, desc.Multisample, out _);

            attachments = attachments with { ColorRenderBufferId = rboId };
        }

        if (desc.DepthStencilBuffer)
        {
            var rboId = CreateAttachRenderBuffer(fboRef, size,
                FrameBufferAttachmentSlot.DepthStencil, desc.Multisample, out _);
            attachments = attachments with { DepthRenderBufferId = rboId };
        }

        _driver.ValidateComplete(fboRef, desc.ColorTexture is not null);

        var fboMeta = new FrameBufferMeta(size, attachments, desc.Multisample);
        var fboId = _fboStore.Add(in fboMeta, fboRef);
        return fboId;
    }


    public void RecreateSizedFrameBuffer(ReadOnlySpan<(FrameBufferId Id, Size2D Size)> newSizes)
    {
        foreach (var (fboId, size) in newSizes)
            RecreateFrameBuffer(fboId, size);
    }

    public void RecreateFrameBuffer(FrameBufferId fboId, Size2D newSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fboId.Value, 0, nameof(fboId));
        var oldFboRef = _fboStore.GetRefAndMeta(fboId, out var oldMeta);
        _disposer.EnqueueReplace(oldFboRef);

        var newMeta = FrameBufferMeta.MakeResizeCopy(in oldMeta, newSize);
        var fboRef = _driver.CreateFrameBuffer();
        _fboStore.Replace(fboId, in newMeta, fboRef, out _);

        var attachments = newMeta.Attachments;
        if (attachments.ColorTextureId.IsValid())
        {
            var texDes = new GfxReplaceTexture(newSize.Width, newSize.Height);
            var texRef = _gfxTextures.ReplaceTexture(attachments.ColorTextureId, in texDes);
            _gfxTextures.ApplyProperties(attachments.ColorTextureId);
            AttachTexture(fboRef, texRef, FrameBufferAttachmentSlot.Color);
        }

        if (attachments.DepthTextureId.IsValid())
        {
            var texDes = new GfxReplaceTexture(newSize.Width, newSize.Height);
            var texRef = _gfxTextures.ReplaceTexture(attachments.DepthTextureId, in texDes);
            _gfxTextures.ApplyProperties(attachments.DepthTextureId);
            AttachTexture(fboRef, texRef, FrameBufferAttachmentSlot.Depth);
        }

        if (attachments.ColorRenderBufferId.IsValid())
        {
            RecreateAttachRenderBuffer(attachments.ColorRenderBufferId, fboRef, newSize,
                FrameBufferAttachmentSlot.Color, newMeta.MultiSample, out _);
        }

        if (attachments.DepthRenderBufferId.IsValid())
        {
            RecreateAttachRenderBuffer(attachments.DepthRenderBufferId, fboRef, newSize,
                FrameBufferAttachmentSlot.DepthStencil, newMeta.MultiSample, out _);
        }

        _driver.ValidateComplete(fboRef, attachments.ColorTextureId.IsValid());
    }

    private RenderBufferId CreateAttachRenderBuffer(GfxRefToken<FrameBufferId> fbo, Size2D size,
        FrameBufferAttachmentSlot attachmentSlot, RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var samples = msaa.ToSamples();
        var rboRef = _driver.CreateRenderBuffer(attachmentSlot, size, samples);
        _driver.AttachRenderBuffer(fbo, rboRef, attachmentSlot);
        meta = new RenderBufferMeta(size, attachmentSlot, msaa);
        return _rboStore.Add(in meta, rboRef);
    }

    private RenderBufferId RecreateAttachRenderBuffer(RenderBufferId rboId, GfxRefToken<FrameBufferId> fboRef,
        Size2D size, FrameBufferAttachmentSlot attachmentSlot,
        RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var rboRef = _rboStore.GetRefHandle(rboId);
        _disposer.EnqueueReplace(rboRef);

        var samples = msaa.ToSamples();
        var newRboRef = _driver.CreateRenderBuffer(attachmentSlot, size, samples);
        _driver.AttachRenderBuffer(fboRef, newRboRef, attachmentSlot);
        meta = new RenderBufferMeta(size, attachmentSlot, msaa);
        return _rboStore.Replace(rboId, in meta, in newRboRef, out _);
    }

    private void AttachTexture(GfxRefToken<FrameBufferId> fbo, GfxRefToken<TextureId> tex,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)attachmentSlot, (int)FrameBufferAttachmentSlot.DepthStencil,
            nameof(attachmentSlot));

        _driver.AttachTexture(fbo, tex, attachmentSlot);
    }


    private static void EnsureCreateFrameBuffer(in GfxFrameBufferDescriptor desc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.Size.Width, 1, nameof(desc.Size.Width));
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.Size.Height, 1, nameof(desc.Size.Height));


        if (desc.ColorTexture is { } colorTexture)
        {
            if (desc.Size.Width > GfxLimits.MaxTextureSize || desc.Size.Height > GfxLimits.MaxTextureSize)
                throw new GraphicsException($"Texture Size exceeds {GfxLimits.MaxTextureSize}");

            if (colorTexture.PixelFormat is TexturePixelFormat.Depth or TexturePixelFormat.Unknown)
                throw new GraphicsException($"Invalid value for ColorTexture {nameof(desc)}");

            if (desc.Multisample != RenderBufferMsaa.None && colorTexture.TexturePreset != TexturePreset.None)
                throw new GraphicsException($"Multisample require None for {nameof(TexturePreset)}");
        }

        if (desc.DepthTexture is { } depthTexture)
        {
            if (desc.Size.Width > GfxLimits.MaxDepthTextureSize || desc.Size.Height > GfxLimits.MaxDepthTextureSize)
                throw new GraphicsException($"DepthTexture Size exceeds {GfxLimits.MaxDepthTextureSize}");

            if (depthTexture.PixelFormat is not TexturePixelFormat.Depth)
                throw new GraphicsException($"Invalid value for DepthTexture {nameof(desc)}");
        }
    }
}