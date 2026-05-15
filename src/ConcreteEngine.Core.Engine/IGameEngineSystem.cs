namespace ConcreteEngine.Core.Engine;

public interface IGameEngineSystem
{
    void Shutdown();
}

public interface IEngineSystemManager
{
    T GetSystem<T>() where T : class, IGameEngineSystem;
}
