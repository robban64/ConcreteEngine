using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Processor;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Renderer.Draw;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class DrawEntityAssembler
{
    private const int DefaultCapacity = 512;
    private const int MaxCapacity = 1024 * 50;

    private static int _idx;
    private static int _prevIdx;
    private  static EntityId _highEntityId;

    //...
    private static int[] _byEntityId = new int[DefaultCapacity];
    private static EntityId[] _entityIndices = new EntityId[DefaultCapacity];
    private static DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    //...

    private readonly World _world;
    public ModelId CubeId;
    public MaterialTagKey EmptyMaterialKey;

    //private static readonly FrameProfiler RenderProfiler = new(144, 144 * 10);

    public ReadOnlySpan<EntityId> VisibleEntities => _entityIndices.AsSpan(0, _idx);

    internal DrawEntityAssembler(World world)
    {
        _world = world;
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

        var entityLen = _world.EntityCount + extraEntities;
        var animationLen = _world.Entities.Animations.Count + extraAnimations;

        EnsureDrawEntityData(entityLen);
        commandBuffer.EnsureBufferCapacity(entityLen);
        commandBuffer.EnsureBoneBuffer(animationLen);
    }

    public void Execute(DrawCommandBuffer commandBuffer)
    {
        Ensure(commandBuffer);
        Validate();

        // start
        DrawWorldProcessor.SubmitWorldObjects(_world, commandBuffer.GetDrawUploaderCtx());

        var worldEntities = _world.Entities;
        var ecsLen = worldEntities.EntityCount;

        // cull
        var len = _idx = DrawEntityCulling.CullEntities(_entityIndices, _byEntityId, _world);

        if (len == 0) return;
        if ((uint)len > _entities.Length || (uint)len > _entityIndices.Length || (uint)ecsLen > _byEntityId.Length)
            throw new IndexOutOfRangeException();

        var ctx = new DrawEntityContext(_entities.AsSpan(0, len), _entityIndices.AsSpan(0, len),
            _byEntityId.AsSpan(0, ecsLen));

        var coreEntities = worldEntities.Core.GetReadView();

        // collect
        _highEntityId = DrawEntityCollector.CollectEntities(in ctx, in coreEntities);

        // tag
        TagDrawEntities(in ctx, in coreEntities);

        ExecuteUploader(in ctx, in coreEntities, commandBuffer);
        ExecuteProcessors(in ctx, commandBuffer);
        // end
    }

    private void TagDrawEntities(in DrawEntityContext ctx, in EntitiesReadView coreEntities)
    {
        DrawTagResolver.TagEffectResolvers(in ctx, _world.Entities);
        DrawTagResolver.TagDepthKeys(in ctx, in coreEntities, _world.Camera.RenderView);
        DrawParticleProcessor.TagParticles(in ctx, _world.Particles, _world.Entities);
    }

    private void ExecuteUploader(in DrawEntityContext ctx, in EntitiesReadView coreEntities,
        DrawCommandBuffer commandBuffer)
    {
        var meshTable = _world.MeshTableImpl;
        var uploader = commandBuffer.GetDrawUploaderCtx();
        StaticProfileTimer.RenderTimer.Begin();
        DrawEntityUploader.UploadDrawCommands(_world, in ctx, in uploader);
        StaticProfileTimer.RenderTimer.EndPrint();
        DrawTransformUploader.UploadTransform(in ctx, in coreEntities, in uploader, meshTable);

    }

    private void ExecuteProcessors(in DrawEntityContext ctx, DrawCommandBuffer commandBuffer)
    {
        var skinningUploader = commandBuffer.GetSkinningUploaderCtx();
        var animationView = _world.AnimationTableImpl.GetDataView();

        DrawAnimatorProcessor.Execute(_world.Entities, in ctx, in skinningUploader, in animationView);
        DrawParticleProcessor.Execute(_world.Particles);
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