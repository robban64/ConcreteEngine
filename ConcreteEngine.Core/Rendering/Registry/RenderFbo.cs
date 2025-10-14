#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Passes;
using ConcreteEngine.Graphics.Gfx.Definitions;
using ConcreteEngine.Graphics.Gfx.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Registry;

public delegate Size2D CalcFboOutputDel(Size2D outputSize, Vector2 ratio);

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public FrameBufferId FboId { get; }
    public FboTagKey TagKey { get; }
    public int Version { get; private set; }

    public Size2D Size { get; private set; }
    public FboAttachmentIds Attachments { get; private set; }
    public RenderBufferMsaa MultiSample { get; private set; }


    private readonly SizePolicy _sizePolicy;

    internal RenderFbo(FrameBufferId fboId, FboTagKey tagKey, int version, SizePolicy sizePolicy)
    {
        FboId = fboId;
        TagKey = tagKey;
        _sizePolicy = sizePolicy;
        Version = version;
    }


    internal void UpdateFromMeta(in FrameBufferMeta meta)
    {
        Size = meta.Size;
        Attachments = meta.Attachments;
        MultiSample = meta.MultiSample;
    }

    public bool IsFixedSize => _sizePolicy.Mode == SizePolicy.ResizeMode.Fixed;

    public Size2D CalculateNewSize(Size2D outputSize) => _sizePolicy.Calculate(outputSize);

    public int CompareTo(RenderFbo? other) => TagKey.CompareTo(other!.TagKey);

    internal readonly struct RenderFboKeyComparer(FboTagKey key) : IComparer<RenderFbo>, IComparable<RenderFbo>
    {
        public int Compare(RenderFbo x, RenderFbo _) => x.TagKey.CompareTo(key);
        public int CompareTo(RenderFbo? other) => key.CompareTo(other!.TagKey);
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

    public sealed class SizePolicy
    {
        public enum ResizeMode : byte
        {
            Default,
            Fixed,
            Calculated
        }

        public ResizeMode Mode { get; }
        private readonly CalcFboOutputDel? _calc;
        private readonly Vector2 _ratio;
        private readonly Size2D _fixed;

        private SizePolicy(ResizeMode mode, CalcFboOutputDel? calc, Vector2 ratio, Size2D fixedSize)
        {
            Mode = mode;
            _calc = calc;
            _ratio = ratio;
            _fixed = fixedSize;
        }

        public static SizePolicy Default() => new(ResizeMode.Default, null, Vector2.One, default);
        public static SizePolicy Fixed(Size2D size) => new(ResizeMode.Fixed, null, Vector2.One, size);

        public static SizePolicy Calculated(CalcFboOutputDel calcFboOutput, Vector2 ratio) =>
            new(ResizeMode.Calculated, calcFboOutput, ratio, default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Size2D Calculate(Size2D outputSize)
        {
            return Mode switch
            {
                ResizeMode.Fixed => _fixed,
                ResizeMode.Calculated => _calc!(outputSize, _ratio),
                _ => outputSize
            };
        }
    }
}