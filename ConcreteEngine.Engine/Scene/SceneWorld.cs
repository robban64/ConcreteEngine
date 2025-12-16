using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Engine.Worlds.Entities;

namespace ConcreteEngine.Engine.Scene;

public abstract class GameEntity
{
}

public sealed class SceneWorld
{
    private readonly World _world;
    private readonly WorldEntities _worldEntities;

    private GameEntity[] _entities = new GameEntity[WorldEntities.DefaultEntityCapacity];
    Dictionary<Guid, EntityHandle> _handles = new (WorldEntities.DefaultEntityCapacity);
    
    internal SceneWorld(World world)
    {
        _world = world;
        _worldEntities = world.Entities;
    }
    
    private EntityCoreStore CoreEntities => _worldEntities.Core;

    public void AddEntity(EntityId entityId){}

    private void ValidateEntity(EntityId entity)
    {
        if(!entity.IsValid || entity > CoreEntities.Count) throw new ArgumentOutOfRangeException(nameof(entity));
        
        var actualEntity = CoreEntities.GetEntity(entity);
        if(actualEntity != entity)
            throw new InvalidOperationException($"Entity: {entity} does not match actual: {actualEntity}");
    }

}