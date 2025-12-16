using System.Numerics;
using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Scene.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Objects;
using ConcreteEngine.Engine.Worlds.Tables;
using ConcreteEngine.Engine.Worlds.Utility;
using ConcreteEngine.Renderer.Data;

namespace ConcreteEngine.Engine.Scene;

public abstract class GameEntity
{
    protected virtual void OnCreate() { }
    public virtual void OnDestroy() { }
}

public sealed class SceneWorld
{
    private static int _entityCount = 0;
    private static SceneEntityId MakeId(EntityId entity, ushort gen = 0) => new(++_entityCount, entity, gen);

    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;

    private readonly World _world;

    private SceneEntityId[] _entities = new SceneEntityId[WorldEntities.DefaultEntityCapacity];
    private GameEntity[] _gameEntities = new GameEntity[WorldEntities.DefaultEntityCapacity];

    private readonly Dictionary<SceneEntityId, Guid> _fromGuid = new(WorldEntities.DefaultEntityCapacity);
    private readonly Dictionary<string, SceneEntityId> _byName = new(WorldEntities.DefaultEntityCapacity);

    internal SceneWorld(AssetSystem assetSystem, World world)
    {
        _assetStore = assetSystem.StoreImpl;
        _materialStore = assetSystem.MaterialStoreImpl;
        _world = world;
    }

    private WorldEntities Ecs => _world.Entities;
    private EntityCoreStore CoreEcs => _world.Entities.Core;

    private int i = 0;

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
        if (name.StartsWith("Tree"))
        {
            Ecs.AddComponent(entityId, new DebugBoundsComponent{ ByPart = true});
        }

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

        _fromGuid.Add(id, Guid.NewGuid());

        _entities[index] = id;
    }

    private void ValidateEntity(SceneEntityId sceneEntity)
    {
        if (!sceneEntity.IsValid || sceneEntity > CoreEcs.Count)
            throw new ArgumentOutOfRangeException(nameof(sceneEntity));

        var actualEntity = CoreEcs.GetEntity(sceneEntity);
        if (actualEntity != sceneEntity)
            throw new InvalidOperationException($"Entity: {sceneEntity} does not match actual: {actualEntity}");
    }

    private void EnsureCapacity(int amount)
    {
        var len = _entityCount + amount;
        if (len >= _entities.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_entities.Length, len);
            Array.Resize(ref _entities, newSize);
            Console.WriteLine("Scene Entities resize");
        }

        if (len >= _gameEntities.Length)
        {
            var newSize = Arrays.CapacityGrowthSafe(_gameEntities.Length, len);
            Array.Resize(ref _gameEntities, newSize);
            Console.WriteLine("GameEntities resize");
        }
    }
}