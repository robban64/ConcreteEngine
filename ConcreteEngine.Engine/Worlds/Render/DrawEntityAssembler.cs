using System.Runtime.CompilerServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.ECS.RenderComponent;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Processor;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.View;
using ConcreteEngine.Renderer.Data;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class DrawEntityAssembler
{
    private const int DefaultCapacity = 512;
    private const int MaxCapacity = 1024 * 50;

    private static int _idx;
    private static int _prevIdx;
    private static RenderEntityId _highEntityId;

    //...
    private static int[] _byEntityId = new int[DefaultCapacity];
    private static RenderEntityId[] _entityIndices = new RenderEntityId[DefaultCapacity];
    private static DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    //...

    private readonly World _world;
    private readonly RenderEntityHub _renderEntityHub;
    private readonly WorldParticles _worldParticles;
    private readonly MeshTable _meshTable;
    private readonly AnimationTable _animationTable;
    private readonly Camera3D _camera;

    public MaterialId BoundsMaterial;

    //private static readonly FrameProfiler RenderProfiler = new(144, 144 * 10);

    public ReadOnlySpan<RenderEntityId> VisibleEntities => _entityIndices.AsSpan(0, _idx);

    internal DrawEntityAssembler(World world)
    {
        _world = world;
        _renderEntityHub = world.Entities;
        _worldParticles = world.Particles;
        _meshTable = world.MeshTableImpl;
        _animationTable = world.AnimationTableImpl;
        _camera = world.Camera;
        /*
        RenderProfiler.Register("Collect");
        RenderProfiler.Register("Tag");
        RenderProfiler.Register("Particle");
        RenderProfiler.Register("Animator");
        RenderProfiler.Register("DrawCommands");
        RenderProfiler.Register("Transforms");
        RenderProfiler.Enabled = false;
        */
    }


    public void Reset()
    {
        _entityIndices.AsSpan(0, _idx).Clear();
        _byEntityId.AsSpan(0, _highEntityId).Fill(-1);

        _prevIdx = _idx;
        _idx = 0;
        _highEntityId = default;
    }

    private void Ensure(DrawCommandBuffer commandBuffer)
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityLen = _renderEntityHub.Core.Count + extraEntities;
        var animationLen = _renderEntityHub.GetStore<AnimationComponent>().Count + extraAnimations;

        EnsureDrawEntityData(entityLen);
        commandBuffer.EnsureBufferCapacity(entityLen);
        commandBuffer.EnsureBoneBuffer(animationLen);
    }

    public void Execute(DrawCommandBuffer commandBuffer)
    {
        Ensure(commandBuffer);
        Validate();

        // start
        DrawWorldProcessor.SubmitWorldObjects(commandBuffer, _world);

        // cull
        StaticProfileTimer.RenderTimer.Begin();
        var renderView = _camera.RenderView;
        var len = CullEntities(in renderView);
        
        // execute
        var ctx = new DrawEntityContext(_entities.AsSpan(0, len), _entityIndices.AsSpan(0, len), _byEntityId);
        
        ExecuteCollectCommands(in ctx, in renderView);
        ExecuteUploader(in ctx, commandBuffer);
        ExecuteProcessors(in ctx, commandBuffer);
        StaticProfileTimer.RenderTimer.EndPrint();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CullEntities(in CameraRenderView renderView)
    {
        var ecsLen = _renderEntityHub.EntityCount;
        var len = _idx = DrawEntityCulling.CullEntities(_entityIndices, _byEntityId, _renderEntityHub, in renderView);
        if (len == 0) return 0;
        if ((uint)len > _entities.Length || (uint)len > _entityIndices.Length || (uint)ecsLen > _byEntityId.Length)
            throw new IndexOutOfRangeException();

        return len;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteCollectCommands(in DrawEntityContext ctx, in CameraRenderView renderView)
    {
        var coreEntities = _renderEntityHub.Core.GetCoreView();

        _highEntityId = DrawEntityCollector.CollectEntities(in ctx, in coreEntities);
        DrawTagResolver.TagResolveEntities(in ctx, _renderEntityHub);
        DrawEntityCulling.TagDepthKeys(in ctx, in coreEntities, in renderView);
        DrawParticleProcessor.TagParticles(in ctx, _worldParticles, _renderEntityHub);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteUploader(in DrawEntityContext ctx, DrawCommandBuffer commandBuffer)
    {
        var coreEntities = _renderEntityHub.Core.GetCoreView();

        var uploader = commandBuffer.GetDrawUploaderCtx();
        DrawEntityUploader.UploadDrawCommands(_world, in ctx, in uploader);
        DrawTransformUploader.UploadTransform(in ctx, in coreEntities, in uploader, _meshTable);
        DrawTagResolver.UploadDebugBounds(in ctx, in uploader, _renderEntityHub, _meshTable, BoundsMaterial);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExecuteProcessors(in DrawEntityContext ctx, DrawCommandBuffer commandBuffer)
    {
        var skinningUploader = commandBuffer.GetSkinningUploaderCtx();
        var animationView = _animationTable.GetDataView();

        DrawAnimatorProcessor.Execute(_renderEntityHub, in ctx, in skinningUploader, in animationView);
        DrawParticleProcessor.Execute(_worldParticles);
    }


    private void Validate()
    {
        if (_entityIndices.Length == 0 || _entities.Length == 0)
            throw new InvalidOperationException();

        var view = _world.Entities.Core.GetCoreView();

        if (_entities.Length != _entityIndices.Length || _entities.Length != _byEntityId.Length)
            throw new InvalidOperationException();

        if (view.Boxes.Length != view.Transforms.Length || view.Boxes.Length != view.Sources.Length)
            throw new InvalidOperationException();

        var len = view.Boxes.Length;
        if ((uint)len > _entities.Length || (uint)len > view.Transforms.Length)
            throw new IndexOutOfRangeException();
    }

    private void EnsureDrawEntityData(int amount)
    {
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entities.Length);
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entityIndices.Length);

        if (_entities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_entities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        Array.Resize(ref _entities, newCap);
        Array.Resize(ref _entityIndices, newCap);
        Array.Resize(ref _byEntityId, newCap);
        Logger.LogString(LogScope.World, $"Entity buffer resize {newCap}", LogLevel.Warn);
    }
}