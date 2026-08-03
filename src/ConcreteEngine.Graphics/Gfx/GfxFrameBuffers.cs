using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.OpenGL;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxFrameBuffers
{
    private readonly GfxResourceDisposer _disposer;

    private readonly GfxTextures _gfxTextures;


    internal GfxFrameBuffers(GfxResourceDisposer disposer, GfxTextures gfxTextures)
    {
        _disposer = disposer;
        _gfxTextures = gfxTextures;
    }

    public FrameBufferId CreateFrameBuffer(CreateFboInfo desc)
    {
        EnsureCreateFrameBuffer(in desc);
        var size = desc.Size;
        var fboHandle = GlFrameBuffers.CreateFrameBuffer();

        var isMultisample = desc.Multisample != RenderBufferMsaa.None;

        FboAttachmentIds attachments = default;
        if (!desc.ColorTexture.IsEmpty())
        {
            var texKind = !isMultisample ? TextureKind.Texture2D : TextureKind.Multisample2D;

            var textureId = _gfxTextures.CreateTextureEmpty(size: size.ToSize3D(1),
                kind: texKind,
                format: desc.ColorTexture.PixelFormat,
                samples: desc.Multisample,
                border: desc.ColorTexture.ColorBorder);
            var texRef = GfxRegistry.TextureStore.GetHandle(textureId);
            AttachTexture(fboHandle, texRef, FrameBufferAttachmentSlot.Color);
            attachments = attachments with { ColorTexture = textureId };
        }

        if (!desc.DepthTexture.IsEmpty())
        {
            var textureId = _gfxTextures.CreateTextureEmpty(size: size.ToSize3D(1),
                kind: TextureKind.Texture2D,
                format: TexturePixelFormat.Depth,
                border: desc.ColorTexture.ColorBorder);

            var texRef = GfxRegistry.TextureStore.GetHandle(textureId);
            AttachTexture(fboHandle, texRef, FrameBufferAttachmentSlot.Depth);
            attachments = attachments with { DepthTexture = textureId };
        }

        if (desc.ColorBuffer)
        {
            var rboId = CreateAttachRenderBuffer(fboHandle, size,
                FrameBufferAttachmentSlot.Color, desc.Multisample, out _);

            attachments = attachments with { ColorRbo = rboId };
        }

        if (desc.DepthStencilBuffer)
        {
            var rboId = CreateAttachRenderBuffer(fboHandle, size,
                FrameBufferAttachmentSlot.DepthStencil, desc.Multisample, out _);
            attachments = attachments with { DepthRbo = rboId };
        }

        GlFrameBuffers.ValidateComplete(fboHandle, !desc.ColorTexture.IsEmpty());

        var fboMeta = new FrameBufferMeta(size, attachments, desc.Multisample);
        var fboId = GfxRegistry.FboStore.Add(in fboMeta, fboHandle);
        return fboId;
    }

    public void RecreateFrameBuffer(FrameBufferId fboId, Size2D newSize)
    {
        ArgumentOutOfRangeException.ThrowIfZero(fboId.Id, nameof(fboId));
        var oldFboHandle = GfxRegistry.FboStore.GetHandleAndMeta(fboId, out var oldMeta);
        _disposer.EnqueueReplace(fboId, oldFboHandle);

        var newMeta = FrameBufferMeta.MakeResizeCopy(in oldMeta, newSize);
        var fboHandle = GlFrameBuffers.CreateFrameBuffer();
        GfxRegistry.FboStore.Replace(fboId, in newMeta, fboHandle);

        var attachments = newMeta.Attachments;
        if (attachments.ColorTexture.IsValid())
        {
            var texRef = _gfxTextures.ReplaceTexture(attachments.ColorTexture, newSize.ToSize3D(1));
            _gfxTextures.ApplyProperties(attachments.ColorTexture);
            AttachTexture(fboHandle, texRef, FrameBufferAttachmentSlot.Color);
        }

        if (attachments.DepthTexture.IsValid())
        {
            var texRef = _gfxTextures.ReplaceTexture(attachments.DepthTexture, newSize.ToSize3D(1));
            _gfxTextures.ApplyProperties(attachments.DepthTexture);
            AttachTexture(fboHandle, texRef, FrameBufferAttachmentSlot.Depth);
        }

        if (attachments.ColorRbo.IsValid())
        {
            RecreateAttachRenderBuffer(attachments.ColorRbo, fboHandle, newSize,
                FrameBufferAttachmentSlot.Color, newMeta.MultiSample, out _);
        }

        if (attachments.DepthRbo.IsValid())
        {
            RecreateAttachRenderBuffer(attachments.DepthRbo, fboHandle, newSize,
                FrameBufferAttachmentSlot.DepthStencil, newMeta.MultiSample, out _);
        }

        GlFrameBuffers.ValidateComplete(fboHandle, attachments.ColorTexture.IsValid());
    }

    private GfxId<RenderBufferMeta> CreateAttachRenderBuffer(NativeHandle fbo, Size2D size,
        FrameBufferAttachmentSlot attachmentSlot, RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var samples = msaa.ToSamples();
        var rboHandle = GlFrameBuffers.CreateRenderBuffer(attachmentSlot, size, samples);
        GlFrameBuffers.AttachRenderBuffer(fbo, rboHandle, attachmentSlot);
        meta = new RenderBufferMeta(size, attachmentSlot, msaa);
        return GfxRegistry.RboStore.Add(in meta, rboHandle);
    }

    private GfxId<RenderBufferMeta> RecreateAttachRenderBuffer(GfxId<RenderBufferMeta> rboId, NativeHandle fboHandle,
        Size2D size, FrameBufferAttachmentSlot attachmentSlot, RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var rboHandle = GfxRegistry.RboStore.GetHandle(rboId);
        _disposer.EnqueueReplace(rboId, rboHandle);

        var newRboHandle = GlFrameBuffers.CreateRenderBuffer(attachmentSlot, size, msaa.ToSamples());
        GlFrameBuffers.AttachRenderBuffer(fboHandle, newRboHandle, attachmentSlot);
        
        meta = new RenderBufferMeta(size, attachmentSlot, msaa);
        return GfxRegistry.RboStore.Replace(rboId, in meta, newRboHandle);
    }

    private void AttachTexture(NativeHandle fbo, NativeHandle tex,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        ArgumentOutOfRangeException.ThrowIfEqual((int)attachmentSlot, (int)FrameBufferAttachmentSlot.DepthStencil,
            nameof(attachmentSlot));

        GlFrameBuffers.AttachTexture(fbo, tex, attachmentSlot);
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void EnsureCreateFrameBuffer(in CreateFboInfo desc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.Size.Width, 1, nameof(desc.Size.Width));
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.Size.Height, 1, nameof(desc.Size.Height));

        if (!desc.ColorTexture.IsEmpty() )
        {
            if (desc.Size.Width > GfxLimits.MaxTextureSize || desc.Size.Height > GfxLimits.MaxTextureSize)
                throw new GraphicsException($"Texture Size exceeds {GfxLimits.MaxTextureSize}");

            if (desc.ColorTexture.PixelFormat is TexturePixelFormat.Depth or TexturePixelFormat.Unknown)
                throw new GraphicsException($"Invalid value for ColorTexture {nameof(desc)}");

            if (desc.Multisample != RenderBufferMsaa.None && desc.ColorTexture.TexturePreset != TexturePreset.None)
                throw new GraphicsException($"Multisample require None for {nameof(TexturePreset)}");
        }

        if (!desc.DepthTexture.IsEmpty())
        {
            if (desc.Size.Width > GfxLimits.MaxDepthTextureSize || desc.Size.Height > GfxLimits.MaxDepthTextureSize)
                throw new GraphicsException($"DepthTexture Size exceeds {GfxLimits.MaxDepthTextureSize}");

            if (desc.DepthTexture.PixelFormat is not TexturePixelFormat.Depth)
                throw new GraphicsException($"Invalid value for DepthTexture {nameof(desc)}");
        }
    }
}