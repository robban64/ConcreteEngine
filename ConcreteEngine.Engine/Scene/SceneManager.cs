using ConcreteEngine.Engine.Configuration;
using ConcreteEngine.Engine.Scene.Modules;
using ConcreteEngine.Engine.Worlds;
using ConcreteEngine.Renderer.Descriptors;

namespace ConcreteEngine.Engine.Scene;

internal sealed class SceneManager
{
    private int _pendingIndex = -1;

    private readonly ModuleManager _modules;
    private readonly List<Func<GameScene>> _sceneFactories;

    public GameScene? Current { get; private set; }

    internal SceneManager(List<Func<GameScene>> sceneFactories)
    {
        _sceneFactories = sceneFactories ?? throw new ArgumentNullException(nameof(sceneFactories));
        _modules = new ModuleManager();
    }

    public bool HasPendingSwitch => _pendingIndex >= 0;

    public void QueueSwitch(int sceneIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sceneIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(sceneIndex, _sceneFactories.Count);
        _pendingIndex = sceneIndex;
    }

    public void UpdateTick(float deltaTime)
    {
        if(Current is null) return;
        _modules.UpdateTick(deltaTime);
        Current.UpdateTick(deltaTime);
    }
    

    public void ApplyPendingScene(GameSceneConfigBuilder builder, IEngineSystemManager systems, World world)
    {
        if (_pendingIndex < 0) return;

        var index = _pendingIndex;
        if (index >= _sceneFactories.Count)
            throw new IndexOutOfRangeException($"Switch scene, index {index} is out of range.");

        Current?.Unload();

        var sceneContext = new GameSceneContext(systems, world, _modules) ;

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