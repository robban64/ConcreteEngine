using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace ConcreteEngine.Graphics.OpenGL;

internal sealed class GlFboFactory : GlFactory
{
    private readonly GlTextureFactory _textureFactory;

    public GlFboFactory(GlTextureFactory textureFactory)
    {
        _textureFactory = textureFactory;
    }

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
        in FrameBufferDesc desc,
        out GlFboHandleMeta fbo,
        out GlTexHandleMeta fboTex,
        out GlRboHandleMeta rboTex,
        out GlRboHandleMeta rboDepth
    )
    {
        var outputSize = desc.AbsoluteSize;
        var size = new Vector2D<int>((int)(outputSize.X * desc.SizeRatio.X), (int)(outputSize.Y * desc.SizeRatio.Y));

        GlTextureHandle colTexHandle = default;
        TextureMeta colTexMeta = default;

        GlRboHandle rboTexHandle = default, rboDepthHandle = default;
        RenderBufferMeta rboTexMeta = default, rboDepthMeta = default;


        var handle = Gl.GenFramebuffer();
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, handle);

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

        fbo = new GlFboHandleMeta(new GlFboHandle(handle), new FrameBufferMeta(
            desc.TexturePreset,
            desc.SizeRatio, size,
            desc.DepthStencilBuffer, desc.Msaa, (byte)desc.Samples
        ));

        fboTex = colTexHandle.Handle > 0 ? new GlTexHandleMeta(colTexHandle, in colTexMeta) : default;
        rboTex = rboTexHandle.Handle > 0 ? new GlRboHandleMeta(rboTexHandle, in rboTexMeta) : default;
        rboDepth = rboDepthHandle.Handle > 0 ? new GlRboHandleMeta(rboDepthHandle, in rboDepthMeta) : default;
    }
}