using ConcreteEngine.Core.Assets;
using ConcreteEngine.Core.Input;
using ConcreteEngine.Graphics;

namespace ConcreteEngine.Core;

public abstract class GameScene
{
    private readonly FeatureRegistry _featureRegistry = new();

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

    protected FeatureRegistry RegisterFeature<T>() where T : GameFeature, new()
        => _featureRegistry.RegisterFeature<T>();
    
    protected T GetFeature<T>() where T : GameFeature
        => _featureRegistry.Get<T>();


    internal void AttachContext(GameEngineContext context)
    {
        Context = context;
    }

    internal void LoadInternal()
    {
        Configure();
        _featureRegistry.Load(Context);
        OnReady();
    }

    internal void UnloadInternal()
    {
        Unload();
        _featureRegistry.Unload();
    }

    internal void Update(float dt)
    {
        _featureRegistry.Update(dt);
    }

    internal void Render(float dt)
    {
    }
}