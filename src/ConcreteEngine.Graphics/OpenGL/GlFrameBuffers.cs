using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Handles;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlFrameBuffers : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly GlCapabilities _capabilities;
    private readonly BackendResourceStore<FrameBufferId, GlFboHandle> _fboStore;
    private readonly BackendResourceStore<RenderBufferId, GlRboHandle> _rboStore;
    private readonly BackendResourceStore<TextureId, GlTextureHandle> _textureStore;


    internal GlFrameBuffers(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _capabilities = ctx.Capabilities;
        _fboStore = ctx.Store.FrameBuffer;
        _rboStore = ctx.Store.RenderBuffer;
        _textureStore = ctx.Store.Texture;
    }


    // Fix ClearBufferMask and Filter, depth/stencil use filter = Nearest
    public void Blit(GfxRefToken<FrameBufferId> readFbo, GfxRefToken<FrameBufferId> drawFbo,
        Size2D srcSize, Size2D dstSize, bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        var read = _fboStore.GetHandle(readFbo);
        var draw = _fboStore.GetHandle(drawFbo);
        _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
        _gl.DrawBuffer(DrawBufferMode.ColorAttachment0);

        _gl.BlitNamedFramebuffer(
            read, draw,
            0, 0, srcSize.Width, srcSize.Height,
            0, 0, dstSize.Width, dstSize.Height,
            ClearBufferMask.ColorBufferBit, filter
        );
    }

    public void BlitDefault(GfxRefToken<FrameBufferId> readFbo, Size2D srcSize, Size2D dstSize, bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        var read = _fboStore.GetHandle(readFbo);
        //_gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
        //_gl.DrawBuffer(DrawBufferMode.ColorAttachment0);

        _gl.BlitNamedFramebuffer(
            read, 0,
            0, 0, srcSize.Width, srcSize.Height,
            0, 0, dstSize.Width, dstSize.Height,
            ClearBufferMask.ColorBufferBit, filter
        );
    }

    public GfxRefToken<FrameBufferId> CreateFrameBuffer()
    {
        _gl.CreateFramebuffers(1, out uint fbo);
        return _fboStore.Add(new GlFboHandle(fbo));
    }

    public GfxRefToken<RenderBufferId> CreateRenderBuffer(FrameBufferAttachmentSlot attachment, Size2D size,
        int samples)
    {
        var internalFormat = attachment.ToGlInternalFormatEnum();
        var (width, height) = size.ToUnsigned();

        _gl.CreateRenderbuffers(1, out uint rbo);
        if (samples > 0)
            _gl.NamedRenderbufferStorageMultisample(rbo, (uint)samples, internalFormat, width, height);
        else
            _gl.NamedRenderbufferStorage(rbo, internalFormat, width, height);

        return _rboStore.Add(new GlRboHandle(rbo));
    }

    public void AttachTexture(GfxRefToken<FrameBufferId> fboRef, GfxRefToken<TextureId> texture,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        var fboH = _fboStore.GetHandle(fboRef);
        var texH = _textureStore.GetHandle(texture);
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        _gl.NamedFramebufferTexture(fboH, glAttachment, texH, 0);
    }

    public void AttachRenderBuffer(GfxRefToken<FrameBufferId> fboRef, GfxRefToken<RenderBufferId> rboRef,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        var fboHandle = _fboStore.GetHandle(fboRef);
        var rboHandle = _rboStore.GetHandle(rboRef);
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        _gl.NamedFramebufferRenderbuffer(fboHandle, glAttachment, RenderbufferTarget.Renderbuffer, rboHandle);
    }

    public void SetDrawReadBuffer(GfxRefToken<FrameBufferId> fboRef, bool colorAttachment)
    {
        var handle = _fboStore.GetHandle(fboRef);
        var glEnum = colorAttachment ? GLEnum.ColorAttachment0 : GLEnum.None;
        _gl.NamedFramebufferDrawBuffer(handle, glEnum);
        _gl.NamedFramebufferReadBuffer(handle, glEnum);
    }

    public void ValidateComplete(GfxRefToken<FrameBufferId> fboRef, bool colorAttachment)
    {
        var handle = _fboStore.GetHandle(fboRef);
        var glEnum = colorAttachment ? GLEnum.ColorAttachment0 : GLEnum.None;
        _gl.NamedFramebufferDrawBuffer(handle, glEnum);
        _gl.NamedFramebufferReadBuffer(handle, glEnum);

        var status = _gl.CheckNamedFramebufferStatus(handle, GLEnum.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            GraphicsException.ThrowFramebufferIncomplete(nameof(fboRef), status.ToString());
    }
}