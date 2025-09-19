using ConcreteEngine.Graphics.Contracts;
using ConcreteEngine.Graphics.Descriptors;
using ConcreteEngine.Graphics.Error;
using ConcreteEngine.Graphics.Gfx.Internal;
using ConcreteEngine.Graphics.Resources;
using Silk.NET.Maths;

namespace ConcreteEngine.Graphics.Gfx;

internal sealed class GfxFrameBuffersInvoker
{
    private readonly GfxTexturesInvoker _textureInvoker;
    private readonly IGraphicsDriver _driver;

    internal GfxFrameBuffersInvoker(GfxContext context, GfxTexturesInvoker textureInvoker)
    {
        _textureInvoker = textureInvoker;
        _driver = context.Driver;
    }
    
    public GfxRefToken<FrameBufferId> CreateFrameBuffer(in FrameBufferDesc desc,
        out FboAttachmentHandleResult attachments)
    {
        if (desc.Attachments.DepthTexture) GraphicsException.ThrowUnsupportedFeature("DepthTexture");
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(desc.AbsoluteSize.Y, 16);

        var (abs, ratio) = (desc.AbsoluteSize, desc.DownscaleRatio);
        var size = new Vector2D<int>((int)(abs.X * ratio.X), (int)(abs.Y * ratio.Y));

        ArgumentOutOfRangeException.ThrowIfLessThan(size.X, 16);
        ArgumentOutOfRangeException.ThrowIfLessThan(size.Y, 16);

        var fboRef = 
        var result = new FboAttachmentHandleResult();

        if (desc.Attachments.ColorTexture)
        {
            var texDesc =
                new GpuTextureDescriptor((uint)size.X, (uint)size.Y, desc.TexturePreset, TextureKind.Texture2D);
            var textureRef = _textureInvoker.CreateTexture(ReadOnlySpan<byte>.Empty, in texDesc, out _);
            _driver.FrameBuffers.AttachTexture(fboRef.Handle, textureRef.Handle, FrameBufferTarget.Color);
            result = result with { ColorTexture = textureRef };
        }

        if (desc.Attachments.ColorRenderBuffer)
        {
            var rboRef = CreateRenderBufferFor(fboRef, size, FrameBufferTarget.Color, desc.Multisample);
            _driver.FrameBuffers.AttachRenderBuffer(fboRef.Handle, rboRef.Handle, FrameBufferTarget.Color);
            result = result with { ColorRenderBuffer = rboRef };
        }

        if (desc.Attachments.DepthStenRenderBuffer)
        {
            var rboRef = CreateRenderBufferFor(fboRef, size, FrameBufferTarget.DepthStencil, desc.Multisample);
            _driver.FrameBuffers.AttachRenderBuffer(fboRef.Handle, rboRef.Handle, FrameBufferTarget.DepthStencil);
            result = result with { DepthRenderBuffer = rboRef };
        }

        attachments = result;
        return fboRef;
    }
    
    private GfxRefToken<RenderBufferId> CreateRenderBufferFor(in GfxRefToken<FrameBufferId> fbo, Vector2D<int> size,
        FrameBufferTarget target, RenderBufferMsaa msaa)
    {
        var samples = msaa.ToSamples();
        var rboRef = _driver.FrameBuffers.CreateRenderBuffer(target, size, samples > 0, samples);
        _driver.FrameBuffers.AttachRenderBuffer(in fbo.Handle, in rboRef.Handle, target);
        return rboRef;
    }
}