namespace ConcreteEngine.Common.Numerics;

public readonly record struct Bounds2D(int X, int Y, int Width, int Height)
{
    public float AspectRatio => Height == 0 ? 0f : (float)Width / Height;

    public Bounds2D ScaleUniform(float factor) => new(X, Y, (int)(Width * factor), (int)(Height * factor));
    public Bounds2D Scale(float fx, float fy) => new(X, Y, (int)(Width * fx), (int)(Height * fy));
}