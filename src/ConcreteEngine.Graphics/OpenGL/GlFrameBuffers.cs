using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlFrameBuffers
{
    private readonly GL _gl = GlBackendDriver.Gl;
    private readonly BackendResourceStore _fboStore = GfxRegistry.GetBackendStore<FrameBufferMeta>();
    private readonly BackendResourceStore _rboStore =  GfxRegistry.GetBackendStore<RenderBufferMeta>();
    private readonly BackendResourceStore _textureStore = GfxRegistry.GetBackendStore<TextureMeta>();


    // Fix ClearBufferMask and Filter, depth/stencil use filter = Nearest
    public void Blit(GfxHandle readFbo, GfxHandle drawFbo,
        Size2D srcSize, Size2D dstSize, bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        var read = _fboStore.Get(readFbo);
        var draw = _fboStore.Get(drawFbo);
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
        var read = _fboStore.Get(readFbo);
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
        return _fboStore.Add(new NativeHandle(fbo));
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

        return _rboStore.Add(new NativeHandle(rbo));
    }

    public void AttachTexture(GfxHandle fboRef, GfxHandle texture,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        var fboH = _fboStore.Get(fboRef);
        var texH = _textureStore.Get(texture);
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        _gl.NamedFramebufferTexture(fboH, glAttachment, texH, 0);
    }

    public void AttachRenderBuffer(GfxHandle fboRef, GfxHandle rboRef,
        FrameBufferAttachmentSlot attachmentSlot)
    {
        var fboHandle = _fboStore.Get(fboRef);
        var rboHandle = _rboStore.Get(rboRef);
        var glAttachment = attachmentSlot.ToGlAttachmentEnum();
        _gl.NamedFramebufferRenderbuffer(fboHandle, glAttachment, RenderbufferTarget.Renderbuffer, rboHandle);
    }

    public void SetDrawReadBuffer(GfxHandle fboRef, bool colorAttachment)
    {
        var handle = _fboStore.Get(fboRef);
        var glEnum = colorAttachment ? GLEnum.ColorAttachment0 : GLEnum.None;
        _gl.NamedFramebufferDrawBuffer(handle, glEnum);
        _gl.NamedFramebufferReadBuffer(handle, glEnum);
    }

    public void ValidateComplete(GfxHandle fboRef, bool colorAttachment)
    {
        var handle = _fboStore.Get(fboRef);
        var glEnum = colorAttachment ? GLEnum.ColorAttachment0 : GLEnum.None;
        _gl.NamedFramebufferDrawBuffer(handle, glEnum);
        _gl.NamedFramebufferReadBuffer(handle, glEnum);

        var status = _gl.CheckNamedFramebufferStatus(handle, GLEnum.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            GraphicsException.ThrowFramebufferIncomplete(nameof(fboRef), status.ToString());
    }
}