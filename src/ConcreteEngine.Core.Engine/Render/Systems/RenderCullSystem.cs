using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.Graphics;

namespace ConcreteEngine.Core.Engine.Render.Systems;

public sealed class RenderCullSystem : RenderWorldSystem
{
    public int VisibleCount { get; private set; }

    private readonly Vector4[] _frustum = new Vector4[12];

    private readonly RenderWorld.RenderBitSet _ignoreSet;

    public RenderCullSystem()
    {
        _ignoreSet = RenderWorld.CreateSet();
    }

    private ref BoundingFrustum LightFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref _frustum[0]);
    private ref BoundingFrustum SceneFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref _frustum[6]);

    internal void Execute(long frameId, Camera camera)
    {
        FrameVersion = frameId;

        BuildFrustum(camera);

        if (RenderWorld.EntityCount == 0) return;

        _ignoreSet.Set.Clear();
        FilterEntities();

        var visibleCount = CullEntities2();
        VisibleCount = visibleCount;

        if (visibleCount == 0) return;

        var sortKeys64 = RenderWorld.Dense<DrawEntityKey>().AsView().Reinterpret<ulong>().AsSpan(0, visibleCount);
        sortKeys64.Sort();
    }

    private void BuildFrustum(Camera camera)
    {
        var transposed = Matrix4x4.Transpose(camera.LightTransforms.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out LightFrustum);

        transposed = Matrix4x4.Transpose(camera.FrameTransforms.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out SceneFrustum);
    }

    private void FilterEntities()
    {
        var ignore = _ignoreSet.Set;

        foreach (var query in RenderWorld.Query.New<DrawPolicy>().Filter())
        {
            var chunk = Bit256.Zero;
            query.ForEachLane(ref chunk, static (ref ctx, lane, start, block, data1) =>
            {
                var ignoreBits = Bit64.Zero;
                var span = data1.Slice(start);
                foreach (var bit in block)
                {
                    var policy = span[bit];
                    if (policy.Cull == EntityCullStatus.ForceHidden)
                    {
                        ignoreBits.Enable(bit);
                    }
                }

                ctx.SetBit64(lane, ignoreBits);
            });

            ignore.SetBlock256(query.BlockIdx, chunk);
        }
    }

    private int CullEntities2()
    {
        var visibleIndex = 0;
        var visibilitySet = RenderWorld.Meta.VisibleSet;
        var sortKeySpan = RenderWorld.Dense<DrawEntityKey>().AsSpan();
        foreach (var query in RenderWorld.Query.New<DrawPolicy, WorldBox>()
                     .Filter(BitOp.AndNot, RenderWorld.Meta.EntitySet, _ignoreSet.Set))
        {
            var chunk = Bit256.Zero;
            var lanes = query.Lanes;
            for (int lane = 0; lane < lanes; lane++)
            {
                var idx = 0;
                var block = Bit64.Zero;
                var sortKeys = sortKeySpan.Slice(visibleIndex);
                foreach (var it in query.EnumerateLane(lane))
                {
                    var culledPasses = Intersects(it.Component1.Passes, in it.Component2, out float distance);
                    var passes = it.Component1.Cull == EntityCullStatus.AlwaysVisible
                        ? it.Component1.Passes
                        : culledPasses;
                    
                    if (passes != 0)
                    {
                        sortKeys[idx++] = DrawEntityKey.Create(it.Entity, passes, distance, it.Component1.Queue);
                        block.Enable(it.EntityBit);
                    }
                }

                chunk.SetBit64(lane, block);
                visibleIndex += idx;
            }

            visibilitySet.SetBit256(query.BlockIdx, chunk);
        }

        return visibleIndex;
    }

    private int CullEntities(int entityCount)
    {
        var sortKeys = RenderWorld.Dense<DrawEntityKey>().AsSpan();
        var policies = RenderWorld.Dense<DrawPolicy>().AsReadOnlySpan();
        var worldBounds = RenderWorld.Dense<WorldBox>().AsReadOnlySpan();

        var entitySet = RenderWorld.Meta.EntitySet;
        var visibilitySet = RenderWorld.Meta.VisibleSet;

        int visibleCount = 0;

        for (int start = 0; start < entityCount; start += 64)
        {
            var length = int.Min(start + 64, entityCount) - start;

            var innerSortKeys = sortKeys.Slice(visibleCount, length);
            var innerPolices = policies.Slice(start, length);
            var innerBounds = worldBounds.Slice(start, length);

            var entityBits = Filter(entitySet.GetAtBit64(start), innerPolices);

            int visibleIndex = 0;
            Bit64 visibilityBits = new(0);
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

            visibilitySet.SetAtBit64(start, visibilityBits);
            visibleCount += visibleIndex;
        }

        return visibleCount;
    }

    private PassMask Intersects(PassMask passes, in WorldBox box, out float distance)
    {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalculateDistance(in WorldBox box)
    {
        var centerExtent = Unsafe.BitCast<WorldBox, Vector256<float>>(box);
        return DistanceFromPlane(in _frustum[^1], in centerExtent);
    }

    private static Bit64 Filter(Bit64 bits, ReadOnlySpan<DrawPolicy> span)
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