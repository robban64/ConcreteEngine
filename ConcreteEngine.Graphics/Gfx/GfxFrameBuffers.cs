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

    internal GfxFrameBuffers(GfxContextInternal context, GfxTextures gfxTextures)
    {
        _driver = context.Driver.FrameBuffers;
        _gfxTextures = gfxTextures;
        _resources = context.Stores;
    }

    public FrameBufferId CreateFrameBuffer(in GfxFrameBufferDescriptor desc)
    {
        EnsureCreateFrameBuffer(in desc);
        var size = desc.Size;
        var fboRef = _driver.CreateFrameBuffer();

        var isMultisample = desc.Multisample != RenderBufferMsaa.None;

        FboAttachmentIds attachments = default;
        if (desc.Attachments.ColorTexture)
        {
            var texKind = !isMultisample ? TextureKind.Texture2D : TextureKind.Multisample2D;
            var texDesc = new GfxTextureDescriptor(size.Width, size.Height,
                texKind, desc.PixelFormat,
                1, desc.Multisample);

            var texProps = new GfxTextureProperties(desc.TexturePreset, 0, 0);

            var textureId = _gfxTextures.BuildEmptyTexture(texDesc, texProps);
            var texRef = _resources.TextureStore.GetRefHandle(textureId);
            AttachTexture(fboRef, texRef, FrameBufferAttachmentKind.Color);
            attachments = attachments with { ColorTextureId = textureId };
        }

        if (desc.Attachments.DepthTexture)
        {
            var texDesc = new GfxTextureDescriptor(size.Width, size.Height, TextureKind.Texture2D,
                GfxPixelFormat.Depth, 1);
            var texProps = new GfxTextureProperties(TexturePreset.LinearClampBorder, 0, 0);
            var textureId = _gfxTextures.BuildEmptyTexture(texDesc, texProps);
            _gfxTextures.ApplyCompareAndBorder(textureId);
            var texRef = _resources.TextureStore.GetRefHandle(textureId);
            AttachTexture(fboRef, texRef, FrameBufferAttachmentKind.Depth);
            attachments = attachments with { DepthTextureId = textureId };
        }

        if (desc.Attachments.ColorBuffer)
        {
            var rboRef = CreateAttachRenderBuffer(fboRef, size,
                FrameBufferAttachmentKind.Color, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, rboRef);
            attachments = attachments with { ColorRenderBufferId = rboId };
        }

        if (desc.Attachments.DepthStencilBuffer)
        {
            var rboRef = CreateAttachRenderBuffer(fboRef, size,
                FrameBufferAttachmentKind.DepthStencil, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, rboRef);
            attachments = attachments with { DepthRenderBufferId = rboId };
        }
        
        _driver.ValidateComplete(fboRef, desc.Attachments.ColorTexture);

        var fboMeta = new FrameBufferMeta(size, attachments, desc.Multisample);
        var fboId = _resources.FboStore.Add(in fboMeta, fboRef);
        return fboId;
    }

    internal void RecreateFrameBuffer(FrameBufferId fboId, Size2D newSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fboId.Value, 0, nameof(fboId));

        var oldTokenRef = _resources.FboStore.GetRefAndMeta(fboId, out var oldMeta);
        var newMeta = FrameBufferMeta.MakeResizeCopy(in oldMeta, newSize);
        var fboRef = _driver.CreateFrameBuffer();
        _resources.FboStore.Replace(fboId, in newMeta, fboRef, out _);

        var attachments = newMeta.Attachments;
        if (attachments.ColorTextureId.IsValid())
        {
            var texDes = new GfxReplaceTexture(newSize.Width, newSize.Height);
            var texRef = _gfxTextures.ReplaceTexture(attachments.ColorTextureId, in texDes);
            _gfxTextures.ApplyProperties(attachments.ColorTextureId);
            AttachTexture(fboRef, texRef, FrameBufferAttachmentKind.Color);
        }
        
        if (attachments.DepthTextureId.IsValid())
        {
            var texDes = new GfxReplaceTexture(newSize.Width, newSize.Height);
            var texRef = _gfxTextures.ReplaceTexture(attachments.DepthTextureId, in texDes);
            _gfxTextures.ApplyProperties(attachments.DepthTextureId);
            AttachTexture(fboRef, texRef, FrameBufferAttachmentKind.Depth);
        }

        if (attachments.ColorRenderBufferId.IsValid())
        {
            InvalidOpThrower.ThrowIfNot(attachments.ColorRenderBufferId.IsValid());
            var rbo = CreateAttachRenderBuffer(fboRef, newSize,
                FrameBufferAttachmentKind.Color, newMeta.MultiSample, out var meta);
            _resources.RboStore.Add(in meta, rbo);
            _resources.RboStore.Replace(attachments.ColorRenderBufferId, in meta, rbo, out _);
        }

        if (attachments.DepthRenderBufferId.IsValid())
        {
            InvalidOpThrower.ThrowIfNot(attachments.DepthRenderBufferId.IsValid());
            var rbo = CreateAttachRenderBuffer(fboRef, newSize,
                FrameBufferAttachmentKind.DepthStencil, newMeta.MultiSample, out var meta);
            _resources.RboStore.Replace(attachments.DepthRenderBufferId, in meta, rbo, out _);
        }
        
        _driver.ValidateComplete(fboRef, attachments.ColorTextureId.IsValid());

    }

    private GfxRefToken<RenderBufferId> CreateAttachRenderBuffer(GfxRefToken<FrameBufferId> fbo, Size2D size,
        FrameBufferAttachmentKind attachmentKind, RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var samples = msaa.ToSamples();
        var rboRef = _driver.CreateRenderBuffer(attachmentKind, size, samples);
        _driver.AttachRenderBuffer(fbo, rboRef, attachmentKind);
        meta = new RenderBufferMeta(size, attachmentKind, msaa);
        return rboRef;
    }

    private void AttachTexture(GfxRefToken<FrameBufferId> fbo, GfxRefToken<TextureId> tex, FrameBufferAttachmentKind attachmentKind)
    {
        _driver.AttachTexture(fbo, tex, attachmentKind);
    }


    private static void EnsureCreateFrameBuffer(in GfxFrameBufferDescriptor desc)
    {
        if (desc.Multisample != RenderBufferMsaa.None && desc.TexturePreset != TexturePreset.None)
            throw new InvalidOperationException($"Multisample require None for {nameof(TexturePreset)}");
    }
}