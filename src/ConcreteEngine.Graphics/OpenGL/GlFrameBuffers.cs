using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlFrameBuffers
{
    private readonly GL _gl = GlBackendDriver.Gl;

    // Fix ClearBufferMask and Filter, depth/stencil use filter = Nearest
    public void Blit(GfxHandle readFbo, GfxHandle drawFbo,
        Size2D srcSize, Size2D dstSize, bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
        _gl.DrawBuffer(DrawBufferMode.ColorAttachment0);

        _gl.BlitNamedFramebuffer(
            readFbo, drawFbo,
            0, 0, srcSize.Width, srcSize.Height,
            0, 0, dstSize.Width, dstSize.Height,
            ClearBufferMask.ColorBufferBit, filter
        );
    }

    public void BlitDefault(GfxHandle readFbo, Size2D srcSize, Size2D dstSize, bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        //_gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
        //_gl.DrawBuffer(DrawBufferMode.ColorAttachment0);

        _gl.BlitNamedFramebuffer(
            readFbo, 0,
            0, 0, srcSize.Width, srcSize.Height,
            0, 0, dstSize.Width, dstSize.Height,
            ClearBufferMask.ColorBufferBit, filter
        );
    }

    public GfxHandle CreateFrameBuffer()
    {
        _gl.CreateFramebuffers(1, out uint fbo);
        return new GfxHandle(fbo);
    }

    public GfxHandle CreateRenderBuffer(FrameBufferAttachmentSlot attachment, Size2D size, int samples)
    {
        var internalFormat = attachment.ToGlInternalFormatEnum();
        var (width, height) = size.ToUnsigned();

        _gl.CreateRenderbuffers(1, out uint rbo);
        if (samples > 0)
            _gl.NamedRenderbufferStorageMultisample(rbo, (uint)samples, internalFormat, width, height);
        else
            _gl.NamedRenderbufferStorage(rbo, internalFormat, width, height);

        return new GfxHandle(rbo);
    }

    public void AttachTexture(GfxHandle fboHandle, GfxHandle textureHandle, FrameBufferAttachmentSlot attachmentSlot)
    {
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        _gl.NamedFramebufferTexture(fboHandle, glAttachment, textureHandle, 0);
    }

    public void AttachRenderBuffer(GfxHandle fboHandle, GfxHandle rboHandle,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        _gl.NamedFramebufferRenderbuffer(fboHandle, glAttachment, RenderbufferTarget.Renderbuffer, rboHandle);
    }

    public void SetDrawReadBuffer(GfxHandle fboHandle, bool colorAttachment)
    {
        var glEnum = colorAttachment ? GLEnum.ColorAttachment0 : GLEnum.None;
        _gl.NamedFramebufferDrawBuffer(fboHandle, glEnum);
        _gl.NamedFramebufferReadBuffer(fboHandle, glEnum);
    }

    public void ValidateComplete(GfxHandle fboHandle, bool colorAttachment)
    {
        var glEnum = colorAttachment ? GLEnum.ColorAttachment0 : GLEnum.None;
        _gl.NamedFramebufferDrawBuffer(fboHandle, glEnum);
        _gl.NamedFramebufferReadBuffer(fboHandle, glEnum);

        var status = _gl.CheckNamedFramebufferStatus(fboHandle, GLEnum.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            GraphicsException.ThrowFramebufferIncomplete(nameof(fboHandle), status.ToString());
    }
}