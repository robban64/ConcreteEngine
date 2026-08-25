using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Numerics.Extensions;

namespace ConcreteEngine.Core.Common.Numerics;

public struct Vector2D : IEquatable<Vector2D>
{
    public double X;
    public double Y;

    public Vector2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2D(Vector2 v) => new(v.X, v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector2(Vector2D v) => new((float)v.X, (float)v.Y);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector2D a, Vector2D b) => a.AsVector256() == b.AsVector256();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector2D a, Vector2D b) => !(a == b);
    
    
    public readonly bool Equals(Vector2D other) => this.AsVector256() == other.AsVector256();

    public override readonly bool Equals(object? obj) => obj is Vector2D other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(X, Y);
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D Lerp(Vector2D v1, Vector2D v2, double t) =>
        Vector256.Lerp(v1.AsVector256(), v2.AsVector256(), Vector256.Create(t)).AsVector2D();


}