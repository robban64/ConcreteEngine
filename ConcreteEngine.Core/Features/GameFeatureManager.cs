using ConcreteEngine.Graphics.Descriptors;

namespace ConcreteEngine.Core.Features;

public interface IGameFeatureManager
{
    public T Get<T>() where T : IGameFeature;
}

public sealed class FeatureManager : IGameFeatureManager
{
    private readonly SortedList<int, IGameFeature> _features = new(8);

    public void AddFeature<T>(int order, T feature) where T : class, IGameFeature
    {
        _features.Add(order, feature);
    }

    public T Get<T>() where T : IGameFeature
    {
        foreach (var feature in _features.Values)
        {
            if (feature is T tFeature) return tFeature;
        }

        throw new InvalidOperationException($"Feature {typeof(T).Name} is not registered.");
    }


    internal void Update(in FrameMetaInfo frameInfo)
    {
        if (_features.Count == 0) return;

        foreach (var feature in _features.Values)
        {
            if (feature.IsUpdateable)
                feature.Update(frameInfo);
        }
    }

    internal void GameTickUpdate(int tick)
    {
        if (_features.Count == 0) return;

        foreach (var feature in _features.Values)
        {
            if (feature.IsUpdateable)
                feature.UpdateTick(tick);
        }
    }

    internal void Load(GameFeatureContext context)
    {
        foreach (var (order, feature) in _features)
        {
            feature.AttachContext(context, order);
        }

        foreach (var feature in _features.Values)
        {
            feature.Initialize();
        }
    }

    internal void Unload()
    {
        foreach (var feature in _features.Values)
        {
            feature.Unload();
        }
    }
}