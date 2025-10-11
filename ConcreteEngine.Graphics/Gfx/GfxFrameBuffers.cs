#region

using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxFrameBuffers
{
    private readonly GfxStoreHub _resources;
    private readonly GfxTextures _gfxTextures;
    private readonly GlFrameBuffers _driver;

    private GraphicsConfiguration Configuration { get; }

    internal GfxFrameBuffers(GfxContextInternal context, GfxTextures gfxTextures)
    {
        _driver = context.Driver.FrameBuffers;
        _gfxTextures = gfxTextures;
        _resources = context.Stores;

        Configuration = context.Driver.Configuration;
    }

    public FrameBufferId CreateFrameBuffer(in GfxFrameBufferDescriptor desc)
    {
        EnsureCreateFrameBuffer(Configuration, in desc);
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
            var texRef = _resources.TextureStore.GetRefHandle(textureId);
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
            var texRef = _resources.TextureStore.GetRefHandle(textureId);
            AttachTexture(fboRef, texRef, FrameBufferAttachmentSlot.Depth);
            attachments = attachments with { DepthTextureId = textureId };
        }

        if (desc.ColorBuffer)
        {
            var rboRef = CreateAttachRenderBuffer(fboRef, size,
                FrameBufferAttachmentSlot.Color, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, rboRef);
            attachments = attachments with { ColorRenderBufferId = rboId };
        }

        if (desc.DepthStencilBuffer)
        {
            var rboRef = CreateAttachRenderBuffer(fboRef, size,
                FrameBufferAttachmentSlot.DepthStencil, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, rboRef);
            attachments = attachments with { DepthRenderBufferId = rboId };
        }

        _driver.ValidateComplete(fboRef, desc.ColorTexture is not null);

        var fboMeta = new FrameBufferMeta(size, attachments, desc.Multisample);
        var fboId = _resources.FboStore.Add(in fboMeta, fboRef);
        return fboId;
    }

    internal void RecreateFrameBuffer(FrameBufferId fboId, Size2D newSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fboId.Value, 0, nameof(fboId));

        _resources.FboStore.GetRefAndMeta(fboId, out var oldMeta);
        var newMeta = FrameBufferMeta.MakeResizeCopy(in oldMeta, newSize);
        var fboRef = _driver.CreateFrameBuffer();
        _resources.FboStore.Replace(fboId, in newMeta, fboRef, out _);

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
            InvalidOpThrower.ThrowIfNot(attachments.ColorRenderBufferId.IsValid());
            var rbo = CreateAttachRenderBuffer(fboRef, newSize,
                FrameBufferAttachmentSlot.Color, newMeta.MultiSample, out var meta);
            _resources.RboStore.Add(in meta, rbo);
            _resources.RboStore.Replace(attachments.ColorRenderBufferId, in meta, rbo, out _);
        }

        if (attachments.DepthRenderBufferId.IsValid())
        {
            InvalidOpThrower.ThrowIfNot(attachments.DepthRenderBufferId.IsValid());
            var rbo = CreateAttachRenderBuffer(fboRef, newSize,
                FrameBufferAttachmentSlot.DepthStencil, newMeta.MultiSample, out var meta);
            _resources.RboStore.Replace(attachments.DepthRenderBufferId, in meta, rbo, out _);
        }

        _driver.ValidateComplete(fboRef, attachments.ColorTextureId.IsValid());
    }

    private GfxRefToken<RenderBufferId> CreateAttachRenderBuffer(GfxRefToken<FrameBufferId> fbo, Size2D size,
        FrameBufferAttachmentSlot attachmentSlot, RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var samples = msaa.ToSamples();
        var rboRef = _driver.CreateRenderBuffer(attachmentSlot, size, samples);
        _driver.AttachRenderBuffer(fbo, rboRef, attachmentSlot);
        meta = new RenderBufferMeta(size, attachmentSlot, msaa);
        return rboRef;
    }

    private void AttachTexture(GfxRefToken<FrameBufferId> fbo, GfxRefToken<TextureId> tex,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        _driver.AttachTexture(fbo, tex, attachmentSlot);
    }


    private static void EnsureCreateFrameBuffer(GraphicsConfiguration config, in GfxFrameBufferDescriptor desc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.Size.Width, 1, nameof(desc.Size.Width));
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.Size.Height, 1, nameof(desc.Size.Height));


        if (desc.ColorTexture is { } colorTexture)
        {
            if (desc.Size.Width > config.MaxTextureSize || desc.Size.Height > config.MaxTextureSize)
                throw new ArgumentOutOfRangeException(nameof(desc.Size),
                    $"Texture Size exceeds {config.MaxTextureSize}");

            if (colorTexture.PixelFormat is TexturePixelFormat.Depth or TexturePixelFormat.Unknown)
                throw new InvalidOperationException($"Invalid value for ColorTexture {nameof(desc)}");

            if (desc.Multisample != RenderBufferMsaa.None && colorTexture.TexturePreset != TexturePreset.None)
                throw new InvalidOperationException($"Multisample require None for {nameof(TexturePreset)}");
        }

        if (desc.DepthTexture is { } depthTexture)
        {
            if (desc.Size.Width > config.MaxDepthTextureSize || desc.Size.Height > config.MaxDepthTextureSize)
                throw new ArgumentOutOfRangeException(nameof(desc.Size),
                    $"DepthTexture Size exceeds {config.MaxDepthTextureSize}");

            if (depthTexture.PixelFormat is not TexturePixelFormat.Depth)
                throw new InvalidOperationException($"Invalid value for DepthTexture {nameof(desc)}");
        }
    }
}