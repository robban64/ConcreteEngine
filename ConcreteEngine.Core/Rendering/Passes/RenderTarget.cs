using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Common.Numerics;
using ConcreteEngine.Core.Rendering.Gfx;
using ConcreteEngine.Graphics.Resources;

namespace ConcreteEngine.Core.Rendering;

public delegate Size2D CalcTargetOutputDel(Size2D outputSize, Vector2 ratio);

public sealed class RenderTarget
{
    public RenderTargetId TargetId { get; }
    public FrameBufferId FboId { get; private set; }
    public RenderTargetId ResolveTo { get; private set; }
    
    public SizePolicy TargetSizePolicy { get; private set; }
    
    public RenderTarget UseCalculatedSize(CalcTargetOutputDel calc, Vector2 ratio)
    {
        TargetSizePolicy = SizePolicy.Calculated(calc, ratio);
        return this;
    }

    public RenderTarget UseFixedSize(Size2D fixedSize)
    {
        TargetSizePolicy = SizePolicy.Fixed(fixedSize);
        return this;
    }
    
    public sealed class SizePolicy
    {
        private enum Mode : byte { Default, Fixed, Calculated }

        private readonly Mode _mode;
        private readonly CalcTargetOutputDel? _calc;
        private readonly Vector2 _ratio;
        private readonly Size2D _fixed;

        private SizePolicy(Mode mode, CalcTargetOutputDel? calc, Vector2 ratio, Size2D fixedSize)
        {
            _mode = mode; _calc = calc; _ratio = ratio; _fixed = fixedSize;
        }

        public static SizePolicy Default() => new(Mode.Default, null, Vector2.One, default);
        public static SizePolicy Fixed(Size2D size) => new(Mode.Fixed, null, Vector2.One, size);
        public static SizePolicy Calculated(CalcTargetOutputDel calcTargetOutput, Vector2 ratio) => new(Mode.Calculated, calcTargetOutput, ratio, default);

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
