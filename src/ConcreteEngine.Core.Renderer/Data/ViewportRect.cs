using ConcreteEngine.Core.Common.Numerics;

namespace ConcreteEngine.Core.Renderer.Data;

public record struct ViewportRect(Vector2I Position, Size2D Size)
{
    public ViewportRect(Size2D size) : this(default, size){}
    
    public Vector2I Position = Position;
    public Size2D Size = Size;

    public static implicit operator Size2D(in ViewportRect v) => v.Size;
    public static implicit operator Vector2I(in ViewportRect v) => v.Position;
 
}