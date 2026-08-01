namespace ConcreteEngine.Core.Engine.GameEntity.GameComponent;

public interface IGameComponent<T> where T : unmanaged, IGameComponent<T> { }