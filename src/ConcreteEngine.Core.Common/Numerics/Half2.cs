using System.Numerics;

namespace ConcreteEngine.Core.Common.Numerics;

public struct Half2(Half x, Half y)
{
    public Half X = x;
    public Half Y = y;

    public Half2(float x, float y) : this((Half)x, (Half)y) { }
    public Half2(Vector2 v) : this((Half)v.X, (Half)v.Y) { }

    public float X32
    {
        readonly get => (float)X;
        set => X = (Half)value;
    }

    public float Y32
    {
        readonly get => (float)Y;
        set => Y = (Half)value;
    }


    public static implicit operator Vector2(Half2 v) => new((float)v.X, (float)v.Y);

    public static explicit operator Half2(Vector2 v) => new(v);

    public override readonly string ToString() => $"({X32}, {Y32})";
}