using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics.Maths;

public static class VectorMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DistanceNearlyEqual(in Vector3 a, in Vector3 b, float eps = FloatMath.DefaultEpsilon) =>
        Vector3.DistanceSquared(a, b) < eps * eps;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(Vector2 a, Vector2 b, float eps = FloatMath.DefaultEpsilon) =>
        MathF.Abs(a.X - b.X) < eps && MathF.Abs(a.Y - b.Y) < eps;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(in Vector3 a, in Vector3 b, float eps = FloatMath.DefaultEpsilon) =>
        MathF.Abs(a.X - b.X) < eps && MathF.Abs(a.Y - b.Y) < eps && MathF.Abs(a.Z - b.Z) < eps;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NearlyEqual(in Vector4 a, in Vector4 b, float eps = FloatMath.DefaultEpsilon) =>
        MathF.Abs(a.X - b.X) < eps && MathF.Abs(a.Y - b.Y) < eps && MathF.Abs(a.Z - b.Z) < eps &&
        MathF.Abs(a.W - b.W) < eps;


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
    public static void UnProject(in Vector3 ndc, in Matrix4x4 invViewProjection, out Vector3 point)
    {
        var vec = Vector4.Transform(new Vector4(ndc, 1.0f), invViewProjection);

        if (vec.W > float.Epsilon || vec.W < -float.Epsilon)
        {
            vec.X /= vec.W;
            vec.Y /= vec.W;
            vec.Z /= vec.W;
        }

        point = vec.AsVector3();
    }
}