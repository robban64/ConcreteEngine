using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
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
        var forward = new Vector4(CameraManager.Instance.Camera.Forward, 0);
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;

        int count = 0;
        var indices = _indices.AsView().Ptr;
        foreach (var query in CullQuery())
        {
            var status = query.Item1.Status == EntityDrawStatus.AlwaysVisible;
            var mask = status
                ? query.Item1.Passes
                : _frustum.Intersects(query.Item1.Passes, in query.Item2);

            if (mask != 0)
            {
                var depthKey = FrustumMath.MakeDepthKey(forward, in query.Item2.Center, nearFar, viewZ);
                indices[count++] = new DrawEntityIndex(query.Entity, mask, query.Item1.Queue, (ushort)depthKey);
                
                var res = query.Item1.WithMask(mask);
                query.Item1 = res;
            }
        }

        return VisibleCount = count;
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