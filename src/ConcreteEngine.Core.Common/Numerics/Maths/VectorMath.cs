using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class VectorMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> Normalize(Vector128<float> v)
    {
        var dot = Vector128.Create(Vector128.Dot(v, v));
        var sqrt = Vector128.Sqrt(dot);
        return Vector128.Divide(v, sqrt);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(Vector2 a, Vector2 b, float eps = FloatMath.DefaultEpsilon) =>
        MathF.Abs(a.X - b.X) < eps && MathF.Abs(a.Y - b.Y) < eps;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(Vector128<float> a, Vector128<float> b, float eps = 1e-4f)
    {
        var diff = Vector128.Abs(a - b);
        var cmp = Vector128.LessThanOrEqual(diff, Vector128.Create(eps));
        return Vector128.EqualsAll(cmp, Vector128<float>.AllBitsSet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(Vector256<double> a, Vector256<double> b, double eps = 1e-4)
    {
        var diff = Vector256.Abs(a - b);
        var cmp = Vector256.LessThanOrEqual(diff, Vector256.Create(eps));
        return Vector256.EqualsAll(cmp, Vector256<double>.AllBitsSet);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float BarryCentric(Vector3 p1, Vector3 p2, Vector3 p3, Vector2 pos)
    {
        float det = (p2.Z - p3.Z) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Z - p3.Z);
        float l1 = ((p2.Z - p3.Z) * (pos.X - p3.X) + (p3.X - p2.X) * (pos.Y - p3.Z)) / det;
        float l2 = ((p3.Z - p1.Z) * (pos.X - p3.X) + (p1.X - p3.X) * (pos.Y - p3.Z)) / det;
        float l3 = 1.0f - l1 - l2;
        return l1 * p1.Y + l2 * p2.Y + l3 * p3.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UnProject(Vector3 ndc, in Matrix4x4 invViewProjection, out Vector3 point)
    {
        var ndc4 = new Vector4(ndc, 1.0f);
        var vec = Vector4.Transform(ndc4, invViewProjection);
        if (vec.W > float.Epsilon || vec.W < -float.Epsilon)
        {
            point = Unsafe.As<Vector4, Vector3>(ref vec) / vec.W;
            return;
        }

        point = vec.AsVector3();
    }
}