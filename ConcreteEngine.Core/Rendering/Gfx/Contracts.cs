using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;

namespace ConcreteEngine.Core.Rendering.Gfx;

public sealed class RegisterFboEntry
{
    public EnginePixelFormat PixelFormat { get; }
    public RenderBufferMsaa Multisample { get; }
    public TexturePreset Preset { get; }
    public GfxFrameBufferDescriptor.AttachmentsDef Attachments { get; private set; }

    public RenderFbo.SizePolicy FboSizePolicy { get; }

    public RegisterFboEntry(
        EnginePixelFormat pixelFormat = EnginePixelFormat.SrgbAlpha,
        RenderBufferMsaa multisample = RenderBufferMsaa.None,
        TexturePreset texturePreset = TexturePreset.LinearClamp)
    {
        PixelFormat = pixelFormat;
        Multisample = multisample;
        Preset = texturePreset;
    }

    private RegisterFboEntry(
        EnginePixelFormat pixelFormat,
        RenderBufferMsaa multisample,
        TexturePreset texturePreset,
        in GfxFrameBufferDescriptor.AttachmentsDef attachments,
        RenderFbo.SizePolicy sizePolicy
    )
    {
        PixelFormat = pixelFormat;
        Multisample = multisample;
        Preset = texturePreset;
        Attachments = attachments;
        FboSizePolicy = sizePolicy;
    }
    
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
    
    public RegisterFboEntry UseCalculatedSize(CalcFboOutputDel calcDel, Vector2 ratio) =>
        new(PixelFormat, Multisample, Preset, Attachments, RenderFbo.SizePolicy.Calculated(calcDel, ratio));

    public RegisterFboEntry UseFixedSize(Size2D fixedSize) =>
        new(PixelFormat, Multisample, Preset, Attachments, RenderFbo.SizePolicy.Fixed(fixedSize));


    internal GfxFrameBufferDescriptor ToGfxDescriptor(Size2D outputSize) =>
        new(outputSize, Attachments, PixelFormat, Multisample, Preset);

    public static RegisterFboEntry MakeDefault(bool hasMips) => 
        new(pixelFormat: EnginePixelFormat.SrgbAlpha, texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);

    public static RegisterFboEntry MakeMsaa(RenderBufferMsaa multisample) =>
        new(pixelFormat: EnginePixelFormat.SrgbAlpha, texturePreset: TexturePreset.None, multisample: multisample);

    public static RegisterFboEntry MakePost(bool hasMips) =>
        new(pixelFormat: EnginePixelFormat.SrgbAlpha, texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);

    public static RegisterFboEntry MakePostB(bool hasMips) =>
        new(pixelFormat: EnginePixelFormat.Rgba, texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);

    public static RegisterFboEntry MakeScreen() =>
        new(pixelFormat: EnginePixelFormat.SrgbAlpha, texturePreset: TexturePreset.LinearClamp);


}