using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Gfx.Contracts;

namespace ConcreteEngine.Core.Rendering.Descriptors;


public sealed class RegisterFboEntry(
    EnginePixelFormat pixelFormat = EnginePixelFormat.SrgbAlpha,
    RenderBufferMsaa multisample = RenderBufferMsaa.None,
    TexturePreset texturePreset = TexturePreset.LinearClamp
)
{
    public EnginePixelFormat PixelFormat { get; } = pixelFormat;
    public RenderBufferMsaa Multisample { get; } = multisample;
    public TexturePreset Preset { get; } = texturePreset;
    public GfxFrameBufferDescriptor.AttachmentsDef Attachments { get; private set; }

    public RenderFbo.SizePolicy? FboSizePolicy { get; private set; }

    public RegisterFboEntry AttachColorTexture()
    {
        Attachments = Attachments with { ColorTexture = true };
        return this;
    }

    public RegisterFboEntry AttachDepthStencilBuffer()
    {
        Attachments = Attachments with { DepthStencilBuffer = true };
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


    internal GfxFrameBufferDescriptor ToGfxDescriptor(Size2D outputSize) =>
        new(outputSize, Attachments, PixelFormat, Multisample, Preset);

    public static RegisterFboEntry MakeDefault(bool hasMips) =>
        new(pixelFormat: EnginePixelFormat.SrgbAlpha,
            texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);

    public static RegisterFboEntry MakeMsaa(RenderBufferMsaa multisample) =>
        new(pixelFormat: EnginePixelFormat.SrgbAlpha, texturePreset: TexturePreset.None, multisample: multisample);

    public static RegisterFboEntry MakePost(bool hasMips) =>
        new(pixelFormat: EnginePixelFormat.SrgbAlpha,
            texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);

    public static RegisterFboEntry MakePostB(bool hasMips) =>
        new(pixelFormat: EnginePixelFormat.Rgba,
            texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);

    public static RegisterFboEntry MakeScreen() =>
        new(pixelFormat: EnginePixelFormat.SrgbAlpha, texturePreset: TexturePreset.LinearClamp);
}