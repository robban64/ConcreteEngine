using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;

namespace ConcreteEngine.Common.Numerics;

public readonly record struct Bounds2D(int X, int Y, int Width, int Height)
{
    public Bounds2D(int X, int Y, Size2D size) : this(X, Y, size.Width, size.Height)
    {
    }

    public float AspectRatio => Height == 0 ? 0f : (float)Width / Height;

    public Bounds2D ScaleUniform(float factor) => new(X, Y, (int)(Width * factor), (int)(Height * factor));

    public Bounds2D Scale(float fx, float fy) => new(X, Y, (int)(Width * fx), (int)(Height * fy));

    public Size2D ToSize2D() => new(Width, Height);
    public Vector2I SizeVector() => new(Width, Height);
    public Vector2I PositionVector() => new(X, Y);

    public Vector2D<int> ToVector2D() => new(Width, Height);

    public static Bounds2D FromSize(Size2D size) => new(0, 0, size.Width, size.Height);

    public static Bounds2D FromVector2D(Vector2D<int> vec) => new(0, 0, vec.X, vec.Y);
}