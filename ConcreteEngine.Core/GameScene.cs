using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Input;
using ConcreteEngine.Core.Module;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core;

public abstract class GameScene
{
    private readonly ModuleRegistry _moduleRegistry = new();

    protected GameEngineContext Context { get; private set; } = null!;
    protected InputManager Input => Context.Input;
    protected AssetManager Assets => Context.Assets;
    protected IGraphicsDevice Graphics => Context.Graphics;

    protected abstract void Configure();
    protected abstract void OnReady();
    protected abstract void Unload();


    protected GameScene()
    {
    }

    protected ModuleRegistry RegisterModule<T>() where T : GameModule, new()
        => _moduleRegistry.RegisterModule<T>();
    
    protected T GetModule<T>() where T : GameModule
        => _moduleRegistry.Get<T>();


    internal void AttachContext(GameEngineContext context)
    {
        Context = context;
    }

    internal void LoadInternal()
    {
        Configure();
        _moduleRegistry.Load(Context);
        OnReady();
    }

    internal void UnloadInternal()
    {
        Unload();
        _moduleRegistry.Unload();
    }

    internal void Update(float dt)
    {
        _moduleRegistry.Update(dt);
    }

    internal void Render(float dt)
    {
        _moduleRegistry.Render(dt);

    }
}