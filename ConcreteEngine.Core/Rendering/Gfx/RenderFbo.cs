using System.Numerics;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Gfx;

public sealed class RenderFbo
{
    public delegate Size2D CalcFboSizeDel(Size2D outputSize, Vector2 ratio);

    public FrameBufferId FboId { get; }

    private readonly GetMetaDel<FrameBufferId, FrameBufferMeta> _getMetaDel;
    
    private CalcFboSizeDel? _calculateSizeDel;

    public Vector2 CalculateRatio { get; private set; } = Vector2.One;
    public Size2D? FixedSize { get; private set; }

    internal RenderFbo(FrameBufferId fboId, GetMetaDel<FrameBufferId, FrameBufferMeta> getMetaDel)
    {
        FboId = fboId;
        _getMetaDel = getMetaDel;
    }

    internal void UseCalculatedSize(CalcFboSizeDel calculateSizeDel, Vector2 calculateRatio)
    {
        _calculateSizeDel = calculateSizeDel;
        CalculateRatio = calculateRatio;
        FixedSize = null;
    }

    internal void UseFixedSize(Size2D fixedSize)
    {
        FixedSize = fixedSize;
        _calculateSizeDel = null;
        CalculateRatio = Vector2.One;
    }

    public Size2D CalculateNewSize(Size2D outputSize)
    {
        if (FixedSize is { } fixedSize)
            return fixedSize;
        if (_calculateSizeDel is { } calculate)
            return calculate(outputSize, CalculateRatio);
        
        return outputSize;
    }

    public FboRenderData RenderData()
    {
        ref readonly var meta = ref _getMetaDel(FboId);
        return new FboRenderData(meta.Size, meta.Attachments, meta.MultiSample);
    }
}

public readonly struct FboRenderData(Size2D outputSize, FboAttachmentIds attachments, RenderBufferMsaa samples)
{
    public readonly Size2D OutputSize = outputSize;
    public readonly FboAttachmentIds Attachments = attachments;
    public readonly RenderBufferMsaa Samples = samples;
}


public interface IFboTag;
public readonly struct FboSceneTag : IFboTag;
public readonly struct FboMsaaTag : IFboTag;
public readonly struct FboLightTag : IFboTag;
public readonly struct FboPostProcessTag : IFboTag;
