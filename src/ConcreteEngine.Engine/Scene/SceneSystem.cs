using ConcreteEngine.Engine.Assets;
using ConcreteEngine.Engine.Configuration.Setup;
using ConcreteEngine.Engine.Render;
using ConcreteEngine.Engine.Scene.Modules;

namespace ConcreteEngine.Engine.Scene;

internal sealed class SceneSystem : GameEngineSystem
{
    public GameScene? Current { get; private set; }
    public bool Enabled { get; private set; }

    internal GameSystem GameSystem { get; }
    internal SceneManager SceneManager { get; }

    private readonly ModuleManager _modules;
    
    private int _pendingIndex = -1;
    private readonly List<Func<GameScene>> _sceneFactories;


    internal SceneSystem(List<Func<GameScene>> sceneFactories, AssetSystem assetSystem, EngineRenderSystem renderSystem)
    {
        _sceneFactories = sceneFactories ?? throw new ArgumentNullException(nameof(sceneFactories));
        _modules = new ModuleManager();
        SceneManager = new SceneManager(assetSystem,renderSystem);
        GameSystem = new GameSystem(assetSystem.Store, SceneManager,renderSystem);
    }


    public bool HasPendingSwitch => _pendingIndex >= 0;

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

    public void UpdateScene(float deltaTime)
    {
        if (Current is null || !Enabled) return;
        _modules.UpdateTick(deltaTime);
        Current.UpdateTick(deltaTime);
        
        GameSystem.Update(deltaTime);
    }


    public void ApplyPendingScene(GameSceneConfigBuilder builder, IEngineSystemManager systems)
    {
        if (_pendingIndex < 0) return;

        var index = _pendingIndex;
        if (index >= _sceneFactories.Count)
            throw new IndexOutOfRangeException($"Switch scene, index {index} is out of range.");

        Current?.Unload();

        var sceneContext = new GameSceneContext(systems, _modules, SceneManager);

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
}