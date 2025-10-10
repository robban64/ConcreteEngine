#region

using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Data;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

#endregion

namespace ConcreteEngine.Core.Rendering.Gfx;

public delegate Size2D CalcFboOutputDel(Size2D outputSize, Vector2 ratio);

public sealed class RenderFbo : IComparable<RenderFbo>, IComparable<FrameBufferId>, IComparable<FboTagKey>
{
    public FrameBufferId FboId { get; }
    public FboTagKey TagKey { get; }
    public int Version {get; private set;}

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

    public int CompareTo(RenderFbo? other) => FboId.Value.CompareTo(other!.FboId.Value);

    public int CompareTo(FrameBufferId other) => FboId.Value.CompareTo(other!.Value);

    public int CompareTo(FboTagKey other) => TagKey.CompareTo(other);

    internal readonly struct FrameBufferIdComparer(FrameBufferId id) : IComparer<RenderFbo>
    {
        public int Compare(RenderFbo? x, RenderFbo? _) => x!.CompareTo(id);
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