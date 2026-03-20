using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher
{
    private readonly RenderEntityCore _ecs;
    private readonly CameraTransform _camera;
    
    private WorldBundle _worldBundle = null!;
    private AnimationTable _animationTable = null!;
    private DrawCommandBuffer _commandBuffer = null!;

    private AnimatorProcessor _animatorProcessor = null!;

    private RenderEntityId[] _visibleEntities;
    private int[] _visibleByIndices;

    public int VisibleCount { get; private set; }

    internal RenderDispatcher(RenderEntityCore ecs)
    {
        _visibleEntities = new RenderEntityId[ecs.Capacity];
        _visibleByIndices = new int[ecs.Capacity];
        _ecs = ecs;

        _camera = CameraSystem.Instance.Camera;
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

        var len = VisibleCount =
            SpatialProcessor.CullEntities(_visibleEntities, _visibleByIndices, _camera);

        if (len == 0) return;
        if ((uint)len > (uint)_visibleEntities.Length || (uint)_ecs.Count > (uint)_visibleByIndices.Length)
            throw new InvalidOperationException();

        var visibleEntities = _visibleEntities.AsSpan(0, len);
        var visibleByIndices = _visibleByIndices.AsSpan(0, _ecs.Count);
        ProcessEntities(new DrawEntityContext(visibleEntities, visibleByIndices, _commandBuffer));
        UploadDrawCommands(visibleEntities);

        _animatorProcessor.Execute();

        ParticleProcessor.Execute(_worldBundle.ParticleSystem);
    }

    private void ProcessEntities(in DrawEntityContext ctx)
    {
        CollectEntities(in ctx);
        DrawTagResolver.TagResolveEntities(in ctx);
        SpatialProcessor.TagDepthKeys(in ctx, _camera);
        ParticleProcessor.TagParticles(in ctx, _worldBundle.ParticleSystem);
        DrawTagResolver.UploadDebugBounds(in ctx, _commandBuffer);
    }
    
    private void CollectEntities(in DrawEntityContext ctx)
    {
        var ecs = _ecs;
        foreach (var it in ctx)
        {
            ref readonly var source = ref ecs.GetSource(it.Entity);
            it.Command = new DrawCommand(source.Mesh, source.Material);
            it.Meta = new DrawCommandMeta(DrawCommandId.Model, source.Queue, source.Mask);
        }
    }

    private void UploadDrawCommands(ReadOnlySpan<RenderEntityId> entities)
    {
        var ecs = _ecs;
        var buffer = _commandBuffer;
        foreach (var it in entities)
        {
            ref readonly var world = ref ecs.GetParentMatrix(it);
            ref var bufferData = ref buffer.SubmitDraw();
            bufferData.Model = world;
            MatrixMath.CreateNormalMatrix(in world, out bufferData.Normal);
        }
    }


    private void EnsureCapacity()
    {
        var len = _visibleEntities.Length;
        if (_ecs.Capacity <= len) return;

        _visibleEntities = new RenderEntityId[_ecs.Capacity];
        _visibleByIndices = new int[_ecs.Capacity];

        if (len > 0)
            Logger.LogString(LogScope.World, $"{nameof(RenderDispatcher)} _drawEntities resize {_ecs.Capacity}",
                LogLevel.Warn);
    }
}