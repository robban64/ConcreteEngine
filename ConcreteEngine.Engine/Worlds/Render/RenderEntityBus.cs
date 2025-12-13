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

    private static int _visibleIdx;

    public const int DefaultCapacity = 512;
    public const int MaxCapacity = 1024 * 50;

    //...
    private int[] _byEntityId = new int[DefaultCapacity];
    private int[] _visibleIndices = new int[DefaultCapacity];
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    //...

    private World? _world;
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
    }


    private WorldEntities WorldEntities => _world!.Entities;

    internal void AttachWorld(World world) => _world = world;

    private DrawEntityContext MakeContext() => new(_idx, _visibleIdx, _entities, _byEntityId, _visibleIndices);

    public void Reset()
    {
        _prevIdx = _idx;
        _idx = 0;
        _visibleIdx = 0;
    }

    public void Execute()
    {
        const int extraEntities = 64;
        const int extraAnimations = 8;
        if (_world is null) return;

        var entityCount = WorldEntities.EntityCount;
        var ensureLen = entityCount + extraEntities;
        var animationLen = WorldEntities.Animations.Count + extraAnimations;
        EnsureDrawEntityData(ensureLen);
        DrawDataProvider.EnsureBuffer(ensureLen, animationLen);

        Validate();

        // start
        RenderProfiler.Begin(0);
        CollectEntities();
        RenderProfiler.End();

        RenderProfiler.Begin(1);
        TagCollectedEntities();
        RenderProfiler.End();

        SubmitWorldObjects();

        
        RenderProfiler.Begin(2);
        DrawParticleProcessor.Execute(MakeContext(), _world.Particles);
        RenderProfiler.End();

        RenderProfiler.Begin(3);
        DrawAnimatorProcessor.Execute();
        RenderProfiler.End();

        RenderProfiler.Begin(4);
        DrawCommandUploader.UploadDrawCommands(MakeContext());
        RenderProfiler.End();

        RenderProfiler.Begin(5);
        DrawTransformUploader.UploadTransform(MakeContext());
        if (RenderProfiler.End())
            RenderProfiler.PrintTotal();

        // end
    }

    private void Validate()
    {
        if (_visibleIndices.Length == 0 || _entities.Length == 0)
            throw new InvalidOperationException();

        var view = WorldEntities.Core.GetCoreView();

        if (_entities.Length != _visibleIndices.Length || _entities.Length != _byEntityId.Length)
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
        var idx = 0;
        foreach (var query in DrawDataProvider.WorldEntities.CoreQuery())
        {
            var entityId = query.Entity;
            ref var drawEntity = ref _entities[query.Index];
            DrawEntityCollector.CollectEntity(ref drawEntity, entityId, in query.Source);
            _byEntityId[entityId] = query.Index;
            idx++;
        }

        _idx = idx;
    }

    private void TagCollectedEntities()
    {
        if(_idx == 0) return;
        if((uint)_idx > _entities.Length || _entities.Length != _byEntityId.Length)
            throw  new IndexOutOfRangeException();

        var ctx = MakeContext();
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
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _visibleIndices.Length);

        if (_entities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_entities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        Array.Resize(ref _entities, newCap);
        Array.Resize(ref _visibleIndices, newCap);
        Array.Resize(ref _byEntityId, newCap);
        Logger.LogString(LogScope.World, $"Entity buffer resize {newCap}", LogLevel.Warn);
    }
}