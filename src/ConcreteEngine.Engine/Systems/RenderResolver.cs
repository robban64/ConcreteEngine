using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
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
        _drawIndices = NativeArray.Allocate<DrawCommandIndex>(RenderEcs.Core.Capacity);
        _transforms = NativeArray.Allocate<DrawObjectUniform>(RenderEcs.Core.Capacity);
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
        //ProcessSelectionEffect();
        SubmitDrawPolicy();
        SubmitTransforms();
        //SubmitDebugBounds();
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
        foreach (var it in RenderEcs.Core.DepthPolicyQuery(RenderEcs.Frame.VisibleEntities))
        {
            var depthKey = MakeDepthKey( forward, it.Item2.Translation, nearFar, viewZ);
            *indices = new DrawCommandIndex(++index, it.Item1.Passes, it.Item1.Queue, depthKey);
            ++indices;
        }
    }

    private unsafe void SubmitTransforms()
    {
        var transforms = _transforms.Ptr;
        foreach (var query in RenderEcs.Core.MatrixQuery(RenderEcs.Frame.VisibleEntities))
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
            _drawIndices.Resize(RenderEcs.Core.Capacity, true);
            _transforms.Resize(RenderEcs.Core.Capacity, true);
            Console.WriteLine("Resized draw resolver");
        }
    }


/*

  private void ProcessSelectionEffect()
      {
          if (RenderEcs.GetRenderStore<SelectionComponent>().Count == 0) return;

          foreach (var query in RenderEcs.GetRenderStore<SelectionComponent>().VisibilityQuery())
          {
              var slot = EffectBuffer.Submit(new EffectUniformParams(query.Component.HighlightColor));
              ref var source = ref RenderEcs.Core.GetSource(query.Entity);
              source.Resolver = DrawCommandResolver.Highlight;
              source.ResolverSlot = slot;

              RenderEcs.Core.GetDrawPolicy(query.Entity).Passes = PassMask.Effect | PassMask.Depth;
          }
      }
    private void SubmitDebugBounds()
    {
        if (RenderEcs.GetRenderStore<DebugBoundsComponent>().Count == 0) return;

        var materialId = AssetStore.Core.DebugBoundsMaterial.MaterialId;

        var index = VisibleCount;
        foreach (var query in RenderEcs.GetRenderStore<DebugBoundsComponent>().VisibilityQuery())
        {
            ref var bufferDst = ref _drawBuffer.TransformRef(index);
            ref readonly var worldBounds = ref RenderEcs.Core.GetWorldBounds(query.Entity);
            MatrixMath.CreateModelMatrix(
                worldBounds.Center,
                worldBounds.Extent,
                Quaternion.Identity,
                out bufferDst.Model
            );
            bufferDst.Normal = Matrix3X4.Identity;

            var depthKey = CameraManager.Instance.Camera.MakeDepthKey(bufferDst.Model.Translation);

            var slot = _effectBuffer.Submit(new EffectUniformParams(query.Component.Color));

            _drawBuffer.CommandRef(index) = new DrawCommand(GfxMeshes.Cube, materialId,
                resolver: DrawCommandResolver.BoundingVolume, resolverSlot: slot);
            _drawBuffer.IndexRef(index) = new DrawCommandIndex(index, PassMask.Effect, DrawQueue.Effect, depthKey);

            ++index;
        }

        _drawBuffer.IncrementDrawCount(index);
    }
*/

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort MakeDepthKey( Vector3 forward, Vector3 worldPos, Vector2 nearFar, float viewZ)
    {
        const float maxValueF = 65535f;

        var d = Vector3.Dot(forward, worldPos) - viewZ;
        if (d <= nearFar.X) return 0;
        if (d >= nearFar.Y) return ushort.MaxValue;
        var t = (d - nearFar.X) / (nearFar.Y - nearFar.X);
        return (ushort)(t * maxValueF + 0.5f);
    }
}