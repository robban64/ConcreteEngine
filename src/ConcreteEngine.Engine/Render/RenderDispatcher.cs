using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Memory;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.Diagnostics;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render;

public sealed class RenderDispatcher
{
    private readonly RenderEntityCore _ecs;
    private readonly FrameEntityBuffer _frameBuffer;
    private readonly DrawCommandBuffer _commandBuffer;

    private readonly Camera _camera;
    private readonly WorldBundle _worldBundle;

    private DrawEntity[] _drawEntities = [];

    internal RenderDispatcher(RenderEntityCore ecs, WorldBundle worldBundle, FrameEntityBuffer frameBuffer, DrawCommandBuffer commandBuffer)
    {
        _worldBundle = worldBundle;
        _frameBuffer = frameBuffer;
        _commandBuffer = commandBuffer;
        _ecs = ecs;
        _camera = worldBundle.Camera;
    }


    internal void Execute()
    {
        EnsureCommandBuffer();
        EnsureCapacity(_ecs.Capacity);
        
        _frameBuffer.BeginFrame();

        WorldObjectProcessor.SubmitWorldObjects(_commandBuffer, _worldBundle);

        var len = SpatialProcessor.CullEntities(_frameBuffer, _camera.RenderView);
        if (len == 0) return;
        if ((uint)len > _drawEntities.Length) throw new IndexOutOfRangeException();

        _frameBuffer.VisibleCount = len;
        _frameBuffer.GetWriteSpans(out var visible, out var worldMatrices, out var map);
        var ctx = new DrawEntityContext(len, _drawEntities, map, visible, worldMatrices);

        ExecuteCollectCommands(in ctx);
        ExecuteUploader(in ctx);

        AnimatorProcessor.Execute(_commandBuffer, _worldBundle.AnimationTable, new UnsafeSpan<int>(map));
        ParticleProcessor.Execute(in ctx, _worldBundle.ParticleSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteCollectCommands(in DrawEntityContext ctx)
    {
        RenderEntityCollector.CollectEntities(in ctx);
        DrawTagResolver.TagResolveEntities(in ctx);
        SpatialProcessor.TagDepthKeys(in ctx, _camera.RenderView);
        ParticleProcessor.TagParticles(in ctx, _worldBundle.ParticleSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteUploader(in DrawEntityContext ctx)
    {
        var uploader = _commandBuffer.GetDrawUploaderCtx();
        RenderEntityCollector.UploadDrawCommands(_worldBundle, in ctx, in uploader);
        SpatialProcessor.UploadTransform(_worldBundle, in ctx, in uploader);
        DrawTagResolver.UploadDebugBounds(_worldBundle, in ctx, in uploader);
    }

    private void EnsureCommandBuffer()
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = Ecs.Render.Core.Count + extraEntities;
        var animationLen = Ecs.Render.Stores<RenderAnimationComponent>.Store.Count + extraAnimations;

        EnsureCapacity(entityLen);
        _commandBuffer.EnsureBufferCapacity(entityLen);
        _commandBuffer.EnsureBoneBuffer(animationLen);
    }

    private void EnsureCapacity(int amount)
    {
        if (_drawEntities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_drawEntities.Length, amount);
        if (newCap > FrameEntityBuffer.MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        _drawEntities = new DrawEntity[newCap];

        Logger.LogString(LogScope.World, $"Entity buffer resize {newCap}", LogLevel.Warn);
    }
}