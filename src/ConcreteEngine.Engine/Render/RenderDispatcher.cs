using System.Numerics;
using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Core.Renderer;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Mesh;
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher
{
    private RenderEntityId[] _visibleEntities;
    private int[] _visibleByIndices;

    private readonly RenderEntityCore _ecs;
    private readonly CameraTransform _camera;

    private DrawCommandBuffer _commandBuffer = null!;

    private AnimatorProcessor _animatorProcessor = null!;

    private Skybox _skybox = null!;
    private AnimationTable _animationTable = null!;
    private ParticleSystem _particleSystem = null!;

    public static TerrainMeshGenerator TerrainMesh;

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
        _commandBuffer = commandBuffer;
        _animationTable = worldBundle.Animations;
        _skybox = worldBundle.Sky;
        _particleSystem = worldBundle.ParticleSystem;
        _animatorProcessor = new AnimatorProcessor(_animationTable, _commandBuffer);

        EnvironmentUploader.RefreshMatrices();
    }

    private int PrepareExecute()
    {
        EnsureCapacity();

        EnvironmentUploader.SubmitDrawTerrain(_commandBuffer, TerrainMesh);
        EnvironmentUploader.SubmitDrawSkybox(_commandBuffer, _skybox);

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
        ParticleProcessor.Execute(_particleSystem);
    }

    private void ProcessEntities(int submitOffset, Span<RenderEntityId> visibleEntities, Span<int> visibleByIndices)
    {
        var drawCommands = _commandBuffer.GetDrawCommands(submitOffset);
        var ctx = new DrawEntityContext(visibleEntities, visibleByIndices, drawCommands);

        CollectEntities(in ctx);
        DrawTagResolver.TagResolveEntities(in ctx);
        SpatialProcessor.TagDepthKeys(in ctx, _camera);
        ParticleProcessor.TagParticles(in ctx, _particleSystem);
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