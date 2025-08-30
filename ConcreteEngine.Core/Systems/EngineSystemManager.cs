using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering;

namespace ConcreteEngine.Core.Systems;

public interface IGameEngineSystem
{
    void Shutdown();
}

public interface IEngineSystemManager
{
    public T GetSystem<T>() where T : IGameEngineSystem;
}

public class EngineSystemManagerManager : IEngineSystemManager
{
    private readonly TypeRegistryCollection<IGameEngineSystem> _systems = new(4);

    private readonly RenderSystem _renderer;
    private readonly InputSystem _inputSystem;
    private readonly AssetSystem _assets;
    private readonly CameraSystem _camera;

    internal EngineSystemManagerManager(RenderSystem renderer, InputSystem inputSystem, AssetSystem assets,
        CameraSystem camera)
    {
        _renderer = renderer;
        _inputSystem = inputSystem;
        _assets = assets;
        _camera = camera;
    }


    public T GetSystem<T>() where T : IGameEngineSystem
    {
        var system = _systems.Get<T>();

        if (typeof(T).IsClass)
            throw new ArgumentException($"GetSystem only allow interfaces for T");

        if (system == null)
            throw new NullReferenceException($"System of type {typeof(T)} not found");

        if (system is T engineSystem)
            return engineSystem;

        throw new InvalidOperationException($"System of type {typeof(T)} not found");
    }

    internal void RegisterSystems()
    {
        _systems.Register<IInputSystem>(_inputSystem);
        _systems.Register<ICameraSystem>(_camera);
        _systems.Register<IAssetSystem>(_assets);
        _systems.Register<IRenderSystem>(_renderer);
    }
}