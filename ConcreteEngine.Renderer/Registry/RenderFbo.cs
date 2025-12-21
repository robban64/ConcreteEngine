using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;
using ConcreteEngine.Graphics.Gfx.Resources.Data;
using ConcreteEngine.Graphics.Gfx.Resources.Handles;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Registry;

public delegate Size2D FboSizePolicyDel(Size2D outputSize, Vector2 ratio);

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public readonly FrameBufferId FboId;
    public readonly FboTagKey TagKey;
    public int Version { get; private set; }

    public Size2D Size { get; private set; }
    public FboAttachmentIds Attachments { get; private set; }
    public RenderBufferMsaa MultiSample { get; private set; }

    public RenderFboSizePolicy SizePolicy { get; private set; }

    internal RenderFbo(FrameBufferId fboId, FboTagKey tagKey, int version, RenderFboSizePolicy sizePolicy)
    {
        FboId = fboId;
        TagKey = tagKey;
        SizePolicy = sizePolicy;
        Version = version;
    }

    internal void UpdateFromMeta(in FrameBufferMeta meta)
    {
        Size = meta.Size;
        Attachments = meta.Attachments;
        MultiSample = meta.MultiSample;
    }

    internal void ChangeSizePolicy(RenderFboSizePolicy sizePolicy)
    {
        ArgumentNullException.ThrowIfNull(sizePolicy, nameof(sizePolicy));
        SizePolicy = sizePolicy;
    }

    public bool IsFixedSize => SizePolicy.Mode == FboResizeMode.Fixed;

    public Size2D CalculateNewSize(Size2D outputSize) => SizePolicy.Calculate(outputSize);


    public int CompareTo(RenderFbo? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : TagKey.CompareTo(other.TagKey);
    }

    internal sealed class FboKeyComparer : IComparer<RenderFbo>
    {
        public static readonly FboKeyComparer Instance = new();

        public int Compare(RenderFbo? x, RenderFbo? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            return x.TagKey.CompareTo(y.TagKey);
        }
    }
}

public sealed class RenderFboSizePolicy
{
    public FboResizeMode Mode { get; }
    private readonly FboSizePolicyDel? _calc;
    private readonly Vector2 _ratio;
    private readonly Size2D _fixed;

    private RenderFboSizePolicy(FboResizeMode mode, FboSizePolicyDel? calc, Vector2 ratio, Size2D fixedSize)
    {
        Mode = mode;
        _calc = calc;
        _ratio = ratio;
        _fixed = fixedSize;

        switch (mode)
        {
            case FboResizeMode.Fixed:
                ArgOutOfRangeThrower.ThrowIfSizeTooSmall(fixedSize, RenderLimits.MinOutputSize);
                ArgOutOfRangeThrower.ThrowIfSizeTooBig(fixedSize, RenderLimits.MaxOutputSize);
                break;
            case FboResizeMode.Calculated:
                ArgumentOutOfRangeException.ThrowIfEqual(ratio.X, 0, nameof(ratio));
                ArgumentOutOfRangeException.ThrowIfEqual(ratio.Y, 0, nameof(ratio));
                break;
        }
    }

    public static RenderFboSizePolicy Default() => new(FboResizeMode.Default, null, Vector2.One, default);
    public static RenderFboSizePolicy Fixed(Size2D size) => new(FboResizeMode.Fixed, null, Vector2.One, size);

    public static RenderFboSizePolicy Calculated(FboSizePolicyDel fboSizePolicy, Vector2 ratio) =>
        new(FboResizeMode.Calculated, fboSizePolicy, ratio, default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Size2D Calculate(Size2D outputSize)
    {
        return Mode switch
        {
            FboResizeMode.Fixed => _fixed,
            FboResizeMode.Calculated => _calc!(outputSize, _ratio),
            _ => outputSize
        };
    }
}