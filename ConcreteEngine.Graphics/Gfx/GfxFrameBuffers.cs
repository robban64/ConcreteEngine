#region

using System.Diagnostics;
using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.OpenGL;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

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

    internal FrameBufferId CreateFrameBuffer(in GfxFrameBufferDescriptor desc)
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

            var texRef = _resources.TextureStore.GetRef(textureId);
            AttachTexture(fboRef, texRef);
            attachments = attachments with { ColorTextureId = textureId };
        }

        if (desc.Attachments.ColorBuffer)
        {
            var rboRef = CreateAttachRenderBuffer(fboRef, size,
                FrameBufferTarget.Color, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, rboRef);
            attachments = attachments with { ColorRenderBufferId = rboId };
        }

        if (desc.Attachments.DepthStencilBuffer)
        {
            var rboRef = CreateAttachRenderBuffer(fboRef, size,
                FrameBufferTarget.DepthStencil, desc.Multisample, out var meta);
            var rboId = _resources.RboStore.Add(in meta, rboRef);
            attachments = attachments with { DepthRenderBufferId = rboId };
        }

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
            AttachTexture(fboRef, texRef);
        }

        if (attachments.ColorRenderBufferId.IsValid())
        {
            InvalidOpThrower.ThrowIfNot(attachments.ColorRenderBufferId.IsValid());
            var rbo = CreateAttachRenderBuffer(fboRef, newSize,
                FrameBufferTarget.Color, newMeta.MultiSample, out var meta);
            _resources.RboStore.Add(in meta, rbo);
            _resources.RboStore.Replace(attachments.ColorRenderBufferId, in meta, rbo, out _);
        }

        if (attachments.DepthRenderBufferId.IsValid())
        {
            InvalidOpThrower.ThrowIfNot(attachments.DepthRenderBufferId.IsValid());
            var rbo = CreateAttachRenderBuffer(fboRef, newSize,
                FrameBufferTarget.DepthStencil, newMeta.MultiSample, out var meta);
            _resources.RboStore.Replace(attachments.DepthRenderBufferId, in meta, rbo, out _);
        }
    }
    
    private GfxRefToken<RenderBufferId> CreateAttachRenderBuffer(GfxRefToken<FrameBufferId> fbo, Size2D size,
        FrameBufferTarget target, RenderBufferMsaa msaa, out RenderBufferMeta meta)
    {
        var samples = msaa.ToSamples();
        var rboRef = _driver.CreateRenderBuffer(target, size, samples);
        _driver.AttachRenderBuffer(fbo, rboRef, target);
        meta = new RenderBufferMeta(size, target, msaa);
        return rboRef;
    }
    
    private void AttachTexture(GfxRefToken<FrameBufferId> fbo, GfxRefToken<TextureId> tex)
    {
        _driver.AttachTexture(fbo, tex, FrameBufferTarget.Color);
    }
    

    private static void EnsureCreateFrameBuffer(in GfxFrameBufferDescriptor desc)
    {
        ArgumentExceptionThrower.ThrowIf(desc.Attachments.DepthTexture, nameof(desc.Attachments.DepthTexture));
        if(desc.Multisample != RenderBufferMsaa.None && desc.TexturePreset != TexturePreset.None)
            throw new InvalidOperationException($"Multisample require None for {nameof(TexturePreset)}");
    }
}