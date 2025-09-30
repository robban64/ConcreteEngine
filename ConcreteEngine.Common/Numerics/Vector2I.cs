using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace ConcreteEngine.Common.Numerics;


[StructLayout(LayoutKind.Sequential)]
[DataContract]
public readonly struct Vector2I(int x, int y)
    : IEquatable<Vector2I>, IComparable<Vector2I>
{
    [DataMember(Name = "x")] public readonly int X = x;
    [DataMember(Name = "y")] public readonly int Y = y;

    public static Vector2I Zero => new(0, 0);
    public static Vector2I UnitX => new(1, 0);
    public static Vector2I UnitY => new(0, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 ToVector2() => new(X, Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I From(Vector2 v) => new((int)v.X, (int)v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I From((int x, int y) t) => new(t.x, t.y);

    public (int x, int y) ToTuple() => (X, Y);

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
    public static Vector2I Min(in Vector2I a, in Vector2I b)
    {
        return new Vector2I(a.X < b.X ? a.X : b.X, a.Y < b.Y ? a.Y : b.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Max(in Vector2I a, in Vector2I b)
    {
        return new Vector2I(a.X > b.X ? a.X : b.X, a.Y > b.Y ? a.Y : b.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Clamp(in Vector2I v, in Vector2I min, in Vector2I max) => Max(min, Min(v, max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Abs(in Vector2I v)
    {
        int ax = v.X < 0 ? -v.X : v.X;
        int ay = v.Y < 0 ? -v.Y : v.Y;
        return new Vector2I(ax, ay);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Dot(in Vector2I a, in Vector2I b)
    {
        return a.X * b.X + a.Y * b.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PerpDot(in Vector2I a, in Vector2I b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Manhattan()
    {
        int ax = X < 0 ? -X : X;
        int ay = Y < 0 ? -Y : Y;
        return ax + ay;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length() => MathF.Sqrt(X * X + Y * Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared() => X * X + Y * Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2I Scaled(float k)
    {
        return new Vector2I((int)MathF.Round(X * k), (int)MathF.Round(Y * k));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Lerp(in Vector2I a, in Vector2I b, float t)
    {
        float ix = a.X + (b.X - a.X) * t;
        float iy = a.Y + (b.Y - a.Y) * t;
        return new Vector2I((int)MathF.Round(ix), (int)MathF.Round(iy));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2I PerpendicularCW() => new(Y, -X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2I PerpendicularCCW() => new(-Y, X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyTo(Span<int> dst)
    {
        if (dst.Length < 2) return false;
        dst[0] = X; dst[1] = Y;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<int> dst)
    {
        dst[0] = X; dst[1] = Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vector2I other) => X == other.X && Y == other.Y;

    public override bool Equals(object? obj) => obj is Vector2I v && Equals(v);

    public override int GetHashCode()
    {
        unchecked { return (X * 397) ^ Y; }
    }

    public static bool operator ==(Vector2I a, Vector2I b) => a.Equals(b);
    public static bool operator !=(Vector2I a, Vector2I b) => !a.Equals(b);

    public int CompareTo(Vector2I other)
    {
        if (X != other.X) return X < other.X ? -1 : 1;
        if (Y != other.Y) return Y < other.Y ? -1 : 1;
        return 0;
    }

    public void Deconstruct(out int xOut, out int yOut)
    {
        xOut = X; yOut = Y;
    }
}
