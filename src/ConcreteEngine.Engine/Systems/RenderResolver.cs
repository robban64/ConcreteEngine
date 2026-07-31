using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderEcsSystem : IDisposable
{
    public int VisibleCount { get; private set; }

    public static RenderEcsSystem Instance { get; private set; } = null!;

    private readonly CameraFrustum _frustum;

    private NativeArray<RenderEntityId> _visibleEntities;
    private NativeArray<DrawCommandIndex> _drawIndices;
    private NativeArray<DrawObjectUniform> _transforms;

    internal RenderEcsSystem(CameraFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;
        _visibleEntities = NativeArray.Allocate<RenderEntityId>(RenderEcs.Core.Capacity);
        _drawIndices = NativeArray.Allocate<DrawCommandIndex>(RenderEcs.Core.Capacity);
        _transforms = NativeArray.Allocate<DrawObjectUniform>(RenderEcs.Core.Capacity);
        Instance = this;
    }

    public NativeView<RenderEntityId> VisibleEntities => _visibleEntities.Slice(0, VisibleCount);
    public NativeView<DrawCommandIndex> DrawIndices => _drawIndices.Slice(0, VisibleCount);
    public NativeView<DrawObjectUniform> Transforms => _transforms.Slice(0, VisibleCount);

        
    public void Dispose()
    {
        _visibleEntities.Dispose();
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
        var visibleEntities = _visibleEntities.AsView();
        if ((uint)RenderEcs.Core.Count > (uint)visibleEntities.Length)
            Throwers.BufferOverflow(nameof(visibleEntities));

        var visibleCount = 0;
        foreach (var query in RenderEcs.Core.CullQuery())
        {
            var visible = query.Item1.Status == EntityStatus.AlwaysVisible ||
                          _frustum.IntersectsBox(in query.Item2);
            
            if (visible) visibleEntities[visibleCount++] = query.Entity;
            query.Item1.Visible = visible;
        }

        return VisibleCount = visibleCount;
    }

    private unsafe void SubmitDrawPolicy()
    {
        var forward = CameraManager.Instance.Camera.Forward;
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;

        var indices = _drawIndices.Ptr;

        var index = -1;
        foreach (var it in RenderEcs.Core.DepthPolicyQuery(VisibleEntities))
        {
            var depthKey = MakeDepthKey( forward, it.Item2.Translation, nearFar, viewZ);
            *indices = new DrawCommandIndex(++index, it.Item1.Passes, it.Item1.Queue, depthKey);
            ++indices;
        }
    }

    private unsafe void SubmitTransforms()
    {
        var transforms = _transforms.Ptr;
        foreach (var query in RenderEcs.Core.MatrixQuery(VisibleEntities))
        {
            transforms->Model = query.Item1;
            transforms->Normal = query.Item2;
            ++transforms;
        }
    }

    private void Ensure()
    {
        var ecsCapacity = RenderEcs.Core.Capacity;
        if ((uint)ecsCapacity > (uint)_visibleEntities.Length)
        {
            _drawIndices.Resize(ecsCapacity, true);
            _visibleEntities.Resize(ecsCapacity, true);
            _transforms.Resize(ecsCapacity, true);
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