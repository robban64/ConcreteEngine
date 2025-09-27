#region

using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.OpenGL.Utilities;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

#endregion

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlFrameBuffers : IGraphicsDriverModule
{
    private readonly GL _gl;
    private readonly BackendOpsHub _store;
    private readonly GlCapabilities _capabilities;


    internal GlFrameBuffers(GlCtx ctx)
    {
        _gl = ctx.Gl;
        _capabilities = ctx.Capabilities;
        _store = ctx.Store;
    }

    private GlFboHandle GetFboHandle(GfxRefToken<FrameBufferId> fboRef) => _store.FrameBuffer.GetRef(fboRef);
    private GlRboHandle GetRboHandle(GfxRefToken<RenderBufferId> rboRef) => _store.RenderBuffer.GetRef(rboRef);
    private GlTextureHandle GetTextureHandle(GfxRefToken<TextureId> texRef) => _store.Texture.GetRef(texRef);


    // Fix ClearBufferMask and Filter, depth/stencil use filter = Nearest
    public void Blit(GfxRefToken<FrameBufferId> readFbo, GfxRefToken<FrameBufferId> drawFbo,
        Size2D srcSize, Size2D dstSize, bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        var read = GetFboHandle(readFbo).Handle;
        var draw = GetFboHandle(drawFbo).Handle;
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
        var read = GetFboHandle(readFbo).Handle;
        _gl.ReadBuffer(ReadBufferMode.ColorAttachment0);
        _gl.DrawBuffer(DrawBufferMode.ColorAttachment0);

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
        return _store.FrameBuffer.Add(new GlFboHandle(fbo));
    }

    public GfxRefToken<RenderBufferId> CreateRenderBuffer(FrameBufferTarget attachment, Size2D size, int samples)
    {
        var internalFormat = attachment.ToGlInternalFormatEnum();
        var (width, height) = size.ToUnsigned();

        _gl.CreateRenderbuffers(1, out uint rbo);
        if (samples > 0)
            _gl.NamedRenderbufferStorageMultisample(rbo, (uint)samples, internalFormat, width, height);
        else
            _gl.NamedRenderbufferStorage(rbo, internalFormat, width, height);

        return _store.RenderBuffer.Add(new GlRboHandle(rbo));
    }

    public void AttachTexture(GfxRefToken<FrameBufferId> fboRef, GfxRefToken<TextureId> texture,
        FrameBufferTarget target)
    {
        var fboH = GetFboHandle(fboRef).Handle;
        var texH = GetTextureHandle(texture).Handle;
        var glAttachment = target.ToGlAttachmentEnum();
        _gl.NamedFramebufferTexture(fboH, glAttachment, texH, 0);
    }

    public void AttachRenderBuffer(GfxRefToken<FrameBufferId> fboRef, GfxRefToken<RenderBufferId> rboRef,
        FrameBufferTarget target)
    {
        var fboHandle = GetFboHandle(fboRef).Handle;
        var rboHandle = GetRboHandle(rboRef).Handle;
        var glAttachment = target.ToGlAttachmentEnum();
        _gl.NamedFramebufferRenderbuffer(fboHandle, glAttachment, RenderbufferTarget.Renderbuffer, rboHandle);
    }

    public void SetDrawBuffers(GfxRefToken<FrameBufferId> fboRef, FrameBufferTarget target)
    {
        var fboHandle = GetFboHandle(fboRef).Handle;
        var glAttachment = target.ToGlAttachmentEnum();
        _gl.NamedFramebufferDrawBuffers(fboHandle, 1, glAttachment);
    }

    public void ValidateComplete(GfxRefToken<FrameBufferId> fboRef)
    {
        var handle = GetFboHandle(fboRef).Handle;
        _gl.NamedFramebufferDrawBuffer(handle, GLEnum.ColorAttachment0);
        _gl.NamedFramebufferReadBuffer(handle, GLEnum.ColorAttachment0);

        var status = _gl.CheckNamedFramebufferStatus(handle, FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            GraphicsException.ThrowFramebufferIncomplete(nameof(fboRef), status.ToString());
    }
}