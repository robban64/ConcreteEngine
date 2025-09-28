using System.Numerics;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Descriptors;

namespace ConcreteEngine.Core.Rendering.Gfx;

public sealed record RegisterFboEntry(
    EnginePixelFormat PixelFormat = EnginePixelFormat.Rgba,
    RenderBufferMsaa Multisample = RenderBufferMsaa.None,
    TexturePreset TexturePreset = TexturePreset.LinearClamp
)
{
    public static RegisterFboEntry MakeMsaa(RenderBufferMsaa multisample) => new(Multisample: multisample);

    public static RegisterFboEntry MakePost(bool hasMips) =>
        new(TexturePreset: hasMips ? TexturePreset.LinearMipmapClamp : TexturePreset.LinearClamp);


    public Size2D? FixedSize { get; private set; } = null;
    public Vector2? CalculateRatio { get; private set; } = null;
    public RenderFbo.CalcFboSizeDel? CalculateSizeDel { get; private set; } = null;
    public GfxFrameBufferDescriptor.AttachmentsDef Attachments { get; private set; }

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

    public void UseCalculatedSize(RenderFbo.CalcFboSizeDel calculateSizeDel, Vector2 calculateRatio)
    {
        if (CalculateSizeDel is not null || FixedSize is not null)
            throw new InvalidOperationException();

        CalculateSizeDel = calculateSizeDel;
        CalculateRatio = calculateRatio;
        FixedSize = null;
    }

    public void UseFixedSize(Size2D fixedSize)
    {
        if (CalculateSizeDel is not null || FixedSize is not null)
            throw new InvalidOperationException();

        FixedSize = fixedSize;
        CalculateSizeDel = null;
        CalculateRatio = null;
    }

    internal GfxFrameBufferDescriptor ToGfxDescriptor(Size2D outputSize) =>
        new(outputSize, Attachments, PixelFormat, Multisample, TexturePreset);
}