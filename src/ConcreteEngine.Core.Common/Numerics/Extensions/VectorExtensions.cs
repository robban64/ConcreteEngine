using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Silk.NET.Maths;

namespace ConcreteEngine.Core.Common.Numerics.Extensions;

public static class VectorExtensions
{
    public static Int2 ToVec2Int(this Vector2D<int> v) => new(v.X, v.Y);

    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2D AsVector2D(this Vector256<double> value)
    {
        ref byte address = ref Unsafe.As<Vector256<double>, byte>(ref value);
        return Unsafe.ReadUnaligned<Vector2D>(ref address);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3D AsVector3D(this Vector256<double> value)
    {
        ref byte address = ref Unsafe.As<Vector256<double>, byte>(ref value);
        return Unsafe.ReadUnaligned<Vector3D>(ref address);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4D AsVector4D(this Vector256<double> value)
    {
        return Unsafe.As<Vector256<double>, Vector4D>(ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Color4 AsColor4(this Vector128<float> value) => Unsafe.As<Vector128<float>, Color4>(ref value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> AsVector128(this Color4 value) => Unsafe.As<Color4, Vector128<float>>(ref value);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<double> AsVector256(this Vector2D value) =>
        Vector4D.Create(value.X, value.Y, 0, 0).AsVector256();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<double> AsVector256(this Vector3D value) =>
        Vector4D.Create(value.X, value.Y, value.Z, 0).AsVector256();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<double> AsVector256(this Vector4D value)
    {
        return Unsafe.As<Vector4D, Vector256<double>>(ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<double> AsVector256Unsafe(this Vector3D value)
    {
        Unsafe.SkipInit(out Vector256<double> result);
        Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<double>, byte>(ref result), value);
        return result;
    }
}