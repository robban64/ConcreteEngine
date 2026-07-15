using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public struct Int4(int x, int y, int z, int w) : IEquatable<Int4>, IComparable<Int4>
{
    [JsonInclude] public int X = x;
    [JsonInclude] public int Y = y;
    [JsonInclude] public int Z = z;
    [JsonInclude] public int W = w;

    public static Int4 One => new(1, 1, 1, 1);
    public static Int4 NegativeOne => new(-1, -1, -1, -1);
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Int4 a, Int4 b) => a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Int4 a, Int4 b) => !a.Equals(b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(Int4 other) => X == other.X && Y == other.Y && Z == other.Z  && W == other.W;

    public override readonly bool Equals(object? obj) => obj is Int4 v && Equals(v);
    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public override readonly string ToString() => $"({X}, {Y}, {Z}, {W})";

    public readonly int CompareTo(Int4 other)
    {
        var c = X.CompareTo(other.X);
        if (c != 0) return c;
        c = Y.CompareTo(other.Y);
        if (c != 0) return c;
        c = Z.CompareTo(other.Z);
        return c != 0 ? c : W.CompareTo(other.W);
    }
}