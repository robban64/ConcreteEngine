using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderResolver : IDisposable
{
    private readonly CameraFrustum _frustum;
    private NativeArray<TransformUniform> _transforms;

    internal RenderResolver(CameraFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;
        _transforms = NativeArray.Allocate<TransformUniform>(RenderEcs.Core.Capacity, false);
    }

    public NativeView<TransformUniform> Transforms => _transforms.Slice(0, RenderEcs.Frame.VisibleCount);

    public void Setup() { }

    public void Execute()
    {
        avg.BeginSample();
        Ensure();
        var visibleCount = CullEntities();
        RenderEcs.Frame.CommitFrame(visibleCount);
        if (visibleCount == 0) return;
        SubmitTransforms();
        avg.EndSample();
    }

    public static AvgFrameTimer avg;


    private int CullEntities()
    {
        var forward = CameraManager.Instance.Camera.Forward;
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;

        var index = 0;
        var visibleEntities = RenderEcs.Frame.WriteVisibleEntities();
        foreach (var query in RenderEcs.Core.CullQuery())
        {
            var it = query.Item1;
            var mask = it.Status == EntityDrawStatus.AlwaysVisible
                ? it.Passes
                : _frustum.Intersects(it.Passes, in query.Item2);

            if (mask != 0)
            {
                query.Item1.VisiblePassMask = mask;

                var depthKey = FrustumMath.MakeDepthKey(forward, query.Item2.Center, nearFar, viewZ);
                visibleEntities[index++] = new DrawEntityIndex(query.Entity, mask, it.Queue, (ushort)depthKey);
            }
        }
        return index;
    }

    private unsafe void SubmitTransforms()
    {
        var transforms = _transforms.Ptr;
        foreach (var query in RenderEcs.Core.TransformQuery(RenderEcs.Frame.VisibleEntities))
        {
            transforms->Model = query.Item1;
            transforms->Normal = query.Item2;
            ++transforms;
        }
    }

    private void Ensure()
    {
        if (RenderEcs.Core.Capacity != _transforms.Length)
        {
            _transforms.ReAlloc(RenderEcs.Core.Capacity, false);
            Logger.Log(LogScope.Ecs, "Transform uniform buffer resized", LogLevel.Warn);
        }
    }
    
    public void Dispose() => _transforms.Dispose();


/*
    private unsafe void SubmitDrawPolicy()
    {
        var forward = CameraManager.Instance.Camera.Forward;
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;

        var indices = _drawIndices.Ptr;

        var index = -1;
        foreach (var it in RenderEcs.Core.DrawPolicyQuery(RenderEcs.Frame.VisibleEntities))
        {
            var depthKey = FrustumMath.MakeDepthKey(forward, it.Item2.Center, nearFar, viewZ);
            *indices = new DrawCommandIndex(++index, it.Item1.VisibleMask, it.Item1.Queue, depthKey);
            ++indices;
        }
    }
*/
}