using Silk.NET.Maths;

namespace ConcreteEngine.Common.Numerics;

public record struct Bounds2D(int X, int Y, int Width, int Height)
{
    public Bounds2D(int X, int Y, Size2D size) : this(X, Y, size.Width, size.Height)
    {
    }

    public readonly float AspectRatio => Height == 0 ? 0f : (float)Width / Height;

    public void ScaleUniform(float factor) =>
        this = this with { X = X, Y = Y, Width = (int)(Width * factor), Height = (int)(Height * factor) };

    public void Scale(float fx, float fy) =>
        this = this with { X = X, Y = Y, Width = (int)(Width * fx), Height = (int)(Height * fy) };

    public readonly Size2D ToSize2D() => new(Width, Height);
    public readonly Vector2I SizeVector() => new(Width, Height);
    public readonly Vector2I PositionVector() => new(X, Y);

    public static Bounds2D FromSize(Size2D size) => new(0, 0, size.Width, size.Height);

    public static Bounds2D FromVector2D(Vector2D<int> vec) => new(0, 0, vec.X, vec.Y);
}