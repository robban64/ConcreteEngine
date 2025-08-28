using ConcreteEngine.Core.Scene.Modules;
using ConcreteEngine.Graphics.Data;

namespace ConcreteEngine.Core.Scene;

public interface IModuleManager
{
    T Get<T>() where T : GameModule;
}

public sealed class ModuleManager : IModuleManager
{
    private readonly SortedList<int, GameModule> _modules = new(8);
    
    public void AddModule<T>(int order, T module) where T : GameModule
    {
        _modules.Add(order, module);
    }

    public T Get<T>() where T : GameModule
    {
        foreach (var module in _modules.Values)
        {
            if (module is T tModule) return tModule;
        }

        throw new InvalidOperationException($"Module {typeof(T).Name} is not registered.");
    }

    internal void GameTickUpdate(int tick)
    {
        if (_modules.Count == 0) return;

        foreach (var module in _modules.Values)
        {
            module.UpdateTick(tick);
        }
    }
    
    internal void Update(in FrameMetaInfo frameInfo)
    {
        if (_modules.Count == 0) return;

        foreach (var module in _modules.Values)
        {
            module.Update(in frameInfo);
        }
    }

    internal void Load(GameModuleContext context)
    {
        foreach (var (order, module) in _modules)
        {
            module.AttachContext(context, order);
        }
        
        foreach (var module in _modules.Values)
        {
            module.Initialize();
        }

    }

    internal void Unload()
    {
        foreach (var module in _modules.Values)
        {
            module.Unload();
        }
    }
}