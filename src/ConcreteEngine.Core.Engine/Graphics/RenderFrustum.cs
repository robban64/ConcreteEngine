using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Extensions;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine.ECS.Render;

namespace ConcreteEngine.Core.Engine.Graphics;

public sealed class RenderFrustum
{
    private readonly Vector4[] _frustumPlanes = new Vector4[12];

    private Vector4 _forward;
    private Vector2 _scaleBias;

    private Span<Vector4> LightPlanes => _frustumPlanes.AsSpan(0, 6);
    private Span<Vector4> ScenePlanes => _frustumPlanes.AsSpan(6, 6);

    private ref Vector4 LightPlaneRef => ref MemoryMarshal.GetArrayDataReference(_frustumPlanes);
    private ref Vector4 ScenePlaneRef => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_frustumPlanes), 6);

    private ref BoundingFrustum LightFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref LightPlaneRef);
    private ref BoundingFrustum MainFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref ScenePlaneRef);

    internal void Update(Camera camera)
    {
        var viewZ = camera.ViewMatrix.M43;
        var scale = 65535f / camera.NearFarPlane.Range();
        var bias = 0.5f - (viewZ + camera.NearFarPlane.X) * scale;
        _forward = camera.Forward.AsVector4();
        _scaleBias = new Vector2(scale, bias);
    }

    internal void UpdateMain(in Matrix4x4 projectionViewMatrix)
    {
        var transposed = Matrix4x4.Transpose(projectionViewMatrix);
        BoundingFrustum.From(in transposed, out MainFrustum);
    }

    internal void UpdateLight(in Matrix4x4 projectionViewMatrix)
    {
        var transposed = Matrix4x4.Transpose(projectionViewMatrix);
        BoundingFrustum.From(in transposed, out LightFrustum);
    }

    public PassMask Intersects(PassMask passes, in BoundingAxisBox box, out float distance)
    {
        var center = new Vector4(box.Center, 1f);
        var extent = new Vector4(box.Extent, 0f);
        
        var culledPasses = PassMask.None;
        
        ref var plane = ref MemoryMarshal.GetArrayDataReference(_frustumPlanes);
        if ((passes & PassMask.Depth) != 0)
        {
            var test = TestIntersect(ref plane, in center, in extent, PassMask.Depth);
            culledPasses |= test;
        }

        if ((passes & PassMask.Main) != 0)
        {
            var test = TestIntersect(ref Unsafe.Add(ref plane, 6), in center, in extent, PassMask.Main);
            var dist = CalcDistance(center, extent, in Unsafe.Add(ref plane, 10));
            distance = dist;
            culledPasses |= test;
        }
        else
        {
            distance = 0;
        }
        //if((passes & PassMask.Effect) != 0) mask |= PassMask.Effect;

        return culledPasses;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PassMask TestIntersect(ref Vector4 frustum, in Vector4 center4, in Vector4 extent4, PassMask pass)
    {
        ref var plane = ref frustum;
        ref readonly var end = ref Unsafe.Add(ref plane, 5);
        while (Unsafe.IsAddressLessThanOrEqualTo(ref plane, in end))
        {
            bool isOutside = CollisionMethods.IsOutsidePlane(center4, extent4, in plane);
            if (isOutside) return 0;
            plane = ref Unsafe.Add(ref plane, 1);
        }

        return pass;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CalcDistance(Vector4 center4, Vector4 extent4, in Vector4 plane)
    {
        var d1 = Vector256.Create(center4.AsVector128(), extent4.AsVector128());
        var d2 = Vector256.Create(plane.AsVector128(), Vector128.Abs(plane.AsVector128()));
        return Vector256.Dot(d1, d2);
    }

    public ushort GetNearDistance(in BoundingAxisBox bounds)
    {
        var vPlane = _frustumPlanes[10].AsVector128();
        var d1 = Vector256.Create(bounds.Center.AsVector128(), bounds.Extent.AsVector128());
        var d2 = Vector256.Create(vPlane, Vector128.Abs(vPlane));
        var d = Vector256.Dot(d1, d2);
        return (ushort)float.Min(0f, float.Max(d, 65535f));
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort CalculateDepthKey(in Vector4 center)
    {
        const float maxValue = 65535f;
        var dot = Vector4.Dot(_forward, center);
        var scaleBias = _scaleBias;
        var key = float.FusedMultiplyAdd(dot, scaleBias.X, scaleBias.Y);
        return (ushort)float.Min(0f, float.Max(key, maxValue));
    }

/*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PassMask TestIntersect(Span<Vector4> span, in Vector4 center4, in Vector4 extent4, PassMask pass)
    {
        bool isOutside =
            CollisionMethods.IsOutsidePlane(center4, extent4, in span[0]) ||
            CollisionMethods.IsOutsidePlane(center4, extent4, in span[1]) ||
            CollisionMethods.IsOutsidePlane(center4, extent4, in span[2]) ||
            CollisionMethods.IsOutsidePlane(center4, extent4, in span[3]) ||
            CollisionMethods.IsOutsidePlane(center4, extent4, in span[4]) ||
            CollisionMethods.IsOutsidePlane(center4, extent4, in span[5]);

        return isOutside ? 0 : pass;
    }
*/
}