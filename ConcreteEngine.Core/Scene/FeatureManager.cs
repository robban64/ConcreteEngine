using ConcreteEngine.Core.Features;

namespace ConcreteEngine.Core.Scene;

public interface IFeatureRegistry
{
    public T Get<T>() where T : IGameFeature;
}

public sealed class FeatureRegistry : IFeatureRegistry
{
    private readonly SortedList<int, IGameFeature> _features = new(8);

    public void AddFeature<T>(int order, T feature) where T : class, IGameFeature
    {
        _features.Add(order, feature);
    }

    public T Get<T>() where T : IGameFeature
    {
        foreach (var (_, feature) in _features)
        {
            if (feature is T tFeature) return tFeature;
        }

        throw new InvalidOperationException($"Feature {typeof(T).Name} is not registered.");
    }

    internal void GameTickUpdate(int tick)
    {
        if (_features.Count == 0) return;

        foreach (var (_, service) in _features)
        {
            if (service.IsUpdateable)
                service.UpdateTick(tick);
        }
    }

    internal void Load(GameFeatureContext context)
    {
        foreach (var (order, feature) in _features)
        {
            feature.AttachContext(context, order);
        }
        
        foreach (var (_, feature) in _features)
        {
            feature.Initialize();
        }

    }

    internal void Unload()
    {
        foreach (var (order, feature) in _features)
        {
            feature.Unload();
        }
    }
}