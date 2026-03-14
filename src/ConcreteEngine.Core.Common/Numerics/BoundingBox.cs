using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public record struct BoundingBox(in Vector3 Min, in Vector3 Max)
{
    public Vector3 Min = Min;
    public Vector3 Max = Max;

    public static BoundingBox Identity = new(Vector3.Zero, Vector3.Zero);

    public readonly bool IsIdentity => Min == Vector3.Zero && Max == Vector3.Zero;

    public readonly Vector3 Center
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Min + Max) / 2f;
    }

    public readonly Vector3 Extent
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (Max - Min) / 2f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FromPoint(Vector3 point)
    {
        Min = Vector3.Min(Min, point);
        Max = Vector3.Max(Max, point);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void FillCorners(Span<Vector3> corners)
    {
        ref readonly var min = ref Min;
        ref readonly var max = ref Max;

        corners[0] = new Vector3(min.X, min.Y, min.Z);
        corners[1] = new Vector3(max.X, min.Y, min.Z);
        corners[2] = new Vector3(max.X, max.Y, min.Z);
        corners[3] = new Vector3(min.X, max.Y, min.Z);

        corners[4] = new Vector3(min.X, min.Y, max.Z);
        corners[5] = new Vector3(max.X, min.Y, max.Z);
        corners[6] = new Vector3(max.X, max.Y, max.Z);
        corners[7] = new Vector3(min.X, max.Y, max.Z);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Merge(in BoundingBox boxA, in BoundingBox boxB, out BoundingBox result) =>
        result = new BoundingBox(Vector3.Min(boxA.Min, boxB.Min), Vector3.Max(boxA.Max, boxB.Max));


    public static void FromAxisBox(in BoundingAxisBox axisBox, out BoundingBox result) =>
        result = new BoundingBox(axisBox.Min, axisBox.Max);

    public static void FromPoints(Span<Vector3> points, out BoundingBox result)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (ref readonly var point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        result = new BoundingBox(min, max);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetWorldBounds(in BoundingBox local, in Matrix4x4 matrix, out BoundingBox world)
    {
        var worldCenter = Vector3.Transform(local.Center, matrix);
        var localExtent = local.Extent;

        var m11 = MathF.Abs(matrix.M11);
        var m12 = MathF.Abs(matrix.M12);
        var m13 = MathF.Abs(matrix.M13);
        var m21 = MathF.Abs(matrix.M21);
        var m22 = MathF.Abs(matrix.M22);
        var m23 = MathF.Abs(matrix.M23);
        var m31 = MathF.Abs(matrix.M31);
        var m32 = MathF.Abs(matrix.M32);
        var m33 = MathF.Abs(matrix.M33);

        float wEx = localExtent.X * m11 + localExtent.Y * m21 + localExtent.Z * m31;
        float wEy = localExtent.X * m12 + localExtent.Y * m22 + localExtent.Z * m32;
        float wEz = localExtent.X * m13 + localExtent.Y * m23 + localExtent.Z * m33;

        world = new BoundingBox(
            new Vector3(worldCenter.X - wEx, worldCenter.Y - wEy, worldCenter.Z - wEz),
            new Vector3(worldCenter.X + wEx, worldCenter.Y + wEy, worldCenter.Z + wEz)
        );
    }
}