namespace ConcreteEngine.Engine.ECS.GameComponent;

public interface IGameComponent<T> where T : unmanaged, IGameComponent<T>
{
    
}