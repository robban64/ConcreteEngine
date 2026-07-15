using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Int2(int x, int y) : IEquatable<Int2>, IComparable<Int2>
{
    [JsonInclude] public int X = x;
    [JsonInclude] public int Y = y;

    public Int2(float x, float y) : this((int)x, (int)y) { }

    //
    public static readonly Int2 Zero = new(0, 0);
    public static readonly Int2 One = new(1, 1);
    public static readonly Int2 UnitX = new(1, 0);
    public static readonly Int2 UnitY = new(0, 1);
    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Int2((int x, int y) t) => new(t.x, t.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(Int2 v) => new(v.X, v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Int2(Vector2 v) => new((int)v.X, (int)v.Y);

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(Int2 a, Int2 b) => new(a.X + b.X, a.Y + b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator +(Int2 a, int b) => new(a.X + b, a.Y + b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator -(Int2 a, Int2 b) => new(a.X - b.X, a.Y - b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator -(Int2 v) => new(-v.X, -v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator *(Int2 v, int k) => new(v.X * k, v.Y * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator *(int k, Int2 v) => new(v.X * k, v.Y * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator *(Int2 a, Int2 b) => new(a.X * b.X, a.Y * b.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 operator /(Int2 v, int k) => new(v.X / k, v.Y / k);

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Min(Int2 a, Int2 b) => new(int.Min(a.X, b.X), int.Min(a.Y, b.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Max(Int2 a, Int2 b) => new(int.Max(a.X, b.X), int.Max(a.Y, b.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Clamp(Int2 v, Int2 min, Int2 max) => Max(min, Min(v, max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Clamp(Int2 v, int min, int max) =>
        new(int.Clamp(v.X, min, max), int.Clamp(v.Y, min, max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int2 Abs(Int2 v) => new(int.Abs(v.X), int.Abs(v.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Dot(Int2 a, Int2 b) => a.X * b.X + a.Y * b.Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Cross(Int2 a, Int2 b) => a.X * b.Y - a.Y * b.X;

    public readonly float Length() => MathF.Sqrt(X * X + Y * Y);
    public readonly float LengthSquared() => X * X + Y * Y;
    public readonly int ManhattanLength() => int.Abs(X) + int.Abs(Y);

    // Utilities
    public readonly Int2 PerpendicularCw() => new(Y, -X);
    public readonly Int2 PerpendicularCcw() => new(-Y, X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsNegative() => X < 0 || Y < 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsZero() => X == 0 && Y == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsNegativeOrZero() => IsNegative() || IsZero();

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Int2 a, Int2 b) => a.X > b.X && a.Y > b.Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Int2 a, Int2 b) => a.X < b.X && a.Y < b.Y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Int2 a, Int2 b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Int2 a, Int2 b) => !a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Int2 other) => X == other.X && Y == other.Y;

    public override readonly bool Equals(object? obj) => obj is Int2 v && Equals(v);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);

    public readonly int CompareTo(Int2 other)
    {
        var c = X.CompareTo(other.X);
        return c != 0 ? c : Y.CompareTo(other.Y);
    }

    public override readonly string ToString() => $"({X}, {Y})";
}