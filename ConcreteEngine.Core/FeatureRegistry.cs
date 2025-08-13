
namespace ConcreteEngine.Core;

public interface IFeatureRegistry
{
    public FeatureRegistry RegisterFeature<T>() where T : GameFeature, new();
    public T Get<T>() where T : GameFeature;
}

public sealed class FeatureRegistry: IFeatureRegistry
{
    private readonly List<(Func<GameEngineContext, GameFeature> factory, int)> _handlerRegistry = new(8);
    private readonly SortedList<int, GameFeature> _features = new(8);
    
    public bool IsReady { get; private set; } = false;
    
    public FeatureRegistry RegisterFeature<T>()where T : GameFeature, new()
    {
        _handlerRegistry.Add((context => new T(), _handlerRegistry.Count));
        return this;
    }
    
    public T Get<T>() where T : GameFeature
    {
        foreach (var feature in _features.Values)
        {
            if(feature is T tFeature) return tFeature;
        }
        throw new InvalidOperationException($"Feature {typeof(T).Name} is not registered.");
    }
    
    internal void Update(float dt)
    {
        foreach (var service in _features.Values)
        {
            if (service.IsUpdateable)
                service.Update(dt);
        }
    }

    internal void Load(GameEngineContext context)
    {
        foreach (var (factory, order) in _handlerRegistry)
        {
            if (_features.ContainsKey(order))
                throw new InvalidOperationException($"Duplicate feature registered for order: {order}");

            var feature = factory(context);
            _features.Add(order, feature);
        }

        foreach (var (order,feature) in _features)
            feature.AttatchContext(context, order);
        
        foreach (var feature in _features.Values)
            feature.Load();


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