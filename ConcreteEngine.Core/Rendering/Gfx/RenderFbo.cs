using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Graphics;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering.Gfx;

public delegate Size2D CalcFboOutputDel(Size2D outputSize, Vector2 ratio);

public sealed class RenderFbo : IComparable<RenderFbo>
{
    public FrameBufferId FboId { get; }
    
    private FrameBufferMeta _meta;

    private readonly SizePolicy _sizePolicy;

    internal RenderFbo(FrameBufferId fboId, SizePolicy sizePolicy)
    {
        FboId = fboId;
        _sizePolicy = sizePolicy;
    }
    
        
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly FrameBufferMeta GetMeta() => ref _meta;
    
    internal void UpdateFromMeta(in FrameBufferMeta meta) => _meta = meta;

    public Size2D CalculateNewSize(Size2D outputSize) => _sizePolicy.Calculate(outputSize);

    public int CompareTo(RenderFbo? other) => other!.FboId.Value.CompareTo(FboId.Value);
    
    
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

