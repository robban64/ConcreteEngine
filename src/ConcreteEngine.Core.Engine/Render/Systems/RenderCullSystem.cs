using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Render.Systems;

public sealed class RenderCullSystem : RenderWorldSystem
{
    public int VisibleCount { get; private set; }

    private readonly RenderMetaStore _metaStore;

    private readonly Vector4[] _frustum = new Vector4[12];

    public RenderCullSystem(RenderMetaStore metaStore)
    {
        ArgumentNullException.ThrowIfNull(metaStore);
        _metaStore = metaStore;
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

        _metaStore.RawSortKeys.AsSpan(0, visibleCount).Sort();
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
        var sortKeys = _metaStore.SortKeys.Slice(0, entityCount);
        var policies = _metaStore.Policies.AsReadOnlySpan();
        var worldBounds = _metaStore.WorldBounds.AsReadOnlySpan();
        var visibilitySet = _metaStore.VisibleSet;
        var entitySet = _metaStore.EntitySet;

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
                var alwaysVisible = policy.Cull == EntityCullStatus.AlwaysVisible;

                var cullPasses = alwaysVisible ? 0 : policy.Passes;
                var testPasses = Intersects(cullPasses, in innerBounds[i], out float distance);
                var passes = alwaysVisible ? policy.Passes : testPasses;

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

    private PassMask Intersects(PassMask passes, in WorldBox box, out float distance)
    {
        //var center = new Vector4(bounds.Center, 1f).AsVector128();
        //var extent = new Vector4(bounds.Extent, 0f).AsVector128();
        //var centerExtent = Vector256.Create(center, extent);

        var centerExtent = Unsafe.BitCast<WorldBox, Vector256<float>>(box);
        ReadOnlySpan<Vector4> planes = _frustum;
        var depthTest = (passes & PassMask.Depth) != 0 && TestIntersect(planes.Slice(0, 6), in centerExtent);
        var sceneTest = (passes & PassMask.Main) != 0 && TestIntersect(planes.Slice(6, 6), in centerExtent);

        distance = DistanceFromPlane(in planes[^1], in centerExtent);

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
    private static bool TestIntersect(ReadOnlySpan<Vector4> frustum, in Vector256<float> centerExtent)
    {
        foreach (ref readonly var plane in frustum)
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