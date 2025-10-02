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
        private enum Mode : byte
        {
            Default,
            Fixed,
            Calculated
        }

        private readonly Mode _mode;
        private readonly CalcFboOutputDel? _calc;
        private readonly Vector2 _ratio;
        private readonly Size2D _fixed;

        private SizePolicy(Mode mode, CalcFboOutputDel? calc, Vector2 ratio, Size2D fixedSize)
        {
            _mode = mode;
            _calc = calc;
            _ratio = ratio;
            _fixed = fixedSize;
        }

        public static SizePolicy Default() => new(Mode.Default, null, Vector2.One, default);
        public static SizePolicy Fixed(Size2D size) => new(Mode.Fixed, null, Vector2.One, size);

        public static SizePolicy Calculated(CalcFboOutputDel calcFboOutput, Vector2 ratio) =>
            new(Mode.Calculated, calcFboOutput, ratio, default);

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


}