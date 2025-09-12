using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal readonly record struct CreateFboResult(GlFboHandle Fbo, in FrameBufferMeta FboMeta);

internal readonly record struct CreateFboTexResult(GlTextureHandle Tex, in TextureMeta TexMeta) ;

internal readonly record struct CreateRboResult(GlRboHandle Rbo, in RenderBufferMeta RboDepthMeta);

internal readonly record struct FboCreatedResult(
    in CreateFboResult Fbo,
    in CreateFboTexResult FboTex,
    in CreateRboResult RboTex,
    in CreateRboResult RboDepth);


internal sealed class GlFboFactory(GL gl, DeviceCapabilities capabilities, GlTextureFactory textureFactory)
    : GlFactory(gl, capabilities)
{
    private readonly GlTextureFactory _textureFactory = textureFactory;

    private GlRboHandle CreateRenderBufferForFbo(RenderBufferKind kind, Vector2D<int> size, bool multisample,
        uint samples, out RenderBufferMeta meta)
    {
        var (width, height) = ((uint)size.X, (uint)size.Y);
        var (format, attachment) = kind switch
        {
            RenderBufferKind.Color => (InternalFormat.Rgba8, FramebufferAttachment.ColorAttachment0),
            RenderBufferKind.DepthStencil => (InternalFormat.Depth24Stencil8,
                FramebufferAttachment.DepthStencilAttachment),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        var handle = Gl.GenRenderbuffer();
        Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, handle);

        if (multisample)
            Gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, samples, format, width, height);
        else
            Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, format, width, height);

        Gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, handle);
        Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        meta = new RenderBufferMeta(kind, size, multisample);

        return new GlRboHandle(handle);
    }


    public void CreateFrameBuffer(
        Vector2D<int> viewport,
        in FrameBufferDesc desc,
        out FboCreatedResult result
    )
    {
        var size = new Vector2D<int>((int)(viewport.X * desc.SizeRatio.X), (int)(viewport.Y * desc.SizeRatio.Y));

        GlTextureHandle colTexHandle = default;
        TextureMeta colTexMeta = default;

        GlRboHandle rboTexHandle = default, rboDepthHandle = default;
        RenderBufferMeta rboTexMeta = default, rboDepthMeta = default;


        var fbo = Gl.GenFramebuffer();
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);

        if (desc.Msaa)
        {
            rboTexHandle = CreateRenderBufferForFbo(RenderBufferKind.Color, size, true, desc.Samples, out rboTexMeta);
            if (desc.DepthStencilBuffer)
            {
                rboDepthHandle = CreateRenderBufferForFbo(RenderBufferKind.DepthStencil, size, true, desc.Samples,
                    out rboDepthMeta);
            }
        }
        else
        {
            var texDesc = new GpuTextureDescriptor(size.X, size.Y, EnginePixelFormat.Rgba,
                desc.TexturePreset, NullPtrData: true);
            colTexHandle = _textureFactory.CreateTexture2D(new GpuTextureData(ReadOnlySpan<byte>.Empty), in texDesc,
                out colTexMeta);

            Gl.BindTexture(TextureTarget.Texture2D, colTexHandle.Handle);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, colTexHandle.Handle, 0);

            // Depth-stencil renderbuffer (single-sample)
            if (desc.DepthStencilBuffer)
            {
                rboDepthHandle =
                    CreateRenderBufferForFbo(RenderBufferKind.DepthStencil, size, false, 0, out rboDepthMeta);
            }

            Gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        Gl.DrawBuffers(1, GLEnum.ColorAttachment0);
        var status = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            GraphicsException.ThrowFramebufferIncomplete(nameof(fbo), status.ToString());
        }

        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        var outFbo = new CreateFboResult(new GlFboHandle(fbo), new FrameBufferMeta(
            desc.TexturePreset,
            desc.SizeRatio, size,
            desc.DepthStencilBuffer, desc.Msaa, (byte)desc.Samples
        ));

        var outTex = colTexHandle.Handle > 0 ? new CreateFboTexResult(colTexHandle, in colTexMeta) : default;
        var outRboTex = rboTexHandle.Handle > 0 ? new CreateRboResult(rboTexHandle, in rboTexMeta) : default;
        var outRboDepth = rboDepthHandle.Handle > 0 ? new CreateRboResult(rboDepthHandle, in rboDepthMeta) :default;
        
        result = new FboCreatedResult(in outFbo, in outTex, in outRboTex, in outRboDepth);
    }
}