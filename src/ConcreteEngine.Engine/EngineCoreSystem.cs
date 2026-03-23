using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;

namespace ConcreteEngine.Engine;

public abstract class GameEngineSystem
{
    internal virtual void Shutdown() { }
}

public interface IEngineSystemManager
{
    T GetSystem<T>() where T : GameEngineSystem;
}

public sealed class EngineCoreSystem : IEngineSystemManager
{
    private readonly Dictionary<Type, GameEngineSystem> _systems = new(4);

    internal EngineCoreSystem(InputSystem inputSystem, AssetSystem assets, SceneSystem sceneSystem)
    {
        Register(inputSystem);
        Register(assets);
        Register(sceneSystem);
    }


    private void Register<T>(T system) where T : GameEngineSystem
    {
        if (!_systems.TryAdd(typeof(T), system))
            throw new InvalidOperationException($"System of type {typeof(T)} is already registered");
    }

    public T GetSystem<T>() where T : GameEngineSystem
    {
        if (!_systems.TryGetValue(typeof(T), out var system) || system is not T t)
            throw new InvalidOperationException($"System  of type {typeof(T)} is not registered or wrong type");

        return t;
    }
}