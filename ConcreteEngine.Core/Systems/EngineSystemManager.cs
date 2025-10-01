#region

using ConcreteEngine.Common.Collections;
using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Rendering;

#endregion

namespace ConcreteEngine.Core.Systems;

public interface IGameEngineSystem
{
    void Shutdown();    // Not used
}

public interface IEngineSystemManager
{
    public T GetSystem<T>() where T : IGameEngineSystem;
}

//Old code.
//precisely renamed from EngineSystemManager to EngineCoreSystem, Or Kernel (but use core in this code base).
public class EngineCoreSystem : IEngineSystemManager
{
    private readonly DictionaryTypeRegistry<IGameEngineSystem, IGameEngineSystem> _systems = new(4);

    private readonly RenderSystem _renderer;
    private readonly InputSystem _inputSystem;
    private readonly AssetSystem _assets;

    internal EngineCoreSystem(RenderSystem renderer, InputSystem inputSystem, AssetSystem assets)
    {
        _renderer = renderer;
        _inputSystem = inputSystem;
        _assets = assets;
    }

    // Old code.... don't judge
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

    internal void Initialize()
    {
        _systems.Register<IInputSystem>(_inputSystem);
        _systems.Register<IAssetSystem>(_assets);
        _systems.Register<IRenderSystem>(_renderer);
    }
}