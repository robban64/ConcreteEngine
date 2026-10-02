using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed class EcsCullSystem
{
    public long Version { get; private set; }
    public int VisibleCount { get; private set; }

    private readonly EntityDataStore _data;

    private readonly Vector4[] _frustumPlanes = new Vector4[12];

    public EcsCullSystem(EntityDataStore data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _data = data;
    }

    private ref Vector4 LightPlaneRef => ref MemoryMarshal.GetArrayDataReference(_frustumPlanes);
    private ref Vector4 ScenePlaneRef => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_frustumPlanes), 6);
    private ref BoundingFrustum LightFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref LightPlaneRef);
    private ref BoundingFrustum SceneFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref ScenePlaneRef);

    internal void BuildFrustum(CameraTransformSnapshot sceneTransform, CameraTransformSnapshot lightTransform)
    {
        var transposed = Matrix4x4.Transpose(lightTransform.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out LightFrustum);

        transposed = Matrix4x4.Transpose(sceneTransform.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out SceneFrustum);
    }

    internal void Execute()
    {
        ++Version;

        var visibleCount = CullEntities(RenderEcs.EntityCount);
        VisibleCount = visibleCount;

        if (visibleCount == 0) return;

        _data.RawSortKeys.AsSpan(0, visibleCount).Sort();
    }


    private int CullEntities(int length)
    {
        var sortKeys = _data.SortKeys.AsSpan();
        var policies = _data.Policies.AsReadOnlySpan();
        var worldBounds = _data.WorldBounds.AsReadOnlySpan();
        var visibilitySet = _data.VisibleSet;

        int visibleCount = 0;
        for (int start = 0; start < length; start += 64)
        {
            int end = int.Min(start + 64, length);

            BitBlock visibilityBits = default;
            for (int index = start; index < end; ++index)
            {
                var policy = policies[index];
                if (policy.Status == EntityDrawStatus.ForceHidden) continue;
                
                ref readonly var bounds = ref worldBounds[index];
                var passes = Intersects(policy.Passes, policy.Status, in bounds, out float distance);

                if (passes != 0)
                {
                    sortKeys[visibleCount++] = DrawEntityKey.Create(index, passes, distance, policy.Queue);
                    visibilityBits.ToggleOn(index);
                }
            }

            visibilitySet.SetBlockAt(start, visibilityBits.Block);
        }

        return visibleCount;
    }

    private PassMask Intersects(PassMask passes, EntityDrawStatus status, in BoundingAxisBox bounds, out float distance)
    {
        var center = new Vector4(bounds.Center, 1f).AsVector128();
        var extent = new Vector4(bounds.Extent, 0f).AsVector128();

        ref var plane = ref MemoryMarshal.GetArrayDataReference(_frustumPlanes);
        if (status == EntityDrawStatus.AlwaysVisible)
        {
            distance = DistanceFromPlane(in Unsafe.Add(ref plane, 11), center, extent);
            return passes;
        }

        var depthTest = (passes & PassMask.Depth) != 0 && TestIntersect(ref plane, center, extent);
        var sceneTest = (passes & PassMask.Main) != 0 && TestIntersect(ref Unsafe.Add(ref plane, 6), center, extent);

        distance = DistanceFromPlane(in Unsafe.Add(ref plane, 11), center, extent);

        byte culledMask = 0;
        culledMask |= (byte)(-Unsafe.BitCast<bool, byte>(depthTest) & (int)PassMask.Depth);
        culledMask |= (byte)(-Unsafe.BitCast<bool, byte>(sceneTest) & (int)PassMask.Main);
        return (PassMask)culledMask;
        
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestIntersect(ref Vector4 frustum, Vector128<float> center4, Vector128<float> extent4)
    {
        ref var plane = ref frustum;
        ref readonly var end = ref Unsafe.Add(ref plane, 5);
        while (Unsafe.IsAddressLessThanOrEqualTo(ref plane, in end))
        {
            var d = DistanceFromPlane(in plane, center4, extent4);
            if (d <= 0f) return false;
            plane = ref Unsafe.Add(ref plane, 1);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float DistanceFromPlane(in Vector4 plane, Vector128<float> center4, Vector128<float> extent4)
    {
        var p = plane.AsVector128();
        return Vector256.Dot(Vector256.Create(center4, extent4), Vector256.Create(p, Vector128.Abs(p)));
    }
}