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

public sealed class RenderEcsSystem
{
    public ulong FrameVersion {get; private set;}
    public int VisibleCount { get; private set; }
    
    private readonly Vector4[] _frustumPlanes = new Vector4[12];

    private readonly RenderEntityCore.EntityDataStore _entityDataStore;

    public RenderEcsSystem(RenderEntityCore.EntityDataStore entityDataStore)
    {
        _entityDataStore = entityDataStore;
    }
    
    internal NativeView<DrawEntityKey> SortKeys() => _entityDataStore.DrawKeyView(VisibleCount).Reinterpret<DrawEntityKey>();

    private ref Vector4 LightPlaneRef => ref MemoryMarshal.GetArrayDataReference(_frustumPlanes);
    private ref Vector4 ScenePlaneRef => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_frustumPlanes), 6);

    private ref BoundingFrustum LightFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref LightPlaneRef);
    private ref BoundingFrustum MainFrustum => ref Unsafe.As<Vector4, BoundingFrustum>(ref ScenePlaneRef);

    internal void BuildFrustum(CameraTransformSnapshot sceneTransform, CameraTransformSnapshot lightTransform)
    {
        var transposed = Matrix4x4.Transpose(lightTransform.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out Unsafe.As<Vector4, BoundingFrustum>(ref LightPlaneRef));

        transposed = Matrix4x4.Transpose(sceneTransform.ProjectionViewMatrix);
        BoundingFrustum.From(in transposed, out Unsafe.As<Vector4, BoundingFrustum>(ref ScenePlaneRef));

    }

    private AvgFrameTimer avg;

    internal void Execute()
    {
        avg.BeginSample();
        var visibleCount = VisibleCount = CullEntities();
        if (avg.EndSample() > 144) avg.ResetAndPrint();

        if (visibleCount > 1)
            _entityDataStore.DrawKeyView(VisibleCount).AsSpan().Sort();
        
        ++FrameVersion;
    }

    private unsafe int CullEntities()
    {
        var indicesView = _entityDataStore.DrawKeyView(RenderEcs.Core.Count).Reinterpret<DrawEntityKey>();
        var indices = indicesView.Ptr;

        var visibilitySet = _entityDataStore.VisibleSet;
        var policies = _entityDataStore.PolicyView(RenderEcs.Core.Count).AsSpan();
        var worldBounds = _entityDataStore.WorldBoundView(RenderEcs.Core.Count).AsSpan();

        int blockCount = visibilitySet.BlockCount;
        for (int blockIndex = 0; blockIndex < blockCount; ++blockIndex)
        {
            var start = blockIndex * 64;
            var end = start + 64 <= policies.Length ? start + 64 : start + policies.Length & 63;

            if ((uint)start >= (uint)policies.Length) break;

            BitBlock block = default;
            for (int index = start; index < end; ++index)
            {
                var policy = policies[index];
                if (policy.Status == EntityDrawStatus.ForceHidden) continue;

                ref readonly var bounds = ref worldBounds[index];
                var passes = Intersects(policy.Passes, in bounds, out var distance);
                passes = policy.Status == EntityDrawStatus.AlwaysVisible ? policy.Passes : passes;

                if (passes != 0)
                {
                    ushort depthKey = (ushort)float.Min(0f, float.Max(distance, 65535f));
                    *indices++ = DrawEntityKey.Create(index, passes, depthKey, policy.Queue);
                    block.ToggleOn(index);
                }
            }

            visibilitySet.SetBlock(blockIndex, block.Block);
        }

        return (int)(indices - indicesView.Ptr);
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
            var dist = DistanceFromPlane(center, extent, in Unsafe.Add(ref plane, 10));
            distance = dist;
            culledPasses |= test;
        }
        else
        {
            distance = 0;
        }

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
    private static float DistanceFromPlane(Vector4 center4, Vector4 extent4, in Vector4 plane)
    {
        var d1 = Vector256.Create(center4.AsVector128(), extent4.AsVector128());
        var d2 = Vector256.Create(plane.AsVector128(), Vector128.Abs(plane.AsVector128()));
        return Vector256.Dot(d1, d2);
    }

}