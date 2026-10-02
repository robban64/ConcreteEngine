using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.ECS.Render;

public sealed class EcsCullSystem
{
    public ulong Version { get; private set; }
    public int VisibleCount { get; private set; }

    private readonly EntityDataStore _data;

    private readonly Vector4[] _frustumPlanes = new Vector4[12];

    public EcsCullSystem(EntityDataStore data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _data = data;
    }

    private ref BoundingFrustum LightFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref _frustumPlanes[0]);
    private ref BoundingFrustum SceneFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref _frustumPlanes[6]);

    private AvgFrameTimer avg;

    //
    internal void Execute(CameraTransformSnapshot sceneTransform, CameraTransformSnapshot lightTransform)
    {
        ++Version;

        BuildFrustum(sceneTransform, lightTransform);

        avg.BeginSample();
        var visibleCount = CullEntities(RenderEcs.EntityCount);
        VisibleCount = visibleCount;
        if (avg.EndSample() > 100) avg.ResetAndPrint();

        if (visibleCount == 0) return;

        _data.RawSortKeys.AsSpan(0, visibleCount).Sort();
    }

    private void BuildFrustum(CameraTransformSnapshot sceneTransform, CameraTransformSnapshot lightTransform)
    {
        var transposed = Matrix4x4.Transpose(lightTransform.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out LightFrustum);

        transposed = Matrix4x4.Transpose(sceneTransform.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out SceneFrustum);
    }

    private static ulong Filter(BitBlock bits, ReadOnlySpan<DrawPolicy> span)
    {
        var entityBits = bits;
        for (int i = 0; i < span.Length; ++i)
        {
            if (span[i].Status == DrawStatus.ForceHidden) entityBits.Disable(i);
        }

        return entityBits;
    }

    private int CullEntities(int entityCount)
    {
        var sortKeys = _data.SortKeys.AsSpan(0, entityCount);
        var policies = _data.Policies.AsReadOnlySpan(0, entityCount);
        var worldBounds = _data.WorldBounds.AsReadOnlySpan(0, entityCount);
        var visibilitySet = _data.VisibleSet;
        var entitySet = _data.EntitySet;

        int visibleCount = 0;

        for (int start = 0; start < entityCount; start += 64)
        {
            var length = int.Min(start + 64, entityCount) - start;

            var innerSortKeys = sortKeys.Slice(visibleCount, length);
            var innerPolices = policies.Slice(start, length);
            var innerBounds = worldBounds.Slice(start, length);

            ulong entityBits = Filter(entitySet.GetBlockAtBit(start), innerPolices);

            int visibleIndex = 0;
            BitBlock visibilityBits = default;
            while (entityBits != 0)
            {
                var i = BitOperations.TrailingZeroCount(entityBits);
                entityBits &= entityBits - 1;

                var policy = innerPolices[i];
                ref readonly var bounds = ref innerBounds[i];
                var passes = Intersects(policy.Passes, policy.Status, in bounds, out float distance);

                if (passes != 0)
                {
                    var entity = start + i;
                    innerSortKeys[visibleIndex++] = DrawEntityKey.Create(entity, passes, distance, policy.Queue);
                    visibilityBits.Enable(entity);
                }
            }

            visibilitySet.SetBlockAtBit(start, visibilityBits);
            visibleCount += visibleIndex;
        }

        return visibleCount;
    }

    private PassMask Intersects(PassMask passes, DrawStatus status, in BoundingAxisBox bounds, out float distance)
    {
        var center = new Vector4(bounds.Center, 1f).AsVector128();
        var extent = new Vector4(bounds.Extent, 0f).AsVector128();

        ref var plane = ref MemoryMarshal.GetArrayDataReference(_frustumPlanes);
        if (status == DrawStatus.AlwaysVisible)
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