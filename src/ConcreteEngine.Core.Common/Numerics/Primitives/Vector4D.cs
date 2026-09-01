using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Numerics.Extensions;

namespace ConcreteEngine.Core.Common.Numerics;


public struct Vector4D : IEquatable<Vector4D>
{
    public double X;
    public double Y;
    public double Z;
    public double W;

    public Vector4D(double x, double y, double z, double w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }
    
    internal static Vector4D Create(double value) => Vector256.Create(value).AsVector4D();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector4D Create(Vector3D value, double w) => value.AsVector256Unsafe().WithElement(3, w).AsVector4D();

    internal static Vector4D Create(double x, double y, double z, double w) => Vector256.Create(x, y, z, w).AsVector4D();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector4D(Vector4 v) => new(v.X, v.Y, v.Z,v.W);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector4(Vector4D v) => new((float)v.X, (float)v.Y, (float)v.Z, (float)v.W);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector4D a, Vector4D b)  => a.AsVector256() == b.AsVector256();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector4D a, Vector4D b)=> !(a == b);
    
    public readonly bool Equals(Vector4D other) => this.AsVector256() == other.AsVector256();

    public override readonly bool Equals(object? obj) => obj is Vector4D other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4D Lerp(Vector4D v1, Vector4D v2, double t)
    {
        return Vector256.Lerp(v1.AsVector256(), v2.AsVector256(), Vector256.Create(t)).AsVector4D();
    }


}