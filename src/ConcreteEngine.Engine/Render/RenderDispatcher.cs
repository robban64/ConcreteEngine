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

    private WorldBundle _worldBundle = null!;
    private AnimationTable _animationTable = null!;
    private DrawCommandBuffer _commandBuffer = null!;

    private AnimatorProcessor _animatorProcessor = null!;

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
        _commandBuffer = commandBuffer;
        _animationTable = _worldBundle.Animations;

        _animatorProcessor = new AnimatorProcessor(_animationTable, _commandBuffer);
    }

    internal void Execute()
    {
        if (_ecs.Capacity > _drawEntities.Length)
            EnsureCapacity();

        WorldObjectProcessor.SubmitWorldObjects(_commandBuffer, _worldBundle);

        var len = SpatialProcessor.CullEntities(_frameBuffer, CameraSystem.Instance.Camera);
        if (len == 0) return;
        if ((uint)len > (uint)_drawEntities.Length) throw new InvalidOperationException();

        _frameBuffer.VisibleCount = len;

        ProcessEntities(new DrawEntityContext(len, _ecs.Count, _drawEntities, _frameBuffer.EntityToVisibleIdx,
            _frameBuffer.VisibleEntityIds));

        _animatorProcessor.Execute(_frameBuffer.EntityToVisibleIdx);

        ParticleProcessor.Execute(_frameBuffer.EntityToVisibleIdx, _worldBundle.ParticleSystem);
    }

    private void ProcessEntities(in DrawEntityContext ctx)
    {
        RenderEntityCollector.CollectEntities(in ctx);
        DrawTagResolver.TagResolveEntities(in ctx);
        SpatialProcessor.TagDepthKeys(in ctx, CameraSystem.Instance.Camera);
        ParticleProcessor.TagParticles(in ctx, _worldBundle.ParticleSystem);

        RenderEntityCollector.UploadDrawCommands(_commandBuffer, in ctx);

        DrawTagResolver.UploadDebugBounds(in ctx, _commandBuffer);
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