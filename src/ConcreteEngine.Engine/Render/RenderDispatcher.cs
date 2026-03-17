using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher
{
    private readonly RenderEntityCore _ecs;

    private WorldBundle _worldBundle = null!;
    private AnimationTable _animationTable = null!;
    private DrawCommandBuffer _commandBuffer = null!;

    private AnimatorProcessor _animatorProcessor = null!;

    private DrawEntity[] _drawEntities;
    private RenderEntityId[] _visibleEntities;
    private int[] _visibleByIndices;

    public int VisibleCount { get; private set; }


    internal RenderDispatcher(RenderEntityCore ecs)
    {
        _drawEntities = new DrawEntity[ecs.Capacity];
        _visibleEntities = new RenderEntityId[ecs.Capacity];
        _visibleByIndices = new int[ecs.Capacity];
        _ecs = ecs;
    }

    public ReadOnlySpan<RenderEntityId> GetVisibleEntities() => _visibleEntities.AsSpan(0, VisibleCount);

    public void Init(WorldBundle worldBundle, DrawCommandBuffer commandBuffer)
    {
        _worldBundle = worldBundle;
        _commandBuffer = commandBuffer;
        _animationTable = _worldBundle.Animations;

        _animatorProcessor = new AnimatorProcessor(_animationTable, _commandBuffer);
    }

    internal void Execute()
    {
        EnsureCapacity();
        
        WorldObjectProcessor.SubmitWorldObjects(_commandBuffer, _worldBundle);

        var len = VisibleCount = SpatialProcessor
            .CullEntities(_visibleEntities, _visibleByIndices, CameraSystem.Instance.Camera);
        
        if (len == 0) return;
        if ((uint)len > (uint)_drawEntities.Length) throw new InvalidOperationException();

        ProcessEntities(new DrawEntityContext(len, _drawEntities, _visibleEntities, _visibleByIndices));

        _animatorProcessor.Execute(_visibleByIndices);

        ParticleProcessor.Execute(_worldBundle.ParticleSystem);
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
        if (_ecs.Capacity <= len) return;

        _drawEntities = new DrawEntity[_ecs.Capacity];
        _visibleEntities = new RenderEntityId[_ecs.Capacity];
        _visibleByIndices = new int[_ecs.Capacity];

        if (len > 0)
            Logger.LogString(LogScope.World, $"{nameof(RenderDispatcher)} _drawEntities resize {_ecs.Capacity}",
                LogLevel.Warn);
    }
}