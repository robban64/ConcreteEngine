using Silk.NET.Maths;

namespace ConcreteEngine.Core.Common.Numerics;

public readonly record struct Bounds2D(int X, int Y, int Width, int Height)
{
    public Bounds2D(int X, int Y, Size2D size) : this(X, Y, size.Width, size.Height) { }
    public Bounds2D(Vector2I position, Size2D size) : this(position.X, position.Y, size.Width, size.Height) { }

    public Bounds2D(Size2D size) : this(0, 0, size.Width, size.Height) { }

    public readonly float AspectRatio => Height == 0 ? 0f : (float)Width / Height;

    public static implicit operator Size2D(Bounds2D v) => new(v.Width, v.Height);
    public static implicit operator Vector2I(Bounds2D v) => new(v.X, v.Y);

    public Bounds2D ScaleUniform(float f) => this with { Width = (int)(Width * f), Height = (int)(Height * f) };
    public Bounds2D Scale(float fx, float fy) => this with { Width = (int)(Width * fx), Height = (int)(Height * fy) };
}