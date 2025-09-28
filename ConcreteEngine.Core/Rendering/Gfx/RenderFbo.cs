using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Gfx;

public delegate Size2D CalcFboSizeDel(Size2D outputSize, Vector2 ratio);

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public FrameBufferId FboId { get; }

    private readonly GetMetaDel<FrameBufferId, FrameBufferMeta> _getMetaDel;
    
    private readonly FboSizePolicy _sizePolicy;

    internal RenderFbo(FrameBufferId fboId, GetMetaDel<FrameBufferId, FrameBufferMeta> getMetaDel)
    {
        FboId = fboId;
        _getMetaDel = getMetaDel;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Size2D CalculateNewSize(Size2D outputSize) => _sizePolicy.Calculate(outputSize);

    public FboRenderData RenderData()
    {
        ref readonly var meta = ref _getMetaDel(FboId);
        return new FboRenderData(meta.Size, meta.Attachments, meta.MultiSample);
    }

    public int CompareTo(RenderFbo? other) => other!.FboId.Value.CompareTo(FboId.Value);
}

public sealed class FboSizePolicy
{
    private enum Mode : byte { Default, Fixed, Calculated }

    private readonly Mode _mode;
    private readonly CalcFboSizeDel? _calc;
    private readonly Vector2 _ratio;
    private readonly Size2D _fixed;

    private FboSizePolicy(Mode mode, CalcFboSizeDel? calc, Vector2 ratio, Size2D fixedSize)
    {
        _mode = mode; _calc = calc; _ratio = ratio; _fixed = fixedSize;
    }

    public static FboSizePolicy Default() => new(Mode.Default, null, Vector2.One, default);
    public static FboSizePolicy Fixed(Size2D size) => new(Mode.Fixed, null, Vector2.One, size);
    public static FboSizePolicy Calculated(CalcFboSizeDel calcFboSize, Vector2 ratio) => new(Mode.Calculated, calcFboSize, ratio, default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Size2D Calculate(Size2D output)
    {
        return _mode switch
        {
            Mode.Fixed => _fixed,
            Mode.Calculated => _calc!(output, _ratio),
            _ => output
        };
    }
}


public readonly struct FboRenderData(Size2D outputSize, FboAttachmentIds attachments, RenderBufferMsaa samples)
{
    public readonly Size2D OutputSize = outputSize;
    public readonly FboAttachmentIds Attachments = attachments;
    public readonly RenderBufferMsaa Samples = samples;
}

public enum FboTagOp
{
    DrawScene,
    Multisample,
    Depth,
    Blit,
    FullscreenQuad

}

public interface IFboTag
{
    abstract static FboTagOp Op { get; }
}
public readonly struct FboSceneTag : IFboTag;
public readonly struct FboMsaaTag : IFboTag;
public readonly struct FboLightTag : IFboTag;
public readonly struct FboPostProcessTag : IFboTag;
public readonly struct FboScreenPass : IFboTag;
