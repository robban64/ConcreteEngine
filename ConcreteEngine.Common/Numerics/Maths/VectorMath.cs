#region

using System.Numerics;
using System.Runtime.CompilerServices;

#endregion

namespace ConcreteEngine.Common.Numerics.Maths;

public static class VectorMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float BarryCentric(Vector3 p1, Vector3 p2, Vector3 p3, Vector2 pos)
    {
        float det = (p2.Z - p3.Z) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Z - p3.Z);
        float l1 = ((p2.Z - p3.Z) * (pos.X - p3.X) + (p3.X - p2.X) * (pos.Y - p3.Z)) / det;
        float l2 = ((p3.Z - p1.Z) * (pos.X - p3.X) + (p1.X - p3.X) * (pos.Y - p3.Z)) / det;
        float l3 = 1.0f - l1 - l2;
        return l1 * p1.Y + l2 * p2.Y + l3 * p3.Y;
    }
}