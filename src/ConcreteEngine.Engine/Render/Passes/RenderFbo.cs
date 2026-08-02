using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Engine.Render.Passes;

public enum FboResizeMode : byte
{
    Screen, Fixed, Calculated
}

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public readonly FrameBufferId FboId;
    public readonly FboKey Key;
    public readonly RenderTargetKind TargetKind;

    public readonly FboResizeMode ResizeMode;
    private readonly Func<Size2D, Size2D>? _calc;

    internal RenderFbo(FrameBufferId fboId, FboKey key, RenderTargetKind targetKind, FboResizeMode resizeMode,
        Func<Size2D, Size2D>? calc = null)
    {
        if (targetKind == RenderTargetKind.Shadow && resizeMode != FboResizeMode.Fixed)
        {
            Throwers.InvalidArgument("Shadow map require fixed size policy");
        }

        FboId = fboId;
        Key = key;
        TargetKind = targetKind;
        ResizeMode = resizeMode;
        _calc = calc;

    }

    public bool IsShadowFbo => TargetKind == RenderTargetKind.Shadow;
    public bool IsFixedSize => ResizeMode == FboResizeMode.Fixed;

    public Size2D CalculateSize(Size2D outputSize)
    {
        if (IsFixedSize) Throwers.InvalidOperation("Fbo is fixed size");
        if (ResizeMode == FboResizeMode.Calculated) return _calc!(outputSize);
        return outputSize;
    }

    public int CompareTo(RenderFbo? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        return other is null ? 1 : Key.CompareTo(other.Key);
    }
}