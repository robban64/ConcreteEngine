using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Scene;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine;

public interface IGameEngineSystem
{
    void Shutdown();
}

public interface IEngineSystemManager
{
    T GetSystem<T>() where T : class, IGameEngineSystem;
}

public sealed class EngineCoreSystem : IEngineSystemManager
{
    private readonly Dictionary<Type, IGameEngineSystem> _systems = new(4);

    internal EngineCoreSystem(InputSystem inputSystem, AssetSystem assets, World world, SceneManager sceneManager)
    {
        Register(inputSystem);
        Register(assets);
        Register(world);
        Register(sceneManager);
    }


    private void Register<T>(T system) where T : class, IGameEngineSystem
    {
        if (!_systems.TryAdd(typeof(T), system))
            throw new InvalidOperationException($"System of type {typeof(T)} is already registered");
    }

    public T GetSystem<T>() where T : class, IGameEngineSystem
    {
        if (!_systems.TryGetValue(typeof(T), out var system) || system is not T t)
            throw new InvalidOperationException($"System  of type {typeof(T)} is not registered or wrong type");

        return t;
    }
}