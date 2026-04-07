using System.Numerics;

namespace ConcreteEngine.Core.Common.Numerics;

public struct Half2(Half x, Half y) : IEquatable<Half2>
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


    public static bool operator ==(Half2 left, Half2 right) => left.Equals(right);

    public static bool operator !=(Half2 left, Half2 right) => !left.Equals(right);

    public static Half2 operator +(Half2 a, Half2 b) => new(a.X + b.X, a.Y + b.Y);

    public static Half2 operator -(Half2 a, Half2 b) => new(a.X - b.X, a.Y - b.Y);

    public static Half2 operator *(Half2 a, float scalar) => new((float)a.X * scalar, (float)a.Y * scalar);

    public static Half2 operator /(Half2 a, float scalar) => new((float)a.X / scalar, (float)a.Y / scalar);

    public readonly bool Equals(Half2 other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is Half2 other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(X, Y);

    public override readonly string ToString() => $"({X}, {Y})";
}