using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.RenderEntity;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine.Systems;

internal sealed class RenderDispatcher : IDisposable
{
    public int VisibleCount { get; private set; }

    private readonly CameraFrustum _frustum;

    private NativeArray<RenderEntityId> _visibleEntities;
    private NativeArray<DrawCommandIndex> _drawIndices;
    private NativeArray<DrawObjectUniform> _transforms;

    internal RenderDispatcher(CameraFrustum frustum)
    {
        ArgumentNullException.ThrowIfNull(frustum);
        _frustum = frustum;
        _visibleEntities = NativeArray.Allocate<RenderEntityId>(RenderEcs.Core.Capacity);
        _drawIndices = NativeArray.Allocate<DrawCommandIndex>(RenderEcs.Core.Capacity);
        _transforms = NativeArray.Allocate<DrawObjectUniform>(RenderEcs.Core.Capacity);
    }

    public NativeView<RenderEntityId> VisibleEntities => _visibleEntities.Slice(0, VisibleCount);
    public NativeView<DrawCommandIndex> DrawIndices => _drawIndices.Slice(0, VisibleCount);
    public NativeView<DrawObjectUniform> Transforms => _transforms.Slice(0, VisibleCount);

    private unsafe PtrEnumerator<RenderEntityId, DrawCommandIndex> IndexEnumerator() =>
        new(_visibleEntities, _drawIndices, VisibleCount);

    private unsafe PtrEnumerator<RenderEntityId, DrawObjectUniform> TransformEnumerator() =>
        new(_visibleEntities, _transforms, VisibleCount);

    public void Dispose() => _visibleEntities.Dispose();


    public void Setup() { }

    private static AvgFrameTimer avg;

    public void Execute()
    {
        Ensure();
        avg.BeginSample();
        var visibleCount = CullEntities();
        if (avg.EndSample() > 144) avg.ResetAndPrint("Cull");
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
        foreach (var query in RenderEcs.Core.BoundsQuery())
        {
            var visible = query.Meta.Visibility == EntityVisibility.AlwaysVisible ||
                          _frustum.IntersectsBox(in query.Item);
            
            if (visible)
            {
                query.Meta.Visibility = EntityVisibility.Visible;
                visibleEntities[visibleCount++] = query.Entity;
            }
            else
            {
                query.Meta.Visibility = EntityVisibility.Culled;
            }
        }

        return VisibleCount = visibleCount;
    }

    private void SubmitDrawPolicy()
    {
        var forward = CameraManager.Instance.Camera.Forward;
        var viewZ = CameraManager.Instance.Camera.ViewMatrix.M43;
        var nearFar = CameraManager.Instance.Camera.NearFarPlane;

        var index = -1;
        foreach (var it in IndexEnumerator())
        {
            var entity = it.Item1;
            var depthKey = MakeDepthKey(entity, forward, nearFar, viewZ);
            var policy = RenderEcs.Core.GetDrawPolicy(entity);
            it.Item2 = new DrawCommandIndex(++index, policy.Passes, policy.Queue, depthKey);
        }
    }

    private void SubmitTransforms()
    {
        foreach (var it in TransformEnumerator())
        {
            var entity = it.Item1;
            it.Item2.Model = RenderEcs.Core.GetModelMatrix(entity);
            it.Item2.Normal = RenderEcs.Core.GetNormalMatrix(entity);
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
    private static ushort MakeDepthKey(RenderEntityId entity, Vector3 forward, Vector2 nearFar, float viewZ)
    {
        const float maxValueF = 65535f;

        var worldPos = RenderEcs.Core.GetModelMatrix(entity).Translation;
        var d = Vector3.Dot(forward, worldPos) - viewZ;
        if (d <= nearFar.X) return 0;
        if (d >= nearFar.Y) return ushort.MaxValue;
        var t = (d - nearFar.X) / (nearFar.Y - nearFar.X);
        return (ushort)(t * maxValueF + 0.5f);
    }
}