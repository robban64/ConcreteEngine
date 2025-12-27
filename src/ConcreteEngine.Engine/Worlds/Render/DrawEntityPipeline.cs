using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Processor;
using ConcreteEngine.Renderer;
using ConcreteEngine.Renderer.Draw;
using Ecs = ConcreteEngine.Engine.ECS.Ecs;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class DrawEntityPipeline
{
    private const int DefaultCapacity = 512;
    private const int MaxCapacity = 1024 * 50;

    private int _visibleCount;
    private int _prevVisibleCount;
    private RenderEntityId _highEntityId;

    //...
    private int[] _byEntityId = new int[DefaultCapacity];
    private RenderEntityId[] _entityIndices = new RenderEntityId[DefaultCapacity];
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    //...

    public static MaterialId BoundsMaterial;

    public DrawEntityPipeline()
    {
        Array.Fill(_byEntityId, -1);
        _ = _entities[0];
        _ = _entityIndices[0];

        _visibleCount = 0;
        _prevVisibleCount = 0;
        _highEntityId = default;
    }

    public int VisibleCount => _visibleCount;
    public ReadOnlySpan<RenderEntityId> VisibleEntities => _entityIndices.AsSpan(0, _visibleCount);

    public void Reset()
    {
        _entityIndices.AsSpan(0, _visibleCount).Clear();
        Array.Fill(_byEntityId, -1);

        _prevVisibleCount = _visibleCount;
        _visibleCount = 0;
        _highEntityId = default;
    }


    public void ExecuteWorldObjects(DrawCommandBuffer buffer, World world)
    {
        WorldObjectProcessor.SubmitWorldObjects(buffer, world);
    }

    public void Execute(RenderContext renderCtx, DrawCommandBuffer commandBuffer)
    {
        if(_entities.Length == 0) return;
        if (_entities.Length != _entityIndices.Length || _entities.Length != _byEntityId.Length)
            throw new InvalidOperationException();

        Ensure(commandBuffer);
        
        // cull
        var len = _visibleCount = CullEntities(renderCtx.Camera.RenderView);
        if(len == 0) return;

        // execute
        var ctx = new DrawEntityContext(_entities.AsSpan(0, len), _entityIndices.AsSpan(0, len), _byEntityId);

        ExecuteCollectCommands(renderCtx, in ctx);
        ExecuteUploader(renderCtx, commandBuffer, in ctx);

        AnimatorProcessor.Execute(commandBuffer, renderCtx.AnimationTable, new UnsafeSpan<int>(_byEntityId));
        ParticleProcessor.Execute(in ctx, renderCtx.ParticleSystem);
    }

    private int CullEntities(in CameraRenderView renderView)
    {
        var ecsLen = Ecs.Render.EntityCount;
        var len = SpatialProcessor.CullEntities(_entityIndices, _byEntityId, in renderView);
        if (len == 0) return 0;
        if ((uint)len > _entities.Length || (uint)len > _entityIndices.Length || (uint)ecsLen > _byEntityId.Length)
            throw new IndexOutOfRangeException();

        return len;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ExecuteCollectCommands(RenderContext renderCtx, in DrawEntityContext ctx)
    {
        RenderEntityCollector.CollectEntities(in ctx);
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

    private void Ensure(DrawCommandBuffer buffer)
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = Ecs.Render.Core.Count + extraEntities;
        var animationLen = Ecs.Render.Stores<RenderAnimationComponent>.Store.Count + extraAnimations;

        EnsureDrawEntityData(entityLen);
        buffer.EnsureBufferCapacity(entityLen);
        buffer.EnsureBoneBuffer(animationLen);
    }

    private void EnsureDrawEntityData(int amount)
    {
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entities.Length);
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entityIndices.Length);

        if (_entities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_entities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        _entities = new DrawEntity[newCap];
        _entityIndices = new RenderEntityId[newCap];
        _byEntityId = new int[newCap];
        Array.Fill(_byEntityId, -1);

        Logger.LogString(LogScope.World, $"Entity buffer resize {newCap}", LogLevel.Warn);
    }
}