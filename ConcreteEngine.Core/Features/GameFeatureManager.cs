using ConcreteEngine.Graphics.Descriptors;

namespace ConcreteEngine.Core.Features;

public interface IGameFeatureManager
{
    public T Get<T>() where T : IGameFeature;
}

public sealed class FeatureManager : IGameFeatureManager
{
    private readonly List<IGameFeature> _features = new(4);
    private readonly List<IDrawableFeature> _drawableFeatures = new(4);

    internal FeatureManager()
    {
        AddFeature<MeshEntityFeature>();
        AddFeature<TerrainFeature>();
        AddFeature<TilemapFeature>();
        AddFeature<SpriteFeature>();
        AddFeature<LightFeature>();
    }


    public T Get<T>() where T : IGameFeature
    {
        foreach (var feature in _features)
        {
            if (feature is T tFeature) return tFeature;
        }

        throw new InvalidOperationException($"Feature {typeof(T).Name} is not registered.");
    }


    internal void Update(in FrameMetaInfo frameInfo)
    {
        if (_features.Count == 0) return;

        foreach (var feature in _features)
        {
            if (feature.IsUpdateable)
                feature.Update(frameInfo);
        }
    }

    internal void GameTickUpdate(int tick)
    {
        if (_features.Count == 0) return;

        foreach (var feature in _features)
        {
            if (feature.IsUpdateable)
                feature.UpdateTick(tick);
        }
    }

    internal void Load(GameFeatureContext context)
    {
        foreach (var feature in _features)
        {
            feature.AttachContext(context);
        }

        foreach (var feature in _features)
        {
            feature.Initialize();
        }
    }

    internal void Unload()
    {
        foreach (var feature in _features)
        {
            feature.Unload();
        }
    }
    
    private void AddFeature<T>() where T : class, IGameFeature, new()
    {
        var newFeature = new T();
        _features.Add(newFeature);
        if(newFeature is IDrawableFeature drawableFeature)
            _drawableFeatures.Add(drawableFeature);
    }
}