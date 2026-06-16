using ConcreteEngine.Core.Engine.Configuration;
using ConcreteEngine.Core.Engine.Scene;
using ConcreteEngine.Core.Engine.Scene.Modules;

namespace ConcreteEngine.Engine;

internal sealed class SceneSystem
{
    public GameScene? Current { get; private set; }
    public bool Enabled { get; private set; }

    private readonly SceneManager _sceneManager;

    private readonly ModuleManager _modules;

    private readonly List<Func<GameScene>> _sceneFactories;

    private int _pendingIndex = -1;


    internal SceneSystem(List<Func<GameScene>> sceneFactories)
    {
        _sceneFactories = sceneFactories ?? throw new ArgumentNullException(nameof(sceneFactories));
        _sceneManager = new SceneManager();
        
        _modules = new ModuleManager();
    }

    public bool HasPendingSwitch => _pendingIndex >= 0;
    public void SetEnabled(bool enabled) => Enabled = enabled;

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
        _sceneManager.CommitTick();
    }


    public void ApplyPendingScene(GameSceneConfigBuilder builder)
    {
        if (_pendingIndex < 0) return;

        var index = _pendingIndex;
        if (index >= _sceneFactories.Count)
            throw new InvalidOperationException($"Switch scene, index {index} is out of range.");

        Current?.Unload();

        var sceneContext = new GameSceneContext(_modules, _sceneManager);

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

    public void Shutdown()
    {
        Current?.Unload();
    }
}