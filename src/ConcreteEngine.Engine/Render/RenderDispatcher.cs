using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher
{
    private RenderEntityId[] _visibleEntities;
    private int[] _visibleByIndices;

    public int VisibleCount { get; private set; }

    private readonly RenderEntityCore _ecs;
    private readonly Camera _camera;

    private readonly AnimationTable _animationTable;
    private readonly ParticleManager _particleManager;

    private DrawCommandBuffer _commandBuffer = null!;
    private AnimatorProcessor _animatorProcessor = null!;

    internal RenderDispatcher(AnimationTable animations, ParticleManager particleManager)
    {
        _ecs = Ecs.Render.Core;
        _visibleEntities = new RenderEntityId[_ecs.Capacity];
        _visibleByIndices = new int[_ecs.Capacity];

        _animationTable = animations;
        _particleManager = particleManager;
        _camera = CameraManager.Instance.Camera;
    }

    public ReadOnlySpan<RenderEntityId> GetVisibleEntities() => _visibleEntities.AsSpan(0, VisibleCount);

    public void Init(DrawCommandBuffer commandBuffer)
    {
        _commandBuffer = commandBuffer;
        _animatorProcessor = new AnimatorProcessor(_animationTable, _commandBuffer);
        EnvironmentUploader.RefreshMatrices();
    }

    private int PrepareExecute()
    {
        EnsureCommandBuffer();
        EnsureCapacity();

        EnvironmentUploader.SubmitDrawTerrain(_commandBuffer, TerrainManager.Instance);
        EnvironmentUploader.SubmitDrawSkybox(_commandBuffer, Skybox.Instance);

        return VisibleCount =
            SpatialProcessor.CullEntities(_visibleEntities, _visibleByIndices, _camera);
    }

    internal void Execute()
    {
        var len = PrepareExecute();
        if (len == 0) return;
        if ((uint)len > (uint)_visibleEntities.Length || (uint)_ecs.Count > (uint)_visibleByIndices.Length)
            throw new InvalidOperationException();

        var visibleEntities = _visibleEntities.AsSpan(0, len);
        var visibleByIndices = _visibleByIndices.AsSpan(0, _ecs.Count);
        var submitOffset = _commandBuffer.Count;

        ProcessEntities(submitOffset, visibleEntities, visibleByIndices);

        UploadDrawCommands(visibleEntities);
        DrawTagResolver.UploadDebugBounds(submitOffset, visibleByIndices, _commandBuffer);

        _animatorProcessor.Execute();
        ParticleProcessor.Execute(_particleManager);
    }

    private void ProcessEntities(int submitOffset, Span<RenderEntityId> visibleEntities, Span<int> visibleByIndices)
    {
        var drawCommands = _commandBuffer.GetDrawCommands(submitOffset);
        var ctx = new DrawEntityContext(visibleEntities, visibleByIndices, drawCommands);

        CollectEntities(in ctx);
        DrawTagResolver.TagResolveEntities(in ctx);
        SpatialProcessor.TagDepthKeys(in ctx, _camera);
        ParticleProcessor.TagParticles(in ctx, _particleManager);
    }

    private void CollectEntities(in DrawEntityContext ctx)
    {
        var sources = _ecs.GetSourceView();
        foreach (var it in ctx)
        {
            ref readonly var source = ref sources[it.Entity.Index()];
            it.Command = new DrawCommand(source.Mesh, source.Material);
            it.Meta = new DrawCommandMeta(DrawCommandId.Model, source.Queue, source.Mask);
        }
    }

    private void UploadDrawCommands(Span<RenderEntityId> visibleEntities)
    {
        var buffer = _commandBuffer;
        var parentMatrices = _ecs.GetMatrixView();
        var len = visibleEntities.Length;
        for (var i = 0; i < len; i++)
        {
            ref readonly var world = ref parentMatrices[visibleEntities[i].Index()];
            ref var bufferData = ref buffer.SubmitDraw();
            bufferData.Model = world;
            MatrixMath.CreateNormalMatrix(in world, out bufferData.Normal);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCommandBuffer()
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = Ecs.Render.Core.Count + extraEntities;
        var animationLen = Ecs.Render.Stores<RenderAnimationComponent>.Store.Count + extraAnimations;

        _commandBuffer.EnsureBufferCapacity(entityLen);
        _commandBuffer.EnsureBoneBuffer(animationLen);
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