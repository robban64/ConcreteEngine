#region

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ConcreteEngine.Common;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Common.Time;
using ConcreteEngine.Engine.Editor.Diagnostics;
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
    private static int _idx = 0;
    private static int _prevIdx = 0;

    public const int DefaultCapacity = 512;
    public const int MaxCapacity = 1024 * 50;

    //...
    private int[] _byEntityId = new int[DefaultCapacity];
    private DrawEntity[] _entities = new DrawEntity[DefaultCapacity];
    private DrawEntityData[] _entityData = new DrawEntityData[DefaultCapacity];
    //...

    private World? _world;

    public ModelId CubeId { get; set; }
    public MaterialTagKey EmptyMaterialKey { get; set; }

    internal RenderEntityBus()
    {
    }

    public void Reset()
    {
        _prevIdx = _idx;
        _idx = 0;
    }

    private WorldEntities WorldEntities => _world!.Entities;

    private int ActiveSkyCount => _world?.Sky.IsActive ?? false ? 1 : 0;
    private int ActiveTerrainCount => _world?.Terrain.IsActive ?? false ? 1 : 0;
    public int DrawCount => (_world?.EntityCount ?? 0) + ActiveSkyCount + ActiveTerrainCount;

    internal void AttachWorld(World world) => _world = world;

    private DrawEntityContext MakeContext() => new(_idx, _entities, _entityData, _byEntityId);


    public void Execute()
    {
        if (_world is null) return;

        EnsureDrawEntityData(DrawCount);
        DrawDataProvider.EnsureBuffer(_world.EntityCount + 64, _world.Entities.Animations.Count);

        Validate();

        // start
        StaticProfileTimer.RenderTimer.Begin();
        CollectEntities();
        StaticProfileTimer.RenderTimer.EndPrint();

        TagCollectedEntities(MakeContext());

        SubmitWorldObjects();

        DrawParticleProcessor.Execute(MakeContext(), _world.Particles);
        DrawAnimatorProcessor.Execute();

        UploadDrawCommands(MakeContext());
        UploadTransform(MakeContext());
        // end
    }

    private void Validate()
    {
        if (_entityData.Length == 0 || _entities.Length == 0)
            throw new InvalidOperationException();

        var view = WorldEntities.Core.GetCoreView();

        if (_entities.Length != _entityData.Length || _entities.Length != _byEntityId.Length)
            throw new InvalidOperationException();

        if (view.EntityId.Length != view.Transforms.Length || view.EntityId.Length != view.Sources.Length)
            throw new InvalidOperationException();

        var len = view.EntityId.Length;
        if ((uint)len > _entities.Length || (uint)len > view.Transforms.Length)
            throw new IndexOutOfRangeException();
    }


    private void CollectEntities()
    {
        //var ctx = new DrawEntityContext(_entities.Length, _entities, _entityData, _byEntityId);
        var idx = 0;
        foreach (var query in DrawDataProvider.WorldEntities.CoreQuery())
        {
            ref var entityData = ref _entityData[query.Index];
            entityData.Transform = query.Transform;
            entityData.Bounds = query.Box;
            _byEntityId[query.Entity] = idx;
            idx++;
        }

        _idx = idx;

        foreach (var query in DrawDataProvider.WorldEntities.CoreQuery())
        {
            DrawEntityCollector.CollectEntity(ref _entities[query.Index], query.Entity, in query.Source);
        }
    }

    private void SubmitWorldObjects()
    {
        DrawWorldProcessor.SubmitDrawTerrain(_world!.Terrain);
        DrawWorldProcessor.SubmitDrawSkybox(_world!.Sky);
    }

    private static void TagCollectedEntities(DrawEntityContext ctx)
    {
        DrawTagResolver.TagEffectResolvers(ctx);
        DrawSpatialProcessor.TagDepthKeys(ctx);
    }

    private static void UploadTransform(DrawEntityContext ctx)
    {
        var entitiesData = ctx.EntityDataSpan;
        var entities = ctx.EntitySpan;

        var len = _idx;

        if ((uint)len > entities.Length || (uint)len > entitiesData.Length)
            throw new IndexOutOfRangeException();

        for (var i = 0; i < len; i++)
        {
            ref readonly var entity = ref entities[i];
            if (entity.Meta.CommandId == DrawCommandId.Particle) continue;
            DrawEntityUploader.ExecuteSubmitTransform(i, in entity, in entitiesData[i]);
        }
    }


    private static void UploadDrawCommands(DrawEntityContext ctx)
    {
        var entities = ctx.EntitySpan;

        var len = _idx;

        if ((uint)len > entities.Length)
            throw new IndexOutOfRangeException();

        MaterialTag materialTag = default;
        var prevMatKey = new MaterialTagKey(-1);

        for (var i = 0; i < len; i++)
        {
            ref readonly var entity = ref entities[i];
            if (entity.Meta.CommandId != DrawCommandId.Model)
            {
                DrawEntityUploader.ExecuteGeneratedCommand(i, in entity);
                continue;
            }

            var matKey = entity.Source.MaterialKey;
            if (matKey != prevMatKey)
            {
                DrawDataProvider.ResolveMaterial(matKey, out materialTag);
                prevMatKey = matKey;
            }

            DrawEntityUploader.ExecuteSubmitCommand(i, in entity, in materialTag);
        }
    }


    public void EnsureDrawEntityData(int amount)
    {
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entities.Length);
        InvalidOpThrower.ThrowIf(_byEntityId.Length != _entityData.Length);

        if (_entities.Length >= amount) return;
        var newCap = Arrays.CapacityGrowthSafe(_entities.Length, amount);
        if (newCap > MaxCapacity)
            throw new OutOfMemoryException("Entity Buffer exceeded max limit");

        Array.Resize(ref _entities, newCap);
        Array.Resize(ref _entityData, newCap);
        Array.Resize(ref _byEntityId, newCap);
        Logger.LogString(LogScope.World, $"Entity buffer resize {newCap}", LogLevel.Warn);
    }
}