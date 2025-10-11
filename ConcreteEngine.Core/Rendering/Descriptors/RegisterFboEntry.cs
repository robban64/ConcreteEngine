#region

using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Registry;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;

#endregion

namespace ConcreteEngine.Core.Rendering.Descriptors;

public sealed class RegisterFboEntry
{
    public GfxFboColorTextureDesc? ColorTexture { get; private set; }
    public GfxFboDepthTextureDesc? DepthTexture { get; private set; }
    public bool ColorBuffer { get; private set; } = false;
    public bool DepthStencilBuffer { get; private set; }
    public RenderBufferMsaa Multisample { get; private set; } = RenderBufferMsaa.None;

    public RenderFbo.SizePolicy? FboSizePolicy { get; private set; }

    public RegisterFboEntry AttachColorTexture(RenderBufferMsaa multisample = RenderBufferMsaa.None)
    {
        Multisample = multisample;
        ColorTexture = new GfxFboColorTextureDesc();
        return this;
    }

    public RegisterFboEntry AttachDepthTexture()
    {
        DepthTexture = new GfxFboDepthTextureDesc();
        return this;
    }

    public RegisterFboEntry AttachDepthStencilBuffer()
    {
        DepthStencilBuffer = true;
        return this;
    }

    public RegisterFboEntry UseCalculatedSize(CalcFboOutputDel calcDel, Vector2 ratio)
    {
        FboSizePolicy = RenderFbo.SizePolicy.Calculated(calcDel, ratio);
        return this;
    }

    public RegisterFboEntry UseFixedSize(Size2D fixedSize)
    {
        FboSizePolicy = RenderFbo.SizePolicy.Fixed(fixedSize);
        return this;
    }


    internal GfxFrameBufferDescriptor Build(Size2D outputSize)
    {
        FboSizePolicy ??= RenderFbo.SizePolicy.Default();
        var size = FboSizePolicy.Calculate(outputSize);

        InvalidOpThrower.ThrowIf(size.Width < 1 || size.Height < 1, nameof(size));

        return new GfxFrameBufferDescriptor(
            Size: size,
            ColorTexture: ColorTexture,
            DepthTexture: DepthTexture,
            ColorBuffer: ColorBuffer,
            DepthStencilBuffer: DepthStencilBuffer,
            Multisample: Multisample
        );
    }

    public static RegisterFboEntry MakeDefault(bool hasMips) =>
        new(pixelFormat: GfxPixelFormat.SrgbAlpha,
            texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);

    public static RegisterFboEntry MakeMsaa(RenderBufferMsaa multisample) =>
        new(pixelFormat: GfxPixelFormat.SrgbAlpha, texturePreset: TexturePreset.None, multisample: multisample);

    public static RegisterFboEntry MakePost(bool hasMips) =>
        new(pixelFormat: GfxPixelFormat.SrgbAlpha,
            texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);
}