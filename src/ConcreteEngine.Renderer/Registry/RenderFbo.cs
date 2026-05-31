using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Graphics.Handles;
using ConcreteEngine.Renderer.Passes;

namespace ConcreteEngine.Renderer.Registry;

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public readonly FrameBufferId FboId;
    public readonly FboTagKey TagKey;
    public int Version { get; private set; }

    public Size2D Size { get; private set; }
    public FboAttachmentIds Attachments { get; private set; }
    public RenderBufferMsaa MultiSample { get; private set; }

    public bool HasShadowMap { get; internal set; }

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