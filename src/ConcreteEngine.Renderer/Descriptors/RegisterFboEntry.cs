using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Contracts;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Renderer.Registry;

namespace ConcreteEngine.Renderer.Descriptors;

public sealed class RegisterFboEntry
{
    public GfxFboColorTextureDesc? ColorTexture { get; private set; }
    public GfxFboDepthTextureDesc? DepthTexture { get; private set; }
    public bool ColorBuffer { get; private set; } = false;
    public bool DepthStencilBuffer { get; private set; }
    public RenderBufferMsaa Multisample { get; private set; } = RenderBufferMsaa.None;

    public RenderFboSizePolicy? FboSizePolicy { get; private set; }

    public RegisterFboEntry AttachColorTexture(GfxFboColorTextureDesc desc,
        RenderBufferMsaa multisample = RenderBufferMsaa.None)
    {
        Multisample = multisample;
        ColorTexture = desc;
        return this;
    }

    public RegisterFboEntry AttachDepthTexture(GfxFboDepthTextureDesc desc)
    {
        DepthTexture = desc;
        return this;
    }

    public RegisterFboEntry AttachDepthStencilBuffer()
    {
        DepthStencilBuffer = true;
        return this;
    }

    public RegisterFboEntry UseCalculatedSize(FboSizePolicyDel del, Vector2 ratio)
    {
        FboSizePolicy = RenderFboSizePolicy.Calculated(del, ratio);
        return this;
    }

    public RegisterFboEntry UseFixedSize(Size2D fixedSize)
    {
        FboSizePolicy = RenderFboSizePolicy.Fixed(fixedSize);
        return this;
    }


    internal GfxFrameBufferDescriptor Build(Size2D outputSize)
    {
        FboSizePolicy ??= RenderFboSizePolicy.Default();
        var size = FboSizePolicy.Calculate(outputSize);

        InvalidOpThrower.ThrowIf(size.Width < 1 || size.Height < 1, nameof(size));

        if (ColorTexture is { PixelFormat: TexturePixelFormat.Unknown or TexturePixelFormat.Depth } ct)
            throw new InvalidOperationException($"Invalid PixelFormat for ColorTexture {ct.PixelFormat}");

        if (DepthTexture is { PixelFormat: not TexturePixelFormat.Depth } dt)
            throw new InvalidOperationException($"Invalid PixelFormat for ColorTexture {dt.PixelFormat}");


        return new GfxFrameBufferDescriptor(
            size: size,
            colorTexture: ColorTexture,
            depthTexture: DepthTexture,
            colorBuffer: ColorBuffer,
            depthStencilBuffer: DepthStencilBuffer,
            multisample: Multisample
        );
    }
}