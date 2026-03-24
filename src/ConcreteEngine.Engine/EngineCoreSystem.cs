using System.Runtime.CompilerServices;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Render;
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

    internal readonly AssetSystem AssetSystem;

    internal EngineCoreSystem(InputSystem inputSystem, AssetSystem assets, SceneSystem sceneSystem,
        EngineRenderSystem renderSystem)
    {
        Register(inputSystem);
        Register(assets);
        Register(sceneSystem);
        Register(renderSystem);
        AssetSystem = assets;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
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