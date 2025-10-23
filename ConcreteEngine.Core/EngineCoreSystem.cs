#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Platform;
using ConcreteEngine.Core.RenderingSystem;

#endregion

namespace ConcreteEngine.Core;

public interface IGameEngineSystem
{
    void Shutdown(); // Not used
}

public interface IEngineSystemManager
{
    public T GetSystem<T>() where T : IGameEngineSystem;
}

public class EngineCoreSystem : IEngineSystemManager
{
    private readonly DictionaryTypeRegistry<IGameEngineSystem, IGameEngineSystem> _systems = new(4);

    private readonly EngineRenderSystem _renderer;
    private readonly InputSystem _inputSystem;
    private readonly AssetSystem _assets;

    internal EngineCoreSystem(EngineRenderSystem renderer, InputSystem inputSystem, AssetSystem assets)
    {
        _renderer = renderer;
        _inputSystem = inputSystem;
        _assets = assets;
    }

    internal void Initialize()
    {
        _systems.Register<IInputSystem>(_inputSystem);
        _systems.Register<IAssetSystem>(_assets);
        _systems.Register<IRenderingSystem>(_renderer);
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