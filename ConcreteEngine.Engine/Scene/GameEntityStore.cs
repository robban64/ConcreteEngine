using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Editor.Diagnostics;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Shared.Diagnostics;

namespace ConcreteEngine.Engine.Scene;

public sealed class GameEntityStore
{
    private const int DefaultEntityDictCapacity = 128;
    private static int _entityCount = 0;

    private static WorldEntityHandle MakeId(EntityId entity, ushort gen = 0) =>
        new(new WorldEntityId(++_entityCount), entity, gen);

    private GameEntity[] _entities = new GameEntity[WorldEntities.DefaultEntityCapacity];
    private WorldEntityHandle[] _handles = new WorldEntityHandle[WorldEntities.DefaultEntityCapacity];

    private readonly Dictionary<GameEntityId, Guid> _toGuid = new(DefaultEntityDictCapacity);
    private readonly Dictionary<string, WorldEntityHandle> _byName = new(DefaultEntityDictCapacity);

    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;
    private readonly World _world;

    internal GameEntityStore(World world, AssetStore assetStore, MaterialStore materialStore)
    {
        _world = world;
        _assetStore = assetStore;
        _materialStore = materialStore;
    }

    private WorldEntities Ecs => _world.Entities;
    private EntityCoreStore CoreEcs => _world.Entities.Core;


    public void AddModelEntity(string name, Model model, MaterialTag materialTag, in Transform transform)
    {
        EnsureCapacity(1);

        var kind = model.IsAnimated ? EntitySourceKind.AnimatedModel : EntitySourceKind.Model;
        var tagKey = _world.MaterialTableImpl.Add(materialTag);

        var source = new SourceComponent(model.ModelId, tagKey, kind);
        var box = new BoxComponent(model.Bounds);
        var data = new CoreComponentBundle(in source, in transform, in box);

        var entityId = Ecs.AddEntity(in data);
        AddEntityInternal(entityId, name);

        if (kind != EntitySourceKind.AnimatedModel) return;

        var modelAnimation = model.Animation!;
        var clip = modelAnimation.ClipDataSpan[0];
        var animation = new AnimationComponent(model.AnimationId, clip.TicksPerSecond, clip.Duration);
        Ecs.AddComponent(entityId, animation);
    }

    public void AddParticleEntity(string name, ParticleEmitter emitter, in Transform transform)
    {
        EnsureCapacity(1);

        var source = new SourceComponent(emitter.Model, emitter.MaterialKey, EntitySourceKind.Particle);
        var data = new CoreComponentBundle(in source, in transform, ParticleComponent.DefaultParticleBounds);
        var component = new ParticleComponent(emitter.EmitterHandle, emitter.Material);

        var entityId = Ecs.AddEntity(in data);
        Ecs.AddComponent(entityId, component);

        AddEntityInternal(entityId, name);
    }


    private void AddEntityInternal(EntityId entity, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Value, nameof(entity));

        var index = _entityCount;
        var id = MakeId(entity);
        ValidateEntity(id);

        if (!string.IsNullOrEmpty(name))
        {
            if (!_byName.TryAdd(name, id))
                throw new InvalidOperationException($"Entity with name {name} already exists");
        }

        _toGuid.Add(id, Guid.NewGuid());

        _handles[index] = id;
    }

    private void ValidateEntity(WorldEntityHandle entityHandle)
    {
        if (!entityHandle.IsValid || entityHandle.Entity > CoreEcs.Count)
            throw new ArgumentOutOfRangeException(nameof(entityHandle));

        var actualEntity = CoreEcs.GetEntity(entityHandle);
        if (actualEntity != entityHandle)
            throw new InvalidOperationException($"Entity: {entityHandle} does not match actual: {actualEntity}");
    }

    private void EnsureCapacity(int amount)
    {
        var len = _entityCount + amount;
        if (len >= _handles.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_handles.Length, len);
            Array.Resize(ref _handles, newSize);

            Logger.LogString(LogScope.World, $"SceneEntities: resized {newSize}", LogLevel.Warn);
        }

        if (len >= _entities.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
            Array.Resize(ref _entities, newSize);

            Logger.LogString(LogScope.World, $"GameEntities: resized {newSize}", LogLevel.Warn);
        }
    }

    private readonly record struct WorldEntityHandle(WorldEntityId Id, EntityId Entity, ushort Gen)
    {
        public bool IsValid => Id > 0 && Entity > 0;
        public static implicit operator EntityId(WorldEntityHandle handle) => handle.Entity;
        public static implicit operator GameEntityId(WorldEntityHandle handle) => new(handle.Entity, handle.Gen);

/*
        public static implicit operator int(WorldEntityHandle handle) => handle.Id;
        public static implicit operator EntityId(WorldEntityHandle handle) => handle.Entity;
        public static implicit operator GameEntityId(WorldEntityHandle handle) => new(handle.Entity, handle.Gen);
        */
    }
}