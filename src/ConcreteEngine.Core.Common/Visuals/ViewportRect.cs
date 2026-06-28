using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Common.Visuals;

public record struct ViewportRect(Vector2I Position, Size2D Size)
{
    public ViewportRect(Size2D size) : this(default, size) { }
    public ViewportRect(Vector2 position, Vector2 size) : this((Vector2I)position, (Size2D)size) { }

    public Vector2I Position = Position;
    public Size2D Size = Size;

    public static implicit operator Size2D(in ViewportRect v) => v.Size;
    public static implicit operator Vector2I(in ViewportRect v) => v.Position;
    
}