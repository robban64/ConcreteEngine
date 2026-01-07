using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Processor;
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
    private DrawEntity[] _drawEntities = new DrawEntity[DefaultCapacity];

    private int[] _byEntityId = new int[DefaultCapacity];
    private RenderEntityId[] _entityIndices = new RenderEntityId[DefaultCapacity];

    private Matrix4x4[] _entityWorld = new Matrix4x4[DefaultCapacity];
    //...

    public static MaterialId BoundsMaterial;

    public int VisibleCount => _visibleCount;
    public ReadOnlySpan<RenderEntityId> VisibleEntitySpan => _entityIndices.AsSpan(0, _visibleCount);
    public ReadOnlySpan<Matrix4x4> EntityWorldSpan => _entityWorld.AsSpan();

    public DrawEntityPipeline()
    {
        Array.Fill(_byEntityId, -1);
        _ = _drawEntities[0];
        _ = _entityIndices[0];

        _visibleCount = 0;
        _prevVisibleCount = 0;
        _highEntityId = default;
    }


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
        if (_drawEntities.Length == 0) return;
        if (_drawEntities.Length != _entityIndices.Length || _drawEntities.Length != _byEntityId.Length ||
            _drawEntities.Length != _entityWorld.Length)
            throw new InvalidOperationException();

        Ensure(commandBuffer);

        // cull
        var len = _visibleCount = CullEntities(renderCtx.Camera.RenderView);
        if (len == 0) return;

        // execute
        var ctx = new DrawEntityContext(_drawEntities.AsSpan(0, len), _entityIndices.AsSpan(0, len), _byEntityId);

        ExecuteCollectCommands(renderCtx, in ctx);
        ExecuteUploader(renderCtx, commandBuffer, _entityWorld, in ctx);

        AnimatorProcessor.Execute(commandBuffer, renderCtx.AnimationTable, new UnsafeSpan<int>(_byEntityId));
        ParticleProcessor.Execute(in ctx, renderCtx.ParticleSystem);
    }

    private int CullEntities(in CameraRenderView renderView)
    {
        var ecsLen = Ecs.Render.EntityCount;
        var len = SpatialProcessor.CullEntities(_entityIndices, _byEntityId, _entityWorld, in renderView);
        if (len == 0) return 0;
        if ((uint)len > _drawEntities.Length || (uint)len > _entityIndices.Length ||
            (uint)ecsLen > _byEntityId.Length || (uint)ecsLen > _entityWorld.Length)
        {
            throw new IndexOutOfRangeException();
        }

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
    private static void ExecuteUploader(RenderContext renderCtx, DrawCommandBuffer buffer, Matrix4x4[] entityWorld,
        in DrawEntityContext ctx)
    {
        var uploader = buffer.GetDrawUploaderCtx();
        RenderEntityCollector.UploadDrawCommands(renderCtx, in ctx, in uploader);
        SpatialProcessor.UploadTransform(in ctx, entityWorld, in uploader, renderCtx.MeshTable);
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
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _drawEntities.Length);
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entityIndices.Length);

        if (_drawEntities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_drawEntities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        _drawEntities = new DrawEntity[newCap];
        _entityIndices = new RenderEntityId[newCap];
        _byEntityId = new int[newCap];
        _entityWorld = new Matrix4x4[newCap];

        Array.Fill(_byEntityId, -1);

        Logger.LogString(LogScope.World, $"Entity buffer resize {newCap}", LogLevel.Warn);
    }
}