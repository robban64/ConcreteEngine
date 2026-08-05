using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderResolver : IDisposable
{
    private readonly CameraFrustum _frustum;

    private NativeArray<DrawCommandIndex> _drawIndices;
    private NativeArray<DrawObjectUniform> _transforms;

    internal RenderResolver(CameraFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;
        _drawIndices = NativeArray.Allocate<DrawCommandIndex>(RenderEcs.Core.Capacity, false);
        _transforms = NativeArray.Allocate<DrawObjectUniform>(RenderEcs.Core.Capacity, false);
    }

    public NativeView<DrawCommandIndex> DrawIndices => _drawIndices.Slice(0, RenderEcs.Frame.VisibleCount);
    public NativeView<DrawObjectUniform> Transforms => _transforms.Slice(0, RenderEcs.Frame.VisibleCount);

        
    public void Dispose()
    {
        _drawIndices.Dispose();
        _transforms.Dispose();
    }

    
    public void Setup() { }


    public void Execute()
    {
        Ensure();
        var visibleCount = CullEntities();
        if (visibleCount == 0) return;
        SubmitDrawPolicy();
        SubmitTransforms();
    }

    private int CullEntities()
    {
        var visibleEntities = RenderEcs.Frame.WriteVisibleEntities();

        var visibleCount = 0;
        foreach (var query in RenderEcs.Core.CullQuery())
        {
            var visible = query.Item1.Status == EntityStatus.AlwaysVisible ||
                          _frustum.IntersectsBox(in query.Item2);
            
            if (visible) visibleEntities[visibleCount++] = query.Entity;
            query.Item1.Visible = visible;
        }
        
        RenderEcs.Frame.CommitFrame(visibleCount);
        return visibleCount;
    }

    private unsafe void SubmitDrawPolicy()
    {
        var forward = CameraManager.Instance.Camera.Forward;
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;

        var indices = _drawIndices.Ptr;

        var index = -1;
        foreach (var it in RenderEcs.Core.ModelPolicyQuery(RenderEcs.Frame.VisibleEntities))
        {
            var depthKey = FrustumMath.MakeDepthKey( forward, it.Item2.Translation, nearFar, viewZ);
            *indices = new DrawCommandIndex(++index, it.Item1.Passes, it.Item1.Queue, depthKey);
            ++indices;
        }
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
        if (RenderEcs.Core.Capacity != _drawIndices.Length)
        {
            _drawIndices.ReAlloc(RenderEcs.Core.Capacity, true);
            _transforms.ReAlloc(RenderEcs.Core.Capacity, true);
            Console.WriteLine("Resized draw resolver");
        }
    }

}