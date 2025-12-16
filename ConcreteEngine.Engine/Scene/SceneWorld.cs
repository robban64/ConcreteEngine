using System.Numerics;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Assets.Data;
using ConcreteEngine.Engine.Assets.Materials;
using ConcreteEngine.Engine.Assets.Models;
using ConcreteEngine.Engine.Scene.Data;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Data;
using ConcreteEngine.Engine.Worlds.Entities;
using ConcreteEngine.Engine.Worlds.Entities.Components;
using ConcreteEngine.Engine.Worlds.Tables;

namespace ConcreteEngine.Engine.Scene;

public abstract class GameEntity
{
    protected virtual void OnCreate() { }
    public virtual void OnDestroy() { }
}

public sealed class SceneWorld
{
    private static int _count = 0;
    private static WorldEntityId MakeId(EntityId entity, ushort gen = 0) => new(++_count, entity, gen);

    private readonly AssetStore _assetStore;
    private readonly MaterialStore _materialStore;

    private readonly WorldContext _worldCtx;

    private WorldEntityId[] _entities = new WorldEntityId[WorldEntities.DefaultEntityCapacity];
    private GameEntity[] _gameEntities = new GameEntity[WorldEntities.DefaultEntityCapacity];

    private readonly Dictionary<WorldEntityId, Guid> _fromGuid = new(WorldEntities.DefaultEntityCapacity);
    private readonly Dictionary<string, WorldEntityId> _byName = new(WorldEntities.DefaultEntityCapacity);

    internal SceneWorld(AssetSystem assetSystem, WorldContext worldCtx)
    {
        _assetStore = assetSystem.StoreImpl;
        _materialStore = assetSystem.MaterialStoreImpl;
        _worldCtx = worldCtx;
    }

    private WorldEntities Ecs => _worldCtx.Entities;
    private EntityCoreStore CoreEcs => _worldCtx.Entities.Core;


    internal void AddModelEntity(string name, Model model, MaterialTag materialTag, in Transform transform)
    {
        var kind = model.IsAnimated ? EntitySourceKind.AnimatedModel : EntitySourceKind.Model;
        var tagKey = _worldCtx.MaterialTable.Add(materialTag);

        var source = new SourceComponent(model.ModelId, model.DrawCount, tagKey, kind);
        var box = new BoxComponent(model.Bounds);
        var data = new CoreComponentBundle(in source, in transform, in box);

        var entityId = Ecs.AddEntity(in data);

        if (kind != EntitySourceKind.AnimatedModel) return;

        var modelAnimation = model.Animation!;
        var clip = modelAnimation.ClipDataSpan[0];
        var animation = new AnimationComponent(model.AnimationId, clip.TicksPerSecond, clip.Duration);
        Ecs.AddComponent(entityId, animation);
    }


    private void AddEntityInternal(EntityId entity, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(entity.Value, nameof(entity));

        var index = _count;
        var id = MakeId(entity);

        if (!string.IsNullOrEmpty(name))
        {
            if(!_byName.TryAdd(name, id)) throw new InvalidOperationException($"Entity with name {name} already exists");
        }
        
        _fromGuid.Add(id, Guid.NewGuid());
        
    }

    private void AddEntitiesInternal<T>(ReadOnlySpan<CoreComponentBundle> components)
        where T : unmanaged, IEntityComponent
    {
        Span<EntityId> entityIds = stackalloc EntityId[components.Length];
        _worldCtx.Entities.AddEntities(components, entityIds);
    }

    private void ValidateEntity(WorldEntityId worldEntity)
    {
        if (!worldEntity.IsValid || worldEntity > CoreEcs.Count)
            throw new ArgumentOutOfRangeException(nameof(worldEntity));

        var actualEntity = CoreEcs.GetEntity(worldEntity);
        if (actualEntity != worldEntity)
            throw new InvalidOperationException($"Entity: {worldEntity} does not match actual: {actualEntity}");
    }
}