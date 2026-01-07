using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ConcreteEngine.Core.Common.Numerics;

[StructLayout(LayoutKind.Sequential)]
public record struct BoundingBox(in Vector3 Min, in Vector3 Max)
{
    public Vector3 Min = Min;
    public Vector3 Max = Max;

    public static BoundingBox Identity => new(Vector3.Zero, Vector3.Zero);

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
}