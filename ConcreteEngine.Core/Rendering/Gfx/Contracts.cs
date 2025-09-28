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
    public FboSizePolicy SizePolicy { get; }
    public GfxFrameBufferDescriptor.AttachmentsDef Attachments { get; }


    public RegisterFboEntry(
        EnginePixelFormat pixelFormat = EnginePixelFormat.Rgba,
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
        FboSizePolicy sizePolicy)
    {
        PixelFormat = pixelFormat;
        Multisample = multisample;
        Preset = texturePreset;
        Attachments = attachments;
        SizePolicy = sizePolicy;
    }

    public static RegisterFboEntry MakeMsaa(RenderBufferMsaa multisample) =>
        new(pixelFormat: EnginePixelFormat.Rgba, multisample: multisample);

    public static RegisterFboEntry MakePost(bool hasMips) =>
        new(texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);

    public RegisterFboEntry AttachColorTexture() =>
        new(PixelFormat, Multisample, Preset, Attachments with { ColorTexture = true }, SizePolicy);

    public RegisterFboEntry AttachDepthStencilBuffer() =>
        new(PixelFormat, Multisample, Preset, Attachments with { DepthStencilBuffer = true }, SizePolicy);

    public RegisterFboEntry UseCalculatedSize(CalcFboSizeDel calc, Vector2 ratio) =>
        new(PixelFormat, Multisample, Preset, Attachments, FboSizePolicy.Calculated(calc, ratio));

    public RegisterFboEntry UseFixedSize(Size2D fixedSize) =>
        new(PixelFormat, Multisample, Preset, Attachments, FboSizePolicy.Fixed(fixedSize));


    internal GfxFrameBufferDescriptor ToGfxDescriptor(Size2D outputSize) =>
        new(outputSize, Attachments, PixelFormat, Multisample, Preset);
}