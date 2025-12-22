namespace ConcreteEngine.Engine.Scene.Modules;

public interface IModuleManager
{
    T Get<T>() where T : GameModule;
}

public sealed class ModuleManager : IModuleManager
{
    private GameModuleContext _context = null!;
    private readonly List<GameModule> _pending = [];
    private readonly List<GameModule> _modules = [];

    public void Add<T>(T module) where T : GameModule
    {
        if (_pending.Contains(module) || _modules.Contains(module))
            throw new InvalidOperationException($"Module instance is already registered.");

        module.Id = _modules.Count;
        _pending.Add(module);
    }

    public T Get<T>() where T : GameModule
    {
        foreach (var module in _modules)
        {
            if (module is T tModule) return tModule;
        }

        throw new InvalidOperationException($"Module {typeof(T).Name} is not registered.");
    }

    internal void UpdateTick(float deltaTime)
    {
        if (_pending.Count > 0)
        {
            _modules.AddRange(_pending);
            foreach (var module in _pending) module.AttachContext(_context);
            foreach (var module in _pending) module.OnStart();
            _pending.Clear();
        }

        if (_modules.Count == 0) return;
        foreach (var module in _modules) module.UpdateTick(deltaTime);
    }

    internal void Load(GameModuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    internal void Unload()
    {
        foreach (var module in _modules) module.OnDestroy();
        _modules.Clear();
    }
}