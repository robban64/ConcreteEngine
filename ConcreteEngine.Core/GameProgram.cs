namespace ConcreteEngine.Core;

public abstract class GameProgram
{
    private readonly List<Type> _sceneTypeFactory = new(4);

    private GameEngineContext _context = null!;

    private Type? _nextSceneType;
    private GameScene _currentScene = null!;
    
    protected GameEngineContext Context => _context;
    
    protected abstract void Load();
    protected abstract void Unload();
    protected abstract void Update(float deltaTime);
    protected abstract void SceneChanged(GameScene? previous, GameScene current);

    protected void RegisterScene<T>() where T : GameScene
    {
        var type = typeof(T);

        if(_sceneTypeFactory.Contains(type))
            throw new InvalidOperationException($"GameScene of type {type} already registered");
        
        _sceneTypeFactory.Add(type);
    }
    
    protected void RequestSceneChange<T>() where T : GameScene
    {
        var type = typeof(T);
        if (!_sceneTypeFactory.Contains(type))
            throw new InvalidOperationException($"GameScene of type {type.Name} not found");

        //var a = () => new T();
        _nextSceneType = type;
    }
    
    internal void BindGameProgram(GameEngineContext context)
    {
        _context = context;
        Load();

        if(_sceneTypeFactory.Count == 0)
            throw new InvalidOperationException("No GameScene registered. Require at least one GameScene");
        
        CreateAndSwitchScene(_sceneTypeFactory[0]);
    }

    internal void UpdateInternal(float deltaTime)
    {
        _currentScene.Update(deltaTime);
        UpdateSceneTransitionIfNeeded();
    }

    internal void RenderInternal(float deltaTime)
    {
        _currentScene.Render(deltaTime);
    }

    private void UpdateSceneTransitionIfNeeded()
    {
        if (_nextSceneType == null) return;

        var previous = _currentScene;
        var newSceneType = _sceneTypeFactory.Find(x=>x == _nextSceneType);
        if(newSceneType == null) throw new InvalidOperationException($"GameScene of type {_nextSceneType.Name} not found");

        CreateAndSwitchScene(newSceneType);
        _nextSceneType = null;

        SceneChanged(previous, _currentScene);
    }

    private void CreateAndSwitchScene(Type type)
    {
        var newScene = (GameScene)Activator.CreateInstance(type)!;
        newScene.AttachContext(_context);
        newScene.LoadInternal();

        _currentScene?.UnloadInternal();
        _currentScene = newScene;
    }


    /*
    public abstract void Pausing();
    public abstract void Resuming();
    public abstract void Shutdown();
    */
}