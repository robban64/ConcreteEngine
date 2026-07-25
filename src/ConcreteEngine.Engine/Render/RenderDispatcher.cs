using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Enviroment;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Buffer;
using Camera = ConcreteEngine.Core.Engine.Camera;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher : IDisposable
{
    public int VisibleCount { get; private set; }

    private NativeArray<RenderEntityId> _visibleEntities;

    private readonly Camera _camera;
    private readonly CameraFrustum _frustum;
    private readonly DrawCommandBuffer _commandBuffer;
    private readonly EffectBuffer _effectBuffer;
    private readonly TerrainSystem _terrainSystem;

    internal RenderDispatcher(CameraManager cameraManager, TerrainSystem terrainSystem,
        RenderUploadBuffers uploadBuffers)
    {
        ArgumentNullException.ThrowIfNull(cameraManager);
        ArgumentNullException.ThrowIfNull(uploadBuffers);
        if (cameraManager.Camera == null! || cameraManager.Frustum == null!)
            throw new ArgumentNullException(nameof(cameraManager));

        _camera = cameraManager.Camera;
        _frustum = cameraManager.Frustum;
        _effectBuffer = uploadBuffers.Effects;
        _commandBuffer = uploadBuffers.Commands;
        _terrainSystem = terrainSystem;

        _visibleEntities = NativeArray.Allocate<RenderEntityId>(Ecs.RenderCore.Capacity);
    }

    public unsafe void Execute()
    {
        Ensure();
        SubmitEnvironment();

        if (VisibleCount == 0) return;
        ProcessSelectionEffect();

        var visibleSpan = new ReadOnlySpan<RenderEntityId>(_visibleEntities, VisibleCount);
        SubmitDrawPolicy(visibleSpan);
        SubmitCommands(visibleSpan);
        SubmitTransforms(visibleSpan);

        SubmitDebugBounds();
    }


    public unsafe void CullEntities()
    {
        var length = Ecs.RenderCore.Count;
        if ((uint)length > (uint)_visibleEntities.Length)
            Throwers.BufferOverflow(nameof(_visibleEntities));

        var core = Ecs.RenderCore.GetCoreEntityView().Ptr;
        var bounds = Ecs.RenderCore.GetWorldBoundsView().Ptr;

        var visibleCount = 0;
        for (var i = 0; i < length; ++i, ++core, ++bounds)
        {
            if (!core->Alive) continue;

            var visible = _frustum.IntersectsBox(*bounds);

            visible &= core->ToggleVisibility(EntityVisibility.Culled, visible) == 0;
            if (visible) _visibleEntities[visibleCount++] = new RenderEntityId(i + 1);
        }

        VisibleCount = visibleCount;
    }

    private void Ensure()
    {
        var ecsCount = Ecs.RenderCore.Count;
        if ((uint)ecsCount > (uint)_visibleEntities.Length) _visibleEntities.Resize(ecsCount, true);
        _commandBuffer.EnsureCapacity(ecsCount + 64);
    }

    private void SubmitEnvironment()
    {
        var mainTerrain = _terrainSystem.MainTerrain;
        var terrainMat = mainTerrain.MaterialId;
        var foliageMat = mainTerrain.FoliageMaterialId;

        foreach (var it in _terrainSystem.TerrainMesh.GetMeshChunks())
        {
            if (!_frustum.IntersectsBox(mainTerrain.GetChunk(it.Slot).GetBounds())) continue;
            var cmd = new DrawCommand(it.TerrainMeshId, terrainMat);
            _commandBuffer.SubmitIdentity(cmd, PassMask.Default, DrawQueue.Terrain, 0);

            if (it.FoliageCount > 0)
            {
                cmd = new DrawCommand(it.FoliageMeshId, foliageMat, instanceCount: (uint)it.FoliageCount);
                _commandBuffer.SubmitIdentity(cmd, PassMask.Default, DrawQueue.Transparent, 0);
            }
        }

        _commandBuffer.SubmitIdentity(
            new DrawCommand(Skybox.Current.MeshId, Skybox.Current.MaterialId),
            PassMask.Main, DrawQueue.Skybox, 0);
    }

    private void SubmitDrawPolicy(ReadOnlySpan<RenderEntityId> visibleEntities)
    {
        var forward = _camera.Forward;
        var viewZ = _camera.ViewMatrix.M43;
        var nearFar = _camera.NearFarPlane;

        var index = 0;
        var submitIdx = _commandBuffer.Count;

        foreach (var entity in visibleEntities)
        {
            var depthKey = MakeDepthKey(entity, forward, nearFar, viewZ);
            var policy = Ecs.RenderCore.GetDrawPolicy(entity);
            _commandBuffer.IndexRef(index) = new DrawCommandIndex(submitIdx, policy.Passes, policy.Queue, depthKey);
            ++index;
            ++submitIdx;
        }

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort MakeDepthKey(RenderEntityId entity, Vector3 forward, Vector2 nearFar, float viewZ)
        {
            const float maxValueF = 65535f;

            var worldPos = Ecs.RenderCore.GetModelMatrix(entity).Translation;
            var d = Vector3.Dot(forward, worldPos) - viewZ;
            if (d <= nearFar.X) return 0;
            if (d >= nearFar.Y) return ushort.MaxValue;
            var t = (d - nearFar.X) / (nearFar.Y - nearFar.X);
            return (ushort)(t * maxValueF + 0.5f);
        }
    }

    private void SubmitCommands(ReadOnlySpan<RenderEntityId> visibleEntities)
    {
        var index = 0;
        foreach (var entity in visibleEntities)
        {
            ref var source = ref Ecs.RenderCore.GetSource(entity);
            ref var cmd = ref _commandBuffer.CommandRef(index++);
            source.WriteTo(ref cmd);
            /*
            cmd.MeshId = source.Mesh;
            cmd.MaterialId = source.Material;
            cmd.Resolver = source.Resolver;
            cmd.ResolverSlot = source.ResolverSlot;
            */
        }
    }


    private void SubmitTransforms(ReadOnlySpan<RenderEntityId> visibleEntities)
    {
        for (var i = 0; i < visibleEntities.Length; ++i)
        {
            var entity = visibleEntities[i];
            ref var bufferData = ref _commandBuffer.TransformRef(i);
            bufferData.Model = Ecs.RenderCore.GetModelMatrix(entity);
            bufferData.Normal = Ecs.RenderCore.GetNormalMatrix(entity);
        }

        _commandBuffer.IncrementDrawCount(visibleEntities.Length);
    }

    private void ProcessSelectionEffect()
    {
        var store = Ecs.GetRenderStore<SelectionComponent>();
        if (store.Count == 0) return;

        foreach (var query in store.VisibilityQuery())
        {
            var slot = _effectBuffer.Submit(new EffectUniformParams(query.Component.HighlightColor));
            ref var source = ref Ecs.RenderCore.GetSource(query.Entity);
            source.Resolver = DrawCommandResolver.Highlight;
            source.ResolverSlot = slot;

            Ecs.RenderCore.GetDrawPolicy(query.Entity).Passes = PassMask.Effect | PassMask.Depth;
        }
    }

    private void SubmitDebugBounds()
    {
        var store = Ecs.GetRenderStore<DebugBoundsComponent>();
        if (store.Count == 0) return;

        var materialId = AssetStore.Core.DebugBoundsMaterial.MaterialId;

        var index = 0;
        var submitIndex = _commandBuffer.Count;

        foreach (var query in store.VisibilityQuery())
        {
            var slot = _effectBuffer.Submit(new EffectUniformParams(query.Component.Color));

            _commandBuffer.CommandRef(index) = new DrawCommand(GfxMeshes.Cube, materialId,
                resolver: DrawCommandResolver.BoundingVolume,
                resolverSlot: slot);
            _commandBuffer.IndexRef(index) =
                new DrawCommandIndex(submitIndex, PassMask.Effect, DrawQueue.Effect, 0);

            ref var bufferDst = ref _commandBuffer.TransformRef(index);
            ref readonly var worldBounds = ref Ecs.RenderCore.GetWorldBounds(query.Entity);
            MatrixMath.CreateModelMatrix(
                worldBounds.Center,
                worldBounds.Extent,
                Quaternion.Identity,
                out bufferDst.Model
            );
            bufferDst.Normal = Matrix3X4.Identity;

            ++index;
            ++submitIndex;
        }

        _commandBuffer.IncrementDrawCount(index);
    }

    public void Dispose() => _visibleEntities.Dispose();
}