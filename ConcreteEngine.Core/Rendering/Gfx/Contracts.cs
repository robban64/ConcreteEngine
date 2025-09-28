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
        in GfxFrameBufferDescriptor.AttachmentsDef attachments
    )
    {
        PixelFormat = pixelFormat;
        Multisample = multisample;
        Preset = texturePreset;
        Attachments = attachments;
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

    internal GfxFrameBufferDescriptor ToGfxDescriptor(Size2D outputSize) =>
        new(outputSize, Attachments, PixelFormat, Multisample, Preset);


    public static RegisterFboEntry MakeMsaa(RenderBufferMsaa multisample) =>
        new(pixelFormat: EnginePixelFormat.Rgba, multisample: multisample);

    public static RegisterFboEntry MakePost(bool hasMips) =>
        new(texturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);

    

}