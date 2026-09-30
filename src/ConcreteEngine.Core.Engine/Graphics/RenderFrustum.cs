using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    private Span<Vector4> ScenePlanes => _frustumPlanes.AsSpan(6);

    private ref BoundingFrustum LightFrustum
        => ref Unsafe.As<Vector4, BoundingFrustum>(ref MemoryMarshal.GetArrayDataReference(_frustumPlanes));

    private ref BoundingFrustum MainFrustum => ref Unsafe.As<Vector4, BoundingFrustum>
        (ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_frustumPlanes), 6));


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
    
    public PassMask Intersects(PassMask passes, EntityDrawStatus status, in BoundingAxisBox box, out ushort distance)
    {
        var center = new Vector4(box.Center, 1f);
        var extent = new Vector4(box.Extent, 0f);

        distance = CalculateDepthKey(in _forward, in center, _scaleBias);
        
        if (status == EntityDrawStatus.AlwaysVisible) return passes;

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
        if((passes & PassMask.Effect) != 0) mask |= PassMask.Effect;
        return mask;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort CalculateDepthKey(in Vector4 forward, in Vector4 center, Vector2 scaleBias)
    {
        const float maxValue = 65535f;
        var dot = Vector4.Dot(forward, center);
        float key = float.FusedMultiplyAdd(dot, scaleBias.X, scaleBias.Y);
        return (ushort)float.Min(0f, float.Max(key, maxValue));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestIntersect(Span<Vector4> span, in Vector4 center4, in Vector4 extent4)
    {
        ref var plane = ref MemoryMarshal.GetReference(span);
        ref readonly var end = ref Unsafe.Add(ref plane, 5);
        while (Unsafe.IsAddressLessThan(ref plane, in end))
        {
            bool isOutside = CollisionMethods.IsOutsidePlane(center4, extent4, in plane);
            if (isOutside) return false;
            plane = ref Unsafe.Add(ref plane, 1);
        }

        return true;
    }
    
}