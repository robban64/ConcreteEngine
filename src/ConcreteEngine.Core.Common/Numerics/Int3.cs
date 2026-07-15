using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Int3(int x, int y, int z) : IEquatable<Int3>, IComparable<Int3>
{
    [JsonInclude] public int X = x;
    [JsonInclude] public int Y = y;
    [JsonInclude] public int Z = z;

    public static  Int3 Zero => new(0, 0, 0);
    public static  Int3 One => new(1, 1, 1);
    public static  Int3 UnitX => new(1, 0, 0);
    public static  Int3 UnitY => new(0, 1, 0);
    public static  Int3 UnitZ => new(0, 0, 1);

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Int3(Vector3 v) => new((int)v.X, (int)v.Y, (int)v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector3(Int3 v) => new(v.X, v.Y, v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Int3((int x, int y, int z) t) => new(t.x, t.y, t.z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator (int x, int y, int z)(Int3 v) => (v.X, v.Y, v.Z);

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator +(Int3 a, Int3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator -(Int3 a, Int3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator -(Int3 v) => new(-v.X, -v.Y, -v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator *(Int3 v, int k) => new(v.X * k, v.Y * k, v.Z * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator *(int k, Int3 v) => new(v.X * k, v.Y * k, v.Z * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator *(Int3 a, Int3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 operator /(Int3 v, int k) => new(v.X / k, v.Y / k, v.Z / k);

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Min(Int3 a, Int3 b) => new(int.Min(a.X, b.X), int.Min(a.Y, b.Y), int.Min(a.Z, b.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Max(Int3 a, Int3 b) => new(int.Max(a.X, b.X), int.Max(a.Y, b.Y), int.Max(a.Z, b.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Clamp(Int3 v, Int3 min, Int3 max) => Max(min, Min(v, max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Abs(Int3 v) => new(int.Abs(v.X), int.Abs(v.Y), int.Abs(v.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Dot(Int3 a, Int3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Int3 Cross(Int3 a, Int3 b)
    {
        return new Int3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }

    public readonly float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public readonly float LengthSquared() => X * X + Y * Y + Z * Z;

    public readonly int ManhattanLength() => int.Abs(X) + int.Abs(Y) + int.Abs(Z);

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Int3 a, Int3 b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Int3 a, Int3 b) => !a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Int3 other) => X == other.X && Y == other.Y && Z == other.Z;

    public override readonly bool Equals(object? obj) => obj is Int3 v && Equals(v);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override readonly string ToString() => $"({X}, {Y}, {Z})";


    public readonly int CompareTo(Int3 other)
    {
        var c = X.CompareTo(other.X);
        if (c != 0) return c;
        c = Y.CompareTo(other.Y);
        return c != 0 ? c : Z.CompareTo(other.Z);
    }
}