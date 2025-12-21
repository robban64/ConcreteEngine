using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.Data;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Processor;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class DrawEntityPipeline
{
    private const int DefaultCapacity = 512;
    private const int MaxCapacity = 1024 * 50;

    private static int _idx;
    private static int _prevIdx;
    private static RenderEntityId _highEntityId;

    //...
    private static int[] _byEntityId = new int[DefaultCapacity];
    private static RenderEntityId[] _entityIndices = new RenderEntityId[DefaultCapacity];
    private static DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    //...


    public static MaterialId BoundsMaterial;

    public ReadOnlySpan<RenderEntityId> VisibleEntities => _entityIndices.AsSpan(0, _idx);

    public void Reset()
    {
        _entityIndices.AsSpan(0, _idx).Clear();
        _byEntityId.AsSpan(0, _highEntityId).Fill(-1);

        _prevIdx = _idx;
        _idx = 0;
        _highEntityId = default;
    }


    public static void ExecuteWorldObjects(DrawCommandBuffer buffer, World world)
    {
        WorldObjectProcessor.SubmitWorldObjects(buffer, world);
    }

    public void Execute(RenderContext renderCtx, DrawCommandBuffer commandBuffer)
    {
        Ensure(renderCtx, commandBuffer);
        Validate(renderCtx);
        var renderEcs = renderCtx.RenderEcs;

        // cull
        var renderView = renderCtx.Camera.RenderView;
        var len = CullEntities(renderEcs, in renderView);

        // execute
        var ctx = new DrawEntityContext(_entities.AsSpan(0, len), _entityIndices.AsSpan(0, len), _byEntityId);
        ExecuteCollectCommands(renderCtx, in ctx, in renderView);
        ExecuteUploader(renderCtx, commandBuffer, in ctx);
        ExecuteAnimationProcessor(renderCtx, commandBuffer, in ctx);

        ParticleProcessor.Execute(in ctx, renderCtx.ParticleSystem, renderCtx.RenderEcs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CullEntities(RenderEntityHub ecs, in CameraRenderView renderView)
    {
        var ecsLen = ecs.Core.Count;
        var len = _idx = SpatialProcessor.CullEntities(_entityIndices, _byEntityId, ecs, in renderView);
        if (len == 0) return 0;
        if ((uint)len > _entities.Length || (uint)len > _entityIndices.Length || (uint)ecsLen > _byEntityId.Length)
            throw new IndexOutOfRangeException();

        return len;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExecuteCollectCommands(RenderContext renderCtx, in DrawEntityContext ctx,
        in CameraRenderView renderView)
    {
        _highEntityId = RenderEntityCollector.CollectEntities(in ctx, renderCtx.RenderEcs.Core);
        DrawTagResolver.TagResolveEntities(in ctx, renderCtx.RenderEcs);

        SpatialProcessor.TagDepthKeys(in ctx, in renderView, renderCtx.RenderEcs.Core);
        ParticleProcessor.TagParticles(in ctx, renderCtx.ParticleSystem, renderCtx.RenderEcs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExecuteUploader(RenderContext renderCtx, DrawCommandBuffer buffer, in DrawEntityContext ctx)
    {
        var uploader = buffer.GetDrawUploaderCtx();
        RenderEntityCollector.UploadDrawCommands(renderCtx, in ctx, in uploader);
        TransformUploader.UploadTransform(in ctx, in uploader, renderCtx.RenderEcs.Core, renderCtx.MeshTable);
        DrawTagResolver.UploadDebugBounds(in ctx, in uploader, renderCtx.RenderEcs, renderCtx.MeshTable,
            BoundsMaterial);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExecuteAnimationProcessor(RenderContext renderCtx, DrawCommandBuffer buffer,
        in DrawEntityContext ctx)
    {
        var skinningUploader = buffer.GetSkinningUploaderCtx();
        var animationView = renderCtx.AnimationTable.GetDataView();
        AnimatorProcessor.Execute(renderCtx.RenderEcs, in ctx, in skinningUploader, in animationView);
    }

    private static void Validate(RenderContext renderCtx)
    {
        if (_entityIndices.Length == 0 || _entities.Length == 0)
            throw new InvalidOperationException();

        var view = renderCtx.RenderEcs.Core.GetContext();

        if (_entities.Length != _entityIndices.Length || _entities.Length != _byEntityId.Length)
            throw new InvalidOperationException();

        if (view.Boxes.Length != view.Transforms.Length || view.Boxes.Length != view.Sources.Length)
            throw new InvalidOperationException();

        var len = view.Boxes.Length;
        if ((uint)len > _entities.Length || (uint)len > view.Transforms.Length)
            throw new IndexOutOfRangeException();
    }

    private static void Ensure(RenderContext renderCtx, DrawCommandBuffer buffer)
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = renderCtx.RenderEcs.Core.Count + extraEntities;
        var animationLen = renderCtx.RenderEcs.GetStore<RenderAnimationComponent>().Count + extraAnimations;

        EnsureDrawEntityData(entityLen);
        buffer.EnsureBufferCapacity(entityLen);
        buffer.EnsureBoneBuffer(animationLen);
    }

    private static void EnsureDrawEntityData(int amount)
    {
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entities.Length);
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entityIndices.Length);

        if (_entities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_entities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        Array.Resize(ref _entities, newCap);
        Array.Resize(ref _entityIndices, newCap);
        Array.Resize(ref _byEntityId, newCap);
        Logger.LogString(LogScope.World, $"Entity buffer resize {newCap}", LogLevel.Warn);
    }
}