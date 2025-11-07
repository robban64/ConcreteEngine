#region

using System.Numerics;

#endregion

namespace ConcreteEngine.Common.Numerics;

public readonly record struct BoundingBox(in Vector3 Min, in Vector3 Max)
{
    public readonly Vector3 Min = Min;
    public readonly Vector3 Max = Max;

    public Vector3 Center => (Min + Max) / 2f;

    public Vector3 Extent => (Max - Min) / 2f;

    public void Deconstruct(out Vector3 min, out Vector3 max)
    {
        min = Min;
        max = Max;
    }

    public void DrainCorners(Span<Vector3> corners)
    {
        corners[0] = new Vector3(Min.X, Max.Y, Max.Z);
        corners[1] = new Vector3(Max.X, Max.Y, Max.Z);
        corners[2] = new Vector3(Max.X, Min.Y, Max.Z);
        corners[3] = new Vector3(Min.X, Min.Y, Max.Z);
        corners[4] = new Vector3(Min.X, Max.Y, Min.Z);
        corners[5] = new Vector3(Max.X, Max.Y, Min.Z);
        corners[6] = new Vector3(Max.X, Min.Y, Min.Z);
        corners[7] = new Vector3(Min.X, Min.Y, Min.Z);
    }

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