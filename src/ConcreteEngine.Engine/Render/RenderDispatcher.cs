using System.Runtime.CompilerServices;
using ConcreteEngine.Core.Common;
using ConcreteEngine.Core.Common.Collections;
using ConcreteEngine.Core.Common.Numerics.Maths;
using ConcreteEngine.Core.Diagnostics.Logging;
using ConcreteEngine.Core.Diagnostics.Time;
using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.ECS;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;
using ConcreteEngine.Core.Engine.Graphics;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher : IDisposable
{
    private RenderEntityId[] _visibleEntities;
    private int[] _visibleByIndices;

    public int VisibleCount { get; private set; }

    private readonly RenderEntityCore _ecs;
    private readonly CameraSystem _cameraSystem;

    private readonly AnimationTable _animationTable;
    private readonly ParticleSystem _particleSystem;

    private AnimatorProcessor _animatorProcessor = null!;
    private RenderUploadBuffers _uploadBuffers = null!;

    internal RenderDispatcher(AnimationTable animations, ParticleSystem particleSystem)
    {
        _ecs = Ecs.Render.Core;
        _visibleEntities = new RenderEntityId[_ecs.Capacity];
        _visibleByIndices = new int[_ecs.Capacity];

        _animationTable = animations;
        _particleSystem = particleSystem;
        _cameraSystem = CameraSystem.Instance;
    }

    public ReadOnlySpan<RenderEntityId> GetVisibleEntities() => _visibleEntities.AsSpan(0, VisibleCount);

    public void Attach(RenderUploadBuffers uploadBuffers)
    {
        _uploadBuffers = uploadBuffers;
        _animatorProcessor = new AnimatorProcessor(_animationTable, uploadBuffers.Skinning);
        EnvironmentProcessor.RefreshMatrices();
    }

    private int PrepareExecute()
    {
        EnsureCommandBuffer();
        EnsureCapacity();

        EnvironmentProcessor.SubmitDrawTerrain(_uploadBuffers.Commands, TerrainSystem.Instance,
            _cameraSystem.Frustum);
        EnvironmentProcessor.SubmitDrawSkybox(_uploadBuffers.Commands, Skybox.Instance);

       return VisibleCount = SpatialProcessor.CullEntities(
            _visibleEntities,
            new UnsafeSpan<int>(_visibleByIndices),
            _cameraSystem.Frustum
        );

    }

    internal void Execute()
    {
        var len = PrepareExecute();
        if (len == 0) return;
        if ((uint)len > (uint)_visibleEntities.Length || (uint)_ecs.Count > (uint)_visibleByIndices.Length)
            Throwers.InvalidOperation();

        var visibleEntities = _visibleEntities.AsSpan(0, len);
        var visibleByIndices = _visibleByIndices.AsSpan(0, _ecs.Count);
        var submitOffset = _uploadBuffers.Commands.Count;

        ProcessEntities(submitOffset, visibleEntities, visibleByIndices);

        UploadDrawCommands(visibleEntities);
        DrawTagProcessor.UploadDebugBounds(submitOffset, visibleByIndices, _uploadBuffers.Commands,_uploadBuffers.Effects);

        _animatorProcessor.Execute();
        ParticleProcessor.Execute(_particleSystem);
    }


    private void ProcessEntities(int submitOffset, Span<RenderEntityId> visibleEntities, Span<int> visibleByIndices)
    {
        var drawCommands = _uploadBuffers.Commands.GetDrawCommands(submitOffset);
        var ctx = new DrawEntityContext(visibleEntities, visibleByIndices, drawCommands);

        CollectEntities(in ctx);
        DrawTagProcessor.TagAnimationEntities(in ctx);
        ParticleProcessor.TagParticles(in ctx, _particleSystem);
        DrawTagProcessor.TagUploadSelectionEffect(in ctx, _uploadBuffers.Effects);
        SpatialProcessor.TagDepthKeys(in ctx, _cameraSystem);
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
        var buffer = _uploadBuffers.Commands;
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

        _uploadBuffers.Commands.EnsureCapacity(entityLen);
        _uploadBuffers.Skinning.EnsureCapacity(animationLen);
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

    public void Dispose() => _animatorProcessor.Dispose();
}