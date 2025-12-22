using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Processor;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Shared.Diagnostics;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class DrawEntityPipeline
{
    private const int DefaultCapacity = 512;
    private const int MaxCapacity = 1024 * 50;

    private static int _idx;
    private static int _prevIdx;
    private static RenderEntityId _highEntityId;

    //...
    internal static int[] ByEntityId = new int[DefaultCapacity];
    private static RenderEntityId[] _entityIndices = new RenderEntityId[DefaultCapacity];
    private static DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    //...

    public static MaterialId BoundsMaterial;

    public DrawEntityPipeline()
    {
        for (int i = 0; i < 30; i++)
        {
            _ = ByEntityId[i];
            _ = _entities[i];
            _ = _entityIndices[i];
        }
    }

    public ReadOnlySpan<RenderEntityId> VisibleEntities => _entityIndices.AsSpan(0, _idx);

    public void Reset()
    {
        _entityIndices.AsSpan(0, _idx).Clear();
        ByEntityId.AsSpan(0, _highEntityId).Fill(-1);

        _prevIdx = _idx;
        _idx = 0;
        _highEntityId = default;
    }


    public static void ExecuteWorldObjects(DrawCommandBuffer buffer, World world)
    {
        WorldObjectProcessor.SubmitWorldObjects(buffer, world);
    }

    // private static readonly FrameProfileTimer timer = StaticProfileTimer.NewRenderTime();

    public void Execute(RenderContext renderCtx, DrawCommandBuffer commandBuffer)
    {
        Ensure(commandBuffer);
        Validate();
        // cull
        var len = CullEntities(renderCtx.Camera.RenderView);

        // execute
        var ctx = new DrawEntityContext(_entities.AsSpan(0, len), _entityIndices.AsSpan(0, len), ByEntityId);
        ExecuteCollectCommands(renderCtx, in ctx);
        ExecuteUploader(renderCtx, commandBuffer, in ctx);

        AnimatorProcessor.Execute(commandBuffer, renderCtx.AnimationTable);
        ParticleProcessor.Execute(in ctx, renderCtx.ParticleSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CullEntities(in CameraRenderView renderView)
    {
        var ecsLen = Ecs.Render.EntityCount;
        var len = _idx = SpatialProcessor.CullEntities(_entityIndices, ByEntityId, in renderView);
        if (len == 0) return 0;
        if ((uint)len > _entities.Length || (uint)len > _entityIndices.Length || (uint)ecsLen > ByEntityId.Length)
            throw new IndexOutOfRangeException();

        return len;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExecuteCollectCommands(RenderContext renderCtx, in DrawEntityContext ctx)
    {
        _highEntityId = RenderEntityCollector.CollectEntities(in ctx);
        DrawTagResolver.TagResolveEntities(in ctx);
        SpatialProcessor.TagDepthKeys(in ctx, renderCtx.Camera);
        ParticleProcessor.TagParticles(in ctx, renderCtx.ParticleSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExecuteUploader(RenderContext renderCtx, DrawCommandBuffer buffer, in DrawEntityContext ctx)
    {
        var uploader = buffer.GetDrawUploaderCtx();
        RenderEntityCollector.UploadDrawCommands(renderCtx, in ctx, in uploader);
        TransformUploader.UploadTransform(in ctx, in uploader, renderCtx.MeshTable);

        DrawTagResolver.UploadDebugBounds(in ctx, in uploader, renderCtx.MeshTable, BoundsMaterial);
    }


    private static void Validate()
    {
        if (_entityIndices.Length == 0 || _entities.Length == 0)
            throw new InvalidOperationException();

        var view = Ecs.Render.Core.GetContext();

        if (_entities.Length != _entityIndices.Length || _entities.Length != ByEntityId.Length)
            throw new InvalidOperationException();

        if (view.Boxes.Length != view.Transforms.Length || view.Boxes.Length != view.Sources.Length)
            throw new InvalidOperationException();

        var len = view.Boxes.Length;
        if ((uint)len > _entities.Length || (uint)len > view.Transforms.Length)
            throw new IndexOutOfRangeException();
    }

    private static void Ensure( DrawCommandBuffer buffer)
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = Ecs.Render.Core.Count + extraEntities;
        var animationLen = Ecs.Render.Stores<RenderAnimationComponent>.Store.Count + extraAnimations;

        EnsureDrawEntityData(entityLen);
        buffer.EnsureBufferCapacity(entityLen);
        buffer.EnsureBoneBuffer(animationLen);
    }

    private static void EnsureDrawEntityData(int amount)
    {
        InvalidOpThrower.ThrowIf(ByEntityId.Length != _entities.Length);
        InvalidOpThrower.ThrowIf(ByEntityId.Length != _entityIndices.Length);

        if (_entities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_entities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        Array.Resize(ref _entities, newCap);
        Array.Resize(ref _entityIndices, newCap);
        Array.Resize(ref ByEntityId, newCap);
        Logger.LogString(LogScope.World, $"Entity buffer resize {newCap}", LogLevel.Warn);
    }
}