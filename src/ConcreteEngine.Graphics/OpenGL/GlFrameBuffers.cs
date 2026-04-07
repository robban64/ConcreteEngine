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
    private readonly BackendResourceStore<GlHandle> _fboStore;
    private readonly BackendResourceStore<GlHandle> _rboStore;
    private readonly BackendResourceStore<GlHandle> _textureStore;

    internal GlFrameBuffers(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _fboStore = ctx.Store.FboStore;
        _rboStore = ctx.Store.RboStore;
        _textureStore = ctx.Store.TextureStore;
    }


    // Fix ClearBufferMask and Filter, depth/stencil use filter = Nearest
    public void Blit(GfxHandle readFbo, GfxHandle drawFbo,
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

    public void BlitDefault(GfxHandle readFbo, Size2D srcSize, Size2D dstSize, bool linear)
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

    public GfxHandle CreateFrameBuffer()
    {
        _gl.CreateFramebuffers(1, out uint fbo);
        return _fboStore.Add(new GlHandle(fbo));
    }

    public GfxHandle CreateRenderBuffer(FrameBufferAttachmentSlot attachment, Size2D size,
        int samples)
    {
        var internalFormat = attachment.ToGlInternalFormatEnum();
        var (width, height) = size.ToUnsigned();

        _gl.CreateRenderbuffers(1, out uint rbo);
        if (samples > 0)
            _gl.NamedRenderbufferStorageMultisample(rbo, (uint)samples, internalFormat, width, height);
        else
            _gl.NamedRenderbufferStorage(rbo, internalFormat, width, height);

        return _rboStore.Add(new GlHandle(rbo));
    }

    public void AttachTexture(GfxHandle fboRef, GfxHandle texture,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        var fboH = _fboStore.GetHandle(fboRef);
        var texH = _textureStore.GetHandle(texture);
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        _gl.NamedFramebufferTexture(fboH, glAttachment, texH, 0);
    }

    public void AttachRenderBuffer(GfxHandle fboRef, GfxHandle rboRef,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        var fboHandle = _fboStore.GetHandle(fboRef);
        var rboHandle = _rboStore.GetHandle(rboRef);
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        _gl.NamedFramebufferRenderbuffer(fboHandle, glAttachment, RenderbufferTarget.Renderbuffer, rboHandle);
    }

    public void SetDrawReadBuffer(GfxHandle fboRef, bool colorAttachment)
    {
        var handle = _fboStore.GetHandle(fboRef);
        var glEnum = colorAttachment ? GLEnum.ColorAttachment0 : GLEnum.None;
        _gl.NamedFramebufferDrawBuffer(handle, glEnum);
        _gl.NamedFramebufferReadBuffer(handle, glEnum);
    }

    public void ValidateComplete(GfxHandle fboRef, bool colorAttachment)
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