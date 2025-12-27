using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential), DataContract]
public struct Vector2I(int x, int y) : IEquatable<Vector2I>, IComparable<Vector2I>
{
    [DataMember(Name = "x")] public int X = x;
    [DataMember(Name = "y")] public int Y = y;

    public static Vector2I Zero => new(0, 0);
    public static Vector2I UnitX => new(1, 0);
    public static Vector2I UnitY => new(0, 1);

    public Vector2 ToVector2() => new(X, Y);

    public static Vector2I From(Vector2 v) => new((int)v.X, (int)v.Y);

    public static Vector2I From((int x, int y) t) => new(t.x, t.y);


    public static implicit operator Vector2I((int x, int y) t) => new(t.x, t.y);
    public static implicit operator (int x, int y)(Vector2I v) => (v.X, v.Y);
    public static explicit operator Vector2I((uint x, uint y) t) => new((int)t.x, (int)t.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I operator +(Vector2I a, Vector2I b) => new(a.X + b.X, a.Y + b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I operator -(Vector2I a, Vector2I b) => new(a.X - b.X, a.Y - b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I operator -(Vector2I v) => new(-v.X, -v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I operator *(Vector2I v, int k) => new(v.X * k, v.Y * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I operator *(int k, Vector2I v) => new(v.X * k, v.Y * k);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I operator /(Vector2I v, int k)
    {
        return new Vector2I(v.X / k, v.Y / k);
    }

    // helpers
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Min(Vector2I a, Vector2I b)
    {
        return new Vector2I(a.X < b.X ? a.X : b.X, a.Y < b.Y ? a.Y : b.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Max(Vector2I a, Vector2I b)
    {
        return new Vector2I(a.X > b.X ? a.X : b.X, a.Y > b.Y ? a.Y : b.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Clamp(Vector2I v, Vector2I min, Vector2I max) => Max(min, Min(v, max));

    public static Vector2I Abs(Vector2I v)
    {
        int ax = v.X < 0 ? -v.X : v.X;
        int ay = v.Y < 0 ? -v.Y : v.Y;
        return new Vector2I(ax, ay);
    }

    public static int Dot(Vector2I a, Vector2I b) => a.X * b.X + a.Y * b.Y;

    public static int PerpDot(Vector2I a, Vector2I b) => a.X * b.Y - a.Y * b.X;

    public readonly int Manhattan()
    {
        int ax = X < 0 ? -X : X;
        int ay = Y < 0 ? -Y : Y;
        return ax + ay;
    }

    public readonly float Length() => MathF.Sqrt(X * X + Y * Y);

    public readonly float LengthSquared() => X * X + Y * Y;

    public readonly Vector2I Scaled(float k) => this with { X = (int)MathF.Round(X * k), Y = (int)MathF.Round(Y * k) };

    public static Vector2I Lerp(Vector2I a, Vector2I b, float t)
    {
        float ix = a.X + (b.X - a.X) * t;
        float iy = a.Y + (b.Y - a.Y) * t;
        return new Vector2I((int)MathF.Round(ix), (int)MathF.Round(iy));
    }

    public Vector2I PerpendicularCw() => this with { X = Y, Y = -X };

    public Vector2I PerpendicularCcw() => this with { X = -Y, Y = X };

    public readonly bool TryCopyTo(Span<int> dst)
    {
        if (dst.Length < 2) return false;
        dst[0] = X;
        dst[1] = Y;
        return true;
    }

    public readonly void CopyTo(Span<int> dst)
    {
        dst[0] = X;
        dst[1] = Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector2I other) => X == other.X && Y == other.Y;

    public override readonly bool Equals(object? obj) => obj is Vector2I v && Equals(v);

    public override readonly int GetHashCode()
    {
        unchecked
        {
            return (X * 397) ^ Y;
        }
    }

    public static bool operator ==(Vector2I a, Vector2I b) => a.Equals(b);
    public static bool operator !=(Vector2I a, Vector2I b) => !a.Equals(b);

    public readonly int CompareTo(Vector2I other)
    {
        if (X != other.X) return X < other.X ? -1 : 1;
        if (Y != other.Y) return Y < other.Y ? -1 : 1;
        return 0;
    }
}