using ConcreteEngine.Core.Engine.ECS.GameComponent;
using ConcreteEngine.Core.Engine.ECS.RenderComponent;

namespace ConcreteEngine.Core.Engine.ECS.Abstract;

public interface IEntityListener
{
    void EntityAdded(int entity, EcsStore store);
    void EntityRemoved(int entity, EcsStore store);
}

public interface IGameComponentListener<T> where T : unmanaged, IGameComponent<T>
{
    void ComponentAdded(int entity, ref T component);
    void ComponentRemoved(int entity, ref T component);
}

public interface IRenderComponentListener<T> where T : unmanaged, IRenderComponent<T>
{
    void ComponentAdded(int entity, ref T component);
    void ComponentRemoved(int entity, ref T component);
}