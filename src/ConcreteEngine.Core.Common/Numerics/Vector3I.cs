using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Vector3I(int x, int y, int z) : IEquatable<Vector3I>, IComparable<Vector3I>
{
    [JsonInclude] public int X = x;
    [JsonInclude] public int Y = y;
    [JsonInclude] public int Z = z;

    public static readonly Vector3I Zero = new(0, 0, 0);
    public static readonly Vector3I One = new(1, 1, 1);
    public static readonly Vector3I UnitX = new(1, 0, 0);
    public static readonly Vector3I UnitY = new(0, 1, 0);
    public static readonly Vector3I UnitZ = new(0, 0, 1);

    // 

    public static explicit operator Vector3I(Vector3 v) => new((int)v.X, (int)v.Y, (int)v.Z);
    public static implicit operator Vector3(Vector3I v) => new(v.X, v.Y, v.Z);

    public static implicit operator Vector3I((int x, int y, int z) t) => new(t.x, t.y, t.z);
    public static implicit operator (int x, int y, int z)(Vector3I v) => (v.X, v.Y, v.Z);

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I operator +(Vector3I a, Vector3I b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I operator -(Vector3I a, Vector3I b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I operator -(Vector3I v) => new(-v.X, -v.Y, -v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I operator *(Vector3I v, int k) => new(v.X * k, v.Y * k, v.Z * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I operator *(int k, Vector3I v) => new(v.X * k, v.Y * k, v.Z * k);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I operator *(Vector3I a, Vector3I b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I operator /(Vector3I v, int k) => new(v.X / k, v.Y / k, v.Z / k);

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I Min(Vector3I a, Vector3I b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I Max(Vector3I a, Vector3I b) =>
        new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I Clamp(Vector3I v, Vector3I min, Vector3I max) => Max(min, Min(v, max));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I Abs(Vector3I v) => new(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Dot(Vector3I a, Vector3I b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3I Cross(Vector3I a, Vector3I b)
    {
        return new Vector3I(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }

    public readonly float Length() => MathF.Sqrt(X * X + Y * Y + Z * Z);

    public readonly float LengthSquared() => X * X + Y * Y + Z * Z;

    public readonly int ManhattanLength() => Math.Abs(X) + Math.Abs(Y) + Math.Abs(Z);

    // 

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Vector3I other) => X == other.X && Y == other.Y && Z == other.Z;

    public override readonly bool Equals(object? obj) => obj is Vector3I v && Equals(v);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override readonly string ToString() => $"<{X}, {Y}, {Z}>";

    public static bool operator ==(Vector3I a, Vector3I b) => a.Equals(b);
    public static bool operator !=(Vector3I a, Vector3I b) => !a.Equals(b);

    public readonly int CompareTo(Vector3I other)
    {
        var c = X.CompareTo(other.X);
        if (c != 0) return c;
        c = Y.CompareTo(other.Y);
        return c != 0 ? c : Z.CompareTo(other.Z);
    }
}