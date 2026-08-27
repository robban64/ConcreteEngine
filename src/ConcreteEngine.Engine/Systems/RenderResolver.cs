using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderResolver : IDisposable
{
    public int VisibleCount { get; private set; }

    private readonly CameraFrustum _frustum;

    private NativeSoA<RenderEntityId, uint> _visibleIndices;
    private NativeArray<TransformUniform> _transforms;

    internal RenderResolver(CameraFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;
        _visibleIndices = new NativeSoA<RenderEntityId, uint>(RenderEcs.Core.Capacity);
        _transforms = NativeArray.AlignedAllocate<TransformUniform>(RenderEcs.Core.Capacity, 64, false);
    }

    public NativeView<TransformUniform> Transforms => _transforms.Slice(0, VisibleCount);

    public NativeView<uint> SortIndices => _visibleIndices.View2.Slice(0, VisibleCount);
    public NativeView<RenderEntityId> VisibleEntities => _visibleIndices.View1.Slice(0, VisibleCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RenderEntityId GetEntity(int index) => _visibleIndices.At1(index);

    public void Execute()
    {
        Ensure();
        var visibleCount = CullEntities();
        if (visibleCount == 0) return;
        Debug.Assert((uint)visibleCount <= (uint)_visibleIndices.Length);
        var entitySpan = VisibleEntities.Reinterpret<int>().AsSpan();
        SortIndices.AsSpan().Sort(entitySpan);
        SubmitTransforms();
    }

    private unsafe int CullEntities()
    {
        var forward = new Vector4(CameraManager.Instance.Camera.Forward, 0);
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;
        
        var indices = _visibleIndices.View2.Ptr;
        var visibleEntities = _visibleIndices.View1.Ptr;
        foreach (var query in CullQuery())
        {
            var status = query.Item1.Status == EntityDrawStatus.AlwaysVisible;
            var originalMask = query.Item1.Passes;
            var mask = status
                ? query.Item1.Passes
                : _frustum.Intersects(query.Item1.Passes, in query.Item2);

            if (mask != 0)
            {
                var depthKey = FrustumMath.MakeDepthKey(forward, in query.Item2.Center, nearFar, viewZ);
                *indices++ = PackSortKey32(mask, query.Item1.Queue, depthKey);
                *visibleEntities++ = query.Entity;

                var res = query.Item1.WithMask(mask);
                query.Item1 = res;
            }
            else if (originalMask != mask)
            {
                var res = query.Item1.WithMask(mask);
                query.Item1 = res;
            }
        }

        return VisibleCount = (int)(indices - _visibleIndices.View2.Ptr);
    }

    private unsafe void SubmitTransforms()
    {
        var dst = _transforms.AsView().Ptr;
        foreach (var query in TransformQuery())
        {
            *dst++ = query.Item1;
        }
    }

    private void Ensure()
    {
        if (RenderEcs.Core.Capacity == _transforms.Length) return;

        _visibleIndices.ReAlloc(RenderEcs.Core.Capacity, true);
        _transforms.ReAlloc(RenderEcs.Core.Capacity, false);
        Logger.Log(LogScope.Ecs, "Transform uniform buffer resized", LogLevel.Warn);
    }

    public void Dispose()
    {
        _visibleIndices.Dispose();
        _transforms.Dispose();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private RenderEntityCore.SparseQueryEnumerator<TransformUniform> TransformQuery() =>
        new(VisibleEntities, RenderEcs.Core.GetTransformView());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RenderEntityCore.QueryEnumerator<BoundingAxisBox> CullQuery() =>
        new(RenderEcs.Core.GetDrawPolicyView(), RenderEcs.Core.GetWorldBoundView(), EntityDrawStatus.ForceHidden);
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint PackSortKey32(PassMask mask, DrawQueue queue, float depthKey)
    {
        ushort depth;
        if (queue < DrawQueue.Transparent) depth = (ushort)depthKey;
        else depth = (ushort)(ushort.MaxValue - (ushort)depthKey);
        return (uint)mask | ((uint)depth << 8) | ((uint)queue << 24);
    }
}