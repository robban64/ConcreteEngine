using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using ConcreteEngine.Graphics.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlFrameBuffers: IGraphicsDriverModule
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

    private GlFboHandle GetFboHandle(in GfxHandle handle) => _store.FrameBuffer.Get(in handle);
    private GlRboHandle GetRboHandle(in GfxHandle handle) => _store.RenderBuffer.Get(in handle);
    private GlTextureHandle GetTextureHandle(in GfxHandle handle) => _store.Texture.Get(in handle);



    // Fix ClearBufferMask and Filter, depth/stencil use filter = Nearest
    public void Blit(in GfxHandle readFbo, in GfxHandle drawFbo, Vector2D<int> srcSize, Vector2D<int> dstSize,
        bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        var read = _store.FrameBuffer.Get(readFbo).Handle;
        var draw = _store.FrameBuffer.Get(drawFbo).Handle;

        _gl.BlitNamedFramebuffer(
            read, draw,
            0, 0, srcSize.X, srcSize.Y,
            0, 0, dstSize.X, dstSize.Y,
            ClearBufferMask.ColorBufferBit, filter
        );
    }

    public void BlitDefault(in GfxHandle readFbo, Vector2D<int> srcSize, Vector2D<int> dstSize, bool linear)
    {
        var filter = linear ? BlitFramebufferFilter.Linear : BlitFramebufferFilter.Nearest;
        var read = _store.FrameBuffer.Get(readFbo).Handle;
        _gl.BlitNamedFramebuffer(
            read, 0,
            0, 0, srcSize.X, srcSize.Y,
            0, 0, dstSize.X, dstSize.Y,
            ClearBufferMask.ColorBufferBit, filter
        );
    }

    public GfxRefToken<FrameBufferId> CreateFrameBuffer()
    {
        _gl.CreateFramebuffers(1, out uint fbo);
        return _store.FrameBuffer.Add(new GlFboHandle(fbo));
    }

    public GfxRefToken<RenderBufferId> CreateRenderBuffer(FrameBufferTarget attachment, Vector2D<int> size,
        bool multisample, uint samples)
    {
        var glAttachment = attachment.ToGlEnum();
        var (width, height) = ((uint)size.X, (uint)size.Y);

        _gl.CreateRenderbuffers(1, out uint rbo);
        if (multisample)
            _gl.NamedRenderbufferStorageMultisample(rbo, samples, glAttachment, width, height);
        else
            _gl.NamedRenderbufferStorage(rbo, glAttachment, width, height);

        return _store.RenderBuffer.Add(new GlRboHandle(rbo));
    }

    public void AttachTexture(in GfxHandle fbo, in GfxHandle texture, FrameBufferTarget target)
    {
        var (fboHandle, texHandle) = (GetFboHandle(in fbo).Handle, GetRboHandle(in texture).Handle);
        var glAttachment = target.ToGlEnum();
        _gl.NamedFramebufferTexture(fboHandle, glAttachment, texHandle, 0);
    }

    public void AttachRenderBuffer(in GfxHandle fbo, in GfxHandle rbo, FrameBufferTarget target)
    {
        var (fboHandle, rboHandle) = (GetFboHandle(in fbo).Handle, GetRboHandle(in rbo).Handle);
        var glAttachment = target.ToGlEnum();
        _gl.NamedFramebufferRenderbuffer(fboHandle, glAttachment, RenderbufferTarget.Renderbuffer, rboHandle);
    }

    public void SetDrawBuffers(in GfxHandle fbo, FrameBufferTarget target)
    {
        var fboHandle = GetFboHandle(in fbo).Handle;
        var glAttachment = target.ToGlEnum();
        _gl.NamedFramebufferDrawBuffers(fboHandle, 1, glAttachment);
    }

    public void ValidateComplete(in GfxHandle fbo)
    {
        var handle = GetFboHandle(in fbo).Handle;
        _gl.NamedFramebufferDrawBuffer(handle, GLEnum.ColorAttachment0);
        _gl.NamedFramebufferReadBuffer(handle, GLEnum.ColorAttachment0);

        var status = _gl.CheckNamedFramebufferStatus(handle, FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
            GraphicsException.ThrowFramebufferIncomplete(nameof(fbo), status.ToString());
    }
    
    /*
         public void BindFrameBufferReadDraw(in GfxHandle readFbo, in GfxHandle drawFbo)
       {
           var read = !readFbo.IsValid ? 0 : _store.FrameBuffer.Get(readFbo).Handle;
           var draw = !drawFbo.IsValid ? 0 : _store.FrameBuffer.Get(drawFbo).Handle;

           _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, read);
           _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, draw);
       }
     */
}