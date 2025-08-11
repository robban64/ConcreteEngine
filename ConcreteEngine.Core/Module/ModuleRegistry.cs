namespace ConcreteEngine.Core.Module;

public class ModuleRegistry
{
    private readonly List<(Type, int)> _registeredModules = new(8);
    private readonly SortedList<int, GameModule> _modules = new(8);

    public bool IsReady { get; private set; } = false;
    
    public ModuleRegistry RegisterModule<T>() where T : GameModule
    {
        _registeredModules.Add((typeof(T), _registeredModules.Count));
        return this;
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
        foreach (var (type, order) in _registeredModules)
        {
            if (_modules.ContainsKey(order))
                throw new InvalidOperationException($"Duplicate service registered for order: {order}");

            var service = (GameModule?)Activator.CreateInstance(type, context, order);
            if (service == null)
                throw new NullReferenceException($"Service {type.Name} returned null during creation.");
            
            _modules.Add(service.Order, service);
        }
        
        foreach (var service in _modules.Values)
        {
            service.Load();
        }

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