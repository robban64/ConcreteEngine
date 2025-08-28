using System.Runtime.InteropServices;
using ConcreteEngine.Common.Collections;

namespace ConcreteEngine.Core.Scene.Entities;

public interface IEntityManager
{
    int Count { get; }
    void Register<TEntity>() where TEntity : class, IGameEntity;
    bool TryGet<TEntity>(GameEntityInstanceId id, out TEntity entity) where TEntity : class, IGameEntity;
    bool Contains(IGameEntity entity);
    TEntity Create<TEntity>() where TEntity : class, IGameEntity, new();
    void Destroy<TEntity>(TEntity entity) where TEntity : class, IGameEntity;
}

public class GameEntityManager : IEntityManager
{
    private int _idIdx = 1;
    private int _instanceIdx = 1;
    

    private readonly TypeRegistryCollection<GameEntityId> _registry = new (4);
    
    // TODO break out into more efficient solution
    private readonly List<IGameEntity> _entities = new(32);

    private readonly Queue<IGameEntity> _pendingQueue = new(8);
    private readonly Queue<IGameEntity> _removeQueue = new(8);

    public int Count => _entities.Count;


    public void FlushQueue()
    {
        while (_removeQueue.Count > 0)
        {
            var  entity = _removeQueue.Dequeue();
            var idx = BinarySearchEntity(_entities, entity.InstanceId);
            _entities.RemoveAt(idx);
        }

        
        while (_pendingQueue.Count > 0)
        {
            var entity = _pendingQueue.Dequeue();
            _entities.Add(entity);
            entity.Status = GameEntityStatus.Active;
        }
    }
    
    public void Register<TEntity>() where TEntity : class, IGameEntity
    {
        _registry.Register<TEntity>(new GameEntityId(_idIdx++));
    }

    public TEntity Create<TEntity>() where TEntity : class, IGameEntity, new()
    {
        if(!_registry.TryGet<TEntity>(out var id))
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} is not registered");
       
        var entity = new TEntity
        {
            Id = id,
            InstanceId = new GameEntityInstanceId(_instanceIdx++),
            Status = GameEntityStatus.Pending
        };

        if (entity.InstanceId.Id != _instanceIdx)
            throw new InvalidOperationException(
                $"Create EntityInstance {entity.Id} does not match given Id {_instanceIdx}");

        _pendingQueue.Enqueue(entity);
        return entity;
    }


    public bool TryGet<TEntity>(GameEntityInstanceId id, out TEntity entity) where TEntity : class, IGameEntity
    {
        var idx = BinarySearchEntity(_entities, id);
        if (idx < 0)
        {
            entity = null!;
            return false;
        }

        var found = _entities[idx];
        if (found is not TEntity tEntity)
            throw new InvalidOperationException(
                $"Found Entity {found.GetType().Name} does not match request type {typeof(TEntity).Name}");
        
        entity = tEntity;
        return true;
    }

    public bool Contains(IGameEntity entity)
    {
        return BinarySearchEntity(_entities, entity.InstanceId) >= 0;
    }

    public void Destroy<TEntity>(TEntity entity) where TEntity : class, IGameEntity
    {
        _pendingQueue.Enqueue(entity);
    }


    private static int BinarySearchEntity<T>(List<T> collection, GameEntityInstanceId instanceId) where T : class, IGameEntity
    {
        var id = instanceId.Id;

        int lo = 0, hi = collection.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) / 2);
            int midKey = collection[mid].Id.Id;
            if (midKey == id) return mid;
            if (midKey < id) lo = mid + 1;
            else hi = mid - 1;
        }

        return -1;
    }
}