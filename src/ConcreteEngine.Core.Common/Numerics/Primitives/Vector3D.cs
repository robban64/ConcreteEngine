using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Numerics.Extensions;

namespace ConcreteEngine.Core.Common.Numerics;

public struct Vector3D : IEquatable<Vector3D>
{
    public double X;
    public double Y;
    public double Z;

    public Vector3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector3D(Vector3 v) => new(v.X, v.Y, v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3(Vector3D v) => new((float)v.X, (float)v.Y, (float)v.Z);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3D a, Vector3D b) => a.AsVector256() == b.AsVector256();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3D a, Vector3D b) => !(a == b);

    public readonly bool Equals(Vector3D other) => this.AsVector256() == other.AsVector256();

    public override readonly bool Equals(object? obj) => obj is Vector3D other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D Lerp(Vector3D v1, Vector3D v2, double t)
    {
        return Vector256.Lerp(v1.AsVector256(), v2.AsVector256(), Vector256.Create(t)).AsVector3D();
    }

}