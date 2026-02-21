using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher
{
    private readonly RenderEntityCore _ecs;
    private readonly FrameEntityBuffer _frameBuffer;

    private Camera _camera = null!;
    private WorldBundle _worldBundle = null!;
    private AnimationTable _animationTable = null!;
    private DrawCommandBuffer _commandBuffer = null!;

    private DrawEntity[] _drawEntities;

    internal RenderDispatcher(RenderEntityCore ecs, FrameEntityBuffer frameBuffer)
    {
        _drawEntities = new DrawEntity[ecs.Capacity];
        _frameBuffer = frameBuffer;
        _ecs = ecs;
    }

    public void Init(WorldBundle worldBundle, DrawCommandBuffer commandBuffer)
    {
        _worldBundle = worldBundle;
        _camera = worldBundle.Camera;
        _commandBuffer = commandBuffer;
        _animationTable = _worldBundle.Animations;
    }


    internal void Execute()
    {
        if (_ecs.Capacity > _drawEntities.Length)
            EnsureCapacity();

        WorldObjectProcessor.SubmitWorldObjects(_commandBuffer, _worldBundle);

        var len = SpatialProcessor.CullEntities(_frameBuffer, _camera.RenderView);
        if (len == 0) return;
        if ((uint)len > _drawEntities.Length) throw new IndexOutOfRangeException();

        _frameBuffer.VisibleCount = len;
        _frameBuffer.GetWriteSpans(out var visible, out var map);
        var ctx = new DrawEntityContext(len, _ecs.Count, _drawEntities, map, visible);

        ExecuteCollectCommands(in ctx);
        ExecuteUploader(in ctx);

        AnimatorProcessor.Execute(_animationTable, _commandBuffer, map);
        
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
        var uploader = _commandBuffer.GetDrawUploaderCtx(_ecs.Count);
        RenderEntityCollector.UploadDrawCommands(in uploader, in ctx);
        DrawTagResolver.UploadDebugBounds(in ctx, in uploader);
    }

    private void EnsureCapacity()
    {
        var len = _drawEntities.Length;
        var newCap = Arrays.CapacityGrowthSafe(len, _ecs.Capacity);
        if (newCap > FrameEntityBuffer.MaxCapacity)
            throw new OutOfMemoryException($"{nameof(RenderDispatcher)} _drawEntities exceeded max limit");

        _drawEntities = new DrawEntity[newCap];

        if (len > 0)
            Logger.LogString(LogScope.World, $"{nameof(RenderDispatcher)} _drawEntities resize {newCap}",
                LogLevel.Warn);
    }
}