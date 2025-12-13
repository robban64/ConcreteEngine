#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Time;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Render.Data;
using ConcreteEngine.Engine.Worlds.Render.Processor;
using ConcreteEngine.Renderer.Definitions;
using ConcreteEngine.Shared.Diagnostics;

#endregion

namespace ConcreteEngine.Engine.Worlds.Render;

internal sealed class RenderEntityBus
{
    private static int _idx;
    private static int _prevIdx;

    public const int DefaultCapacity = 512;
    public const int MaxCapacity = 1024 * 50;

    //...
    private int[] _byEntityId = new int[DefaultCapacity];
    private EntityId[] _entityIndices = new EntityId[DefaultCapacity];
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    //...

    private World _world = null!;
    public ModelId CubeId;
    public MaterialTagKey EmptyMaterialKey;

    public static readonly FrameProfiler RenderProfiler = new(144, 144 * 10);

    internal RenderEntityBus()
    {
        RenderProfiler.Register("Collect");
        RenderProfiler.Register("Tag");
        RenderProfiler.Register("Particle");
        RenderProfiler.Register("Animator");
        RenderProfiler.Register("DrawCommands");
        RenderProfiler.Register("Transforms");
        RenderProfiler.Enabled = false;
    }


    private WorldEntities WorldEntities => _world!.Entities;

    internal void AttachWorld(World world) => _world = world;


    public void Reset()
    {
        _entityIndices.AsSpan(0, _idx).Clear();
        _byEntityId.AsSpan(0, _idx).Fill(-1);

        _prevIdx = _idx;
        _idx = 0;
    }

    private void Ensure()
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;

        var entityCount = WorldEntities.EntityCount;
        var ensureLen = entityCount + extraEntities;
        var animationLen = WorldEntities.Animations.Count + extraAnimations;
        EnsureDrawEntityData(ensureLen);
        DrawDataProvider.EnsureBuffer(ensureLen, animationLen);
    }

    public void Execute()
    {
        Ensure();
        Validate();
        StaticProfileTimer.RenderTimer.Begin();

        // start
        var len = _idx = DrawSpatialProcessor.CullEntities(_entityIndices, _byEntityId);

        if (len == 0) return;
        if ((uint)len > _entities.Length) throw new IndexOutOfRangeException();

        CollectEntities();
        TagCollectedEntities();

        SubmitWorldObjects();

        var ctx = new DrawEntityContext(_entities.AsSpan(0, len),_entityIndices.AsSpan(0, len), _byEntityId);

        DrawParticleProcessor.Execute(ctx, _world.Particles);
        DrawAnimatorProcessor.Execute(ctx);

        ctx = new DrawEntityContext(_entities.AsSpan(0, len),_entityIndices.AsSpan(0, len), _byEntityId);

        DrawCommandUploader.UploadDrawCommands(ctx);
        DrawTransformUploader.UploadTransform(ctx);

        // end
        StaticProfileTimer.RenderTimer.EndPrint();
    }

    private void Validate()
    {
        if (_entityIndices.Length == 0 || _entities.Length == 0)
            throw new InvalidOperationException();

        var view = WorldEntities.Core.GetCoreView();

        if (_entities.Length != _entityIndices.Length || _entities.Length != _byEntityId.Length)
            throw new InvalidOperationException();

        if (view.EntityId.Length != view.Transforms.Length || view.EntityId.Length != view.Sources.Length)
            throw new InvalidOperationException();

        var len = view.EntityId.Length;
        if ((uint)len > _entities.Length || (uint)len > view.Transforms.Length)
            throw new IndexOutOfRangeException();
    }

    //var ctx = new DrawEntityContext(_entities.Length, _entities, _entityData, _byEntityId);

    private void CollectEntities()
    {
        var len = _idx;
        if (len == 0 || (uint)len > _entities.Length || (uint)len > _entityIndices.Length)
            throw new IndexOutOfRangeException();

        var view  = WorldEntities.Core.GetCoreView();
        for (int i = 0; i < len; i++)
        {
            var entityId = _entityIndices[i];
            ref var drawEntity = ref _entities[i];
            ref readonly var source = ref view.GetSource(entityId);
            drawEntity.Entity = entityId;
            DrawEntityCollector.CollectEntity(ref drawEntity, entityId, in source);

        }
    }

    private void TagCollectedEntities()
    {
        var len = _idx;
        if (len == 0) return;
        if ((uint)len > _entities.Length || _entities.Length != _byEntityId.Length ||
            _entityIndices.Length != _entities.Length)
        {
            throw new IndexOutOfRangeException();
        }

        var ctx = new DrawEntityContext(_entities.AsSpan(0, len), _entityIndices, _byEntityId);

        DrawSpatialProcessor.TagDepthKeys(ctx);
        DrawTagResolver.TagEffectResolvers(ctx);
    }

    private void SubmitWorldObjects()
    {
        DrawWorldProcessor.SubmitDrawTerrain(_world!.Terrain);
        DrawWorldProcessor.SubmitDrawSkybox(_world!.Sky);
    }

    public void EnsureDrawEntityData(int amount)
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