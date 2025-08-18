#region

#endregion

namespace ConcreteEngine.Core;

public sealed class GameSceneContext
{
    private readonly GameEngine _engine;

    public void RegisterFeature<T>() where T : IGameFeature, new()
        => _engine.RegisterFeature<T>();

    public T GetFeature<T>() where T : IGameFeature => _engine.GetFeature<T>();
    public T GetSystem<T>() where T : IGameEngineSystem => _engine.GetSystem<T>();

    internal GameSceneContext(GameEngine engine)
    {
        _engine = engine;
    }
}

public sealed class GameFeatureContext
{
    private readonly GameSceneContext _scene;

    public T GetSystem<T>() where T : IGameEngineSystem => _scene.GetSystem<T>();

    internal GameFeatureContext(GameSceneContext scene)
    {
        _scene = scene;
    }
}