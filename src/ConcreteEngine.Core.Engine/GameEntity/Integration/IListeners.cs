using ConcreteEngine.Core.Engine.GameEntity.GameComponent;

namespace ConcreteEngine.Core.Engine.GameEntity.Integration;


public interface IGameEntityListener
{
    void EntityAdded(GameEntityId entity, EcsStore store);
    void EntityRemoved(GameEntityId entity, EcsStore store);
}

public interface IGameComponentListener<T> where T : unmanaged, IGameComponent<T>
{
    void ComponentAdded(GameEntityId entity, ref T component);
    void ComponentRemoved(GameEntityId entity, ref T component);
}

