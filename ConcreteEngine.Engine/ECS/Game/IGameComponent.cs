namespace ConcreteEngine.Engine.ECS.Game;

public interface IGameComponent<T> where T : unmanaged, IGameComponent<T>
{
    
}