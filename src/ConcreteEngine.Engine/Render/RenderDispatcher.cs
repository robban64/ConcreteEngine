using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Assets;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Processor;
using ConcreteEngine.Graphics.Gfx;
using ConcreteEngine.Renderer.Buffer;
using Camera = ConcreteEngine.Core.Engine.Camera;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher : IDisposable
{
    public int VisibleCount { get; private set; }

    private readonly Camera _camera;
    private readonly CameraFrustum _frustum;
    private readonly DrawCommandBuffer _commandBuffer;
    private readonly RenderUploadBuffers _uploadBuffers;
    private readonly AnimationProcessor _animationProcessor;

    internal RenderDispatcher(CameraManager cameraManager, AnimationManager animations,
        RenderUploadBuffers uploadBuffers)
    {
        ArgumentNullException.ThrowIfNull(cameraManager);
        ArgumentNullException.ThrowIfNull(animations);
        ArgumentNullException.ThrowIfNull(uploadBuffers);
        if (cameraManager.Camera == null! || cameraManager.Frustum == null!)
            throw new ArgumentNullException(nameof(cameraManager));

        _camera = cameraManager.Camera;
        _frustum = cameraManager.Frustum;
        _uploadBuffers = uploadBuffers;
        _commandBuffer = uploadBuffers.Commands;
        _animationProcessor = new AnimationProcessor(animations, uploadBuffers.Skinning);
    }


    public void Prepare(TerrainSystem terrain)
    {
        EnsureCommandBuffer();
        terrain.SubmitDrawTerrain(_commandBuffer, _frustum);

        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passes: PassMask.Main);
        var cmd = new DrawCommand(Skybox.Current.MeshId, Skybox.Current.MaterialId);
        _commandBuffer.Submit(cmd, meta, in DrawCommandBuffer.TransformIdentity);
    }

    internal void Execute()
    {
        VisibleCount = CullEntities();

        if (VisibleCount == 0) return;
        _animationProcessor.Execute();

        TagUploadSelectionEffect();
        CollectEntities();
        UploadDrawCommands();
        SubmitDebugBounds();
    }

    private unsafe int CullEntities()
    {
        var visibleCount = 0;
        var length = Ecs.Render.Core.Count;
        var bounds = Ecs.Render.Core.GetWorldBoundsPtr();
        var coreEntities = Ecs.Render.Core.GetCoreEntityPtr();
        for (var i = 0; i < length; i++)
        {
            if (!coreEntities[i].Alive) continue;
            var visible = _frustum.IntersectsBox(in bounds[i]);
            visible &= coreEntities[i].ToggleVisibility(VisibilityFlags.Culled, visible) == 0;
            if (visible) visibleCount++;
        }

        return visibleCount;
    }

    private unsafe void CollectEntities()
    {
        var index = 0;
        var sources = Ecs.Render.Core.GetSourcePtr();
        var worldMatrices = Ecs.Render.Core.GetWorldMatrixPtr();
        var ctx = _commandBuffer.GetCommandMetaSpan();
        foreach (var entity in Ecs.Render.Core.VisibilityQuery())
        {
            var depth = _camera.MakeDepthKey(worldMatrices[entity.Index()].Translation);

            ref readonly var source = ref sources[entity.Index()];
            source.WriteCommand(ref ctx.At1(index));
            source.WriteMeta(ref ctx.At2(index), depth);
            ++index;
        }
    }

    private unsafe void UploadDrawCommands()
    {
        var parentMatrices = Ecs.Render.Core.GetWorldMatrixPtr();
        foreach (var entity in Ecs.Render.Core.VisibilityQuery())
        {
            ref readonly var world = ref parentMatrices[entity.Index()];
            ref var bufferData = ref _commandBuffer.SubmitDraw();
            bufferData.Model = world;
            MatrixMath.CreateNormalMatrix(ref bufferData.Normal, in world);
        }
    }

    public void TagUploadSelectionEffect()
    {
        var store = Ecs.GetRenderStore<SelectionComponent>();
        if (store.Count == 0) return;

        var effects = _uploadBuffers.Effects;
        foreach (var query in store.VisibilityQuery())
        {
            var slot = effects.Submit(new EffectUniformParams(query.Component.HighlightColor));
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

        var effects = _uploadBuffers.Effects;
        var ecs = Ecs.Render.Core;
        var ctx = _commandBuffer.GetCommandMetaSpan();
        var index = 0;
        foreach (var query in store.VisibilityQuery())
        {
            var slot = effects.Submit(new EffectUniformParams(query.Component.Color));
            var depthKey = _camera.MakeDepthKey(ecs.GetWorldMatrix(query.Entity).Translation);
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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCommandBuffer()
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = Ecs.Render.Core.Count + extraEntities;

        _uploadBuffers.Commands.EnsureCapacity(entityLen);
        _uploadBuffers.Skinning.EnsureCapacity(AnimationManager.Instance.AnimationCount);
    }

    public void Dispose() => _animationProcessor.Dispose();
}