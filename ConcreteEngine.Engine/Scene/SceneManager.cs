using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.ECS;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds;

namespace ConcreteEngine.Engine.Scene;

internal sealed class SceneManager : IGameEngineSystem
{
    private int _pendingIndex = -1;

    private readonly ModuleManager _modules;
    private readonly List<Func<GameScene>> _sceneFactories;

    private readonly SceneWorld _sceneWorld;
    private readonly World  _world;

    private readonly EntityWorld _ecs;

    public GameScene? Current { get; private set; }
    public bool Enabled { get; private set; }

    public bool HasPendingSwitch => _pendingIndex >= 0;

    internal SceneManager(List<Func<GameScene>> sceneFactories, AssetSystem assetSystem, World world, EntityWorld ecs)
    {
        _sceneFactories = sceneFactories ?? throw new ArgumentNullException(nameof(sceneFactories));
        _world = world;
        _ecs = ecs;
        _modules = new ModuleManager();
        _sceneWorld = new SceneWorld(assetSystem, world, ecs);
    }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled;
    }

    public void QueueSwitch(int sceneIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sceneIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(sceneIndex, _sceneFactories.Count);
        _pendingIndex = sceneIndex;
    }

    public void UpdateTick(float deltaTime)
    {
        if(Current is null || !Enabled) return;
        _modules.UpdateTick(deltaTime);
        Current.UpdateTick(deltaTime);
    }
    

    public void ApplyPendingScene(GameSceneConfigBuilder builder, IEngineSystemManager systems)
    {
        if (_pendingIndex < 0) return;

        var index = _pendingIndex;
        if (index >= _sceneFactories.Count)
            throw new IndexOutOfRangeException($"Switch scene, index {index} is out of range.");

        Current?.Unload();

        var sceneContext = new GameSceneContext(systems, _world, _modules, _sceneWorld) ;

        var newScene = _sceneFactories[index]();
        newScene.AttachContext(sceneContext);

        newScene.Build(builder);
        
        for (int i = 0; i < builder.Modules.Count; i++)
            _modules.Add(builder.Modules[i]());

        newScene.Initialize();

        Current = newScene;
        _pendingIndex = -1;
        builder.Clear();
        
        _modules.Load(new GameModuleContext(sceneContext));
    }

    public void Shutdown() {}
}