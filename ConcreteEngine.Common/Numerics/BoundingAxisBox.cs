#region

using System.Numerics;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Common.Numerics;

public readonly record struct BoundingAxisBox(in Vector3 Center, in Vector3 Extent)
{
    public readonly Vector3 Center = Center;
    public readonly Vector3 Extent = Extent;

    public Vector3 Min => Center - Extent;
    public Vector3 Max => Center + Extent;

    public void DrainCorners(Span<Vector3> corners)
    {
        var min = Min;
        var max = Max;
        corners[0] = new Vector3(min.X, max.Y, max.Z);
        corners[1] = new Vector3(max.X, max.Y, max.Z);
        corners[2] = new Vector3(max.X, min.Y, max.Z);
        corners[3] = new Vector3(min.X, min.Y, max.Z);
        corners[4] = new Vector3(min.X, max.Y, min.Z);
        corners[5] = new Vector3(max.X, max.Y, min.Z);
        corners[6] = new Vector3(max.X, min.Y, min.Z);
        corners[7] = new Vector3(min.X, min.Y, min.Z);
    }

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
}