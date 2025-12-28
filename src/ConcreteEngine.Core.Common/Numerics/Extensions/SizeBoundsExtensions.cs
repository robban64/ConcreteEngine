using Silk.NET.Maths;

namespace ConcreteEngine.Core.Common.Numerics.Extensions;

public static class SizeBoundsExtensions
{
    public static Size2D ToSize2D(this Vector2D<int> v) => new(v.X, v.Y);

    public static Bounds2D ClampTo(this Bounds2D b, Size2D canvas)
    {
        int x = b.X;
        int y = b.Y;
        if (x < 0) x = 0;
        if (y < 0) y = 0;
        int maxX = canvas.Width - b.Width;
        if (x > maxX) x = maxX;
        int maxY = canvas.Height - b.Height;
        if (y > maxY) y = maxY;
        return new Bounds2D(x, y, b.Width, b.Height);
    }

    public static Bounds2D CenterInside(this Size2D inner, Size2D outer)
    {
        int x = (outer.Width - inner.Width) >> 1;
        int y = (outer.Height - inner.Height) >> 1;
        return new Bounds2D(x, y, inner.Width, inner.Height);
    }

    public static Bounds2D At(this Size2D inner, Vector2I topLeft) =>
        new(topLeft.X, topLeft.Y, inner.Width, inner.Height);
}