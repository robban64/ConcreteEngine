using ConcreteEngine.Common.Collections;
using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Platform;
using ConcreteEngine.Engine.Worlds.Render;

namespace ConcreteEngine.Engine;

public interface IGameEngineSystem
{
    void Shutdown(); // Not used
}

public interface IEngineSystemManager
{
    T GetSystem<T>() where T : IGameEngineSystem;
}

public class EngineCoreSystem : IEngineSystemManager
{
    private readonly DictionaryTypeRegistry<IGameEngineSystem, IGameEngineSystem> _systems = new(4);

    private readonly WorldRenderer _renderer;
    private readonly InputSystem _inputSystem;
    private readonly AssetSystem _assets;

    internal EngineCoreSystem(WorldRenderer renderer, InputSystem inputSystem, AssetSystem assets)
    {
        _renderer = renderer;
        _inputSystem = inputSystem;
        _assets = assets;
    }

    internal void Initialize()
    {
        _systems.Register<IInputSystem>(_inputSystem);
        _systems.Register<IAssetSystem>(_assets);
        _systems.Freeze();
    }

    public T GetSystem<T>() where T : IGameEngineSystem
    {
        var system = _systems.GetRequired<T>();

        if (typeof(T).IsClass)
            throw new ArgumentException($"GetSystem only allow interfaces for T");

        if (system == null)
            throw new NullReferenceException($"System of type {typeof(T)} not found");

        if (system is T engineSystem)
            return engineSystem;

        throw new InvalidOperationException($"System of type {typeof(T)} not found");
    }
}