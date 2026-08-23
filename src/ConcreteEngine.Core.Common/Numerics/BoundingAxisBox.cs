using System.Numerics;
using System.Runtime.CompilerServices;

namespace ConcreteEngine.Core.Common.Numerics;

public record struct BoundingAxisBox(Vector3 Center, Vector3 Extent)
{
    public Vector3 Center = Center;
    public Vector3 Extent = Extent;

    public static BoundingAxisBox Infinite { get; } =
        new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue)).ToAxisBox();

    public BoundingAxisBox(in BoundingBox box) : this(box.Center, box.Extent) { }

    public readonly Vector3 Min
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Center - Extent;
    }

    public readonly Vector3 Max
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Center + Extent;
    }

    public readonly void FillCorners(Span<Vector3> corners)
    {
        var min = Min;
        var max = Max;
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
    public static void FromBoundingBox(in BoundingBox box, out BoundingAxisBox result) =>
        result = new BoundingAxisBox(box.Center, box.Extent);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Merge(in BoundingAxisBox boxA, in BoundingAxisBox boxB, out BoundingAxisBox result)
    {
        var max = Vector3.Max(boxA.Max, boxB.Max);
        var min = Vector3.Min(boxA.Min, boxB.Min);
        result = new BoundingAxisBox((min + max) / 2.0f, (max - min) / 2.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FromPoints(Span<Vector3> points, out BoundingAxisBox result)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (ref readonly var point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        result = new BoundingAxisBox((min + max) * 0.5f, (max - min) * 0.5f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetWorldBounds(in BoundingBox local, in Matrix4x4 matrix, out BoundingAxisBox world)
    {
        var worldCenter = Vector3.Transform(local.Center, matrix);
        var e = local.Extent;

        var m11 = MathF.Abs(matrix.M11);
        var m12 = MathF.Abs(matrix.M12);
        var m13 = MathF.Abs(matrix.M13);
        var m21 = MathF.Abs(matrix.M21);
        var m22 = MathF.Abs(matrix.M22);
        var m23 = MathF.Abs(matrix.M23);
        var m31 = MathF.Abs(matrix.M31);
        var m32 = MathF.Abs(matrix.M32);
        var m33 = MathF.Abs(matrix.M33);

        var worldExtent = new Vector3(
            e.X * m11 + e.Y * m21 + e.Z * m31,
            e.X * m12 + e.Y * m22 + e.Z * m32,
            e.X * m13 + e.Y * m23 + e.Z * m33
        );

        world = new BoundingAxisBox(worldCenter, worldExtent);
    }
}