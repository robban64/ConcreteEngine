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
using ConcreteEngine.Engine.Render.Data;
using ConcreteEngine.Engine.Render.Processor;
using ConcreteEngine.Renderer.Buffer;

namespace ConcreteEngine.Engine.Render;

internal sealed class RenderDispatcher : IDisposable
{
    public int VisibleCount { get; private set; }

    private RenderEntityId[] _visibleEntities;
    private int[] _visibleByIndices;

    private readonly RenderUploadBuffers _uploadBuffers;
    private readonly DrawCommandBuffer _commandBuffer;

    private readonly CameraManager _cameraManager;

    private readonly SpatialProcessor _spatialProcessor;
    private readonly AnimatorProcessor _animatorProcessor;
    private readonly ParticleSystem _particleSystem;
    
    internal RenderDispatcher(AnimationSystem animations, ParticleSystem particleSystem, RenderUploadBuffers uploadBuffers)
    {
        ArgumentNullException.ThrowIfNull(animations);
        ArgumentNullException.ThrowIfNull(particleSystem);
        ArgumentNullException.ThrowIfNull(uploadBuffers);

        var entityCap = Ecs.Render.Core.Capacity;
        _visibleEntities = new RenderEntityId[entityCap];
        _visibleByIndices = new int[entityCap];

        _cameraManager = CameraManager.Instance;

        _uploadBuffers = uploadBuffers;
        _commandBuffer = uploadBuffers.Commands;
        _particleSystem = particleSystem;
        _spatialProcessor = new SpatialProcessor(_cameraManager.Frustum, _cameraManager.Camera);
        _animatorProcessor = new AnimatorProcessor(animations, uploadBuffers.Skinning);
    }

    public ReadOnlySpan<RenderEntityId> GetVisibleEntities() => _visibleEntities.AsSpan(0, VisibleCount);

    public void Prepare(TerrainSystem terrain)
    {
        EnsureCommandBuffer();
        EnsureCapacity();

        terrain.SubmitDrawTerrain(_commandBuffer, _cameraManager.Frustum);

        {
            var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
            var cmd = new DrawCommand(Skybox.Current.MeshId, Skybox.Current.MaterialId);
            _commandBuffer.Submit(cmd, meta, in DrawCommandBuffer.TransformIdentity);
        }
    }
    
    public void UploadProcessors()
    {
        _animatorProcessor.Execute();
        _particleSystem.Upload();
    }

    internal void Execute()
    {
        var len = VisibleCount = _spatialProcessor.CullEntities(
            _visibleEntities,
            new UnsafeSpan<int>(_visibleByIndices)
        );

        if (len == 0) return;
        if ((uint)len > (uint)_visibleEntities.Length || (uint)Ecs.Render.Core.Capacity > (uint)_visibleByIndices.Length)
            Throwers.InvalidOperation();

        var visibleEntities = _visibleEntities.AsSpan(0, len);
        var visibleByIndices = _visibleByIndices.AsSpan(0, Ecs.Render.Core.Capacity);
        var submitOffset = _commandBuffer.Count;

        ProcessEntities(submitOffset, visibleEntities, visibleByIndices);
        UploadDrawCommands(visibleEntities);
        DrawTagProcessor.UploadDebugBounds(submitOffset, visibleByIndices, _commandBuffer, _uploadBuffers.Effects);

    }

    private void ProcessEntities(int submitOffset, Span<RenderEntityId> visibleEntities, Span<int> visibleByIndices)
    {
        var ctx = new DrawEntityContext(visibleEntities, visibleByIndices, _commandBuffer.GetDrawCommands(submitOffset));
        CollectEntities(in ctx);
        TagParticles(in ctx);
        DrawTagProcessor.TagUploadSelectionEffect(in ctx, _uploadBuffers.Effects);
        _spatialProcessor.TagDepthKeys(in ctx);
        _animatorProcessor.Tag(in ctx);
    }

    private void CollectEntities(in DrawEntityContext ctx)
    {
        var sources = Ecs.Render.Core.GetSourceView();
        foreach (var it in ctx)
        {
            ref readonly var source = ref sources[it.Entity.Index()];
            it.Command = new DrawCommand(source.Mesh, source.Material);
            it.Meta = new DrawCommandMeta(DrawCommandId.Model, source.Queue, source.Mask);
        }
    }

    internal void TagParticles(in DrawEntityContext ctx)
    {
        foreach (var query in Ecs.GetRenderStore<ParticleComponent>().Query())
        {
            var drawItem = ctx.TryGetVisible(query.Entity);
            if (drawItem.Entity.Id == 0) continue;

            drawItem.Command = _particleSystem.MakeDrawCommand(query.Component.Emitter, query.Component.Material);
            drawItem.Meta = ParticleSystem.DrawMeta;
        }
    }

    private void UploadDrawCommands(Span<RenderEntityId> visibleEntities)
    {
        var buffer = _commandBuffer;
        var parentMatrices = Ecs.RenderCore.GetMatrixView();
        for (var i = 0; i < visibleEntities.Length; i++)
        {
            ref readonly var world = ref parentMatrices[visibleEntities[i].Index()];
            ref var bufferData = ref buffer.SubmitDraw();
            bufferData.Model = world;
            MatrixMath.CreateNormalMatrix(ref bufferData.Normal, in world);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCommandBuffer()
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = Ecs.Render.Core.Count + extraEntities;
        var animationLen = Ecs.Render.Stores<SkinningComponent>.Store.Count + extraAnimations;

        _uploadBuffers.Commands.EnsureCapacity(entityLen);
        _uploadBuffers.Skinning.EnsureCapacity(animationLen);
    }

    private void EnsureCapacity()
    {
        var len = _visibleEntities.Length;
        var entityCap = Ecs.Render.Core.Capacity;
        if (entityCap <= len) return;

        _visibleEntities = new RenderEntityId[entityCap];
        _visibleByIndices = new int[entityCap];

        if (len > 0)
            Logger.LogString(LogScope.Ecs, $"{nameof(RenderDispatcher)} _drawEntities resize {entityCap}", LogLevel.Warn);
    }

    public void Dispose() => _animatorProcessor.Dispose();
}