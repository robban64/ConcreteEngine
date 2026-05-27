using ConcreteEngine.Core.Engine;
using ConcreteEngine.Core.Engine.Input;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Render;

namespace ConcreteEngine.Engine;

public sealed class EngineCoreSystem : IEngineSystemManager
{
    private readonly Dictionary<Type, IGameEngineSystem> _systems = new(4);

    internal readonly InputSystem Input;
    internal readonly AssetSystem Assets;
    internal readonly SceneSystem Scene;
    internal readonly EngineRenderSystem Render;

    internal EngineCoreSystem(InputSystem inputSystem, AssetSystem assets, SceneSystem sceneSystem,
        EngineRenderSystem renderSystem)
    {
        Input = inputSystem;
        Scene = sceneSystem;
        Render = renderSystem;
        Assets = assets;

        Register(inputSystem);
        Register(assets);
        Register(sceneSystem);
        Register(renderSystem);
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