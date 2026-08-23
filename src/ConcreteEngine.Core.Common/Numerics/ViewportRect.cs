using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics;

public record struct ViewportRect(Int2 Position, Size2D Size)
{
    public ViewportRect(Size2D size) : this(default, size) { }
    public ViewportRect(Vector2 position, Vector2 size) : this((Int2)position, (Size2D)size) { }

    public Int2 Position = Position;
    public Size2D Size = Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Size2D(in ViewportRect v) => v.Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Int2(in ViewportRect v) => v.Position;
}