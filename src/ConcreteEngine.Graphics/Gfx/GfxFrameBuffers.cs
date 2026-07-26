using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Configuration;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internals;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Graphics.Gfx;

public sealed class GfxFrameBuffers
{
    private readonly GfxResourceDisposer _disposer;

    private readonly FboStore _fboStore;
    private readonly RboStore _rboStore;
    private readonly TextureStore _textureStore;

    private readonly GfxTextures _gfxTextures;


    internal GfxFrameBuffers(GfxContextInternal context, GfxTextures gfxTextures)
    {
        _fboStore = GfxRegistry.GetStore<FrameBufferMeta>();
        _rboStore = GfxRegistry.GetStore<RenderBufferMeta>();
        _textureStore = GfxRegistry.GetStore<TextureMeta>();

        _disposer = context.Disposer;
        _gfxTextures = gfxTextures;
    }

    public FrameBufferId CreateFrameBuffer(in CreateFboInfo desc)
    {
        EnsureCreateFrameBuffer(in desc);
        var size = desc.Size;
        var fboHandle = GlFrameBuffers.CreateFrameBuffer();

        var isMultisample = desc.Multisample != RenderBufferMsaa.None;

        FboAttachmentIds attachments = default;
        if (desc.ColorTexture is { } colTex)
        {
            var texKind = !isMultisample ? TextureKind.Texture2D : TextureKind.Multisample2D;
            var texProps = new CreateTextureProps(
                0f, texKind, colTex.PixelFormat,
                colTex.TexturePreset, TextureAnisotropy.Off,
                DepthMode.Unset, colTex.ColorBorder, desc.Multisample
            );

            var textureId = _gfxTextures.CreateTextureEmpty(size.ToSize3D(1), texProps);
            var texRef = _textureStore.GetHandle(textureId);
            AttachTexture(fboHandle, texRef, FrameBufferAttachmentSlot.Color);
            attachments = attachments with { ColorTexture = textureId };
        }

        if (desc.DepthTexture is { } depTex)
        {
            var texProps = new CreateTextureProps(
                0f, TextureKind.Texture2D, TexturePixelFormat.Depth,
                depTex.TexturePreset, TextureAnisotropy.Off,
                depTex.CompareTextureFunc, depTex.BorderColor);

            var textureId = _gfxTextures.CreateTextureEmpty(size.ToSize3D(1), texProps);
            var texRef = _textureStore.GetHandle(textureId);
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

        GlFrameBuffers.ValidateComplete(fboHandle, desc.ColorTexture is not null);

        var fboMeta = new FrameBufferMeta(size, attachments, desc.Multisample);
        var fboId = _fboStore.Add(in fboMeta, fboHandle);
        return fboId;
    }

    public void RecreateFrameBuffer(FrameBufferId fboId, Size2D newSize)
    {
        ArgumentOutOfRangeException.ThrowIfZero(fboId.Id, nameof(fboId));
        var oldFboHandle = _fboStore.GetHandleAndMeta(fboId, out var oldMeta);
        _disposer.EnqueueReplace(fboId, oldFboHandle);

        var newMeta = FrameBufferMeta.MakeResizeCopy(in oldMeta, newSize);
        var fboHandle = GlFrameBuffers.CreateFrameBuffer();
        _fboStore.Replace(fboId, in newMeta, fboHandle);

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

    private RenderBufferId CreateAttachRenderBuffer(GfxHandle fbo, Size2D size,
        FrameBufferAttachmentSlot attachmentSlot, RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var samples = msaa.ToSamples();
        var rboHandle = GlFrameBuffers.CreateRenderBuffer(attachmentSlot, size, samples);
        GlFrameBuffers.AttachRenderBuffer(fbo, rboHandle, attachmentSlot);
        meta = new RenderBufferMeta(size, attachmentSlot, msaa);
        return _rboStore.Add(in meta, rboHandle);
    }

    private RenderBufferId RecreateAttachRenderBuffer(RenderBufferId rboId, GfxHandle fboHandle,
        Size2D size, FrameBufferAttachmentSlot attachmentSlot, RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var rboHandle = _rboStore.GetHandle(rboId);
        _disposer.EnqueueReplace(rboId, rboHandle);

        var newRboHandle = GlFrameBuffers.CreateRenderBuffer(attachmentSlot, size, msaa.ToSamples());
        GlFrameBuffers.AttachRenderBuffer(fboHandle, newRboHandle, attachmentSlot);
        
        meta = new RenderBufferMeta(size, attachmentSlot, msaa);
        return _rboStore.Replace(rboId, in meta, newRboHandle);
    }

    private void AttachTexture(GfxHandle fbo, GfxHandle tex,
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