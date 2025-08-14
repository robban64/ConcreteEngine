
namespace ConcreteEngine.Core;

public interface IFeatureRegistry
{
    public FeatureRegistry RegisterFeature<T>() where T : IGameFeature, new();
    public T Get<T>() where T : IGameFeature;
}

public sealed class FeatureRegistry: IFeatureRegistry
{
    private readonly List<(Func<IGameFeature> factory, int)> _factoryRegistry = new(8);
    private readonly SortedList<int, IGameFeature> _features = new(8);
    
    public bool IsReady { get; private set; } = false;
    
    public FeatureRegistry RegisterFeature<T>()where T : IGameFeature, new()
    {
        _factoryRegistry.Add((() => new T(), _factoryRegistry.Count));
        return this;
    }
    
    public T Get<T>() where T : IGameFeature
    {
        foreach (var feature in _features.Values)
        {
            if(feature is T tFeature) return tFeature;
        }
        throw new InvalidOperationException($"Feature {typeof(T).Name} is not registered.");
    }
    
    internal void GameTickUpdate(int tick)
    {
        if(_features.Values.Count == 0) return;
        
        foreach (var service in _features.Values)
        {
            if (service.IsUpdateable)
                service.UpdateTick(tick);
        }
    }

    internal void Load(GameFeatureContext context)
    {
        foreach (var (factory, order) in _factoryRegistry)
        {
            if (_features.ContainsKey(order))
                throw new InvalidOperationException($"Duplicate feature registered for order: {order}");

            var feature = factory();
            feature.Order = order;
            _features.Add(order, feature);
        }

        foreach (var feature in _features.Values)
            feature.Load(context);

        _factoryRegistry.Clear();
        IsReady = true;
    }

    internal void Unload()
    {
        foreach (var service in _features.Values)
        {
            service.Unload();
        }
    }

}