using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public readonly FrameBufferId FboId;
    public readonly FboTagKey TagKey;
    public bool IsShadowFbo { get; internal set; }
    public RenderFboSizePolicy SizePolicy { get; private set; }

    internal RenderFbo(FrameBufferId fboId, FboTagKey tagKey, RenderFboSizePolicy sizePolicy)
    {
        FboId = fboId;
        TagKey = tagKey;
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
        return other is null ? 1 : TagKey.CompareTo(other.TagKey);
    }
}