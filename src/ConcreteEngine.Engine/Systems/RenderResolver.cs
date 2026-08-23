using System.Diagnostics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderResolver : IDisposable
{
    public int VisibleCount { get; private set; }

    private readonly CameraFrustum _frustum;

    private NativeArray<DrawEntityIndex> _indices;
    private NativeArray<TransformUniform> _transforms;

    internal RenderResolver(CameraFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;
        _indices = NativeArray.Allocate<DrawEntityIndex>(RenderEcs.Core.Capacity);
        _transforms = NativeArray.Allocate<TransformUniform>(RenderEcs.Core.Capacity, false);
    }

    public NativeView<DrawEntityIndex> DrawIndices => _indices.Slice(0, VisibleCount);
    public NativeView<TransformUniform> Transforms => _transforms.Slice(0, VisibleCount);

    public void Execute()
    {
        Ensure();
        var visibleCount = CullEntities();
        if (visibleCount == 0) return;
        Debug.Assert((uint)visibleCount <= (uint)_indices.Length);
        Debug.Assert(_indices[visibleCount - 1].IsValid());

        DrawIndices.Reinterpret<ulong>().AsSpan().Sort();
        SubmitTransforms();
    }


    private unsafe int CullEntities()
    {
        var forward = CameraManager.Instance.Camera.Forward;
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;

        var visibleEntities = _indices.Ptr;
        foreach (var query in CullQuery())
        {
            var it = query.Item1;
            var mask = it.Status == EntityDrawStatus.AlwaysVisible
                ? it.Passes
                : _frustum.Intersects(it.Passes, in query.Item2);

            if (mask != 0)
            {
                query.Item1.VisiblePassMask = mask;
                var depthKey = FrustumMath.MakeDepthKey(forward, query.Item2.Center, nearFar, viewZ);
                *visibleEntities++ = new DrawEntityIndex(query.Entity, mask, it.Queue, (ushort)depthKey);
            }
        }

        return VisibleCount = (int)(visibleEntities - _indices);
    }

    private unsafe void SubmitTransforms()
    {
        var indexView = DrawIndices;
        var dst = _transforms.Ptr;
        var indices = indexView.Ptr;
        var indicesEnd = indexView.EndPtr;

        var views = RenderEcs.Core.GetTransformView();
        while (indices < indicesEnd)
        {
            *dst++ = views[indices++->Entity.Index()];
        }
    }

    private void Ensure()
    {
        if (RenderEcs.Core.Capacity == _transforms.Length) return;

        _indices.ReAlloc(RenderEcs.Core.Capacity, true);
        _transforms.ReAlloc(RenderEcs.Core.Capacity, false);
        Logger.Log(LogScope.Ecs, "Transform uniform buffer resized", LogLevel.Warn);
    }

    public void Dispose()
    {
        _indices.Dispose();
        _transforms.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static RenderEntityCore.QueryEnumerator<BoundingAxisBox> CullQuery() =>
        new(RenderEcs.Core.GetDrawPolicyView(), RenderEcs.Core.GetWorldBoundView(), EntityDrawStatus.ForceHidden);
}