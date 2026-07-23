using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Engine.Graphics.Enviroment;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using Camera = ConcreteEngine.Core.Engine.Camera;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher
{
    public int VisibleCount { get; private set; }

    private RenderEntityId[] _visibleEntities;

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

        _visibleEntities = new RenderEntityId[Ecs.RenderCore.Capacity];
    }

    public void Execute()
    {
        Ensure();
        SubmitEnvironment();

        if (VisibleCount == 0) return;
        ProcessSelectionEffect();

        SubmitCommands();
        SubmitTransforms();
        SubmitDebugBounds();
    }


    public unsafe void CullEntities()
    {
        var core = Ecs.RenderCore.GetCoreEntityView().Ptr;
        var bounds = Ecs.RenderCore.GetWorldBoundsView().Ptr;

        var visibleCount = 0;
        var visibleEntities = _visibleEntities.AsSpan(0, Ecs.RenderCore.Count);
        for (var i = 0; i < visibleEntities.Length; ++i, ++core, ++bounds)
        {
            if (!core->Alive) continue;

            var visible = _frustum.IntersectsBox(*bounds);

            visible &= core->ToggleVisibility(VisibilityFlags.Culled, visible) == 0;
            if (visible) visibleEntities[visibleCount++] = new RenderEntityId(i + 1);
        }

        VisibleCount = visibleCount;
    }

    private void Ensure()
    {
        var ecsCount = Ecs.RenderCore.Count;
        if(ecsCount > _visibleEntities.Length) Array.Resize(ref _visibleEntities, ecsCount);
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
            _commandBuffer.SubmitIdentity(cmd, PassMask.Default, DrawCommandQueue.Terrain, 0);

            if (it.FoliageCount > 0)
            {
                cmd = new DrawCommand(it.FoliageMeshId, foliageMat, instanceCount: (uint)it.FoliageCount);
                _commandBuffer.SubmitIdentity(cmd, PassMask.Default, DrawCommandQueue.Transparent, 0);
            }
        }

        _commandBuffer.SubmitIdentity(
            new DrawCommand(Skybox.Current.MeshId, Skybox.Current.MaterialId),
            PassMask.Main, DrawCommandQueue.Skybox, 0);
    }

    private void SubmitCommands()
    {
        var forward = _camera.Forward;
        var viewZ = _camera.ViewMatrix.M43;
        var nearFar = _camera.NearFarPlane;

        var index = 0;
        var submitIdx = _commandBuffer.Count; 
        
        foreach (var entity in _visibleEntities.AsSpan(0, VisibleCount))
        {
            var depthKey = MakeDepthKey(entity, forward, nearFar, viewZ);
            ref var source = ref Ecs.RenderCore.GetSource(entity);
            var passes = source.Passes;
            var queue = source.Queue;
            
            _commandBuffer.IndexRef(index) = new DrawCommandIndex(submitIdx, passes, queue, depthKey);
            source.WriteTo(ref _commandBuffer.CommandRef(index));

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


    private void SubmitTransforms()
    {
        var index = 0;
        foreach (var entity in _visibleEntities.AsSpan(0, VisibleCount))
        {
            ref var bufferData = ref _commandBuffer.TransformRef(index++);
            bufferData.Model = Ecs.RenderCore.GetModelMatrix(entity);
            bufferData.Normal = Ecs.RenderCore.GetNormalMatrix(entity);
        }

        _commandBuffer.IncrementDrawCount(index);
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

            Ecs.RenderCore.GetSource(query.Entity).Passes = PassMask.Effect | PassMask.Depth;
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

            _commandBuffer.CommandRef(index) = new DrawCommand(GfxMeshes.Cube, materialId, resolver: DrawCommandResolver.BoundingVolume,
                resolverSlot: slot);
            _commandBuffer.IndexRef(index) = new DrawCommandIndex(submitIndex, PassMask.Effect, DrawCommandQueue.Effect, 0);


            ref readonly var worldBounds = ref Ecs.RenderCore.GetWorldBounds(query.Entity);
            ref var bufferDst = ref _commandBuffer.TransformRef(index);
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
}