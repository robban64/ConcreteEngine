using System.Runtime.CompilerServices;
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
        Console.WriteLine("SIZE " + Unsafe.SizeOf<DrawEntityItem>());
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
        var submitOffset = _commandBuffer.Count;
        var drawCommands = _commandBuffer.GetDrawCommands(submitOffset);
        ProcessEntities(new DrawEntityContext(visibleEntities, visibleByIndices, drawCommands));
        UploadDrawCommands(visibleEntities);
        DrawTagResolver.UploadDebugBounds(submitOffset, visibleByIndices, _commandBuffer);

        _animatorProcessor.Execute();

        ParticleProcessor.Execute(_worldBundle.ParticleSystem);
    }

    private void ProcessEntities(in DrawEntityContext ctx)
    {
        CollectEntities(in ctx);
        DrawTagResolver.TagResolveEntities(in ctx);
        SpatialProcessor.TagDepthKeys(in ctx, _camera);
        ParticleProcessor.TagParticles(in ctx, _worldBundle.ParticleSystem);
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

    private void UploadDrawCommands(Span<RenderEntityId> visibleEntities)
    {
        var ecs = _ecs;
        var buffer = _commandBuffer;
        var len = visibleEntities.Length;
        for (int i = 0; i < len; i++)
        {
            var entity = visibleEntities[i];
            ref readonly var world = ref ecs.GetParentMatrix(entity);
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