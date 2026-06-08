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

        _visibleByIndices = new int[Ecs.Render.Core.Capacity];

        _cameraManager = CameraManager.Instance;

        _uploadBuffers = uploadBuffers;
        _commandBuffer = uploadBuffers.Commands;
        _particleSystem = particleSystem;
        _spatialProcessor = new SpatialProcessor(_cameraManager.Frustum, _cameraManager.Camera);
        _animatorProcessor = new AnimatorProcessor(animations, uploadBuffers.Skinning);
    }

    public ReadOnlySpan<RenderEntityId> GetVisibleEntities() => ReadOnlySpan<RenderEntityId>.Empty;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<int> GetVisibleIndices() => _visibleByIndices.AsSpan(0, Ecs.Render.Core.Capacity);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private UnsafeSpan<int> GetVisibleIndicesUnsafe() => new(_visibleByIndices.AsSpan(0, Ecs.Render.Core.Capacity));


    public void Prepare()
    {
        EnsureCommandBuffer();
        EnsureCapacity();
    }

    public void UploadStaticCommands(TerrainSystem terrain)
    {
        terrain.SubmitDrawTerrain(_commandBuffer, _cameraManager.Frustum);

        var meta = new DrawCommandMeta(DrawCommandId.Skybox, DrawCommandQueue.Skybox, passMask: PassMask.Main);
        var cmd = new DrawCommand(Skybox.Current.MeshId, Skybox.Current.MaterialId);
        _commandBuffer.Submit(cmd, meta, in DrawCommandBuffer.TransformIdentity);
    }
    
    public void UploadProcessors()
    {
        _animatorProcessor.Execute();
        _particleSystem.Upload();
    }

    internal void Execute()
    {
        VisibleCount = _spatialProcessor.CullEntities();
        if (VisibleCount == 0) return;
        
        var capacity = Ecs.Render.Core.Capacity;
        if ((uint)capacity > (uint)_visibleByIndices.Length)
            Throwers.InvalidOperation();

        var submitOffset = _commandBuffer.Count;
        var visibleByIndices = _visibleByIndices.AsSpan(0, capacity);
        ProcessEntities(submitOffset, visibleByIndices);
        UploadDrawCommands();
        DrawTagProcessor.UploadDebugBounds(submitOffset, visibleByIndices, _commandBuffer, _uploadBuffers.Effects);

    }

    private void ProcessEntities(int submitOffset, Span<int> visibleIndices)
    {
        var ctx = _commandBuffer.GetContext(submitOffset);
        CollectEntities(ctx, visibleIndices);
        _animatorProcessor.Tag(ctx, visibleIndices);
        DrawTagProcessor.TagUploadSelectionEffect(ctx, visibleIndices, _uploadBuffers.Effects);
        _spatialProcessor.TagDepthKeys(ctx);
    }

    private void CollectEntities(DrawCommandContext ctx, Span<int> visibleIndices)
    {
        var sources = Ecs.Render.Core.GetSourceView();
        foreach (var query in Ecs.Render.Core.VisibilityQuery())
        {
            visibleIndices[query.Entity.Id] = query.VisibleIndex;
            ref readonly var source = ref sources[query.Entity.Index()];
            ctx.GetCommand(query.VisibleIndex) = new DrawCommand(source.Mesh, source.Material);
            ctx.GetMeta(query.VisibleIndex) = new DrawCommandMeta(DrawCommandId.Model, source.Queue, source.Mask);
        }
    }
    private void UploadDrawCommands()
    {
        var buffer = _commandBuffer;
        var parentMatrices = Ecs.RenderCore.GetMatrixView();
        
        foreach (var query in Ecs.Render.Core.VisibilityQuery())
        {
            ref readonly var world = ref parentMatrices[query.Entity.Index()];
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
        var len = _visibleByIndices.Length;
        var entityCap = Ecs.Render.Core.Capacity;
        if (entityCap <= len) return;

        _visibleByIndices = new int[entityCap];

        if (len > 0)
            Logger.LogString(LogScope.Ecs, $"{nameof(RenderDispatcher)} _drawEntities resize {entityCap}", LogLevel.Warn);
    }

    public void Dispose() => _animatorProcessor.Dispose();
}