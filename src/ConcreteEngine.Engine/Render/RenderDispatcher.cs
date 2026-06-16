using System.Numerics;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using Camera = ConcreteEngine.Core.Engine.Camera;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher
{
    public int VisibleEntities { get; private set; }

    private readonly Camera _camera;
    private readonly CameraFrustum _frustum;
    private readonly DrawCommandBuffer _commandBuffer;
    private readonly EffectBuffer _effectBuffer;
    private readonly TerrainSystem _terrainSystem;

    internal RenderDispatcher(CameraManager cameraManager,TerrainSystem terrainSystem, RenderUploadBuffers uploadBuffers)
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

    }

    public void Execute()
    {
        _commandBuffer.EnsureCapacity(Ecs.Render.Core.Count + 64);
        UploadOthers();

        if (VisibleEntities == 0) return;
        
        TagUploadSelectionEffect();
        CollectEntities();
        UploadDrawCommands();

        SubmitDebugBounds();
    }

    public void CullEntities()
    {
        var visibleCount = 0;
        var length = Ecs.RenderCore.Count;
        var bounds = Ecs.RenderCore.GetWorldBoundsView();
        var coreEntities = Ecs.RenderCore.GetCoreEntityView();
        for (var i = 0; i < length; i++)
        {
            if (!coreEntities[i].Alive) continue;
            var visible = _frustum.IntersectsBox(in bounds[i]);
            visible &= coreEntities[i].ToggleVisibility(VisibilityFlags.Culled, visible) == 0;
            if (visible) visibleCount++;
        }

        VisibleEntities = visibleCount;

    }

    private void UploadOthers( )
    {
        _terrainSystem.SubmitDrawTerrain(_commandBuffer, _frustum);

        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passes: PassMask.Main);
        var cmd = new DrawCommand(Skybox.Current.MeshId, Skybox.Current.MaterialId);
        _commandBuffer.SubmitIdentity(cmd, meta);
    }

    private  void CollectEntities()
    {
        var index = 0;
        var cmd = _commandBuffer.GetCommandMetaSpan();
        foreach (var query in Ecs.RenderCore.VisibleQuery(Ecs.RenderCore.GetSourceView(), Ecs.RenderCore.GetModelView()))
        {
            var depth = _camera.MakeDepthKey(query.Data.Item2.Translation);
            query.Data.Item1.WriteCommand(ref cmd.At1(index));
            query.Data.Item1.WriteMeta(ref cmd.At2(index), depth);
            ++index;
        }
    }

    private void UploadDrawCommands()
    {
        foreach (var entity in Ecs.RenderCore.VisibleQuery(Ecs.RenderCore.GetModelView(), Ecs.RenderCore.GetNormalsView()))
        {
            ref var bufferData = ref _commandBuffer.SubmitDraw();
            bufferData.Model = entity.Data.Item1;
            bufferData.Normal = entity.Data.Item2;
        }
    }

    public void TagUploadSelectionEffect()
    {
        var store = Ecs.GetRenderStore<SelectionComponent>();
        if (store.Count == 0) return;

        foreach (var query in store.VisibilityQuery())
        {
            var slot = _effectBuffer.Submit(new EffectUniformParams(query.Component.HighlightColor));
            ref var source = ref Ecs.RenderCore.GetSource(query.Entity);
            source.Passes = PassMask.Effect | PassMask.Depth;
            source.Resolver = DrawCommandResolver.Highlight;
            source.ResolverSlot = slot;
        }
    }

    private void SubmitDebugBounds()
    {
        var store = Ecs.GetRenderStore<DebugBoundsComponent>();
        if (store.Count == 0) return;

        var effects = _effectBuffer;
        var ecs = Ecs.Render.Core;
        var ctx = _commandBuffer.GetCommandMetaSpan();
        var index = 0;
        foreach (var query in store.VisibilityQuery())
        {
            var slot = effects.Submit(new EffectUniformParams(query.Component.Color));
            var depthKey = _camera.MakeDepthKey(ecs.GetModelMatrix(query.Entity).Translation);
            depthKey = (ushort)(ushort.MaxValue - depthKey);

            ctx.At1(index) = new DrawCommand(GfxMeshes.Cube, Material.BoundsMaterialId);
            ctx.At2(index) =
                new DrawCommandMeta(DrawCommandId.Effect, DrawCommandQueue.Effect, PassMask.Effect, depthKey,
                    DrawCommandResolver.BoundingVolume, resolverSlot: slot);

            ref readonly var worldBounds = ref ecs.GetWorldBounds(query.Entity);
            ref var data = ref _commandBuffer.SubmitDraw();
            MatrixMath.CreateModelMatrix(
                worldBounds.Center,
                worldBounds.Extent,
                Quaternion.Identity,
                out data.Model
            );
            data.Normal = Matrix3X4.Identity;
            index++;
        }
    }

}