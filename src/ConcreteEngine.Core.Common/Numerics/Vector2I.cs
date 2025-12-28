using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Vector2I(int x, int y) : IEquatable<Vector2I>, IComparable<Vector2I>
{
    [JsonInclude] public int X = x;
    [JsonInclude] public int Y = y;
    
    //

    public static readonly Vector2I Zero = new(0, 0);
    public static readonly Vector2I One = new(1, 1);
    public static readonly Vector2I UnitX = new(1, 0);
    public static readonly Vector2I UnitY = new(0, 1);

    //
    
    public static explicit operator Vector2I(Vector2 v) => new((int)v.X, (int)v.Y);
    public static implicit operator Vector2(Vector2I v) => new(v.X, v.Y);

    public static implicit operator Vector2I((int x, int y) t) => new(t.x, t.y);
    public static implicit operator (int x, int y)(Vector2I v) => (v.X, v.Y);

    //
    
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
    public static Vector2I operator *(Vector2I a, Vector2I b) => new(a.X * b.X, a.Y * b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I operator /(Vector2I v, int k) => new(v.X / k, v.Y / k);

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Min(Vector2I a, Vector2I b) => new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Max(Vector2I a, Vector2I b) => new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Clamp(Vector2I v, Vector2I min, Vector2I max) => Max(min, Min(v, max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2I Abs(Vector2I v) => new(Math.Abs(v.X), Math.Abs(v.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Dot(Vector2I a, Vector2I b) => a.X * b.X + a.Y * b.Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Cross(Vector2I a, Vector2I b) => a.X * b.Y - a.Y * b.X;

    public readonly float Length() => MathF.Sqrt(X * X + Y * Y);

    public readonly float LengthSquared() => X * X + Y * Y;

    public readonly int ManhattanLength() => Math.Abs(X) + Math.Abs(Y);

    // Utilities

    public readonly Vector2I PerpendicularCw() => new(Y, -X);
    public readonly Vector2I PerpendicularCcw() => new(-Y, X);

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector2I other) => X == other.X && Y == other.Y;

    public override readonly bool Equals(object? obj) => obj is Vector2I v && Equals(v);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    public static bool operator ==(Vector2I a, Vector2I b) => a.Equals(b);
    public static bool operator !=(Vector2I a, Vector2I b) => !a.Equals(b);

    public readonly int CompareTo(Vector2I other)
    {
        var c = X.CompareTo(other.X);
        return c != 0 ? c : Y.CompareTo(other.Y);
    }
}