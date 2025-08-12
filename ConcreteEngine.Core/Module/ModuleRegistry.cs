namespace ConcreteEngine.Core.Module;

public interface IModuleRegistry
{
    public ModuleRegistry RegisterModule<T>() where T : GameModule, new();
    public T Get<T>() where T : GameModule;
}

public sealed class ModuleRegistry: IModuleRegistry
{
    private readonly List<(Func<GameEngineContext, GameModule> factory, int)> _registeredModules = new(8);
    private readonly SortedList<int, GameModule> _modules = new(8);
    
    public bool IsReady { get; private set; } = false;
    
    public ModuleRegistry RegisterModule<T>()where T : GameModule, new()
    {
        _registeredModules.Add((context => new T(), _registeredModules.Count));
        return this;
    }
    
    public T Get<T>() where T : GameModule
    {
        foreach (var module in _modules.Values)
        {
            if(module is T tModule) return tModule;
        }
        throw new InvalidOperationException($"Module {typeof(T).Name} is not registered.");
    }
    
    internal void Update(float dt)
    {
        foreach (var service in _modules.Values)
        {
            if (service.IsUpdateable)
                service.Update(dt);
        }
    }

    internal void Render(float dt)
    {
        foreach (var service in _modules.Values)
        {
            if (service.IsRenderable)
                service.Render(dt);
        }
    }

    internal void Load(GameEngineContext context)
    {
        foreach (var (factory, order) in _registeredModules)
        {
            if (_modules.ContainsKey(order))
                throw new InvalidOperationException($"Duplicate module registered for order: {order}");

            //var service = (GameModule?)Activator.CreateInstance(type, context, order);
            var module = factory(context);
            _modules.Add(order, module);
        }

        foreach (var (order,module) in _modules)
            module.AttatchContext(context, order);
        
        foreach (var module in _modules.Values)
            module.Load();


        IsReady = true;
    }

    internal void Unload()
    {
        foreach (var service in _modules.Values)
        {
            service.Unload();
        }
    }

}