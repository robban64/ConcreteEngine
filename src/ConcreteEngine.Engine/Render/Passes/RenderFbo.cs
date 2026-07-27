using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Engine.Render.Passes;

public enum FboResizeMode : byte
{
    Default, Fixed, Calculated
}
public delegate Size2D FboSizePolicyDel(Size2D outputSize, Vector2 ratio);

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public readonly FrameBufferId FboId;
    public readonly FboKey Key;
    public bool IsShadowFbo { get; internal set; }
    public RenderFboSizePolicy SizePolicy { get; private set; }

    internal RenderFbo(FrameBufferId fboId, FboKey key, RenderFboSizePolicy sizePolicy)
    {
        FboId = fboId;
        Key = key;
        SizePolicy = sizePolicy;
    }

    internal void ChangeSizePolicy(RenderFboSizePolicy sizePolicy)
    {
        ArgumentNullException.ThrowIfNull(sizePolicy);
        SizePolicy = sizePolicy;
    }

    public bool IsFixedSize => SizePolicy.Mode == FboResizeMode.Fixed;

    public Size2D CalculateNewSize(Size2D outputSize) => SizePolicy.Calculate(outputSize);


    public int CompareTo(RenderFbo? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Key.CompareTo(other.Key);
    }
}


public sealed class RenderFboSizePolicy
{
    public FboResizeMode Mode { get; }
    public Vector2 Ratio { get; }
    public Size2D FixedSize { get; }

    private readonly FboSizePolicyDel? _calc;

    private RenderFboSizePolicy(FboResizeMode mode, FboSizePolicyDel? calc, Vector2 ratio, Size2D fixedSize)
    {
        Mode = mode;
        _calc = calc;
        Ratio = ratio;
        FixedSize = fixedSize;

        switch (mode)
        {
            case FboResizeMode.Fixed:
                if (fixedSize < RenderLimits.MinOutputSize || fixedSize > RenderLimits.MaxOutputSize)
                    Throwers.InvalidArgument(nameof(fixedSize));
                break;
            case FboResizeMode.Calculated:
                ArgumentOutOfRangeException.ThrowIfEqual(ratio.X, 0, nameof(ratio));
                ArgumentOutOfRangeException.ThrowIfEqual(ratio.Y, 0, nameof(ratio));
                break;
        }
    }

    public static RenderFboSizePolicy MakeDefault() => new(FboResizeMode.Default, null, Vector2.One, default);
    public static RenderFboSizePolicy MakeFixed(Size2D size) => new(FboResizeMode.Fixed, null, Vector2.One, size);

    public static RenderFboSizePolicy MakeCalculated(FboSizePolicyDel fboSizePolicy, Vector2 ratio) =>
        new(FboResizeMode.Calculated, fboSizePolicy, ratio, default);

    public Size2D Calculate(Size2D outputSize)
    {
        return Mode switch
        {
            FboResizeMode.Fixed => FixedSize,
            FboResizeMode.Calculated => _calc!(outputSize, Ratio),
            _ => outputSize
        };
    }
}