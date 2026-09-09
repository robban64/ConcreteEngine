using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;

namespace ConcreteEngine.Core.Engine.Graphics;


public sealed class RenderFrustum
{
    private readonly Vector4[] _frustumPlanes = new Vector4[12];

    private Span<Vector4> LightPlanes => _frustumPlanes.AsSpan(0, 6);
    private Span<Vector4> ScenePlanes => _frustumPlanes.AsSpan(6);

    private ref BoundingFrustum LightFrustum
        => ref Unsafe.As<Vector4, BoundingFrustum>(ref MemoryMarshal.GetArrayDataReference(_frustumPlanes));

    private ref BoundingFrustum MainFrustum => ref Unsafe.As<Vector4, BoundingFrustum>
        (ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_frustumPlanes), 6));


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateMain(in Matrix4x4 projectionViewMatrix)
    {
        var transposed = Matrix4x4.Transpose(projectionViewMatrix);
        BoundingFrustum.From(in transposed, out MainFrustum);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateLight(in Matrix4x4 projectionViewMatrix)
    {
        var transposed = Matrix4x4.Transpose(projectionViewMatrix);
        BoundingFrustum.From(in transposed, out LightFrustum);
    }

    public PassMask Intersects(PassMask passes, in BoundingAxisBox box)
    {
        var center = new Vector4(box.Center, 1f);
        var extent = new Vector4(box.Extent, 0f);

        var mask = PassMask.None;
        if ((passes & PassMask.Depth) != 0)
        {
            var test = TestIntersect(LightPlanes, in center, in extent);
            mask |= test ? PassMask.Depth : 0;
        }

        if ((passes & PassMask.Main) != 0)
        {
            var test = TestIntersect(ScenePlanes, in center, in extent);
            mask |= test ? PassMask.Main : 0;
        }

        return mask;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestIntersect(Span<Vector4> span, in Vector4 center4, in Vector4 extent4)
    {
        ref var plane = ref MemoryMarshal.GetReference(span);
        ref readonly var end = ref Unsafe.Add(ref plane, 5);
        while (Unsafe.IsAddressLessThanOrEqualTo(ref plane, in end))
        {
            bool isOutside = CollisionMethods.IsOutsidePlane(center4, extent4, in plane);
            if (isOutside) return false;
            plane = ref Unsafe.Add(ref plane, 1);
        }

        return true;
    }
}