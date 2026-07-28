using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using static ConcreteEngine.Engine.Render.RenderLimits;

namespace ConcreteEngine.Engine.Render.Passes;

public enum FboResizeMode : byte
{
    Screen, Fixed, Calculated
}

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public readonly FrameBufferId FboId;
    public readonly FboKey Key;
    public bool IsShadowFbo { get; internal set; }

    public readonly FboResizeMode ResizeMode;
    private readonly Func<Size2D, Size2D>? _calc;

    internal RenderFbo(FrameBufferId fboId, FboKey key, FboResizeMode resizeMode, Func<Size2D, Size2D>? calc = null)
    {
    
        FboId = fboId;
        Key = key;
        ResizeMode = resizeMode;
        _calc = calc;
    }

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
