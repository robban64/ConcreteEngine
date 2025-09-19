namespace ConcreteEngine.Core;

public interface IModuleManager
{
    T Get<T>() where T : GameModule;
}

public sealed class ModuleManager : IModuleManager
{
    private readonly List<GameModule> _modules = new(8);

    public void AddModule<T>(T module) where T : GameModule
    {
        module.Order = _modules.Count;
        _modules.Add(module);
    }

    public T Get<T>() where T : GameModule
    {
        foreach (var module in _modules)
        {
            if (module is T tModule) return tModule;
        }

        throw new InvalidOperationException($"Module {typeof(T).Name} is not registered.");
    }

    internal void GameTickUpdate(int tick)
    {
        if (_modules.Count == 0) return;

        foreach (var module in _modules)
        {
            module.UpdateTick(tick);
        }
    }

    internal void Update(in UpdateInfo frameCtx)
    {
        if (_modules.Count == 0) return;

        foreach (var module in _modules)
        {
            module.Update(in frameCtx);
        }
    }

    internal void Load(GameModuleContext context)
    {
        foreach (var module in _modules)
        {
            module.AttachContext(context);
        }

        foreach (var module in _modules)
        {
            module.Initialize();
        }
    }

    internal void OnSceneReady()
    {
        foreach (var module in _modules)
        {
            module.OnSceneReady();
        }
    }


    internal void Unload()
    {
        foreach (var module in _modules)
        {
            module.Unload();
        }
        _modules.Clear();
    }
}