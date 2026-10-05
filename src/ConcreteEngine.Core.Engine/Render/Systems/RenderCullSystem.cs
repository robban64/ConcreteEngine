using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Render.Systems;

public sealed class RenderCullSystem : RenderWorldSystem
{
    public int VisibleCount { get; private set; }

    private readonly RenderData _data;

    private readonly Vector4[] _frustum = new Vector4[12];

    public RenderCullSystem(RenderData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _data = data;
    }

    private ref BoundingFrustum LightFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref _frustum[0]);
    private ref BoundingFrustum SceneFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref _frustum[6]);

    //
    internal void Execute(long frameId, Camera camera)
    {
        FrameVersion = frameId;

        BuildFrustum(camera);

        var entityCount = RenderWorld.EntityCount;
        if (entityCount == 0) return;

        var visibleCount = CullEntities(entityCount);
        VisibleCount = visibleCount;

        if (visibleCount == 0) return;

        _data.RawSortKeys.AsSpan(0, visibleCount).Sort();
    }

    private void BuildFrustum(Camera camera)
    {
        var transposed = Matrix4x4.Transpose(camera.LightTransforms.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out LightFrustum);

        transposed = Matrix4x4.Transpose(camera.FrameTransforms.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out SceneFrustum);
    }


    private int CullEntities(int entityCount)
    {
        var sortKeys = _data.SortKeys.Slice(0, entityCount);
        var policies = _data.Policies.AsReadOnlySpan();
        var worldBounds = _data.WorldBounds.AsReadOnlySpan();
        var visibilitySet = _data.VisibleSet;
        var entitySet = _data.EntitySet;

        int visibleCount = 0;

        for (int start = 0; start < entityCount; start += 64)
        {
            var length = int.Min(start + 64, entityCount) - start;

            var innerSortKeys = sortKeys.Slice(visibleCount, length);
            var innerPolices = policies.Slice(start, length);
            var innerBounds = worldBounds.Slice(start, length);

            var entityBits = Filter(entitySet.GetBlockAtBit(start), innerPolices);

            int visibleIndex = 0;
            BitBlock visibilityBits = default;
            foreach (var i in entityBits)
            {
                var policy = innerPolices[i];
                ref readonly var bounds = ref innerBounds[i];

                var passes = Intersects(policy.Passes, policy.Cull, in bounds, out float distance);

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

    private PassMask Intersects(PassMask passes, EntityCullStatus status, in BoundingAxisBox bounds, out float distance)
    {
        var center = new Vector4(bounds.Center, 1f).AsVector128();
        var extent = new Vector4(bounds.Extent, 0f).AsVector128();
        var centerExtent = Vector256.Create(center, extent);

        Span<Vector4> span = _frustum;
        if (status == EntityCullStatus.AlwaysVisible)
        {
            distance = DistanceFromPlane(in span[^1], in centerExtent);
            return passes;
        }

        var depthTest = (passes & PassMask.Depth) != 0 && TestIntersect(span.Slice(0, 6), in centerExtent);
        var sceneTest = (passes & PassMask.Main) != 0 && TestIntersect(span.Slice(6, 6), in centerExtent);

        distance = DistanceFromPlane(in span[^1], in centerExtent);

        PassMask culledMask = 0;
        culledMask |= depthTest ? PassMask.Depth : 0;
        culledMask |= sceneTest ? PassMask.Main : 0;
        return culledMask;
    }

    private static BitBlock Filter(BitBlock bits, ReadOnlySpan<DrawPolicy> span)
    {
        var entityBits = bits;
        for (int i = 0; i < span.Length; ++i)
        {
            var status = span[i].Cull;
            if (status == EntityCullStatus.ForceHidden) bits.Disable(i);
        }

        return entityBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TestIntersect(Span<Vector4> frustum, in Vector256<float> centerExtent)
    {
        foreach (var plane in frustum)
        {
            var d = DistanceFromPlane(in plane, in centerExtent);
            if (d <= 0f) return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float DistanceFromPlane(in Vector4 plane, in Vector256<float> centerExtent)
    {
        var p = plane.AsVector128();
        return Vector256.Dot(centerExtent, Vector256.Create(p, Vector128.Abs(p)));
    }

    public override void Dispose() { }
}